using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using TiaMcpServer.Contracts.Extensions;
using TiaMcpServer.Contracts.Worker;
using TiaMcpServer.OpennessWorker.Openness.Project;
using TiaMcpServer.OpennessWorker.Worker;

namespace TiaMcpServer.OpennessWorker.Openness.Extensions;

internal static class HardwareParameterService
{
    internal static Device FindDevice(ProjectBase project, string name)
    {
        var devices = ProjectDeviceEnumerator.Enumerate(project).Where(d => d.Name == name).ToArray();
        if (devices.Length != 1) throw new WorkerOperationException(
            devices.Length == 0 ? WorkerFailureCategories.TargetNotFound : WorkerFailureCategories.TargetAmbiguous,
            "deviceName must identify exactly one device.");
        return devices[0];
    }

    internal static DeviceItem Resolve(ProjectBase project, EngineeringQuery query)
    {
        var items = FindDevice(project, query.DeviceName!).DeviceItems;
        DeviceItem? found = null;
        foreach (var segment in query.ItemPath)
        {
            var matches = items.Cast<DeviceItem>().Where(d => d.Name == segment).ToArray();
            if (matches.Length != 1) throw new WorkerOperationException(
                matches.Length == 0 ? WorkerFailureCategories.TargetNotFound : WorkerFailureCategories.TargetAmbiguous,
                "itemPath must identify exactly one child at each level: " + segment);
            found = matches[0];
            items = found.DeviceItems;
        }
        return found ?? throw new ArgumentException("itemPath is empty.");
    }

    public static EngineeringExtensionResult Read(ProjectBase project, EngineeringQuery query)
    {
        var item = Resolve(project, query);
        var info = new HardwareParametersInfo
        {
            DeviceName = query.DeviceName!, ItemPath = query.ItemPath.ToArray(),
            TypeIdentifier = Convert.ToString(((IEngineeringObject)item).GetAttribute("TypeIdentifier"), CultureInfo.InvariantCulture) ?? "", PositionNumber = item.PositionNumber,
            Attributes = ReadAttributes((IEngineeringObject)item)
        };
        var index = 0;
        foreach (Channel channel in item.Channels)
        {
            var currentIndex = index++;
            if (query.ChannelIndex is { } selected && currentIndex != selected) continue;
            info.Channels.Add(new HardwareChannelInfo {
                Index = currentIndex, Number = channel.Number, IoType = channel.IoType.ToString(),
                Type = channel.Type.ToString(), Attributes = ReadAttributes((IEngineeringObject)channel)
            });
        }
        if (query.ChannelIndex is { } requested && requested >= index)
            throw new ArgumentException("channelIndex is outside the observed channel list.");
        return new EngineeringExtensionResult { Operation = query.Operation, Hardware = info, StateHash = Hash(info) };
    }

    private static List<HardwareParameterInfo> ReadAttributes(IEngineeringObject target)
    {
        var result = new List<HardwareParameterInfo>();
        foreach (var metadata in target.GetAttributeInfos().OrderBy(m => m.Name, StringComparer.Ordinal))
        {
            var entry = new HardwareParameterInfo {
                Name = metadata.Name,
                CanRead = metadata.AccessMode == EngineeringAttributeAccessMode.Read || metadata.AccessMode == EngineeringAttributeAccessMode.ReadWrite,
                CanWrite = metadata.AccessMode == EngineeringAttributeAccessMode.Write || metadata.AccessMode == EngineeringAttributeAccessMode.ReadWrite,
                SupportedTypes = metadata.SupportedTypes.Select(t => t.FullName ?? t.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray()
            };
            if (entry.CanRead)
                try {
                    var value = target.GetAttribute(metadata.Name);
                    if (value != null && !IsScalar(value.GetType())) entry.Error = "Non-scalar attribute is not supported by this parameter editor.";
                    else entry.Value = JsonSerializer.SerializeToElement(value, value?.GetType() ?? typeof(object));
                } catch (Exception ex) { entry.Error = ex.Message; }
            result.Add(entry);
        }
        return result;
    }

    private static bool IsScalar(Type type) => type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal);

    internal static string Hash(object value)
    {
        using var hash = SHA256.Create();
        return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)))).Replace("-", "").ToLowerInvariant();
    }

    private static IEngineeringObject Target(DeviceItem item, EngineeringQuery query)
    {
        if (query.ChannelIndex is not { } index) return (IEngineeringObject)item;
        var channels = item.Channels.Cast<Channel>().ToArray();
        if (index >= channels.Length) throw new ArgumentException("channelIndex is outside the observed channel list.");
        return (IEngineeringObject)channels[index];
    }

    private static List<KeyValuePair<string, object>> Prepare(IEngineeringObject target, EngineeringQuery query)
    {
        var metadata = target.GetAttributeInfos().ToDictionary(a => a.Name, StringComparer.Ordinal);
        var prepared = new List<KeyValuePair<string, object>>();
        foreach (var change in query.Changes)
        {
            if (!metadata.TryGetValue(change.Key, out var attribute) || attribute.AccessMode != EngineeringAttributeAccessMode.ReadWrite)
                throw new ArgumentException("Attribute is not exposed as ReadWrite: " + change.Key);
            var current = target.GetAttribute(change.Key);
            var type = current?.GetType();
            if (type == null || !IsScalar(type)) throw new ArgumentException("Unsupported scalar type: " + change.Key);
            object value;
            if (type.IsEnum)
            {
                value = change.Value.ValueKind == JsonValueKind.String
                    ? Enum.Parse(type, change.Value.GetString()!, false)
                    : Enum.ToObject(type, JsonSerializer.Deserialize(change.Value.GetRawText(), Enum.GetUnderlyingType(type))!);
                if (!Enum.IsDefined(type, value)) throw new ArgumentException("Undefined enum value: " + change.Key);
            }
            else value = JsonSerializer.Deserialize(change.Value.GetRawText(), type) ?? throw new ArgumentException("Null values cannot be written.");
            prepared.Add(new KeyValuePair<string, object>(change.Key, value));
        }
        return prepared;
    }

    public static EngineeringExtensionResult Check(ProjectBase project, EngineeringQuery query)
    {
        var before = Read(project, query);
        if (!string.Equals(before.StateHash, query.ExpectedStateHash, StringComparison.Ordinal))
            throw new WorkerOperationException(WorkerFailureCategories.StateChanged, "Hardware parameters changed since the read. Inspect again.");
        Prepare(Target(Resolve(project, query), query), query);
        before.Details = JsonSerializer.SerializeToElement(new { query.ChannelIndex, requestedChanges = query.Changes });
        return before;
    }

    public static EngineeringExtensionResult Write(ProjectBase project, EngineeringQuery query)
    {
        var before = Check(project, query);
        var target = Target(Resolve(project, query), query);
        var prepared = Prepare(target, query);
        var applied = new List<string>();
        string? failure = null;
        try {
            foreach (var pair in prepared) { target.SetAttribute(pair.Key, pair.Value); applied.Add(pair.Key); }
        } catch (Exception ex) { failure = ex.Message; }
        var after = Read(project, query);
        foreach (var pair in prepared)
        {
            try { if (!Equals(target.GetAttribute(pair.Key), pair.Value)) failure = failure ?? "Requested value was not retained: " + pair.Key; }
            catch (Exception ex) { failure = failure ?? ex.Message; }
        }
        after.Success = failure == null;
        after.Error = failure;
        after.Details = JsonSerializer.SerializeToElement(new { before = before.Hardware, appliedAttributes = applied });
        after.Warnings.Add("Review returned attributes and re-read other channels: Siemens parameter dependencies may change unrequested values. A channelIndex read/hash covers only that channel and its module attributes. No save, compile, download or rollback was performed.");
        return after;
    }
}

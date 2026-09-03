using Siemens.Engineering;
using Siemens.Engineering.HW;

namespace TiaMcpServer.OpennessWorker.Openness;

/// <summary>Flattens channel metadata into the existing read-only attribute result.</summary>
public static class DeviceItemChannelInspector
{
    public static NetworkAttributeMetadataProcessingResult Inspect(DeviceItem deviceItem)
    {
        var result = new NetworkAttributeMetadataProcessingResult();
        var channelIndex = 0;
        try
        {
            foreach (Channel channel in deviceItem.Channels)
            {
                InspectChannel(channel, channelIndex, result);
                channelIndex++;
            }
        }
        catch (Exception exception)
        {
            result.Diagnostics.Add(
                $"Device item channel collection could not be read: {exception.Message}");
        }

        result.Observations = result.Observations
            .OrderBy(observation => observation.Name, StringComparer.Ordinal)
            .ToList();
        return result;
    }

    private static void InspectChannel(
        Channel channel,
        int channelIndex,
        NetworkAttributeMetadataProcessingResult result)
    {
        var prefix = $"Channels[{channelIndex}].";
        var dynamic = EngineeringAttributeInspector.Inspect((IEngineeringObject)channel, null);
        foreach (var observation in dynamic.Observations)
        {
            observation.Name = prefix + observation.Name;
            result.Observations.Add(observation);
        }

        foreach (var diagnostic in dynamic.Diagnostics)
        {
            result.Diagnostics.Add($"Channel {channelIndex}: {diagnostic}");
        }

        AddTypedObservationIfMissing(
            result.Observations,
            prefix + "Number",
            () => channel.Number,
            typeof(int).FullName!);
        AddTypedObservationIfMissing(
            result.Observations,
            prefix + "IoType",
            () => channel.IoType,
            typeof(ChannelIoType).FullName!);
        AddTypedObservationIfMissing(
            result.Observations,
            prefix + "Type",
            () => channel.Type,
            typeof(ChannelType).FullName!);
    }

    private static void AddTypedObservationIfMissing(
        List<NetworkAttributeObservation> observations,
        string name,
        Func<object?> reader,
        string expectedType)
    {
        if (observations.Any(observation => string.Equals(
                observation.Name,
                name,
                StringComparison.Ordinal)))
        {
            return;
        }

        observations.Add(new NetworkAttributeObservation
        {
            Name = name,
            ReadValue = reader,
            CanRead = true,
            CanWrite = false,
            SupportedTypes = new[] { expectedType },
        });
    }
}

using System.Text.Json;

namespace TiaMcpServer.Contracts.Extensions;

public class EngineeringQuery
{
    public string Operation { get; set; } = "";
    public string? PlcName { get; set; }
    public string? DeviceName { get; set; }
    public string[] ItemPath { get; set; } = Array.Empty<string>();
    [System.ComponentModel.Description("Optional returned collection index. Reads and hashes cover this one channel plus module attributes; omit to inspect all channels. Re-read other channels after edits to check Siemens parameter dependencies.")]
    public int? ChannelIndex { get; set; }
    [System.ComponentModel.Description("Optional address collection index from inspection. Mutually exclusive with channelIndex. Address editing only permits StartAddress; no online I/O write.")]
    public int? AddressIndex { get; set; }
    public string[] AttributeNames { get; set; } = Array.Empty<string>();
    public string? ModuleTypeIdentifier { get; set; }
    public string? ModuleName { get; set; }
    public int? PositionNumber { get; set; }
    public Dictionary<string, JsonElement> Changes { get; set; } = new();
    public string? ExpectedStateHash { get; set; }
    public string InterfaceName { get; set; } = "PressInterface";
    public string InterfaceUri { get; set; } = "urn:pengcanlyu:press";
    public bool KeepFolderStructure { get; set; } = true;
    public bool IncludeVariables { get; set; }
    public int MaxVariables { get; set; } = 200;
    public bool Enabled { get; set; } = true;
    public bool ReplaceExisting { get; set; }
    public string? Author { get; set; }
    public string? ExportPath { get; set; }
    public string? CatalogPath { get; set; }
    public string? AllowedSourcePathsPath { get; set; }
    public string? AllowedSourcePathsSha256 { get; set; }
}

public class HardwareParameterInfo
{
    public string Name { get; set; } = "";
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }
    public string[] SupportedTypes { get; set; } = Array.Empty<string>();
    public JsonElement? Value { get; set; }
    public string? Error { get; set; }
    public string[] EnumNames { get; set; } = Array.Empty<string>();
}

public class HardwareAddressInfo
{
    public int Index { get; set; }
    public string IoType { get; set; } = "";
    public int StartAddress { get; set; }
    public int LengthBits { get; set; }
    public long LengthBytes => ((long)LengthBits + 7) / 8;
    public List<HardwareParameterInfo> Attributes { get; set; } = new();
}

public class HardwareChannelInfo
{
    public int Index { get; set; }
    public int Number { get; set; }
    public string IoType { get; set; } = "";
    public string Type { get; set; } = "";
    public List<HardwareParameterInfo> Attributes { get; set; } = new();
}

public class HardwareParametersInfo
{
    public string DeviceName { get; set; } = "";
    public string[] ItemPath { get; set; } = Array.Empty<string>();
    public string TypeIdentifier { get; set; } = "";
    public int PositionNumber { get; set; }
    public List<HardwareParameterInfo> Attributes { get; set; } = new();
    public List<HardwareChannelInfo> Channels { get; set; } = new();
    public List<HardwareAddressInfo> Addresses { get; set; } = new();
}

public class EngineeringExtensionResult
{
    public string Operation { get; set; } = "";
    public bool Success { get; set; } = true;
    public string? StateHash { get; set; }
    public HardwareParametersInfo? Hardware { get; set; }
    // Siemens model generation has a versioned, heterogeneous information document.
    public JsonElement? Details { get; set; }
    public string? Error { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public static class EngineeringExtensionOperations
{
    public static readonly string[] Reads = { "inspect_hardware_parameters", "inspect_hardware_capabilities", "inspect_hardware_layout", "check_hardware_module", "list_opcua_interfaces", "inspect_opcua_variables" };
    public static readonly string[] Writes = { "set_hardware_parameters", "plug_hardware_module", "compile_hardware", "generate_opcua_interface", "set_opcua_interface_enabled", "delete_opcua_interface", "export_opcua_interface", "export_plc_to_folder" };

    public static string? Validate(EngineeringQuery? query, bool write)
    {
        if (query == null) return "operation is required.";
        if (!(write ? Writes : Reads).Contains(query.Operation, StringComparer.Ordinal)) return "Unknown operation for this tool.";
        if (query.MaxVariables < 0 || query.MaxVariables > 2000) return "maxVariables must be 0..2000.";
        if (query.Operation == "compile_hardware" && (string.IsNullOrWhiteSpace(query.DeviceName)
            || query.ItemPath == null || query.ItemPath.Length == 0 || query.ItemPath.Length > 32 || query.ItemPath.Any(string.IsNullOrWhiteSpace)
            || query.ChannelIndex.HasValue || query.AddressIndex.HasValue))
            return "Hardware compilation requires exact deviceName and nonempty CPU itemPath, with no channel/address selector.";
        if (query.Operation.Contains("hardware_parameters") || query.Operation == "inspect_hardware_capabilities")
        {
            if (string.IsNullOrWhiteSpace(query.DeviceName) || query.ItemPath == null || query.ItemPath.Length == 0 || query.ItemPath.Length > 32 || query.ItemPath.Any(string.IsNullOrWhiteSpace))
                return "deviceName and exact itemPath segments are required.";
            if (query.ChannelIndex < 0) return "channelIndex must be nonnegative.";
            if (query.AddressIndex < 0 || (query.AddressIndex.HasValue && query.ChannelIndex.HasValue)) return "Choose only one nonnegative channelIndex or addressIndex.";
            if (query.AttributeNames == null || query.AttributeNames.Length > 32 || query.AttributeNames.Any(string.IsNullOrWhiteSpace)
                || query.AttributeNames.Distinct(StringComparer.Ordinal).Count() != query.AttributeNames.Length)
                return "attributeNames must contain 0..32 unique exact names.";
            if (query.Operation != "inspect_hardware_capabilities" && query.AttributeNames.Length != 0) return "attributeNames is only available on inspect_hardware_capabilities; omit it for state reads and writes.";
            if (write && (query.Changes == null || query.Changes.Count == 0 || query.Changes.Count > 32 || query.Changes.Keys.Any(string.IsNullOrWhiteSpace))) return "Provide 1..32 named changes.";
            if (write && query.Changes.Keys.Any(k => k is "Name" or "PositionNumber" or "TypeIdentifier"))
                return "Identity/placement attributes cannot be changed by the parameter editor.";
            if (write && query.AddressIndex.HasValue && (query.Changes.Count != 1 || !query.Changes.TryGetValue("StartAddress", out var start)
                || start.ValueKind != JsonValueKind.Number || !start.TryGetInt32(out var number) || number < 0))
                return "Address edits require exactly one nonnegative integer StartAddress.";
            if (write && (string.IsNullOrWhiteSpace(query.ExpectedStateHash) || query.ExpectedStateHash!.Length != 64 || query.ExpectedStateHash.Any(c => !Uri.IsHexDigit(c))))
                return "expectedStateHash from inspect_hardware_parameters is required.";
        }
        if (query.Operation is "inspect_hardware_layout" or "check_hardware_module" or "plug_hardware_module")
        {
            if (string.IsNullOrWhiteSpace(query.DeviceName) || query.ItemPath == null || query.ItemPath.Length > 32 || query.ItemPath.Any(string.IsNullOrWhiteSpace))
                return "Exact deviceName and itemPath are required; [] selects the station container.";
            if (query.ChannelIndex.HasValue || query.AddressIndex.HasValue) return "Module placement selects a hardware container, not a channel/address.";
            if (query.Operation != "inspect_hardware_layout" && (string.IsNullOrWhiteSpace(query.ModuleTypeIdentifier)
                || string.IsNullOrWhiteSpace(query.ModuleName) || query.ModuleName!.Length > 128 || !query.PositionNumber.HasValue || query.PositionNumber < 0))
                return "Exact catalog moduleTypeIdentifier, moduleName and nonnegative positionNumber are required.";
            if (write && (query.ExpectedStateHash?.Length != 64 || !query.ExpectedStateHash.All(Uri.IsHexDigit)))
                return "expectedStateHash from inspect_hardware_layout/check_hardware_module is required.";
        }
        if (query.Operation.Contains("opcua") && string.IsNullOrWhiteSpace(query.PlcName)) return "An exact plcName is required.";
        if (query.AllowedSourcePathsPath != null || query.AllowedSourcePathsSha256 != null)
        {
            if (string.IsNullOrWhiteSpace(query.AllowedSourcePathsPath) || query.AllowedSourcePathsSha256?.Length != 64
                || !query.AllowedSourcePathsSha256.All(Uri.IsHexDigit))
                return "An allowlist path requires its 64-hex allowedSourcePathsSha256 (and vice versa).";
        }
        if (query.Operation.Contains("interface") && string.IsNullOrWhiteSpace(query.InterfaceName)) return "interfaceName is required.";
        if (query.Operation == "export_plc_to_folder" && (string.IsNullOrWhiteSpace(query.DeviceName) || string.IsNullOrWhiteSpace(query.ExportPath)))
            return "deviceName and exportPath are required.";
        if (query.Operation == "export_opcua_interface" && string.IsNullOrWhiteSpace(query.ExportPath)) return "exportPath is required.";
        return null;
    }
}

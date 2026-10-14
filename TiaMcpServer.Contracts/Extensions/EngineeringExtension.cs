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
    public static readonly string[] Reads = { "inspect_hardware_parameters", "list_opcua_interfaces", "inspect_opcua_variables" };
    public static readonly string[] Writes = { "set_hardware_parameters", "generate_opcua_interface", "set_opcua_interface_enabled", "delete_opcua_interface", "export_opcua_interface", "export_plc_to_folder" };

    public static string? Validate(EngineeringQuery? query, bool write)
    {
        if (query == null) return "operation is required.";
        if (!(write ? Writes : Reads).Contains(query.Operation, StringComparer.Ordinal)) return "Unknown operation for this tool.";
        if (query.MaxVariables < 0 || query.MaxVariables > 2000) return "maxVariables must be 0..2000.";
        if (query.Operation.Contains("hardware_parameters"))
        {
            if (string.IsNullOrWhiteSpace(query.DeviceName) || query.ItemPath == null || query.ItemPath.Length == 0 || query.ItemPath.Length > 32 || query.ItemPath.Any(string.IsNullOrWhiteSpace))
                return "deviceName and exact itemPath segments are required.";
            if (query.ChannelIndex < 0) return "channelIndex must be nonnegative.";
            if (write && (query.Changes == null || query.Changes.Count == 0 || query.Changes.Count > 32 || query.Changes.Keys.Any(string.IsNullOrWhiteSpace))) return "Provide 1..32 named changes.";
            if (write && query.Changes.Keys.Any(k => k is "Name" or "PositionNumber" or "TypeIdentifier"))
                return "Identity/placement attributes cannot be changed by the parameter editor.";
            if (write && (string.IsNullOrWhiteSpace(query.ExpectedStateHash) || query.ExpectedStateHash!.Length != 64 || query.ExpectedStateHash.Any(c => !Uri.IsHexDigit(c))))
                return "expectedStateHash from inspect_hardware_parameters is required.";
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

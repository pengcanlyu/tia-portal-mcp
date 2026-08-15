using System.ComponentModel;
using System.Security.Cryptography;
using ModelContextProtocol.Server;
using TiaMcpServer.Safety;
using TiaMcpServer.Worker;

namespace TiaMcpServer.Tools;

[McpServerToolType]
public static class OpcUaTools
{
    public const string DefaultInterfaceName = "三层Ubuntu接口";
    public const string DefaultInterfaceUri = "urn:guoji:zyg850:opcua:layer3";

    [McpServerTool(Name = "list_opcua_interfaces")]
    [Description("List OPC UA user-modelled server interfaces configured for the selected PLC, including enabled state and timestamps.")]
    public static Task<string> ListOpcUaInterfaces(
        OpennessWorkerClient workerClient,
        [Description("Optional PLC device/software name. Uses the first PLC when omitted.")] string? plcName = null,
        [Description("Optional path to a .ap21 project. Uses the active bound project when omitted.")] string? projectPath = null)
    {
        return workerClient.ListOpcUaInterfacesAsync(plcName, projectPath);
    }

    [McpServerTool(Name = "inspect_opcua_variables")]
    [Description("Build an OPC UA interface in memory without changing the TIA project. Reports all accessible variables from global DBs, preserving each variable's project read/write attributes. PLC tag tables and instance DBs are intentionally excluded.")]
    public static Task<string> InspectOpcUaVariables(
        OpennessWorkerClient workerClient,
        [Description("Optional PLC device/software name. Uses the first PLC when omitted.")] string? plcName = null,
        [Description("Name used for the in-memory interface model.")] string interfaceName = DefaultInterfaceName,
        [Description("Namespace URI used for the in-memory interface model.")] string interfaceUri = DefaultInterfaceUri,
        [Description("Keep TIA block-group folders in the OPC UA browse tree.")] bool keepFolderStructure = false,
        [Description("Include individual variable entries. The summary and per-DB counts are always returned.")] bool includeVariables = false,
        [Description("Maximum individual variable entries when includeVariables=true. Range 1..5000.")] int maxVariables = 200,
        [Description("Optional absolute JSON path containing the exact sourcePath allowlist to retain.")] string? allowedSourcePathsPath = null,
        [Description("Optional path to a .ap21 project. Uses the active bound project when omitted.")] string? projectPath = null)
    {
        return workerClient.InspectOpcUaVariablesAsync(
            plcName,
            interfaceName,
            interfaceUri,
            keepFolderStructure,
            includeVariables,
            maxVariables,
            projectPath,
            allowedSourcePathsPath);
    }

    [McpServerTool(Name = "export_opcua_interface")]
    [Description("Export an existing OPC UA server interface to XML and optionally create a UTF-8 JSON NodeId catalog. This does not modify the TIA project.")]
    public static Task<string> ExportOpcUaInterface(
        OpennessWorkerClient workerClient,
        [Description("Existing OPC UA server interface name.")] string interfaceName,
        [Description("Absolute output path ending in .xml.")] string exportPath,
        [Description("Optional absolute output path ending in .json for a NodeId/source-variable catalog.")] string? catalogPath = null,
        [Description("Optional PLC device/software name. Uses the first PLC when omitted.")] string? plcName = null,
        [Description("Optional path to a .ap21 project. Uses the active bound project when omitted.")] string? projectPath = null)
    {
        return workerClient.ExportOpcUaInterfaceAsync(plcName, interfaceName, exportPath, catalogPath, projectPath);
    }

    [McpServerTool(Name = "preview_generate_opcua_interface")]
    [Description("Preview creating or replacing a global-DB OPC UA server interface and return a short-lived safetyToken.")]
    public static async Task<string> PreviewGenerateOpcUaInterface(
        OpennessWorkerClient workerClient,
        [Description("OPC UA server interface name.")] string interfaceName = DefaultInterfaceName,
        [Description("Namespace URI for the generated server interface.")] string interfaceUri = DefaultInterfaceUri,
        [Description("Keep TIA block-group folders in the OPC UA browse tree.")] bool keepFolderStructure = false,
        [Description("Enable the generated server interface immediately.")] bool enabled = true,
        [Description("Allow replacement of an existing server interface with the same name. The old interface is restored if import fails.")] bool replaceExisting = false,
        [Description("Author metadata stored on the interface.")] string? author = "TIA MCP",
        [Description("Optional absolute .xml path for a copy of the generated interface.")] string? exportPath = null,
        [Description("Optional absolute .json path for the generated NodeId catalog.")] string? catalogPath = null,
        [Description("Optional absolute JSON path containing the exact sourcePath allowlist to retain.")] string? allowedSourcePathsPath = null,
        [Description("Optional PLC device/software name. Uses the first PLC when omitted.")] string? plcName = null,
        [Description("Optional path to a .ap21 project. Uses the active bound project when omitted.")] string? projectPath = null)
    {
        var currentState = await workerClient.ListOpcUaInterfacesAsync(plcName, projectPath).ConfigureAwait(false);
        var generatedPreview = await workerClient.InspectOpcUaVariablesAsync(
            plcName,
            interfaceName,
            interfaceUri,
            keepFolderStructure,
            includeVariables: false,
            maxVariables: 0,
            projectPath: projectPath,
            allowedSourcePathsPath: allowedSourcePathsPath).ConfigureAwait(false);

        if (generatedPreview.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
        {
            return $"Could not generate the in-memory OPC UA preview. {generatedPreview}";
        }

        var target = new { plcName, interfaceName };
        var requestedInput = new
        {
            plcName,
            interfaceName,
            interfaceUri,
            keepFolderStructure,
            enabled,
            replaceExisting,
            author,
            exportPath,
            catalogPath,
            allowedSourcePathsPath,
            allowedSourcePathsSha256 = ComputeFileSha256(allowedSourcePathsPath)
        };

        return WriteSafetyTooling.CreatePreview(
            "generate_opcua_interface",
            projectPath,
            target,
            $"Generate OPC UA server interface '{interfaceName}' from accessible variables in global DBs. PLC tags and instance DBs are excluded.",
            requestedInput,
            currentState,
            generatedPreview);
    }

    [McpServerTool(Name = "generate_opcua_interface")]
    [Description("Create or replace a global-DB OPC UA server interface. Requires confirm=true and a safetyToken from preview_generate_opcua_interface.")]
    public static async Task<string> GenerateOpcUaInterface(
        OpennessWorkerClient workerClient,
        [Description("OPC UA server interface name.")] string interfaceName = DefaultInterfaceName,
        [Description("Namespace URI for the generated server interface.")] string interfaceUri = DefaultInterfaceUri,
        [Description("Keep TIA block-group folders in the OPC UA browse tree.")] bool keepFolderStructure = false,
        [Description("Enable the generated server interface immediately.")] bool enabled = true,
        [Description("Allow replacement of an existing server interface with the same name.")] bool replaceExisting = false,
        [Description("Author metadata stored on the interface.")] string? author = "TIA MCP",
        [Description("Optional absolute .xml path for a copy of the generated interface.")] string? exportPath = null,
        [Description("Optional absolute .json path for the generated NodeId catalog.")] string? catalogPath = null,
        [Description("Optional absolute JSON path containing the exact sourcePath allowlist to retain.")] string? allowedSourcePathsPath = null,
        [Description("Optional PLC device/software name. Uses the first PLC when omitted.")] string? plcName = null,
        [Description("Optional path to a .ap21 project. Uses the active bound project when omitted.")] string? projectPath = null,
        [Description("Set true to confirm the project write.")] bool confirm = false,
        [Description("Safety token returned by preview_generate_opcua_interface for this exact request.")] string? safetyToken = null)
    {
        if (!confirm)
        {
            return "Operation not confirmed. Set confirm=true to generate the OPC UA interface.";
        }

        var target = new { plcName, interfaceName };
        var requestedInput = new
        {
            plcName,
            interfaceName,
            interfaceUri,
            keepFolderStructure,
            enabled,
            replaceExisting,
            author,
            exportPath,
            catalogPath,
            allowedSourcePathsPath,
            allowedSourcePathsSha256 = ComputeFileSha256(allowedSourcePathsPath)
        };

        var safety = await WriteSafetyTooling.ValidateForApplyAsync(
            safetyToken,
            "preview_generate_opcua_interface",
            "generate_opcua_interface",
            projectPath,
            target,
            requestedInput,
            () => workerClient.ListOpcUaInterfacesAsync(plcName, projectPath)).ConfigureAwait(false);
        if (!safety.IsValid)
        {
            return safety.Error!;
        }

        var result = await workerClient.GenerateOpcUaInterfaceAsync(
            plcName,
            interfaceName,
            interfaceUri,
            keepFolderStructure,
            enabled,
            replaceExisting,
            author,
            exportPath,
            catalogPath,
            projectPath,
            allowedSourcePathsPath).ConfigureAwait(false);
        var verification = result.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)
            ? null
            : await workerClient.ListOpcUaInterfacesAsync(plcName, projectPath).ConfigureAwait(false);

        WriteSafetyService.Shared.AppendAudit("generate_opcua_interface", projectPath, target, requestedInput, safety.CurrentState, result);
        return WriteSafetyTooling.BuildApplyResult("generate_opcua_interface", result, "list_opcua_interfaces", verification);
    }

    private static string? ComputeFileSha256(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }
        if (!Path.IsPathRooted(path) || !File.Exists(path))
        {
            throw new FileNotFoundException("OPC UA source-path allowlist must be an existing absolute file path.", path);
        }
        return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    }

    [McpServerTool(Name = "preview_set_opcua_interface_enabled")]
    [Description("Preview enabling or disabling an OPC UA server interface and return a short-lived safetyToken.")]
    public static async Task<string> PreviewSetOpcUaInterfaceEnabled(
        OpennessWorkerClient workerClient,
        [Description("Existing OPC UA server interface name.")] string interfaceName,
        [Description("Requested enabled state.")] bool enabled,
        [Description("Optional PLC device/software name.")] string? plcName = null,
        [Description("Optional path to a .ap21 project.")] string? projectPath = null)
    {
        var currentState = await workerClient.ListOpcUaInterfacesAsync(plcName, projectPath).ConfigureAwait(false);
        var target = new { plcName, interfaceName };
        var requestedInput = new { plcName, interfaceName, enabled };
        return WriteSafetyTooling.CreatePreview(
            "set_opcua_interface_enabled",
            projectPath,
            target,
            $"Set OPC UA server interface '{interfaceName}' enabled={enabled}.",
            requestedInput,
            currentState);
    }

    [McpServerTool(Name = "set_opcua_interface_enabled")]
    [Description("Enable or disable an OPC UA server interface. Requires confirm=true and a safetyToken from preview_set_opcua_interface_enabled.")]
    public static async Task<string> SetOpcUaInterfaceEnabled(
        OpennessWorkerClient workerClient,
        [Description("Existing OPC UA server interface name.")] string interfaceName,
        [Description("Requested enabled state.")] bool enabled,
        [Description("Optional PLC device/software name.")] string? plcName = null,
        [Description("Optional path to a .ap21 project.")] string? projectPath = null,
        [Description("Set true to confirm the project write.")] bool confirm = false,
        [Description("Safety token returned by preview_set_opcua_interface_enabled.")] string? safetyToken = null)
    {
        if (!confirm)
        {
            return "Operation not confirmed. Set confirm=true to change the OPC UA interface state.";
        }

        var target = new { plcName, interfaceName };
        var requestedInput = new { plcName, interfaceName, enabled };
        var safety = await WriteSafetyTooling.ValidateForApplyAsync(
            safetyToken,
            "preview_set_opcua_interface_enabled",
            "set_opcua_interface_enabled",
            projectPath,
            target,
            requestedInput,
            () => workerClient.ListOpcUaInterfacesAsync(plcName, projectPath)).ConfigureAwait(false);
        if (!safety.IsValid)
        {
            return safety.Error!;
        }

        var result = await workerClient.SetOpcUaInterfaceEnabledAsync(plcName, interfaceName, enabled, projectPath).ConfigureAwait(false);
        var verification = result.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)
            ? null
            : await workerClient.ListOpcUaInterfacesAsync(plcName, projectPath).ConfigureAwait(false);
        WriteSafetyService.Shared.AppendAudit("set_opcua_interface_enabled", projectPath, target, requestedInput, safety.CurrentState, result);
        return WriteSafetyTooling.BuildApplyResult("set_opcua_interface_enabled", result, "list_opcua_interfaces", verification);
    }

    [McpServerTool(Name = "preview_delete_opcua_interface")]
    [Description("Preview deleting an OPC UA server interface and return a short-lived safetyToken.")]
    public static async Task<string> PreviewDeleteOpcUaInterface(
        OpennessWorkerClient workerClient,
        [Description("Existing OPC UA server interface name.")] string interfaceName,
        [Description("Optional PLC device/software name.")] string? plcName = null,
        [Description("Optional path to a .ap21 project.")] string? projectPath = null)
    {
        var currentState = await workerClient.ListOpcUaInterfacesAsync(plcName, projectPath).ConfigureAwait(false);
        var target = new { plcName, interfaceName };
        var requestedInput = new { plcName, interfaceName };
        return WriteSafetyTooling.CreatePreview(
            "delete_opcua_interface",
            projectPath,
            target,
            $"Delete OPC UA server interface '{interfaceName}'.",
            requestedInput,
            currentState);
    }

    [McpServerTool(Name = "delete_opcua_interface")]
    [Description("Delete an OPC UA server interface. Requires confirm=true and a safetyToken from preview_delete_opcua_interface.")]
    public static async Task<string> DeleteOpcUaInterface(
        OpennessWorkerClient workerClient,
        [Description("Existing OPC UA server interface name.")] string interfaceName,
        [Description("Optional PLC device/software name.")] string? plcName = null,
        [Description("Optional path to a .ap21 project.")] string? projectPath = null,
        [Description("Set true to confirm the project write.")] bool confirm = false,
        [Description("Safety token returned by preview_delete_opcua_interface.")] string? safetyToken = null)
    {
        if (!confirm)
        {
            return "Operation not confirmed. Set confirm=true to delete the OPC UA interface.";
        }

        var target = new { plcName, interfaceName };
        var requestedInput = new { plcName, interfaceName };
        var safety = await WriteSafetyTooling.ValidateForApplyAsync(
            safetyToken,
            "preview_delete_opcua_interface",
            "delete_opcua_interface",
            projectPath,
            target,
            requestedInput,
            () => workerClient.ListOpcUaInterfacesAsync(plcName, projectPath)).ConfigureAwait(false);
        if (!safety.IsValid)
        {
            return safety.Error!;
        }

        var result = await workerClient.DeleteOpcUaInterfaceAsync(plcName, interfaceName, projectPath).ConfigureAwait(false);
        var verification = result.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)
            ? null
            : await workerClient.ListOpcUaInterfacesAsync(plcName, projectPath).ConfigureAwait(false);
        WriteSafetyService.Shared.AppendAudit("delete_opcua_interface", projectPath, target, requestedInput, safety.CurrentState, result);
        return WriteSafetyTooling.BuildApplyResult("delete_opcua_interface", result, "list_opcua_interfaces", verification);
    }
}

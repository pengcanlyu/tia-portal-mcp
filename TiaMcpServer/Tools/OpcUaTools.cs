using System.ComponentModel;
using ModelContextProtocol.Server;
using TiaMcpServer.Contracts;
using TiaMcpServer.Safety;
using TiaMcpServer.Worker;

namespace TiaMcpServer.Tools;

[McpServerToolType]
public static class OpcUaTools
{
    public const string DefaultInterfaceName = "三层Ubuntu接口";
    public const string DefaultInterfaceUri = "urn:guoji:zyg850:opcua:layer3";

    private const string SafetyFlowDescription =
        "Call without safetyToken to preview, then call again with the same arguments, confirm=true, and the returned token.";

    [McpServerTool(Name = "list_opcua_interfaces")]
    [Description("List OPC UA user-modelled server interfaces configured for the selected PLC.")]
    public static async Task<string> ListOpcUaInterfaces(
        OpennessWorkerClient workerClient,
        string? plcName = null,
        string? projectPath = null)
    {
        var result = await workerClient.ListOpcUaInterfacesAsync(plcName, projectPath).ConfigureAwait(false);
        return StandaloneToolResultFormatter.Format(result, "Select a PLC or inspect one interface at a time.");
    }

    [McpServerTool(Name = "inspect_opcua_variables")]
    [Description("Build an OPC UA interface in memory from accessible variables in global DBs. PLC tag tables and instance DBs are excluded.")]
    public static async Task<string> InspectOpcUaVariables(
        OpennessWorkerClient workerClient,
        string? plcName = null,
        string interfaceName = DefaultInterfaceName,
        string interfaceUri = DefaultInterfaceUri,
        bool keepFolderStructure = false,
        bool includeVariables = false,
        int maxVariables = 200,
        string? projectPath = null)
    {
        var result = await workerClient.InspectOpcUaVariablesAsync(
            plcName,
            interfaceName,
            interfaceUri,
            keepFolderStructure,
            includeVariables,
            maxVariables,
            projectPath).ConfigureAwait(false);
        return StandaloneToolResultFormatter.Format(result, "Set includeVariables=false or lower maxVariables.");
    }

    [McpServerTool(Name = "export_opcua_interface")]
    [Description("Export an existing OPC UA server interface to XML and optionally create a UTF-8 JSON NodeId catalog. Does not modify the TIA project.")]
    public static async Task<string> ExportOpcUaInterface(
        OpennessWorkerClient workerClient,
        string interfaceName,
        string exportPath,
        string? catalogPath = null,
        string? plcName = null,
        string? projectPath = null)
    {
        var result = await workerClient.ExportOpcUaInterfaceAsync(
            plcName,
            interfaceName,
            exportPath,
            catalogPath,
            projectPath).ConfigureAwait(false);
        return StandaloneToolResultFormatter.Format(result, "Export one interface at a time.");
    }

    [McpServerTool(Name = "preview_generate_opcua_interface")]
    [Description("Compatibility preview for generate_opcua_interface. Returns a short-lived safetyToken.")]
    public static Task<string> PreviewGenerateOpcUaInterface(
        OpennessWorkerClient workerClient,
        WriteSafetyService safety,
        string interfaceName = DefaultInterfaceName,
        string interfaceUri = DefaultInterfaceUri,
        bool keepFolderStructure = false,
        bool enabled = true,
        bool replaceExisting = false,
        string? author = "TIA MCP",
        string? exportPath = null,
        string? catalogPath = null,
        string? plcName = null,
        string? projectPath = null)
        => CreateGeneratePreview(workerClient, safety, interfaceName, interfaceUri, keepFolderStructure,
            enabled, replaceExisting, author, exportPath, catalogPath, plcName, projectPath);

    [McpServerTool(Name = "generate_opcua_interface")]
    [Description("Create or replace a global-DB OPC UA server interface. " + SafetyFlowDescription)]
    public static async Task<string> GenerateOpcUaInterface(
        OpennessWorkerClient workerClient,
        WriteSafetyService safety,
        string interfaceName = DefaultInterfaceName,
        string interfaceUri = DefaultInterfaceUri,
        bool keepFolderStructure = false,
        bool enabled = true,
        bool replaceExisting = false,
        string? author = "TIA MCP",
        string? exportPath = null,
        string? catalogPath = null,
        string? plcName = null,
        string? projectPath = null,
        bool confirm = false,
        string? safetyToken = null)
    {
        if (string.IsNullOrWhiteSpace(safetyToken))
        {
            return await CreateGeneratePreview(workerClient, safety, interfaceName, interfaceUri,
                keepFolderStructure, enabled, replaceExisting, author, exportPath, catalogPath,
                plcName, projectPath).ConfigureAwait(false);
        }

        if (!confirm) return ConfirmRequired("generate_opcua_interface");

        var target = new { plcName, interfaceName };
        var requestedInput = new { plcName, interfaceName, interfaceUri, keepFolderStructure, enabled,
            replaceExisting, author, exportPath, catalogPath };
        var safetyContext = await WriteSafetyTooling.ValidateForApplyAsync(
            safety,
            safetyToken,
            "generate_opcua_interface without safetyToken or preview_generate_opcua_interface",
            "generate_opcua_interface",
            projectPath,
            target,
            requestedInput,
            () => workerClient.ListOpcUaInterfacesAsync(plcName, projectPath)).ConfigureAwait(false);
        if (!safetyContext.IsValid) return SafetyFailure("generate_opcua_interface", safetyContext);

        var result = await workerClient.GenerateOpcUaInterfaceAsync(
            plcName, interfaceName, interfaceUri, keepFolderStructure, enabled, replaceExisting,
            author, exportPath, catalogPath, projectPath).ConfigureAwait(false);
        var verification = result.Success
            ? (await workerClient.ListOpcUaInterfacesAsync(plcName, projectPath).ConfigureAwait(false)).ToText()
            : null;
        safety.AppendAudit("generate_opcua_interface", projectPath, target, requestedInput,
            safetyContext.CurrentState, result.ToText());
        return WriteSafetyTooling.BuildApplyResult(
            "generate_opcua_interface", result, "list_opcua_interfaces", verification);
    }

    [McpServerTool(Name = "preview_set_opcua_interface_enabled")]
    [Description("Compatibility preview for set_opcua_interface_enabled. Returns a short-lived safetyToken.")]
    public static Task<string> PreviewSetOpcUaInterfaceEnabled(
        OpennessWorkerClient workerClient,
        WriteSafetyService safety,
        string interfaceName,
        bool enabled,
        string? plcName = null,
        string? projectPath = null)
        => CreateSetEnabledPreview(workerClient, safety, interfaceName, enabled, plcName, projectPath);

    [McpServerTool(Name = "set_opcua_interface_enabled")]
    [Description("Enable or disable an OPC UA server interface. " + SafetyFlowDescription)]
    public static async Task<string> SetOpcUaInterfaceEnabled(
        OpennessWorkerClient workerClient,
        WriteSafetyService safety,
        string interfaceName,
        bool enabled,
        string? plcName = null,
        string? projectPath = null,
        bool confirm = false,
        string? safetyToken = null)
    {
        if (string.IsNullOrWhiteSpace(safetyToken))
        {
            return await CreateSetEnabledPreview(workerClient, safety, interfaceName, enabled,
                plcName, projectPath).ConfigureAwait(false);
        }

        if (!confirm) return ConfirmRequired("set_opcua_interface_enabled");
        var target = new { plcName, interfaceName };
        var requestedInput = new { plcName, interfaceName, enabled };
        var safetyContext = await WriteSafetyTooling.ValidateForApplyAsync(
            safety,
            safetyToken,
            "set_opcua_interface_enabled without safetyToken or preview_set_opcua_interface_enabled",
            "set_opcua_interface_enabled",
            projectPath,
            target,
            requestedInput,
            () => workerClient.ListOpcUaInterfacesAsync(plcName, projectPath)).ConfigureAwait(false);
        if (!safetyContext.IsValid) return SafetyFailure("set_opcua_interface_enabled", safetyContext);

        var result = await workerClient.SetOpcUaInterfaceEnabledAsync(
            plcName, interfaceName, enabled, projectPath).ConfigureAwait(false);
        var verification = result.Success
            ? (await workerClient.ListOpcUaInterfacesAsync(plcName, projectPath).ConfigureAwait(false)).ToText()
            : null;
        safety.AppendAudit("set_opcua_interface_enabled", projectPath, target, requestedInput,
            safetyContext.CurrentState, result.ToText());
        return WriteSafetyTooling.BuildApplyResult(
            "set_opcua_interface_enabled", result, "list_opcua_interfaces", verification);
    }

    [McpServerTool(Name = "preview_delete_opcua_interface")]
    [Description("Compatibility preview for delete_opcua_interface. Returns a short-lived safetyToken.")]
    public static Task<string> PreviewDeleteOpcUaInterface(
        OpennessWorkerClient workerClient,
        WriteSafetyService safety,
        string interfaceName,
        string? plcName = null,
        string? projectPath = null)
        => CreateDeletePreview(workerClient, safety, interfaceName, plcName, projectPath);

    [McpServerTool(Name = "delete_opcua_interface")]
    [Description("Delete an OPC UA server interface. " + SafetyFlowDescription)]
    public static async Task<string> DeleteOpcUaInterface(
        OpennessWorkerClient workerClient,
        WriteSafetyService safety,
        string interfaceName,
        string? plcName = null,
        string? projectPath = null,
        bool confirm = false,
        string? safetyToken = null)
    {
        if (string.IsNullOrWhiteSpace(safetyToken))
        {
            return await CreateDeletePreview(workerClient, safety, interfaceName, plcName,
                projectPath).ConfigureAwait(false);
        }

        if (!confirm) return ConfirmRequired("delete_opcua_interface");
        var target = new { plcName, interfaceName };
        var requestedInput = new { plcName, interfaceName };
        var safetyContext = await WriteSafetyTooling.ValidateForApplyAsync(
            safety,
            safetyToken,
            "delete_opcua_interface without safetyToken or preview_delete_opcua_interface",
            "delete_opcua_interface",
            projectPath,
            target,
            requestedInput,
            () => workerClient.ListOpcUaInterfacesAsync(plcName, projectPath)).ConfigureAwait(false);
        if (!safetyContext.IsValid) return SafetyFailure("delete_opcua_interface", safetyContext);

        var result = await workerClient.DeleteOpcUaInterfaceAsync(
            plcName, interfaceName, projectPath).ConfigureAwait(false);
        var verification = result.Success
            ? (await workerClient.ListOpcUaInterfacesAsync(plcName, projectPath).ConfigureAwait(false)).ToText()
            : null;
        safety.AppendAudit("delete_opcua_interface", projectPath, target, requestedInput,
            safetyContext.CurrentState, result.ToText());
        return WriteSafetyTooling.BuildApplyResult(
            "delete_opcua_interface", result, "list_opcua_interfaces", verification);
    }

    private static async Task<string> CreateGeneratePreview(
        OpennessWorkerClient workerClient,
        WriteSafetyService safety,
        string interfaceName,
        string interfaceUri,
        bool keepFolderStructure,
        bool enabled,
        bool replaceExisting,
        string? author,
        string? exportPath,
        string? catalogPath,
        string? plcName,
        string? projectPath)
    {
        var currentState = await workerClient.ListOpcUaInterfacesAsync(plcName, projectPath).ConfigureAwait(false);
        var generatedPreview = await workerClient.InspectOpcUaVariablesAsync(
            plcName, interfaceName, interfaceUri, keepFolderStructure, false, 1, projectPath).ConfigureAwait(false);
        if (!generatedPreview.Success) return WriteSafetyTooling.BuildApplyResult("generate_opcua_interface", generatedPreview);

        var target = new { plcName, interfaceName };
        var requestedInput = new { plcName, interfaceName, interfaceUri, keepFolderStructure, enabled,
            replaceExisting, author, exportPath, catalogPath };
        return WriteSafetyTooling.CreatePreview(
            safety,
            "generate_opcua_interface",
            projectPath,
            target,
            $"Generate OPC UA interface '{interfaceName}' from accessible global-DB variables.",
            requestedInput,
            currentState,
            generatedPreview.Payload,
            ApplyInstructions("generate_opcua_interface"));
    }

    private static async Task<string> CreateSetEnabledPreview(
        OpennessWorkerClient workerClient,
        WriteSafetyService safety,
        string interfaceName,
        bool enabled,
        string? plcName,
        string? projectPath)
    {
        var currentState = await workerClient.ListOpcUaInterfacesAsync(plcName, projectPath).ConfigureAwait(false);
        var target = new { plcName, interfaceName };
        var requestedInput = new { plcName, interfaceName, enabled };
        return WriteSafetyTooling.CreatePreview(
            safety, "set_opcua_interface_enabled", projectPath, target,
            $"Set OPC UA interface '{interfaceName}' enabled={enabled}.", requestedInput,
            currentState, instructions: ApplyInstructions("set_opcua_interface_enabled"));
    }

    private static async Task<string> CreateDeletePreview(
        OpennessWorkerClient workerClient,
        WriteSafetyService safety,
        string interfaceName,
        string? plcName,
        string? projectPath)
    {
        var currentState = await workerClient.ListOpcUaInterfacesAsync(plcName, projectPath).ConfigureAwait(false);
        var target = new { plcName, interfaceName };
        var requestedInput = new { plcName, interfaceName };
        return WriteSafetyTooling.CreatePreview(
            safety, "delete_opcua_interface", projectPath, target,
            $"Delete OPC UA interface '{interfaceName}'.", requestedInput, currentState,
            instructions: ApplyInstructions("delete_opcua_interface"));
    }

    private static string ApplyInstructions(string toolName)
        => $"Preview only. Call {toolName} again with the same arguments, confirm=true, and this safetyToken.";

    private static string ConfirmRequired(string toolName)
        => WriteSafetyTooling.BuildApplyResult(
            toolName,
            WorkerCallResult.Fail(
                WorkerFailureCategories.ValidationError,
                "Safety token provided but confirm=false. Set confirm=true to apply."));

    private static string SafetyFailure(string toolName, WriteSafetyApplyContext safetyContext)
        => WriteSafetyTooling.BuildApplyResult(
            toolName,
            WorkerCallResult.Fail(
                safetyContext.FailureCategory ?? WorkerFailureCategories.ValidationError,
                safetyContext.Error ?? "Safety validation failed."));
}

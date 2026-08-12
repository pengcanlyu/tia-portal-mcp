using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using TiaMcpServer.Contracts;
using TiaMcpServer.Json;
using TiaMcpServer.Safety;
using TiaMcpServer.Worker;

namespace TiaMcpServer.Tools;

[McpServerToolType]
public class OpcUaReadTools
{
    public const string DefaultInterfaceName = "三层Ubuntu接口";
    public const string DefaultInterfaceUri = "urn:guoji:zyg850:opcua:layer3";

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
}

[McpServerToolType]
public class OpcUaWriteTools
{
    public const string DefaultInterfaceName = OpcUaReadTools.DefaultInterfaceName;
    public const string DefaultInterfaceUri = OpcUaReadTools.DefaultInterfaceUri;

    private const string SafetyFlowDescription =
        "Call without safetyToken to preview, then call again with the same arguments, confirm=true, and the returned token.";

    [McpServerTool(Name = "export_opcua_interface")]
    [Description("Export an existing OPC UA server interface to XML and optionally create a UTF-8 JSON NodeId catalog. Writes caller-selected files. " + SafetyFlowDescription)]
    public static async Task<string> ExportOpcUaInterface(
        OpennessWorkerClient workerClient,
        WriteSafetyService safety,
        string interfaceName,
        string exportPath,
        string? catalogPath = null,
        string? plcName = null,
        string? projectPath = null,
        bool confirm = false,
        string? safetyToken = null)
    {
        var requestedInput = new { plcName, interfaceName, exportPath, catalogPath };
        var state = await ReadInterfaceState(workerClient, plcName, projectPath).ConfigureAwait(false);
        if (!state.Success)
        {
            return WriteSafetyTooling.BuildApplyResult("export_opcua_interface", state.Source);
        }

        var target = BuildTarget(state, interfaceName);
        var currentState = BuildPersistentExportState(state.Source.Payload, exportPath, catalogPath);
        if (string.IsNullOrWhiteSpace(safetyToken))
        {
            return safety.CreatePreview(
                "export_opcua_interface",
                state.ProjectPath,
                target,
                $"Export OPC UA interface '{interfaceName}' and replace the requested output files.",
                requestedInput,
                currentState,
                instructions: ApplyInstructions("export_opcua_interface"));
        }

        if (!confirm) return ConfirmRequired("export_opcua_interface");
        var validation = safety.ValidateAndConsume(
            safetyToken,
            "export_opcua_interface",
            state.ProjectPath,
            target,
            requestedInput,
            currentState,
            "export_opcua_interface without safetyToken");
        if (!validation.IsValid) return SafetyFailure("export_opcua_interface", validation);

        var result = await workerClient.ExportOpcUaInterfaceAsync(
            state.PlcName, interfaceName, exportPath, catalogPath, state.ProjectPath).ConfigureAwait(false);
        safety.AppendAudit(
            "export_opcua_interface", state.ProjectPath, target, requestedInput, currentState, result.ToText());
        return WriteSafetyTooling.BuildApplyResult("export_opcua_interface", result);
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
        var requestedInput = new { plcName, interfaceName, interfaceUri, keepFolderStructure, enabled,
            replaceExisting, author, exportPath, catalogPath };
        var interfaceState = await ReadInterfaceState(workerClient, plcName, projectPath).ConfigureAwait(false);
        if (!interfaceState.Success)
        {
            return WriteSafetyTooling.BuildApplyResult("generate_opcua_interface", interfaceState.Source);
        }

        var generatedPreview = await workerClient.InspectOpcUaVariablesAsync(
            interfaceState.PlcName, interfaceName, interfaceUri, keepFolderStructure, false, 1,
            interfaceState.ProjectPath).ConfigureAwait(false);
        if (!generatedPreview.Success)
        {
            return WriteSafetyTooling.BuildApplyResult("generate_opcua_interface", generatedPreview);
        }

        var target = BuildTarget(interfaceState, interfaceName);
        var currentState = BuildGenerateState(
            interfaceState.Source.Payload, generatedPreview.Payload, exportPath, catalogPath);
        var validation = safety.ValidateAndConsume(
            safetyToken,
            "generate_opcua_interface",
            interfaceState.ProjectPath,
            target,
            requestedInput,
            currentState,
            "generate_opcua_interface without safetyToken or preview_generate_opcua_interface");
        if (!validation.IsValid) return SafetyFailure("generate_opcua_interface", validation);

        var result = await workerClient.GenerateOpcUaInterfaceAsync(
            interfaceState.PlcName, interfaceName, interfaceUri, keepFolderStructure, enabled, replaceExisting,
            author, exportPath, catalogPath, interfaceState.ProjectPath).ConfigureAwait(false);
        var verification = result.Success
            ? (await workerClient.ListOpcUaInterfacesAsync(
                interfaceState.PlcName, interfaceState.ProjectPath).ConfigureAwait(false)).ToText()
            : null;
        safety.AppendAudit(
            "generate_opcua_interface", interfaceState.ProjectPath, target, requestedInput,
            currentState, result.ToText());
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
        var requestedInput = new { plcName, interfaceName, enabled };
        var state = await ReadInterfaceState(workerClient, plcName, projectPath).ConfigureAwait(false);
        if (!state.Success)
        {
            return WriteSafetyTooling.BuildApplyResult("set_opcua_interface_enabled", state.Source);
        }

        var target = BuildTarget(state, interfaceName);
        var validation = safety.ValidateAndConsume(
            safetyToken,
            "set_opcua_interface_enabled",
            state.ProjectPath,
            target,
            requestedInput,
            state.Source.Payload,
            "set_opcua_interface_enabled without safetyToken or preview_set_opcua_interface_enabled");
        if (!validation.IsValid) return SafetyFailure("set_opcua_interface_enabled", validation);

        var result = await workerClient.SetOpcUaInterfaceEnabledAsync(
            state.PlcName, interfaceName, enabled, state.ProjectPath).ConfigureAwait(false);
        var verification = result.Success
            ? (await workerClient.ListOpcUaInterfacesAsync(
                state.PlcName, state.ProjectPath).ConfigureAwait(false)).ToText()
            : null;
        safety.AppendAudit(
            "set_opcua_interface_enabled", state.ProjectPath, target, requestedInput,
            state.Source.Payload, result.ToText());
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
        var requestedInput = new { plcName, interfaceName };
        var state = await ReadInterfaceState(workerClient, plcName, projectPath).ConfigureAwait(false);
        if (!state.Success)
        {
            return WriteSafetyTooling.BuildApplyResult("delete_opcua_interface", state.Source);
        }

        var target = BuildTarget(state, interfaceName);
        var validation = safety.ValidateAndConsume(
            safetyToken,
            "delete_opcua_interface",
            state.ProjectPath,
            target,
            requestedInput,
            state.Source.Payload,
            "delete_opcua_interface without safetyToken or preview_delete_opcua_interface");
        if (!validation.IsValid) return SafetyFailure("delete_opcua_interface", validation);

        var result = await workerClient.DeleteOpcUaInterfaceAsync(
            state.PlcName, interfaceName, state.ProjectPath).ConfigureAwait(false);
        var verification = result.Success
            ? (await workerClient.ListOpcUaInterfacesAsync(
                state.PlcName, state.ProjectPath).ConfigureAwait(false)).ToText()
            : null;
        safety.AppendAudit(
            "delete_opcua_interface", state.ProjectPath, target, requestedInput,
            state.Source.Payload, result.ToText());
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
        var interfaceState = await ReadInterfaceState(workerClient, plcName, projectPath).ConfigureAwait(false);
        if (!interfaceState.Success)
        {
            return WriteSafetyTooling.BuildApplyResult("generate_opcua_interface", interfaceState.Source);
        }

        var generatedPreview = await workerClient.InspectOpcUaVariablesAsync(
            interfaceState.PlcName, interfaceName, interfaceUri, keepFolderStructure, false, 1,
            interfaceState.ProjectPath).ConfigureAwait(false);
        if (!generatedPreview.Success)
        {
            return WriteSafetyTooling.BuildApplyResult("generate_opcua_interface", generatedPreview);
        }

        var target = BuildTarget(interfaceState, interfaceName);
        var requestedInput = new { plcName, interfaceName, interfaceUri, keepFolderStructure, enabled,
            replaceExisting, author, exportPath, catalogPath };
        return safety.CreatePreview(
            "generate_opcua_interface",
            interfaceState.ProjectPath,
            target,
            $"Generate OPC UA interface '{interfaceName}' from accessible global-DB variables.",
            requestedInput,
            BuildGenerateState(interfaceState.Source.Payload, generatedPreview.Payload, exportPath, catalogPath),
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
        var state = await ReadInterfaceState(workerClient, plcName, projectPath).ConfigureAwait(false);
        if (!state.Success)
        {
            return WriteSafetyTooling.BuildApplyResult("set_opcua_interface_enabled", state.Source);
        }

        var target = BuildTarget(state, interfaceName);
        var requestedInput = new { plcName, interfaceName, enabled };
        return safety.CreatePreview(
            "set_opcua_interface_enabled", state.ProjectPath, target,
            $"Set OPC UA interface '{interfaceName}' enabled={enabled}.", requestedInput,
            state.Source.Payload, instructions: ApplyInstructions("set_opcua_interface_enabled"));
    }

    private static async Task<string> CreateDeletePreview(
        OpennessWorkerClient workerClient,
        WriteSafetyService safety,
        string interfaceName,
        string? plcName,
        string? projectPath)
    {
        var state = await ReadInterfaceState(workerClient, plcName, projectPath).ConfigureAwait(false);
        if (!state.Success)
        {
            return WriteSafetyTooling.BuildApplyResult("delete_opcua_interface", state.Source);
        }

        var target = BuildTarget(state, interfaceName);
        var requestedInput = new { plcName, interfaceName };
        return safety.CreatePreview(
            "delete_opcua_interface", state.ProjectPath, target,
            $"Delete OPC UA interface '{interfaceName}'.", requestedInput, state.Source.Payload,
            instructions: ApplyInstructions("delete_opcua_interface"));
    }

    private static async Task<ResolvedInterfaceState> ReadInterfaceState(
        OpennessWorkerClient workerClient,
        string? plcName,
        string? projectPath)
    {
        var source = await workerClient.ListOpcUaInterfacesAsync(plcName, projectPath).ConfigureAwait(false);
        if (!source.Success)
        {
            return new ResolvedInterfaceState(source, source.ResolvedProjectPath ?? projectPath, null, null);
        }

        try
        {
            using var document = JsonDocument.Parse(source.Payload);
            var root = document.RootElement;
            var deviceName = ReadOptionalString(root, "resolvedDeviceName");
            var resolvedPlcName = ReadOptionalString(root, "resolvedPlcName");
            if (string.IsNullOrWhiteSpace(deviceName) || string.IsNullOrWhiteSpace(resolvedPlcName))
            {
                return new ResolvedInterfaceState(
                    WorkerCallResult.Fail(
                        WorkerFailureCategories.PostconditionFailed,
                        "OPC UA state response did not identify the resolved device and PLC."),
                    source.ResolvedProjectPath ?? projectPath,
                    deviceName,
                    resolvedPlcName);
            }

            return new ResolvedInterfaceState(
                source, source.ResolvedProjectPath ?? projectPath, deviceName, resolvedPlcName);
        }
        catch (JsonException ex)
        {
            return new ResolvedInterfaceState(
                WorkerCallResult.Fail(
                    WorkerFailureCategories.PostconditionFailed,
                    $"OPC UA state response was not valid JSON: {ex.Message}"),
                source.ResolvedProjectPath ?? projectPath,
                null,
                null);
        }
    }

    private static object BuildTarget(ResolvedInterfaceState state, string interfaceName)
        => new { state.DeviceName, state.PlcName, interfaceName };

    private static string BuildGenerateState(
        string interfaceState,
        string generatedModel,
        string? exportPath,
        string? catalogPath)
        => JsonSerializer.Serialize(
            new
            {
                interfaceState,
                generatedModel,
                exportPathState = DescribeOptionalPathState(exportPath),
                catalogPathState = DescribeOptionalPathState(catalogPath)
            },
            TiaJson.Presentation);

    private static string BuildPersistentExportState(
        string interfaceState,
        string exportPath,
        string? catalogPath)
        => JsonSerializer.Serialize(
            new
            {
                interfaceState,
                exportPathState = WriteSafetyTooling.DescribePathState(exportPath),
                catalogPathState = DescribeOptionalPathState(catalogPath)
            },
            TiaJson.Presentation);

    private static string? DescribeOptionalPathState(string? path)
        => string.IsNullOrWhiteSpace(path) ? null : WriteSafetyTooling.DescribePathState(path);

    private static string? ReadOptionalString(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string ApplyInstructions(string toolName)
        => $"Preview only. Call {toolName} again with the same arguments, confirm=true, and this safetyToken.";

    private static string ConfirmRequired(string toolName)
        => WriteSafetyTooling.BuildApplyResult(
            toolName,
            WorkerCallResult.Fail(
                WorkerFailureCategories.ValidationError,
                "Safety token provided but confirm=false. Set confirm=true to apply."));

    private static string SafetyFailure(string toolName, WriteSafetyValidationResult validation)
        => WriteSafetyTooling.BuildApplyResult(
            toolName,
            WorkerCallResult.Fail(
                validation.FailureCategory ?? WorkerFailureCategories.ValidationError,
                validation.Error ?? "Safety validation failed."));

    private sealed record ResolvedInterfaceState(
        WorkerCallResult Source,
        string? ProjectPath,
        string? DeviceName,
        string? PlcName)
    {
        public bool Success => Source.Success;
    }
}

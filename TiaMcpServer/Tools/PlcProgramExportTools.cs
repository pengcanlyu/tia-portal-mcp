using System.ComponentModel;
using ModelContextProtocol.Server;
using TiaMcpServer.Contracts;
using TiaMcpServer.Safety;
using TiaMcpServer.Worker;

namespace TiaMcpServer.Tools;

[McpServerToolType]
public class PlcProgramExportTools
{
    [McpServerTool(
        Name = "preview_export_plc_to_folder",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = true)]
    [Description("Preview exporting a complete PLC hardware and software program to a new Windows folder for SIMATIC Automation Tool ProgramUpdate. Returns a short-lived safetyToken.")]
    public static async Task<string> PreviewExportPlcToFolder(
        OpennessWorkerClient workerClient,
        WriteSafetyService safety,
        [Description("Exact top-level TIA Portal device name, for example 'S7-1200 station_1'.")] string deviceName,
        [Description("New Windows directory that will receive the SIMATIC.S7S program folder. Existing paths are rejected.")] string exportPath,
        [Description("Optional path to a .ap21 project file. If omitted, uses the project currently open in TIA Portal.")] string? projectPath = null)
    {
        var normalizedExportPath = Path.GetFullPath(exportPath);
        var target = new { projectPath, deviceName, exportPath = normalizedExportPath };
        var requestedInput = new { projectPath, deviceName, exportPath = normalizedExportPath };
        var currentState = await ReadCurrentState(workerClient, projectPath, normalizedExportPath)
            .ConfigureAwait(false);

        return WriteSafetyTooling.CreatePreview(
            safety,
            "export_plc_to_folder",
            projectPath,
            target,
            $"Export complete PLC device '{deviceName}' to new folder '{normalizedExportPath}'.",
            requestedInput,
            currentState,
            instructions: "Preview only - nothing was exported. Call export_plc_to_folder with the same arguments, confirm=true, and this safetyToken.");
    }

    [McpServerTool(
        Name = "export_plc_to_folder",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = true)]
    [Description("Export a complete PLC hardware and software program to a new Windows folder containing SIMATIC.S7S for SIMATIC Automation Tool ProgramUpdate. Requires confirm=true and a safetyToken from preview_export_plc_to_folder. Does not download to a PLC.")]
    public static async Task<string> ExportPlcToFolder(
        OpennessWorkerClient workerClient,
        WriteSafetyService safety,
        [Description("Exact top-level TIA Portal device name, for example 'S7-1200 station_1'.")] string deviceName,
        [Description("New Windows directory that will receive the SIMATIC.S7S program folder. Existing paths are rejected.")] string exportPath,
        [Description("Optional path to a .ap21 project file. If omitted, uses the project currently open in TIA Portal.")] string? projectPath = null,
        [Description("Set true to confirm the export operation.")] bool confirm = false,
        [Description("Safety token returned by preview_export_plc_to_folder for this exact export request.")] string? safetyToken = null)
    {
        if (!confirm)
        {
            return WriteSafetyTooling.BuildApplyResult(
                "export_plc_to_folder",
                WorkerCallResult.Fail(
                    WorkerFailureCategories.ValidationError,
                    "Operation not confirmed. Set confirm=true and provide a safetyToken from preview_export_plc_to_folder."));
        }

        var normalizedExportPath = Path.GetFullPath(exportPath);
        var target = new { projectPath, deviceName, exportPath = normalizedExportPath };
        var requestedInput = new { projectPath, deviceName, exportPath = normalizedExportPath };
        var apply = await WriteSafetyTooling.ValidateAndExecuteForApplyAsync(
            workerClient,
            safety,
            safetyToken,
            "preview_export_plc_to_folder",
            "export_plc_to_folder",
            projectPath,
            target,
            requestedInput,
            () => ReadCurrentState(workerClient, projectPath, normalizedExportPath),
            () => workerClient.ExportPlcToFolderAsync(deviceName, normalizedExportPath, projectPath),
            (context, operationResult) =>
            {
                var verification = WriteSafetyTooling.DescribePathState(normalizedExportPath);
                safety.AppendAudit(
                    "export_plc_to_folder",
                    projectPath,
                    target,
                    requestedInput,
                    context.CurrentState,
                    operationResult.ToText());
                return Task.FromResult<string?>(verification);
            }).ConfigureAwait(false);

        if (!apply.SafetyContext.IsValid)
        {
            return WriteSafetyTooling.BuildApplyResult(
                "export_plc_to_folder",
                WorkerCallResult.Fail(
                    apply.SafetyContext.FailureCategory ?? WorkerFailureCategories.ValidationError,
                    apply.SafetyContext.Error ?? "Safety validation failed."));
        }

        return WriteSafetyTooling.BuildApplyResult(
            "export_plc_to_folder",
            apply.OperationResult!,
            "export_directory_state",
            apply.VerificationResult);
    }

    private static async Task<WorkerCallResult> ReadCurrentState(
        OpennessWorkerClient workerClient,
        string? projectPath,
        string exportPath)
    {
        var projectStatus = await workerClient.GetProjectStatusAsync(projectPath).ConfigureAwait(false);
        if (!projectStatus.Success)
        {
            return projectStatus;
        }

        return projectStatus with
        {
            Payload = $"project={projectStatus.Payload}\nexportPath={WriteSafetyTooling.DescribePathState(exportPath)}"
        };
    }
}

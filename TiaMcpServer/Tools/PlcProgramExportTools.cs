using System.ComponentModel;
using ModelContextProtocol.Server;
using TiaMcpServer.Safety;
using TiaMcpServer.Worker;

namespace TiaMcpServer.Tools;

[McpServerToolType]
public static class PlcProgramExportTools
{
    [McpServerTool(Name = "preview_export_plc_to_folder")]
    [Description("Preview exporting a complete PLC hardware and software program to a new Windows folder for SIMATIC Automation Tool ProgramUpdate. Returns a short-lived safetyToken.")]
    public static async Task<string> PreviewExportPlcToFolder(
        OpennessWorkerClient workerClient,
        [Description("Exact top-level TIA Portal device name, for example 'S7-1200 station_1'.")] string deviceName,
        [Description("New Windows directory that will receive the SIMATIC.S7S program folder. Existing paths are rejected.")] string exportPath,
        [Description("Optional path to a .ap21 project file. If omitted, uses the project currently open in TIA Portal.")] string? projectPath = null)
    {
        var normalizedExportPath = Path.GetFullPath(exportPath);
        var currentState = await ReadCurrentState(workerClient, projectPath, normalizedExportPath).ConfigureAwait(false);
        var target = new { projectPath, deviceName, exportPath = normalizedExportPath };
        var requestedInput = new { projectPath, deviceName, exportPath = normalizedExportPath };

        return WriteSafetyTooling.CreatePreview(
            "export_plc_to_folder",
            projectPath,
            target,
            $"Export complete PLC device '{deviceName}' to new folder '{normalizedExportPath}'.",
            requestedInput,
            currentState);
    }

    [McpServerTool(Name = "export_plc_to_folder")]
    [Description("Export a complete PLC hardware and software program to a new Windows folder containing SIMATIC.S7S for SIMATIC Automation Tool ProgramUpdate. Requires confirm=true and a safetyToken from preview_export_plc_to_folder. Does not download to a PLC.")]
    public static async Task<string> ExportPlcToFolder(
        OpennessWorkerClient workerClient,
        [Description("Exact top-level TIA Portal device name, for example 'S7-1200 station_1'.")] string deviceName,
        [Description("New Windows directory that will receive the SIMATIC.S7S program folder. Existing paths are rejected.")] string exportPath,
        [Description("Optional path to a .ap21 project file. If omitted, uses the project currently open in TIA Portal.")] string? projectPath = null,
        [Description("Set true to confirm the export operation.")] bool confirm = false,
        [Description("Safety token returned by preview_export_plc_to_folder for this exact export request.")] string? safetyToken = null)
    {
        if (!confirm)
        {
            return "Operation not confirmed. Set confirm=true to proceed with exporting the PLC program.";
        }

        var normalizedExportPath = Path.GetFullPath(exportPath);
        var target = new { projectPath, deviceName, exportPath = normalizedExportPath };
        var requestedInput = new { projectPath, deviceName, exportPath = normalizedExportPath };
        var safety = await WriteSafetyTooling.ValidateForApplyAsync(
            safetyToken,
            "preview_export_plc_to_folder",
            "export_plc_to_folder",
            projectPath,
            target,
            requestedInput,
            () => ReadCurrentState(workerClient, projectPath, normalizedExportPath)).ConfigureAwait(false);
        if (!safety.IsValid)
        {
            return safety.Error!;
        }

        var result = await workerClient.ExportPlcToFolderAsync(deviceName, normalizedExportPath, projectPath)
            .ConfigureAwait(false);
        var verification = WriteSafetyTooling.DescribePathState(normalizedExportPath);

        WriteSafetyService.Shared.AppendAudit(
            "export_plc_to_folder",
            projectPath,
            target,
            requestedInput,
            safety.CurrentState,
            result);

        return WriteSafetyTooling.BuildApplyResult(
            "export_plc_to_folder",
            result,
            "export_directory_state",
            verification);
    }

    private static async Task<string> ReadCurrentState(
        OpennessWorkerClient workerClient,
        string? projectPath,
        string exportPath)
    {
        var projectStatus = await workerClient.GetProjectStatusAsync(projectPath).ConfigureAwait(false);
        if (projectStatus.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
        {
            return projectStatus;
        }

        return $"project={projectStatus}\nexportPath={WriteSafetyTooling.DescribePathState(exportPath)}";
    }
}

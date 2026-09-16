using System.ComponentModel;
using System.Reflection;
using ModelContextProtocol.Server;
using TiaMcpServer.Tools;
using TiaMcpServer.Worker;
using Xunit;

namespace TiaMcpServer.Tests;

public class PlcProgramExportToolTests
{
    [Theory]
    [InlineData("PreviewExportPlcToFolder", "preview_export_plc_to_folder")]
    [InlineData("ExportPlcToFolder", "export_plc_to_folder")]
    public void ToolsHaveMcpMetadata(string methodName, string expectedToolName)
    {
        var method = typeof(PlcProgramExportTools).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(method);
        var toolAttribute = method.GetCustomAttribute<McpServerToolAttribute>();
        Assert.NotNull(toolAttribute);
        Assert.Equal(expectedToolName, toolAttribute.Name);
        Assert.False(string.IsNullOrWhiteSpace(method.GetCustomAttribute<DescriptionAttribute>()?.Description));
    }

    [Fact]
    public void WorkerClientExposesPlcProgramExportMethod()
    {
        var method = typeof(OpennessWorkerClient).GetMethod("ExportPlcToFolderAsync");

        Assert.NotNull(method);
        Assert.Equal(typeof(Task<WorkerCallResult>), method.ReturnType);
    }

    [Fact]
    public async Task ExportRejectsUnconfirmedRequestBeforeWorkerUse()
    {
        var result = await PlcProgramExportTools.ExportPlcToFolder(
            workerClient: null!,
            safety: null!,
            deviceName: "S7-1200 station_1",
            exportPath: "C:\\Exports\\ProgramUpdate");

        Assert.Contains("Operation not confirmed", result);
        Assert.Contains("confirm=true", result);
    }

    [Fact]
    public async Task ConfirmedExportRejectsMissingSafetyTokenBeforeWorkerUse()
    {
        using var audit = new TempAuditDirectory();
        var result = await PlcProgramExportTools.ExportPlcToFolder(
            workerClient: null!,
            safety: audit.CreateSafety(),
            deviceName: "S7-1200 station_1",
            exportPath: "C:\\Exports\\ProgramUpdate",
            confirm: true);

        Assert.Contains("Safety token required", result);
        Assert.Contains("preview_export_plc_to_folder", result);
    }
}

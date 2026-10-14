using System.Text.Json;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using TiaMcpServer.Contracts.Block;
using TiaMcpServer.Contracts.Extensions;
using TiaMcpServer.Contracts.Plc;
using TiaMcpServer.OpennessWorker.Openness.Block;

namespace TiaMcpServer.OpennessWorker.Openness.Extensions;

internal static class HardwareCompileService
{
    public static EngineeringExtensionResult Probe(ProjectBase project, EngineeringQuery query)
    {
        var item = HardwareParameterService.Resolve(project, query);
        if (((IEngineeringServiceProvider)item).GetService<ICompilable>() == null)
            throw new InvalidOperationException("Selected DeviceItem does not expose ICompilable. Select the CPU module, not an arbitrary child.");
        var details = new { query.DeviceName, query.ItemPath, item.PositionNumber, scope = "hardware", offline = true };
        return new EngineeringExtensionResult { Operation = query.Operation,
            StateHash = HardwareParameterService.Hash(details), Details = JsonSerializer.SerializeToElement(details) };
    }

    public static EngineeringExtensionResult Compile(ProjectBase project, EngineeringQuery query)
    {
        var item = HardwareParameterService.Resolve(project, query);
        var compiler = ((IEngineeringServiceProvider)item).GetService<ICompilable>()
            ?? throw new InvalidOperationException("Selected DeviceItem does not expose ICompilable.");
        // DeviceItem compilation is hardware-only. No Device or online download overload is used.
        var compiled = compiler.Compile();
        var projection = CompileReportProjection.Flatten(compiled.Messages,
            m => m.Description, _ => "", m => m.State.ToString(), m => m.Messages,
            new CompileReportProjection.Budget());
        var info = new PlcCompileInfo { DeviceName = query.DeviceName, PlcName = item.Name,
            State = compiled.State.ToString(), ErrorCount = compiled.ErrorCount,
            WarningCount = compiled.WarningCount, Messages = projection.Messages };
        info.DiagnosticNotes.Add("Hardware-only DeviceItem compile; diagnostic paths are not collected. Software compile_check is separate.");
        if (projection.WasTruncated) info.DiagnosticNotes.Add("Some diagnostic details were omitted by bounded projection.");
        var report = new CompileCheckReport { Scope = "hardware", BlockPath = string.Join("/", query.ItemPath),
            OverallState = info.State, TotalErrorCount = info.ErrorCount, TotalWarningCount = info.WarningCount,
            Plcs = new List<PlcCompileInfo> { info } };
        CompileReportProjection.BoundSerializedReport(report);
        var result = new EngineeringExtensionResult { Operation = query.Operation,
            Success = compiled.ErrorCount == 0 && compiled.State != CompilerResultState.Error };
        if (!result.Success) result.Error = "Hardware compiler reported errors. Inspect diagnostics; no save/download was performed.";
        result.Details = JsonSerializer.SerializeToElement(report);
        while (JsonSerializer.Serialize(result).Length > 58000 && info.Messages.Count > 0)
        {
            info.Messages.RemoveAt(info.Messages.Count - 1);
            if (result.Warnings.Count == 0) result.Warnings.Add("Diagnostic details were truncated to fit the extension result budget; error/warning counts are retained.");
            result.Details = JsonSerializer.SerializeToElement(report);
        }
        return result;
    }
}

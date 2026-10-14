using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using TiaMcpServer.Contracts.Extensions;
using TiaMcpServer.Contracts.Safety;
using TiaMcpServer.Contracts.Worker;
using TiaMcpServer.Json;
using TiaMcpServer.OperationBatches;
using TiaMcpServer.Safety.Pipeline;
using TiaMcpServer.Tools;
using TiaMcpServer.Worker;

namespace TiaMcpServer.Extensions;

public sealed class EngineeringOperation : EngineeringQuery, IOperationBatchItem
{
    public string OperationId { get; set; } = "extension";
    public string? ProjectPath { get; set; }
}

public sealed record EngineeringReadResponse(string Tool, string ContractVersion, bool Success,
    WriteToolError? Error, EngineeringExtensionResult? Result);
public sealed record EngineeringVerification(bool Success);
public sealed record EngineeringWriteResponse(string Tool, string ContractVersion, string Phase, bool Success,
    WriteToolError? Error, IReadOnlyList<string> Warnings, IReadOnlyList<WriteGuardReport> Guards,
    IReadOnlyList<WriteEffect<EngineeringExtensionResult>> Effects, StructuredOperationBatch? Batch,
    EngineeringVerification? Verification);

[McpServerToolType]
public sealed class EngineeringReadTools
{
    [McpServerTool(Name = "engineering_read", ReadOnly = true, Destructive = false, OpenWorld = false,
        UseStructuredContent = true, OutputSchemaType = typeof(EngineeringReadResponse))]
    [Description("Read pengcanlyu extensions in an already-bound standalone project. Operations: inspect_hardware_parameters (exact deviceName and itemPath name-segment array starting at the device's direct children; returns module and channel attribute metadata, values and stateHash), list_opcua_interfaces, inspect_opcua_variables (exact plcName; optional allowlist path plus SHA256). Channel index is the returned collection index, not the I/O address. Hardware only exposes attributes supported by Siemens for the exact module/firmware. Reads do not change, save or download the project.")]
    public static async Task<CallToolResult> Read(OpennessWorkerClient workerClient, EngineeringOperation operation)
    {
        var error = EngineeringExtensionOperations.Validate(operation, false);
        if (error != null) return StructuredToolResult.Create(new EngineeringReadResponse("engineering_read", "1.0", false,
            new(WorkerFailureCategories.ValidationError, error), null), true);
        var call = await workerClient.EngineeringExtensionAsync(operation.Operation, operation, operation.ProjectPath).ConfigureAwait(false);
        if (!call.Success) return StructuredToolResult.Create(new EngineeringReadResponse("engineering_read", "1.0", false,
            new(call.FailureCategory ?? WorkerFailureCategories.WorkerOperationFailed, call.Error ?? "Read failed."), null), true);
        try {
            var result = EngineeringWriteDomain.Decode(call);
            return StructuredToolResult.Create(new EngineeringReadResponse("engineering_read", "1.0", result.Success, null, result), !result.Success);
        } catch (Exception ex) {
            return StructuredToolResult.Create(new EngineeringReadResponse("engineering_read", "1.0", false,
                new(WorkerFailureCategories.ProtocolError, ex.Message), null), true);
        }
    }
}

[McpServerToolType]
public sealed class EngineeringWriteTools
{
    [McpServerTool(Name = "engineering_write", ReadOnly = false, Destructive = true, OpenWorld = false,
        UseStructuredContent = true, OutputSchemaType = typeof(EngineeringWriteResponse))]
    [Description("One guarded OFFLINE engineering operation. Defaults to dryRun:true; explicitly use dryRun:false to apply after reviewing the effect. Operations: set_hardware_parameters (deviceName, exact itemPath, optional channelIndex, changes object, expectedStateHash from engineering_read), generate_opcua_interface, set_opcua_interface_enabled, delete_opcua_interface, export_opcua_interface (XML and optional catalogPath), export_plc_to_folder (deviceName, NEW absolute exportPath for SAT SIMATIC.S7S). OPC UA requires exact plcName. Mutations recheck prepared state and return verification; hardware returns every module/channel attribute for dependency review. Only metadata ReadWrite scalar attributes are editable. Not all GUI settings are exposed. Never saves, connects to an online PLC, downloads to hardware or changes RUN/STOP. No automatic retry or batch rollback.")]
    public static Task<CallToolResult> Write(OpennessWorkerClient workerClient, WriteExecution execution,
        EngineeringOperation operation, bool dryRun = true, CancellationToken cancellationToken = default)
        => execution.RunAsync(new EngineeringWriteDomain(workerClient),
            new WriteCall<EngineeringOperation>(operation?.ProjectPath, operation == null ? [] : new[] { operation }, dryRun), cancellationToken: cancellationToken);
}

public sealed class EngineeringWriteDomain(OpennessWorkerClient client)
    : IWriteDomain<EngineeringOperation, EngineeringExtensionResult, EngineeringVerification, EngineeringWriteResponse>
{
    private readonly Dictionary<string, string> _hashes = new();
    private bool _verified;
    public string ToolName => "engineering_write";
    public string ContractVersion => "1.0";
    public WriteValidation Validate(IReadOnlyList<EngineeringOperation> items, McpAccessMode mode)
    {
        if (items.Count != 1 || items[0] == null || string.IsNullOrWhiteSpace(items[0].OperationId) || items[0].OperationId.Length > 256)
            return WriteValidation.Invalid(WorkerFailureCategories.ValidationError, "Exactly one operation with a 1..256-character operationId is required.");
        var error = EngineeringExtensionOperations.Validate(items[0], true);
        if (error != null) return WriteValidation.Invalid(WorkerFailureCategories.ValidationError, error);
        if (!OperationPolicyCatalog.IsAllowed(mode, items[0].Operation))
            return WriteValidation.Invalid(WorkerFailureCategories.AccessDenied, "Write operation denied by access mode.");
        return WriteValidation.Valid();
    }
    public async Task<WritePlan<EngineeringExtensionResult>> PlanAsync(string? path, IReadOnlyList<EngineeringOperation> items)
    {
        var item = items[0];
        var call = await client.EngineeringExtensionAsync("engineering_extension_probe", item, path).ConfigureAwait(false);
        if (!call.Success) return WritePlan<EngineeringExtensionResult>.Fail(call.FailureCategory ?? WorkerFailureCategories.WorkerOperationFailed, call.Error ?? "Preparation failed.");
        try {
            var effect = Decode(call);
            if (!effect.Success || string.IsNullOrEmpty(effect.StateHash))
                return WritePlan<EngineeringExtensionResult>.Fail(WorkerFailureCategories.PostconditionFailed, effect.Error ?? "Preparation has no verified state.");
            _hashes[item.OperationId] = effect.StateHash;
            return WritePlan<EngineeringExtensionResult>.Ok(new[] { ItemPlan<EngineeringExtensionResult>.Resolved(effect) });
        } catch (Exception ex) { return WritePlan<EngineeringExtensionResult>.Fail(WorkerFailureCategories.ProtocolError, ex.Message); }
    }
    public IReadOnlyList<FiredGuard> EvaluateGuards(IReadOnlyList<EngineeringOperation> items, IReadOnlyList<ItemPlan<EngineeringExtensionResult>> plans) => Array.Empty<FiredGuard>();
    public async Task<ItemReplan<EngineeringExtensionResult>> ReplanAsync(string? path, EngineeringOperation item)
    {
        var result = await PlanAsync(path, new[] { item }).ConfigureAwait(false);
        return result.Success ? ItemReplan<EngineeringExtensionResult>.Ok(result.Items[0])
            : ItemReplan<EngineeringExtensionResult>.Fail(result.Error!.Category, result.Error.Message);
    }
    public Task<WorkerCallResult> MutateAsync(string? path, EngineeringOperation item)
        => client.EngineeringExtensionAsync(item.Operation, item, path, _hashes[item.OperationId]);

    internal static EngineeringExtensionResult Decode(WorkerCallResult call)
    {
        if (call.Payload.Length > 60000) throw new InvalidOperationException("Extension result exceeds 60000 characters; inspect one hardware channelIndex or request fewer OPC UA variables. Re-read state before any write retry.");
        return CanonicalJson.DeserializeWorkerPayload<EngineeringExtensionResult>(call.Payload);
    }
    public StructuredOperationItem Project(EngineeringOperation item, WorkerCallResult call)
    {
        EngineeringExtensionResult? value = null;
        string? error = call.Error;
        var category = call.FailureCategory ?? WorkerFailureCategories.PostconditionFailed;
        if (call.Success)
            try { value = Decode(call); error = value.Error; }
            catch (Exception ex) { error = ex.Message; category = WorkerFailureCategories.ProtocolError; }
        _verified = call.Success && value?.Success == true;
        return new(item.OperationId, item.Operation, _verified ? OperationBatchStatus.Succeeded : OperationBatchStatus.Failed,
            value == null ? null : ToElement(value),
            _verified ? null : new StructuredOperationFailure(category, error ?? "Postcondition failed."),
            null, null, value?.Warnings ?? call.Warnings.ToList());
    }
    private static JsonElement ToElement(EngineeringExtensionResult value)
    {
        using var document = JsonDocument.Parse(CanonicalJson.Serialize(value));
        return document.RootElement.Clone();
    }
    public Task<EngineeringVerification?> VerifyAsync(string? path, StructuredOperationBatch batch)
        => Task.FromResult<EngineeringVerification?>(new(_verified && batch.IsFullySuccessful));
    public bool VerificationSucceeded(EngineeringVerification? verification) => verification?.Success == true;
    public EngineeringWriteResponse Compose(WriteReport<EngineeringExtensionResult, EngineeringVerification> report) =>
        new(ToolName, ContractVersion, report.Phase, report.Success, report.Error, report.Warnings,
            report.Guards, report.Effects, report.Batch, report.Verification);
}

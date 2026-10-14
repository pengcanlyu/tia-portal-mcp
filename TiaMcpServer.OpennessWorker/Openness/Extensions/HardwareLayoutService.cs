using System.Text.Json;
using Siemens.Engineering.HW;
using TiaMcpServer.Contracts.Extensions;
using TiaMcpServer.Contracts.Worker;
using TiaMcpServer.OpennessWorker.Worker;

namespace TiaMcpServer.OpennessWorker.Openness.Extensions;

internal static class HardwareLayoutService
{
    private sealed class Item
    {
        public string Name { get; set; } = "";
        public string TypeIdentifier { get; set; } = "";
        public int PositionNumber { get; set; }
        public bool IsBuiltIn { get; set; }
        public string[] ItemPath { get; set; } = Array.Empty<string>();
    }

    private static HardwareObject Container(Siemens.Engineering.ProjectBase project, EngineeringQuery query) =>
        query.ItemPath.Length == 0 ? HardwareParameterService.FindDevice(project, query.DeviceName!) : HardwareParameterService.Resolve(project, query);

    private static Item[] Items(Siemens.Engineering.ProjectBase project, HardwareObject container, EngineeringQuery query)
    {
        // Siemens composition parent and physical slot container differ (notably S7-1500 racks).
        var members = HardwareEditingRules.ContainerMembers(HardwareParameterService.FindDevice(project, query.DeviceName!).DeviceItems,
            i => i.DeviceItems, i => i.Name, i => Equals(i.Container, container));
        if (members.Count > 256) throw new InvalidOperationException("Container is too large; select a narrower itemPath.");
        return members.Select(m => new Item { Name = m.Item.Name, TypeIdentifier = m.Item.TypeIdentifier,
            PositionNumber = m.Item.PositionNumber, IsBuiltIn = m.Item.IsBuiltIn, ItemPath = m.Path })
            .OrderBy(i => i.PositionNumber).ThenBy(i => i.Name, StringComparer.Ordinal).ToArray();
    }

    public static EngineeringExtensionResult Read(Siemens.Engineering.ProjectBase project, EngineeringQuery query)
    {
        var container = Container(project, query);
        var items = Items(project, container, query);
        var children = container.DeviceItems.Cast<DeviceItem>().Take(257).Select(i => new Item { Name = i.Name,
            TypeIdentifier = i.TypeIdentifier, PositionNumber = i.PositionNumber, IsBuiltIn = i.IsBuiltIn,
            ItemPath = query.ItemPath.Concat(new[] { i.Name }).ToArray() }).ToArray();
        if (children.Length > 256) throw new InvalidOperationException("Too many composition children.");
        var layout = new { query.DeviceName, query.ItemPath, containerTypeIdentifier = container.TypeIdentifier, items, children };
        bool? canPlug = null;
        var reasons = new List<string>();
        if (query.Operation != "inspect_hardware_layout")
        {
            if (items.Any(i => i.Name == query.ModuleName)) reasons.Add("The requested module name already exists in the container.");
            if (items.Any(i => i.PositionNumber == query.PositionNumber)) reasons.Add("The requested slot is occupied; no replacement or deletion is performed.");
            canPlug = reasons.Count == 0 && container.CanPlugNew(query.ModuleTypeIdentifier!, query.ModuleName!, query.PositionNumber!.Value);
            if (canPlug == false && reasons.Count == 0) reasons.Add("Siemens CanPlugNew returned false for this exact container, type and slot.");
        }
        return new EngineeringExtensionResult { Operation = query.Operation, StateHash = HardwareParameterService.Hash(layout),
            Details = JsonSerializer.SerializeToElement(new { layout, canPlug, reasons }) };
    }

    public static EngineeringExtensionResult Check(Siemens.Engineering.ProjectBase project, EngineeringQuery query)
    {
        var result = Read(project, query);
        if (result.StateHash != query.ExpectedStateHash)
            throw new WorkerOperationException(WorkerFailureCategories.StateChanged, "Container layout changed; inspect again before adding a module.");
        if (!result.Details!.Value.GetProperty("canPlug").GetBoolean())
            throw new InvalidOperationException("Module cannot be plugged. Read check_hardware_module for the precise placement result.");
        return result;
    }

    public static EngineeringExtensionResult Plug(Siemens.Engineering.ProjectBase project, EngineeringQuery query)
    {
        var before = Check(project, query);
        var container = Container(project, query);
        var previous = Items(project, container, query);
        string? failure = null;
        DeviceItem? created = null;
        try { created = container.PlugNew(query.ModuleTypeIdentifier!, query.ModuleName!, query.PositionNumber!.Value); }
        catch (Exception ex) { failure = ex.Message; }
        var after = Items(project, Container(project, query), query);
        var added = after.Where(i => i.Name == query.ModuleName && i.PositionNumber == query.PositionNumber && i.TypeIdentifier == query.ModuleTypeIdentifier).ToArray();
        var preserved = previous.All(p => after.Count(a => a.Name == p.Name && a.PositionNumber == p.PositionNumber && a.TypeIdentifier == p.TypeIdentifier && a.IsBuiltIn == p.IsBuiltIn) == 1);
        var success = failure == null && created != null && added.Length == 1 && after.Length == previous.Length + 1 && preserved;
        return new EngineeringExtensionResult { Operation = query.Operation, Success = success,
            Error = success ? null : failure ?? "Placement postcondition failed; inspect before any retry.",
            Details = JsonSerializer.SerializeToElement(new { before = before.Details, after, added, preserved }),
            Warnings = new() { "Offline hardware edit only. Inspect auto-created submodules and addresses, configure channels, compile and save separately. No automatic retry, move, delete, replacement or rollback." } };
    }
}

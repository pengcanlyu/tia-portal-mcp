using System.Text.Json;
using Siemens.Engineering;
using Siemens.Engineering.Download;
using Siemens.Engineering.Download.Configurations;
using Siemens.Engineering.HW;
using TiaMcpServer.Contracts.Extensions;
using TiaMcpServer.Contracts.Worker;
using TiaMcpServer.OpennessWorker.Openness.OpcUa;
using TiaMcpServer.OpennessWorker.Worker;

namespace TiaMcpServer.OpennessWorker.Openness.Extensions;

internal static class EngineeringExtensionService
{
    public static EngineeringExtensionResult Read(ProjectBase project, EngineeringQuery query)
    {
        var error = EngineeringExtensionOperations.Validate(query, false);
        if (error != null) throw new ArgumentException(error);
        if (query.Operation == "inspect_hardware_parameters") return HardwareParameterService.Read(project, query);
        var value = query.Operation == "list_opcua_interfaces"
            ? OpcUaInterfaceService.List(project, query.PlcName)
            : Inspect(project, query);
        return Wrap(query, value);
    }

    private static object Inspect(ProjectBase project, EngineeringQuery q) =>
        OpcUaInterfaceService.Inspect(project, q.PlcName, q.InterfaceName, q.InterfaceUri,
            q.KeepFolderStructure, q.IncludeVariables, q.MaxVariables, q.AllowedSourcePathsPath, q.AllowedSourcePathsSha256);

    public static EngineeringExtensionResult Probe(ProjectBase project, EngineeringQuery query)
    {
        var error = EngineeringExtensionOperations.Validate(query, true);
        if (error != null) throw new ArgumentException(error);
        if (query.Operation == "set_hardware_parameters") return HardwareParameterService.Check(project, query);
        CheckOutput(query.ExportPath, query.Operation == "export_plc_to_folder");
        CheckOutput(query.CatalogPath, false);
        if (query.Operation == "export_plc_to_folder")
        {
            var device = HardwareParameterService.FindDevice(project, query.DeviceName!);
            if (FindDownloadProvider(device.DeviceItems) == null) throw new InvalidOperationException("Device has no folder download provider.");
            return Wrap(query, new { deviceName = device.Name, exportPath = Path.GetFullPath(query.ExportPath!), offlineFolderExport = true });
        }
        var list = OpcUaInterfaceService.List(project, query.PlcName);
        var generation = query.Operation == "generate_opcua_interface" ? Inspect(project, query) : null;
        return Wrap(query, new { existing = list, generation, query.InterfaceName, query.Enabled,
            query.ReplaceExisting, query.ExportPath, query.CatalogPath });
    }

    private static void CheckOutput(string? path, bool directory)
    {
        if (string.IsNullOrEmpty(path)) return;
        if (!Path.IsPathRooted(path)) throw new ArgumentException("Output paths must be absolute.");
        if (directory && (Directory.Exists(path) || File.Exists(path)))
            throw new ArgumentException("Program export requires a new output directory.");
        if (!directory && Directory.Exists(path)) throw new ArgumentException("Expected an output file, not a directory.");
    }

    public static EngineeringExtensionResult Apply(ProjectBase project, EngineeringQuery query, string expectedProbeHash)
    {
        var before = Probe(project, query);
        if (before.StateHash != expectedProbeHash) throw new WorkerOperationException(
            WorkerFailureCategories.StateChanged, "Target state changed after preparation. Inspect before retrying.");
        if (query.Operation == "set_hardware_parameters") return HardwareParameterService.Write(project, query);
        object value = query.Operation switch {
            "generate_opcua_interface" => OpcUaInterfaceService.Generate(project, query.PlcName, query.InterfaceName,
                query.InterfaceUri, query.KeepFolderStructure, query.Enabled, query.ReplaceExisting, query.Author,
                query.ExportPath, query.CatalogPath, query.AllowedSourcePathsPath, query.AllowedSourcePathsSha256),
            "set_opcua_interface_enabled" => OpcUaInterfaceService.SetEnabled(project, query.PlcName, query.InterfaceName, query.Enabled),
            "delete_opcua_interface" => OpcUaInterfaceService.Delete(project, query.PlcName, query.InterfaceName),
            "export_opcua_interface" => OpcUaInterfaceService.Export(project, query.PlcName, query.InterfaceName, query.ExportPath!, query.CatalogPath),
            "export_plc_to_folder" => ExportProgram(project, query),
            _ => throw new ArgumentException("Unknown extension write.")
        };
        var result = Wrap(query, value);
        if (query.Operation.Contains("opcua"))
        {
            var actual = JsonSerializer.SerializeToElement(OpcUaInterfaceService.List(project, query.PlcName));
            var interfaces = actual.GetProperty("interfaces").EnumerateArray().ToArray();
            var matches = interfaces.Where(i => string.Equals(i.GetProperty("Name").GetString(), query.InterfaceName, StringComparison.OrdinalIgnoreCase)).ToArray();
            var verified = query.Operation == "delete_opcua_interface" ? matches.Length == 0 :
                matches.Length == 1 && (query.Operation == "export_opcua_interface" ||
                matches[0].GetProperty("Enabled").GetBoolean() == query.Enabled);
            result.Success = verified;
            result.Error = verified ? null : "OPC UA postcondition did not match.";
            result.Details = JsonSerializer.SerializeToElement(new { mutation = value, after = actual });
        }
        return result;
    }

    private static EngineeringExtensionResult Wrap(EngineeringQuery query, object value) => new() {
        Operation = query.Operation, Details = JsonSerializer.SerializeToElement(value), StateHash = HardwareParameterService.Hash(value)
    };

    private static object ExportProgram(ProjectBase project, EngineeringQuery query)
    {
        var device = HardwareParameterService.FindDevice(project, query.DeviceName!);
        var provider = FindDownloadProvider(device.DeviceItems) ?? throw new InvalidOperationException("No PLC folder export provider.");
        var directory = new DirectoryInfo(Path.GetFullPath(query.ExportPath!));
        if (directory.Exists || File.Exists(directory.FullName)) throw new ArgumentException("Export output already exists.");
        directory.Create();
        // This overload writes a memory-card folder only. It takes no network/online target.
        var result = provider.Download(directory, ConfigureFolderDownload);
        var files = directory.EnumerateFiles("*", SearchOption.AllDirectories).ToArray();
        var simatic = directory.EnumerateDirectories("SIMATIC.S7S", SearchOption.AllDirectories).FirstOrDefault();
        if (result.ErrorCount != 0 || files.Length == 0 || simatic == null)
            throw new InvalidOperationException("Folder export failed verification; partial files remain at " + directory.FullName);
        return new { deviceName = device.Name, exportPath = directory.FullName, simaticS7sPath = simatic.FullName,
            fileCount = files.Length, sizeBytes = files.Sum(f => f.Length), result.ErrorCount, result.WarningCount };
    }

    private static DownloadProvider? FindDownloadProvider(DeviceItemComposition items)
    {
        foreach (DeviceItem item in items)
        {
            var provider = ((IEngineeringServiceProvider)item).GetService<DownloadProvider>();
            if (provider != null) return provider;
            provider = FindDownloadProvider(item.DeviceItems);
            if (provider != null) return provider;
        }
        return null;
    }

    private static void ConfigureFolderDownload(DownloadConfiguration configuration)
    {
        switch (configuration)
        {
            case TargetForSoftware software: software.CurrentSelection = TargetForSoftwareSelections.CPU; break;
            case OverwriteOnMemoryCard overwrite: overwrite.CurrentSelection = OverwriteOnMemoryCardSelections.Load; break;
            case ConsistentBlocksDownload blocks: blocks.CurrentSelection = ConsistentBlocksDownloadSelections.ConsistentDownload; break;
            case AlarmTextLibrariesDownload alarms: alarms.CurrentSelection = AlarmTextLibrariesDownloadSelections.ConsistentDownload; break;
        }
    }
}

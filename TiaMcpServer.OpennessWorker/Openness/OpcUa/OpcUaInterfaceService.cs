using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Siemens.Engineering;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.OpcUa;
using TiaMcpServer.OpennessWorker.Openness.OpcUa.SiemensGenerator;

namespace TiaMcpServer.OpennessWorker.Openness.OpcUa;

internal static class OpcUaInterfaceService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static object List(Project project, string? plcName)
    {
        var resolved = PlcSoftwareLocator.FindWithIdentity(project, plcName);
        var software = resolved.Software;
        var interfaces = GetInterfaces(software);

        return new
        {
            requestedPlcName = plcName,
            resolvedDeviceName = resolved.DeviceName,
            resolvedPlcName = software.Name,
            count = interfaces.Count,
            interfaces = interfaces.Select(ToInfo).ToArray()
        };
    }

    public static object Inspect(
        Project project,
        string? plcName,
        string interfaceName,
        string interfaceUri,
        bool keepFolderStructure,
        bool includeVariables,
        int maxVariables)
    {
        var resolved = PlcSoftwareLocator.FindWithIdentity(project, plcName);
        var software = resolved.Software;
        var generated = OpcUaInterfaceGenerator.Generate(
            software,
            interfaceName,
            interfaceUri,
            keepFolderStructure);

        return BuildGenerationInfo(
            plcName, resolved.DeviceName, software.Name, interfaceName, interfaceUri,
            generated, includeVariables, maxVariables);
    }

    public static object Export(
        Project project,
        string? plcName,
        string interfaceName,
        string exportPath,
        string? catalogPath)
    {
        EnsureOutputPath(exportPath, ".xml");
        if (!string.IsNullOrWhiteSpace(catalogPath))
        {
            EnsureOutputPath(catalogPath!, ".json");
        }

        var resolved = PlcSoftwareLocator.FindWithIdentity(project, plcName);
        var software = resolved.Software;
        var serverInterface = FindInterface(software, interfaceName)
            ?? throw new InvalidOperationException($"OPC UA server interface '{interfaceName}' was not found.");

        var workingDirectory = Path.Combine(Path.GetTempPath(), "tia-mcp-opcua-export", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDirectory);
        var preparedXmlPath = Path.Combine(workingDirectory, "interface.xml");
        var preparedCatalogPath = Path.Combine(workingDirectory, "catalog.json");
        var exportBackup = BackupOutputFile(exportPath, workingDirectory, "export.backup");
        var catalogBackup = BackupOutputFile(catalogPath, workingDirectory, "catalog.backup");
        var outputsTouched = false;

        try
        {
            serverInterface.Export(new FileInfo(preparedXmlPath));
            var document = System.Xml.Linq.XDocument.Load(preparedXmlPath);
            var variables = OpcUaNodeCatalog.Read(document);
            if (!string.IsNullOrWhiteSpace(catalogPath))
            {
                File.WriteAllText(
                    preparedCatalogPath,
                    JsonSerializer.Serialize(variables, JsonOptions),
                    new UTF8Encoding(false));
            }

            outputsTouched = true;
            CommitPreparedFile(preparedXmlPath, exportPath);
            if (!string.IsNullOrWhiteSpace(catalogPath))
            {
                CommitPreparedFile(preparedCatalogPath, catalogPath!);
            }

            return new
            {
                requestedPlcName = plcName,
                resolvedDeviceName = resolved.DeviceName,
                resolvedPlcName = software.Name,
                interfaceName,
                exportPath = Path.GetFullPath(exportPath),
                catalogPath = string.IsNullOrWhiteSpace(catalogPath) ? null : Path.GetFullPath(catalogPath!),
                variableCount = variables.Count,
                readableCount = variables.Count(variable => variable.Readable),
                writableCount = variables.Count(variable => variable.Writable)
            };
        }
        catch
        {
            if (outputsTouched)
            {
                RestoreOutputFile(exportBackup);
                RestoreOutputFile(catalogBackup);
            }

            throw;
        }
        finally
        {
            TryDeleteDirectory(workingDirectory);
        }
    }

    public static object Generate(
        Project project,
        string? plcName,
        string interfaceName,
        string interfaceUri,
        bool keepFolderStructure,
        bool enabled,
        bool replaceExisting,
        string? author,
        string? exportPath,
        string? catalogPath)
    {
        if (!string.IsNullOrWhiteSpace(exportPath))
        {
            EnsureOutputPath(exportPath!, ".xml");
        }

        if (!string.IsNullOrWhiteSpace(catalogPath))
        {
            EnsureOutputPath(catalogPath!, ".json");
        }

        var resolved = PlcSoftwareLocator.FindWithIdentity(project, plcName);
        var software = resolved.Software;
        var composition = GetComposition(software);
        var existing = FindInterface(composition, interfaceName);
        EnsureNoSimaticInterfaceConflict(software, interfaceName);

        if (existing is not null && !replaceExisting)
        {
            throw new InvalidOperationException(
                $"OPC UA server interface '{interfaceName}' already exists. Set replaceExisting=true only after reviewing the preview.");
        }

        var generated = OpcUaInterfaceGenerator.Generate(
            software,
            interfaceName,
            interfaceUri,
            keepFolderStructure);

        var workingDirectory = Path.Combine(Path.GetTempPath(), "tia-mcp-opcua-import", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDirectory);
        var generatedPath = Path.Combine(workingDirectory, "generated.xml");
        File.WriteAllText(generatedPath, generated.Xml, new UTF8Encoding(false));

        string? backupPath = null;
        bool oldEnabled = false;
        string? oldAuthor = null;
        bool existingDeleted = false;
        ServerInterface? created = null;
        var exportBackup = BackupOutputFile(exportPath, workingDirectory, "export.backup");
        var catalogBackup = BackupOutputFile(catalogPath, workingDirectory, "catalog.backup");
        var outputsTouched = false;

        try
        {
            if (existing is not null)
            {
                backupPath = Path.Combine(workingDirectory, "previous.xml");
                existing.Export(new FileInfo(backupPath));
                oldEnabled = existing.Enabled;
                oldAuthor = existing.Author;
                existing.Delete();
                existingDeleted = true;
            }

            created = composition.Create(interfaceName);
            created.Import(new FileInfo(generatedPath));
            created.Author = string.IsNullOrWhiteSpace(author) ? "TIA MCP" : author!;
            created.Enabled = enabled;

            outputsTouched = true;
            if (!string.IsNullOrWhiteSpace(exportPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(exportPath!)!);
                File.WriteAllText(exportPath!, generated.Xml, new UTF8Encoding(false));
            }

            if (!string.IsNullOrWhiteSpace(catalogPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(catalogPath!)!);
                File.WriteAllText(catalogPath!, JsonSerializer.Serialize(generated.Variables, JsonOptions), new UTF8Encoding(false));
            }

            return new
            {
                operation = existing is null ? "created" : "replaced",
                requestedPlcName = plcName,
                resolvedDeviceName = resolved.DeviceName,
                resolvedPlcName = software.Name,
                enabled,
                author = string.IsNullOrWhiteSpace(author) ? "TIA MCP" : author,
                exportPath = string.IsNullOrWhiteSpace(exportPath) ? null : Path.GetFullPath(exportPath!),
                catalogPath = string.IsNullOrWhiteSpace(catalogPath) ? null : Path.GetFullPath(catalogPath!),
                generation = BuildGenerationInfo(
                    plcName, resolved.DeviceName, software.Name, interfaceName, interfaceUri,
                    generated, includeVariables: false, maxVariables: 0)
            };
        }
        catch
        {
            try
            {
                created?.Delete();
            }
            catch
            {
                // Continue with restoring the prior project and filesystem state.
            }

            if (existingDeleted)
            {
                RestorePreviousInterface(composition, interfaceName, backupPath, oldEnabled, oldAuthor);
            }

            if (outputsTouched)
            {
                RestoreOutputFile(exportBackup);
                RestoreOutputFile(catalogBackup);
            }

            throw;
        }
        finally
        {
            TryDeleteDirectory(workingDirectory);
        }
    }

    public static object SetEnabled(Project project, string? plcName, string interfaceName, bool enabled)
    {
        var software = PlcSoftwareLocator.Find(project, plcName);
        var serverInterface = FindInterface(software, interfaceName)
            ?? throw new InvalidOperationException($"OPC UA server interface '{interfaceName}' was not found.");
        var previous = serverInterface.Enabled;
        serverInterface.Enabled = enabled;

        return new { interfaceName, previousEnabled = previous, enabled = serverInterface.Enabled };
    }

    public static object Delete(Project project, string? plcName, string interfaceName)
    {
        var software = PlcSoftwareLocator.Find(project, plcName);
        var serverInterface = FindInterface(software, interfaceName)
            ?? throw new InvalidOperationException($"OPC UA server interface '{interfaceName}' was not found.");
        var info = ToInfo(serverInterface);
        serverInterface.Delete();
        return new { deleted = true, interfaceInfo = info };
    }

    private static object BuildGenerationInfo(
        string? requestedPlcName,
        string resolvedDeviceName,
        string resolvedPlcName,
        string interfaceName,
        string interfaceUri,
        OpcUaGenerationResult generated,
        bool includeVariables,
        int maxVariables)
    {
        var returnedVariables = includeVariables
            ? generated.Variables.Take(Math.Max(1, Math.Min(maxVariables, 5000))).ToArray()
            : null;
        var variablesByDataBlock = generated.Variables
            .GroupBy(variable => GetDataBlockName(variable.SourcePath), StringComparer.Ordinal)
            .Select(group => new { dataBlock = group.Key, variableCount = group.Count() })
            .OrderByDescending(group => group.variableCount)
            .ThenBy(group => group.dataBlock, StringComparer.Ordinal)
            .ToArray();

        return new
        {
            requestedPlcName,
            resolvedDeviceName,
            resolvedPlcName,
            interfaceName,
            interfaceUri,
            modelFingerprint = ComputeFingerprint(generated.Xml),
            defaultNodeCount = generated.DefaultNodeCount,
            dataTypeNodeCount = generated.DataTypeNodeCount,
            globalDbNodeCount = generated.GlobalDbNodeCount,
            variableCount = generated.Variables.Count,
            readableCount = generated.Variables.Count(variable => variable.Readable),
            writableCount = generated.Variables.Count(variable => variable.Writable),
            warnings = generated.Warnings,
            variablesByDataBlock,
            returnedVariableCount = returnedVariables?.Length ?? 0,
            variablesTruncated = returnedVariables is not null && returnedVariables.Length < generated.Variables.Count,
            variables = returnedVariables
        };
    }

    private static string GetDataBlockName(string sourcePath)
    {
        if (sourcePath.StartsWith("\"", StringComparison.Ordinal))
        {
            var closingQuote = sourcePath.IndexOf('\"', 1);
            if (closingQuote > 1)
            {
                return sourcePath.Substring(1, closingQuote - 1);
            }
        }

        var separator = sourcePath.IndexOf('.');
        return separator > 0 ? sourcePath.Substring(0, separator) : sourcePath;
    }

    private static IReadOnlyList<ServerInterface> GetInterfaces(PlcSoftware software)
    {
        return GetComposition(software).ToArray();
    }

    private static ServerInterfaceComposition GetComposition(PlcSoftware software)
    {
        var provider = software.GetService<OpcUaProvider>()
            ?? throw new InvalidOperationException(
                "The selected PLC does not expose the OPC UA Openness provider. Verify that the configured CPU firmware is V4.4 or newer.");
        return provider.CommunicationGroup.ServerInterfaceGroup.ServerInterfaces;
    }

    private static ServerInterface? FindInterface(PlcSoftware software, string name)
    {
        return FindInterface(GetComposition(software), name);
    }

    private static ServerInterface? FindInterface(ServerInterfaceComposition composition, string name)
    {
        return composition.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static void EnsureNoSimaticInterfaceConflict(PlcSoftware software, string interfaceName)
    {
        var provider = software.GetService<OpcUaProvider>()
            ?? throw new InvalidOperationException("The selected PLC does not expose the OPC UA Openness provider.");
        var conflict = provider.CommunicationGroup.ServerInterfaceGroup.SimaticInterfaces
            .FirstOrDefault(item => string.Equals(item.Name, interfaceName, StringComparison.OrdinalIgnoreCase));
        if (conflict is not null)
        {
            throw new InvalidOperationException(
                $"OPC UA interface name '{interfaceName}' conflicts with an existing SIMATIC interface.");
        }
    }

    private static string ComputeFingerprint(string xml)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(xml));
        return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
    }

    private static object ToInfo(ServerInterface item)
    {
        return new
        {
            item.Name,
            item.Author,
            item.Enabled,
            item.CreationTime,
            item.LastModified
        };
    }

    private static void RestorePreviousInterface(
        ServerInterfaceComposition composition,
        string interfaceName,
        string? backupPath,
        bool enabled,
        string? author)
    {
        if (string.IsNullOrWhiteSpace(backupPath) || !File.Exists(backupPath))
        {
            return;
        }

        var restored = composition.Create(interfaceName);
        restored.Import(new FileInfo(backupPath));
        restored.Enabled = enabled;
        if (!string.IsNullOrWhiteSpace(author))
        {
            restored.Author = author;
        }
    }

    private static void EnsureOutputPath(string path, string expectedExtension)
    {
        if (!Path.IsPathRooted(path))
        {
            throw new InvalidOperationException($"Output path must be absolute: '{path}'.");
        }

        if (!string.Equals(Path.GetExtension(path), expectedExtension, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Output path must use the '{expectedExtension}' extension: '{path}'.");
        }
    }

    private static OutputFileBackup BackupOutputFile(string? path, string workingDirectory, string backupName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return new OutputFileBackup(null, null, false);
        }

        var fullPath = Path.GetFullPath(path!);
        if (!File.Exists(fullPath))
        {
            return new OutputFileBackup(fullPath, null, false);
        }

        var backupPath = Path.Combine(workingDirectory, backupName);
        File.Copy(fullPath, backupPath, overwrite: true);
        return new OutputFileBackup(fullPath, backupPath, true);
    }

    private static void RestoreOutputFile(OutputFileBackup backup)
    {
        if (string.IsNullOrWhiteSpace(backup.TargetPath))
        {
            return;
        }

        if (backup.Existed)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(backup.TargetPath!)!);
            File.Copy(backup.BackupPath!, backup.TargetPath!, overwrite: true);
        }
        else if (File.Exists(backup.TargetPath))
        {
            File.Delete(backup.TargetPath);
        }
    }

    private static void CommitPreparedFile(string preparedPath, string targetPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        File.Copy(preparedPath, targetPath, overwrite: true);
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Temporary files are not part of project state.
        }
    }

    private sealed class OutputFileBackup
    {
        public OutputFileBackup(string? targetPath, string? backupPath, bool existed)
        {
            TargetPath = targetPath;
            BackupPath = backupPath;
            Existed = existed;
        }

        public string? TargetPath { get; }
        public string? BackupPath { get; }
        public bool Existed { get; }
    }
}

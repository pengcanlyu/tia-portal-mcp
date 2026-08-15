using System.Text;
using System.Text.Json;
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
        var software = PlcSoftwareLocator.Find(project, plcName);
        var interfaces = GetInterfaces(software);

        return new
        {
            plcName,
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
        int maxVariables,
        string? allowedSourcePathsPath)
    {
        var software = PlcSoftwareLocator.Find(project, plcName);
        var allowedSourcePaths = OpcUaSourcePathFilter.Load(allowedSourcePathsPath);
        var generated = OpcUaInterfaceGenerator.Generate(
            software,
            interfaceName,
            interfaceUri,
            keepFolderStructure,
            allowedSourcePaths);

        return BuildGenerationInfo(interfaceName, interfaceUri, generated, includeVariables, maxVariables);
    }

    public static object Export(
        Project project,
        string? plcName,
        string interfaceName,
        string exportPath,
        string? catalogPath)
    {
        var software = PlcSoftwareLocator.Find(project, plcName);
        var serverInterface = FindInterface(software, interfaceName)
            ?? throw new InvalidOperationException($"OPC UA server interface '{interfaceName}' was not found.");

        EnsureOutputPath(exportPath, ".xml");
        Directory.CreateDirectory(Path.GetDirectoryName(exportPath)!);
        serverInterface.Export(new FileInfo(exportPath));

        var document = System.Xml.Linq.XDocument.Load(exportPath);
        var variables = OpcUaNodeCatalog.Read(document);

        if (!string.IsNullOrWhiteSpace(catalogPath))
        {
            EnsureOutputPath(catalogPath!, ".json");
            Directory.CreateDirectory(Path.GetDirectoryName(catalogPath!)!);
            File.WriteAllText(catalogPath!, JsonSerializer.Serialize(variables, JsonOptions), new UTF8Encoding(false));
        }

        return new
        {
            interfaceName,
            exportPath = Path.GetFullPath(exportPath),
            catalogPath = string.IsNullOrWhiteSpace(catalogPath) ? null : Path.GetFullPath(catalogPath!),
            variableCount = variables.Count,
            readableCount = variables.Count(variable => variable.Readable),
            writableCount = variables.Count(variable => variable.Writable)
        };
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
        string? catalogPath,
        string? allowedSourcePathsPath)
    {
        var software = PlcSoftwareLocator.Find(project, plcName);
        var composition = GetComposition(software);
        var existing = FindInterface(composition, interfaceName);

        if (existing is not null && !replaceExisting)
        {
            throw new InvalidOperationException(
                $"OPC UA server interface '{interfaceName}' already exists. Set replaceExisting=true only after reviewing the preview.");
        }

        var allowedSourcePaths = OpcUaSourcePathFilter.Load(allowedSourcePathsPath);
        var generated = OpcUaInterfaceGenerator.Generate(
            software,
            interfaceName,
            interfaceUri,
            keepFolderStructure,
            allowedSourcePaths);

        var workingDirectory = Path.Combine(Path.GetTempPath(), "tia-mcp-opcua-import", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDirectory);
        var generatedPath = Path.Combine(workingDirectory, "generated.xml");
        File.WriteAllText(generatedPath, generated.Xml, new UTF8Encoding(false));

        string? backupPath = null;
        bool oldEnabled = false;
        string? oldAuthor = null;

        try
        {
            if (existing is not null)
            {
                backupPath = Path.Combine(workingDirectory, "previous.xml");
                existing.Export(new FileInfo(backupPath));
                oldEnabled = existing.Enabled;
                oldAuthor = existing.Author;
                existing.Delete();
            }

            ServerInterface? created = null;
            try
            {
                created = composition.Create(interfaceName);
                created.Import(new FileInfo(generatedPath));
                created.Author = string.IsNullOrWhiteSpace(author) ? "TIA MCP" : author!;
                created.Enabled = enabled;
            }
            catch
            {
                try
                {
                    created?.Delete();
                }
                catch
                {
                    // Continue into restoration of the previous interface.
                }

                RestorePreviousInterface(composition, interfaceName, backupPath, oldEnabled, oldAuthor);
                throw;
            }

            if (!string.IsNullOrWhiteSpace(exportPath))
            {
                EnsureOutputPath(exportPath!, ".xml");
                Directory.CreateDirectory(Path.GetDirectoryName(exportPath!)!);
                File.WriteAllText(exportPath!, generated.Xml, new UTF8Encoding(false));
            }

            if (!string.IsNullOrWhiteSpace(catalogPath))
            {
                EnsureOutputPath(catalogPath!, ".json");
                Directory.CreateDirectory(Path.GetDirectoryName(catalogPath!)!);
                File.WriteAllText(catalogPath!, JsonSerializer.Serialize(generated.Variables, JsonOptions), new UTF8Encoding(false));
            }

            return new
            {
                operation = existing is null ? "created" : "replaced",
                enabled,
                author = string.IsNullOrWhiteSpace(author) ? "TIA MCP" : author,
                exportPath = string.IsNullOrWhiteSpace(exportPath) ? null : Path.GetFullPath(exportPath!),
                catalogPath = string.IsNullOrWhiteSpace(catalogPath) ? null : Path.GetFullPath(catalogPath!),
                allowedSourcePathsPath = string.IsNullOrWhiteSpace(allowedSourcePathsPath)
                    ? null
                    : Path.GetFullPath(allowedSourcePathsPath!),
                generation = BuildGenerationInfo(interfaceName, interfaceUri, generated, includeVariables: false, maxVariables: 0)
            };
        }
        finally
        {
            try
            {
                Directory.Delete(workingDirectory, recursive: true);
            }
            catch
            {
                // Temporary files are not part of the project state.
            }
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
            interfaceName,
            interfaceUri,
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
}

using System.Text.Json;
using System.Xml.Linq;

namespace TiaMcpServer.OpennessWorker.Openness.OpcUa.SiemensGenerator;

internal static class OpcUaSourcePathFilter
{
    public static ISet<string>? Load(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (!Path.IsPathRooted(path))
        {
            throw new InvalidOperationException($"OPC UA source-path allowlist must be an absolute path: '{path}'.");
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("OPC UA source-path allowlist was not found.", path);
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement entries;
        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            entries = document.RootElement;
        }
        else if (document.RootElement.ValueKind == JsonValueKind.Object &&
                 document.RootElement.TryGetProperty("sourcePaths", out var sourcePaths))
        {
            entries = sourcePaths;
        }
        else
        {
            throw new InvalidOperationException(
                "OPC UA source-path allowlist must be a JSON string array or an object with a sourcePaths array.");
        }

        if (entries.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("OPC UA sourcePaths must be a JSON array.");
        }

        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in entries.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(entry.GetString()))
            {
                throw new InvalidOperationException("Every OPC UA source-path allowlist entry must be a non-empty string.");
            }
            result.Add(entry.GetString()!);
        }

        if (result.Count == 0)
        {
            throw new InvalidOperationException("OPC UA source-path allowlist must not be empty.");
        }

        return result;
    }

    public static OpcUaGenerationResult Apply(
        OpcUaGenerationResult generated,
        ISet<string> allowedSourcePaths)
    {
        var available = generated.Variables
            .Select(variable => variable.SourcePath)
            .ToHashSet(StringComparer.Ordinal);
        var missing = allowedSourcePaths
            .Where(path => !available.Contains(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                $"OPC UA source-path allowlist contains {missing.Length} path(s) not present in accessible global DB variables: " +
                string.Join(", ", missing.Take(20)) +
                (missing.Length > 20 ? " ..." : string.Empty));
        }

        var document = XDocument.Parse(generated.Xml, LoadOptions.PreserveWhitespace);
        var root = document.Root
            ?? throw new InvalidOperationException("The generated OPC UA NodeSet has no root element.");
        var ns = root.Name.Namespace;
        var si = root.GetNamespaceOfPrefix("si")
            ?? throw new InvalidOperationException("The generated OPC UA NodeSet has no Siemens namespace.");

        var removable = root.Elements(ns + "UAVariable")
            .Where(variable =>
            {
                var sourcePath = variable.Descendants(si + "VariableMapping").FirstOrDefault()?.Value;
                return !string.IsNullOrWhiteSpace(sourcePath) && !allowedSourcePaths.Contains(sourcePath!);
            })
            .ToArray();
        foreach (var variable in removable)
        {
            variable.Remove();
        }

        var xmlBody = document.ToString(SaveOptions.DisableFormatting);
        var xml = document.Declaration is null
            ? xmlBody
            : document.Declaration + Environment.NewLine + xmlBody;
        var variables = OpcUaNodeCatalog.Read(document);
        var warnings = generated.Warnings
            .Concat(new[]
            {
                $"Filtered OPC UA variables by source-path allowlist: kept {variables.Count}, removed {removable.Length}."
            })
            .ToArray();

        return new OpcUaGenerationResult(
            xml,
            variables,
            generated.DefaultNodeCount,
            generated.DataTypeNodeCount,
            generated.GlobalDbNodeCount,
            warnings);
    }
}

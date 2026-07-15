using System.Xml.Linq;

namespace TiaMcpServer.OpennessWorker.Openness.OpcUa.SiemensGenerator;

internal static class OpcUaNodeCatalog
{
    public static IReadOnlyList<OpcUaNodeCatalogEntry> Read(XDocument document)
    {
        var root = document.Root ?? throw new InvalidOperationException("The OPC UA NodeSet has no root element.");
        var ns = root.Name.Namespace;
        var si = root.GetNamespaceOfPrefix("si")
            ?? throw new InvalidOperationException("The OPC UA NodeSet has no Siemens namespace.");

        return root.Elements(ns + "UAVariable")
            .Select(variable => new OpcUaNodeCatalogEntry(
                variable.Attribute("NodeId")?.Value ?? string.Empty,
                variable.Attribute("BrowseName")?.Value ?? string.Empty,
                variable.Element(ns + "DisplayName")?.Value ?? string.Empty,
                variable.Attribute("DataType")?.Value ?? string.Empty,
                int.TryParse(variable.Attribute("AccessLevel")?.Value, out var accessLevel) ? accessLevel : 0,
                variable.Descendants(si + "VariableMapping").FirstOrDefault()?.Value ?? string.Empty))
            .Where(entry => !string.IsNullOrWhiteSpace(entry.SourcePath))
            .OrderBy(entry => entry.SourcePath, StringComparer.Ordinal)
            .ToArray();
    }
}

internal sealed class OpcUaNodeCatalogEntry
{
    public OpcUaNodeCatalogEntry(
        string nodeId,
        string browseName,
        string displayName,
        string dataType,
        int accessLevel,
        string sourcePath)
    {
        NodeId = nodeId;
        BrowseName = browseName;
        DisplayName = displayName;
        DataType = dataType;
        AccessLevel = accessLevel;
        SourcePath = sourcePath;
    }

    public string NodeId { get; }
    public string BrowseName { get; }
    public string DisplayName { get; }
    public string DataType { get; }
    public int AccessLevel { get; }
    public string SourcePath { get; }
    public bool Readable => (AccessLevel & 1) != 0;
    public bool Writable => (AccessLevel & 2) != 0;
}

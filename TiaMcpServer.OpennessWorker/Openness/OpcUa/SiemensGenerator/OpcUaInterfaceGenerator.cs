using System.Reflection;
using System.Xml.Linq;
using Siemens.Engineering.SW;

namespace TiaMcpServer.OpennessWorker.Openness.OpcUa.SiemensGenerator;

internal static class OpcUaInterfaceGenerator
{
    private const string TemplateResourceName =
        "TiaMcpServer.OpennessWorker.Openness.OpcUa.SiemensGenerator.InterfaceTemplate.xml";

    public static OpcUaGenerationResult Generate(
        PlcSoftware software,
        string interfaceName,
        string interfaceUri,
        bool keepFolderStructure)
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), "tia-mcp-opcua", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDirectory);

        try
        {
            using var context = new OpcUaGenerationContext(
                software,
                interfaceName,
                interfaceUri,
                workingDirectory,
                keepFolderStructure);

            OpcUaGenerationLog.Reset();

            using var template = Assembly.GetExecutingAssembly().GetManifestResourceStream(TemplateResourceName)
                ?? throw new InvalidOperationException($"Embedded OPC UA template '{TemplateResourceName}' was not found.");

            InterfaceTemplate.ImportTemplate(template);
            context.NumberDefaultNodes = InterfaceTemplate.GetTotalInterfaceElements();

            UserConstants.GetUserConstants(context.GetTagTableGroup());
            UserSystemDataTypes.GetUserSystemDataTypeElements(context.GetTypeGroup(), false);
            context.OpcUaInterface.Root!.Add(UserSystemDataTypes.XElementUserSystemDataTypes);
            context.NumberUserSystemDataTypes = UserSystemDataTypes.XElementUserSystemDataTypes.Count;

            BuildDataBlockElements.ResetDatablocksElements();
            DataBlocksGlobal.ResetDatablockElements();
            DataBlocksGlobal.GetDatablockElements(context.GetBlockGroup());
            context.NumberGlobalDBs = BuildDataBlockElements.XElementDataBlocks.Count;
            context.OpcUaInterface.Root.Add(BuildDataBlockElements.XElementDataBlocks);

            context.OpcUaInterface.Save(context.FilePath);
            var xml = File.ReadAllText(context.FilePath);
            var variables = OpcUaNodeCatalog.Read(context.OpcUaInterface);

            return new OpcUaGenerationResult(
                xml,
                variables,
                context.NumberDefaultNodes,
                context.NumberUserSystemDataTypes,
                context.NumberGlobalDBs,
                OpcUaGenerationLog.Messages.ToArray());
        }
        finally
        {
            try
            {
                Directory.Delete(workingDirectory, recursive: true);
            }
            catch
            {
                // Temporary artifacts do not affect the generated result.
            }
        }
    }
}

internal sealed class OpcUaGenerationResult
{
    public OpcUaGenerationResult(
        string xml,
        IReadOnlyList<OpcUaNodeCatalogEntry> variables,
        int defaultNodeCount,
        int dataTypeNodeCount,
        int globalDbNodeCount,
        IReadOnlyList<string> warnings)
    {
        Xml = xml;
        Variables = variables;
        DefaultNodeCount = defaultNodeCount;
        DataTypeNodeCount = dataTypeNodeCount;
        GlobalDbNodeCount = globalDbNodeCount;
        Warnings = warnings;
    }

    public string Xml { get; }
    public IReadOnlyList<OpcUaNodeCatalogEntry> Variables { get; }
    public int DefaultNodeCount { get; }
    public int DataTypeNodeCount { get; }
    public int GlobalDbNodeCount { get; }
    public IReadOnlyList<string> Warnings { get; }
}

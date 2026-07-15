using System.Xml.Linq;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Tags;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Types;
using Siemens.Engineering.SW.Units;

namespace TiaMcpServer.OpennessWorker.Openness.OpcUa.SiemensGenerator;

internal sealed class OpcUaGenerationContext : IDisposable
{
    [ThreadStatic]
    private static OpcUaGenerationContext? _current;

    private readonly PlcSoftware _software;

    public OpcUaGenerationContext(
        PlcSoftware software,
        string interfaceName,
        string interfaceUri,
        string workingDirectory,
        bool keepFolderStructure)
    {
        if (_current is not null)
        {
            throw new InvalidOperationException("An OPC UA generation context is already active on this thread.");
        }

        _software = software;
        InterfaceName = interfaceName;
        InterfaceURI = interfaceUri;
        WorkingDirectory = workingDirectory;
        FilePath = Path.Combine(workingDirectory, "interface.xml");
        KeepFolderStructure = keepFolderStructure;
        _current = this;
    }

    public static OpcUaGenerationContext Current =>
        _current ?? throw new InvalidOperationException("No OPC UA generation context is active.");

    public string InterfaceName { get; }
    public string InterfaceURI { get; }
    public string WorkingDirectory { get; }
    public string FilePath { get; }
    public XDocument OpcUaInterface { get; set; } = new();
    public XNamespace RootNameSpace { get; set; } = XNamespace.None;
    public XNamespace RootNameSpaceSi { get; set; } = XNamespace.None;

    public Dictionary<int, string> AccessLevelDictionary { get; } = new()
    {
        { 0, "Not Accessible" },
        { 1, "Read only" },
        { 2, "Write only" },
        { 3, "Read Write" },
        { 4, "Project's access levels" }
    };

    // Preserve each project's ExternalAccessible/ExternalWritable attributes.
    public int GlobalDBsAccessLevel { get; set; } = 4;
    public int InstanceDBsAccessLevel { get; set; } = 0;
    public int SafetyGlobalDBsAccessLevel { get; set; } = 4;
    public int SafetyInstanceDBsAccessLevel { get; set; } = 0;
    public bool KeepEmptyDBs { get; set; }
    public bool KeepFolderStructure { get; set; }

    public bool IsSoftwareUnit { get; set; }
    public string SoftwareUnitNamespace { get; set; } = string.Empty;

    public int NumberDefaultNodes { get; set; }
    public int NumberUserSystemDataTypes { get; set; }
    public int NumberGlobalDBs { get; set; }

    public PlcTagTableGroup GetTagTableGroup() => _software.TagTableGroup;
    public PlcBlockGroup GetBlockGroup() => _software.BlockGroup;
    public PlcTypeSystemGroup GetTypeGroup() => _software.TypeGroup;

    public PlcUnit GetSelectedSoftwareUnit()
    {
        throw new NotSupportedException("Software Units are not included by the MCP OPC UA generator.");
    }

    public void Dispose()
    {
        if (ReferenceEquals(_current, this))
        {
            _current = null;
        }
    }
}

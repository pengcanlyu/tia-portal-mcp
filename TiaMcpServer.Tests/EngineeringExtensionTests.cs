using System.Security.Cryptography;
using System.Text.Json;
using TiaMcpServer.Contracts.Extensions;
using TiaMcpServer.Contracts.Project;
using TiaMcpServer.Contracts.Safety;
using TiaMcpServer.Extensions;
using TiaMcpServer.Plc;
using Xunit;

namespace TiaMcpServer.Tests;

public class EngineeringExtensionTests
{
    [Theory]
    [InlineData("set_hardware_parameters")]
    [InlineData("plug_hardware_module")]
    [InlineData("generate_opcua_interface")]
    [InlineData("set_opcua_interface_enabled")]
    [InlineData("delete_opcua_interface")]
    [InlineData("export_opcua_interface")]
    [InlineData("export_plc_to_folder")]
    public void WritesAreDeniedReadOnlyAndOnLocalSessions(string operation)
    {
        Assert.False(OperationPolicyCatalog.IsAllowed(McpAccessMode.ReadOnly, operation));
        Assert.True(OperationPolicyCatalog.IsAllowed(McpAccessMode.ReadWrite, operation));
        Assert.True(ProjectCapabilityCatalog.Supports(ProjectContainerKinds.StandaloneProject, operation));
        Assert.False(ProjectCapabilityCatalog.Supports(ProjectContainerKinds.LocalSession, operation));
    }

    private static EngineeringQuery Hardware() => new() {
        Operation = "set_hardware_parameters", DeviceName = "PLC", ItemPath = ["Rack", "SM"],
        Changes = new() { ["Range"] = JsonSerializer.SerializeToElement(1) }, ExpectedStateHash = new string('a', 64)
    };

    [Fact]
    public void HardwareRequiresExactPathChangesAndFreshHash()
    {
        Assert.Null(EngineeringExtensionOperations.Validate(Hardware(), true));
        var q = Hardware();
        q.ExpectedStateHash = new string('z', 64);
        Assert.NotNull(EngineeringExtensionOperations.Validate(q, true));
        q = Hardware(); q.ItemPath = [];
        Assert.NotNull(EngineeringExtensionOperations.Validate(q, true));
        q = Hardware(); q.Changes = null!;
        Assert.NotNull(EngineeringExtensionOperations.Validate(q, true));
        q = Hardware(); q.ChannelIndex = -1;
        Assert.NotNull(EngineeringExtensionOperations.Validate(q, true));
        q = Hardware(); q.Changes = new() { ["Name"] = JsonSerializer.SerializeToElement("renamed") };
        Assert.NotNull(EngineeringExtensionOperations.Validate(q, true));
        Assert.NotNull(EngineeringExtensionOperations.Validate(null, true));
        Assert.NotNull(EngineeringExtensionOperations.Validate(Hardware(), false));
    }

    [Fact]
    public void AllowlistRequiresPinnedBytes()
    {
        var query = new EngineeringQuery { Operation = "inspect_opcua_variables", PlcName = "PLC", AllowedSourcePathsPath = "C:/allowlist.json" };
        Assert.NotNull(EngineeringExtensionOperations.Validate(query, false));
        query.AllowedSourcePathsSha256 = new string('a', 64);
        Assert.Null(EngineeringExtensionOperations.Validate(query, false));
        query.AllowedSourcePathsPath = null;
        Assert.NotNull(EngineeringExtensionOperations.Validate(query, false));
    }

    [Fact]
    public void PlacementRequiresCatalogIdentitySlotAndLayoutHash()
    {
        var q = new EngineeringQuery { Operation = "plug_hardware_module", DeviceName = "Station", ItemPath = [],
            ModuleTypeIdentifier = "OrderNumber:6ES7531-7NF00-0AB0/V1.2", ModuleName = "SM", PositionNumber = 2,
            ExpectedStateHash = new string('a', 64) };
        Assert.Null(EngineeringExtensionOperations.Validate(q, true));
        q.ExpectedStateHash = null;
        Assert.NotNull(EngineeringExtensionOperations.Validate(q, true));
        q.Operation = "check_hardware_module";
        Assert.Null(EngineeringExtensionOperations.Validate(q, false));
        q.PositionNumber = null;
        Assert.NotNull(EngineeringExtensionOperations.Validate(q, false));
        q.Operation = "inspect_hardware_layout";
        Assert.Null(EngineeringExtensionOperations.Validate(q, false));
        q.ItemPath = null!;
        Assert.NotNull(EngineeringExtensionOperations.Validate(q, false));
        q.ItemPath = ["Rack"]; q.ChannelIndex = 0;
        Assert.NotNull(EngineeringExtensionOperations.Validate(q, false));
    }

    [Theory]
    [InlineData("inspect_hardware_layout")]
    [InlineData("check_hardware_module")]
    [InlineData("inspect_hardware_capabilities")]
    public void DiscoveryIsReadOnlyAndStandalone(string operation)
    {
        Assert.True(OperationPolicyCatalog.IsAllowed(McpAccessMode.ReadOnly, operation));
        Assert.False(ProjectCapabilityCatalog.Supports(ProjectContainerKinds.LocalSession, operation));
        Assert.True(ProjectCapabilityCatalog.Supports(ProjectContainerKinds.StandaloneProject, operation));
    }

    [Fact]
    public void AddressChangesAreNarrowAndCannotMixSelectors()
    {
        var q = Hardware(); q.AddressIndex = 0;
        Assert.NotNull(EngineeringExtensionOperations.Validate(q, true));
        q.Changes = new() { ["StartAddress"] = JsonSerializer.SerializeToElement(100) };
        Assert.Null(EngineeringExtensionOperations.Validate(q, true));
        q.ChannelIndex = 0;
        Assert.NotNull(EngineeringExtensionOperations.Validate(q, true));
        q.ChannelIndex = null; q.AddressIndex = -1;
        Assert.NotNull(EngineeringExtensionOperations.Validate(q, true));
        q.AddressIndex = 0; q.Changes["StartAddress"] = JsonSerializer.SerializeToElement(-1);
        Assert.NotNull(EngineeringExtensionOperations.Validate(q, true));
        q.Changes["StartAddress"] = JsonSerializer.SerializeToElement(1.2);
        Assert.NotNull(EngineeringExtensionOperations.Validate(q, true));
        q.Changes["StartAddress"] = JsonSerializer.SerializeToElement("100");
        Assert.NotNull(EngineeringExtensionOperations.Validate(q, true));
    }

    [Fact]
    public void CapabilityProbeNamesAreExactAndCannotSupplyAWriteSnapshot()
    {
        var q = Hardware(); q.Operation = "inspect_hardware_capabilities"; q.AttributeNames = ["OperatingRange"];
        Assert.Null(EngineeringExtensionOperations.Validate(q, false));
        q.AttributeNames = ["OperatingRange", "OperatingRange"];
        Assert.NotNull(EngineeringExtensionOperations.Validate(q, false));
        q.AttributeNames = ["OperatingRange"]; q.Operation = "inspect_hardware_parameters";
        Assert.NotNull(EngineeringExtensionOperations.Validate(q, false));
        q.Operation = "set_hardware_parameters";
        Assert.NotNull(EngineeringExtensionOperations.Validate(q, true));
    }

    [Fact]
    public void HardwareCompileUsesCompilePolicyAndExactCpuPath()
    {
        var q = new EngineeringQuery { Operation = "compile_hardware", DeviceName = "Station", ItemPath = ["CPU"] };
        Assert.Null(EngineeringExtensionOperations.Validate(q, true));
        Assert.Equal(OperationPolicyCatalog.GetCapability("compile_check"), OperationPolicyCatalog.GetCapability(q.Operation));
        Assert.False(ProjectCapabilityCatalog.Supports(ProjectContainerKinds.LocalSession, q.Operation));
        Assert.NotNull(EngineeringExtensionOperations.Validate(q, false));
        q.ItemPath = [];
        Assert.NotNull(EngineeringExtensionOperations.Validate(q, true));
        q.ItemPath = ["CPU"]; q.AddressIndex = 0;
        Assert.NotNull(EngineeringExtensionOperations.Validate(q, true));
    }

    [Theory]
    [InlineData(100, 128, 116, 64, false)]
    [InlineData(100, 128, 115, 64, true)]
    [InlineData(100, 128, 92, 64, false)]
    [InlineData(100, 128, 93, 64, true)]
    [InlineData(100, 128, 104, 16, true)]
    [InlineData(100, 128, 90, 320, true)]
    [InlineData(0, 14, 2, 16, false)]
    [InlineData(0, 14, 1, 16, true)]
    [InlineData(int.MaxValue, 8, 0, 16, false)]
    public void OverlapUsesByteStartsAndBitLengthsWithoutOverflow(int start, int length, int otherStart, int otherLength, bool overlaps)
        => Assert.Equal(overlaps, HardwareEditingRules.Overlaps(start, length, otherStart, otherLength));

    [Fact]
    public void AddressLengthUnitsAreExplicit()
    {
        Assert.Equal(16, new HardwareAddressInfo { LengthBits = 128 }.LengthBytes);
        Assert.Equal(2, new HardwareAddressInfo { LengthBits = 14 }.LengthBytes);
    }

    [Fact]
    public void InvalidAddressLengthCannotBeTreatedAsNoOverlap()
        => Assert.Throws<ArgumentOutOfRangeException>(() => HardwareEditingRules.Overlaps(10, 0, 20, 1));

    private sealed record FixtureItem(string Name, string Container, params FixtureItem[] Children);

    [Fact]
    public void PhysicalContainerIsIndependentOfCompositionParent()
    {
        var cpu = new FixtureItem("CPU", "Rail", new FixtureItem("AI", "CPU"));
        var module = new FixtureItem("SM", "Rail");
        var roots = new[] { new FixtureItem("Rail", "Station"), cpu, module };
        var members = HardwareEditingRules.ContainerMembers(roots, i => i.Children, i => i.Name, i => i.Container == "Rail");
        Assert.Equal(new[] { "CPU", "SM" }, members.Select(m => m.Item.Name));
        Assert.Equal(new[] { "SM" }, members[1].Path);
        var channels = HardwareEditingRules.ContainerMembers(roots, i => i.Children, i => i.Name, i => i.Container == "CPU");
        Assert.Equal(new[] { "CPU", "AI" }, Assert.Single(channels).Path);
    }

    [Fact]
    public void PlacementDiscoveryRejectsUnboundedHierarchy()
    {
        var roots = Enumerable.Range(0, 4097).Select(i => new FixtureItem(i.ToString(), "Rail"));
        Assert.Throws<InvalidOperationException>(() => HardwareEditingRules.ContainerMembers(roots, i => i.Children, i => i.Name, _ => true));
        var cyclic = new FixtureItem("Cycle", "Rail");
        Assert.Throws<InvalidOperationException>(() => HardwareEditingRules.ContainerMembers(new[] { cyclic }, i => new[] { i }, i => i.Name, _ => true));
    }

    [Fact]
    public void LocalSourceVerifiesBytesAndRejectsMixedOrStaleInput()
    {
        var file = Path.GetTempFileName();
        try {
            var bytes = System.Text.Encoding.UTF8.GetBytes("FUNCTION Test : Void\nEND_FUNCTION");
            File.WriteAllBytes(file, bytes);
            PlcOperationRequest Make() => new() { Operation = "update_block_logic", ContentFilePath = file,
                ContentFileSha256 = Convert.ToHexString(SHA256.HashData(bytes)) };
            var op = Make();
            Assert.Null(LocalPlcContent.Resolve([op]));
            Assert.Equal(System.Text.Encoding.UTF8.GetString(bytes), op.Content);
            Assert.NotNull(LocalPlcContent.Resolve([op]));
            op = Make(); op.ContentFileSha256 = new string('0', 64);
            Assert.NotNull(LocalPlcContent.Resolve([op]));
            Assert.Null(op.Content);
            op = Make(); op.ContentFilePath = "relative.scl";
            Assert.NotNull(LocalPlcContent.Resolve([op]));
        } finally { File.Delete(file); }
    }
}

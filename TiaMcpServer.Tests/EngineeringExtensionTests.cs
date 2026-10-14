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

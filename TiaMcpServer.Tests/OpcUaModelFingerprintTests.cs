using TiaMcpServer.OpennessWorker.Openness.OpcUa;
using Xunit;

namespace TiaMcpServer.Tests;

public sealed class OpcUaModelFingerprintTests
{
    [Fact]
    public void GenerationTimestampDoesNotChangeModelIdentity()
    {
        Assert.Equal(OpcUaModelFingerprint.Compute("<UANodeSet LastModified='a'><UAVariable DataType='i=1'/></UANodeSet>"),
                     OpcUaModelFingerprint.Compute("<UANodeSet LastModified='b'><UAVariable DataType='i=1'/></UANodeSet>"));
    }

    [Theory]
    [InlineData("<UAVariable DataType='i=2'/>")]
    [InlineData("<UAVariable DataType='i=1' AccessLevel='3'/>")]
    [InlineData("<UAVariable DataType='i=1'><References/></UAVariable>")]
    public void MaterialChangesStillInvalidateIdentity(string node)
    {
        Assert.NotEqual(OpcUaModelFingerprint.Compute("<UANodeSet><UAVariable DataType='i=1'/></UANodeSet>"),
                        OpcUaModelFingerprint.Compute("<UANodeSet>" + node + "</UANodeSet>"));
    }
}

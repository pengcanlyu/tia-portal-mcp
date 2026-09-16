using TiaMcpServer.Batch;
using System.Text.Json;
using TiaMcpServer.Contracts;
using TiaMcpServer.Tests.Worker;
using TiaMcpServer.Worker;
using Xunit;

namespace TiaMcpServer.Tests.Batch;

public class BatchLocalFileContentResolverTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "tia-mcp-file-source-tests-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void ResolveExpandsAbsoluteFileBeforeSafetyHashing()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "Main.scl");
        File.WriteAllText(path, "FUNCTION_BLOCK Main\r\nEND_FUNCTION_BLOCK\r\n");
        var operations = new[]
        {
            new BatchOperationRequest
            {
                OperationId = "main",
                Operation = "update_block_logic",
                BlockPath = "PLC_1/Blocks/Main",
                YamlContent = "@file:" + path
            }
        };

        var result = BatchLocalFileContentResolver.Resolve(operations);

        Assert.True(result.IsValid, result.Error);
        Assert.Equal("FUNCTION_BLOCK Main\r\nEND_FUNCTION_BLOCK\r\n", operations[0].YamlContent);
    }

    [Fact]
    public void ResolveRejectsRelativePath()
    {
        var operations = new[]
        {
            new BatchOperationRequest
            {
                OperationId = "main",
                Operation = "update_block_logic",
                BlockPath = "PLC_1/Blocks/Main",
                YamlContent = "@file:Main.scl"
            }
        };

        var result = BatchLocalFileContentResolver.Resolve(operations);

        Assert.False(result.IsValid);
        Assert.Contains("not absolute", result.Error);
    }

    [Fact]
    public void ResolveLeavesOtherOperationsUntouched()
    {
        var operations = new[]
        {
            new BatchOperationRequest
            {
                OperationId = "type",
                Operation = "update_type_content",
                TypePath = "PLC_1/Types/T",
                SourceContent = "@file:C:\\Types\\T.udt"
            }
        };

        var result = BatchLocalFileContentResolver.Resolve(operations);

        Assert.True(result.IsValid, result.Error);
        Assert.Equal("@file:C:\\Types\\T.udt", operations[0].SourceContent);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreviewApplyBindsToExpandedContent(bool changeFile)
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "Main.xml");
        File.WriteAllText(path, "--- FILE: Main.xml\r\n<Document Id=\"preview\" />\r\n");
        using var audit = new TempAuditDirectory();
        var binding = new ProjectSessionBinding(null);
        using var client = new OpennessWorkerClient(
            binding,
            logger: null,
            workerExecutablePath: FakeWorkerLocator.Locate());
        await FakeWorkerBinding.BindVerifiedAsync(client, binding, "echo");
        var safety = audit.CreateSafety(projectSessionBinding: binding);
        BatchOperationRequest[] Operations() => new[]
        {
            new BatchOperationRequest
            {
                OperationId = "main",
                Operation = "update_block_logic",
                ProjectPath = "echo",
                BlockPath = "PLC_1/Blocks/Main",
                YamlContent = "@file:" + path
            }
        };

        var preview = await WriteBatchTools.PreviewWriteBatch(client, safety, Operations());
        using var previewDocument = JsonDocument.Parse(preview);
        var token = previewDocument.RootElement.GetProperty("safetyToken").GetString();
        if (changeFile)
        {
            File.WriteAllText(path, "--- FILE: Main.xml\r\n<Document Id=\"changed\" />\r\n");
        }

        var result = await WriteBatchTools.ApplyWriteBatch(
            client,
            safety,
            Operations(),
            confirm: true,
            safetyToken: token);
        using var resultDocument = JsonDocument.Parse(result);
        Assert.Equal(!changeFile, resultDocument.RootElement.GetProperty("success").GetBoolean());
        if (changeFile)
        {
            Assert.Contains("input does not match", result);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}

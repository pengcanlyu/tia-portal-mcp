using TiaMcpServer.Contracts;
using Xunit;

namespace TiaMcpServer.Tests.Tools;

[Collection("Mcp protocol serial")]
public class WriteToolMcpAnnotationProtocolTests
{
    private static readonly string[] ReadOnlyToolNames =
    {
        "browse_project_tree",
        "execute_read_batch",
        "get_project_status",
        "inspect_opcua_variables",
        "list_opcua_interfaces",
        "network_read",
    };

    private static readonly string[] ReadWriteToolNames =
    {
        "apply_write_batch",
        "archive_project",
        "browse_project_tree",
        "close_project",
        "compile_check",
        "create_project",
        "delete_opcua_interface",
        "execute_read_batch",
        "export_opcua_interface",
        "export_plc_to_folder",
        "generate_opcua_interface",
        "get_project_status",
        "inspect_opcua_variables",
        "list_opcua_interfaces",
        "network_read",
        "network_write",
        "open_project",
        "preview_delete_opcua_interface",
        "preview_export_plc_to_folder",
        "preview_generate_opcua_interface",
        "preview_set_opcua_interface_enabled",
        "preview_write_batch",
        "save_project",
        "save_project_as",
        "set_opcua_interface_enabled",
    };

    private static readonly (string Name, bool ReadOnly, bool Destructive, bool OpenWorld)[] ExpectedWriteToolAnnotations =
    {
        ("preview_write_batch", true, false, false),
        ("apply_write_batch", false, true, false),
        ("open_project", false, true, false),
        ("create_project", false, true, false),
        ("save_project", false, true, false),
        ("save_project_as", false, true, false),
        ("archive_project", false, true, false),
        ("close_project", false, true, false),
        ("preview_export_plc_to_folder", false, false, true),
        ("export_plc_to_folder", false, false, true),
    };

    [Fact]
    public async Task ToolsList_ReadWriteProductionSurface_ExposesExactNamesCountsAnnotations_AndRepresentativeSchemas()
    {
        await using var harness = await McpProtocolTestHarness.StartProductionSurfaceAsync(McpAccessMode.ReadWrite);
        var tools = (await harness.Client.ListToolsAsync()).OrderBy(tool => tool.Name).ToArray();
        var byName = tools.ToDictionary(tool => tool.Name, StringComparer.Ordinal);

        Assert.Equal(ReadWriteToolNames, tools.Select(tool => tool.Name));
        Assert.Equal(25, tools.Length);

        foreach (var expected in ExpectedWriteToolAnnotations)
        {
            var annotations = byName[expected.Name].ProtocolTool.Annotations;
            Assert.NotNull(annotations);
            Assert.Equal(expected.ReadOnly, annotations!.ReadOnlyHint);
            Assert.Equal(expected.Destructive, annotations.DestructiveHint);
            Assert.Equal(expected.OpenWorld, annotations.OpenWorldHint);
        }

        Assert.Contains("\"operations\"", byName["preview_write_batch"].ProtocolTool.InputSchema.GetRawText(), StringComparison.Ordinal);
        Assert.Contains("\"projectPath\"", byName["open_project"].ProtocolTool.InputSchema.GetRawText(), StringComparison.Ordinal);
        Assert.Contains("\"confirm\"", byName["open_project"].ProtocolTool.InputSchema.GetRawText(), StringComparison.Ordinal);
        Assert.Contains("\"safetyToken\"", byName["open_project"].ProtocolTool.InputSchema.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToolsList_ReadOnlyProductionSurface_ExposesExactReadOnlyTools_AndNoWriteTools()
    {
        await using var harness = await McpProtocolTestHarness.StartProductionSurfaceAsync(McpAccessMode.ReadOnly);
        var tools = (await harness.Client.ListToolsAsync()).OrderBy(tool => tool.Name).ToArray();
        var toolNames = tools.Select(tool => tool.Name).ToArray();

        Assert.Equal(ReadOnlyToolNames, toolNames);
        Assert.Equal(6, tools.Length);

        foreach (var writeToolName in ReadWriteToolNames.Except(ReadOnlyToolNames, StringComparer.Ordinal))
        {
            Assert.DoesNotContain(writeToolName, toolNames);
        }
    }
}

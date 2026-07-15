using System.ComponentModel;
using System.Reflection;
using ModelContextProtocol.Server;
using TiaMcpServer.Safety;
using TiaMcpServer.Tools;
using TiaMcpServer.Worker;
using Xunit;

namespace TiaMcpServer.Tests;

public class OpcUaToolTests
{
    [Theory]
    [InlineData("ListOpcUaInterfaces", "list_opcua_interfaces", false)]
    [InlineData("InspectOpcUaVariables", "inspect_opcua_variables", false)]
    [InlineData("ExportOpcUaInterface", "export_opcua_interface", false)]
    [InlineData("PreviewGenerateOpcUaInterface", "preview_generate_opcua_interface", false)]
    [InlineData("GenerateOpcUaInterface", "generate_opcua_interface", true)]
    [InlineData("PreviewSetOpcUaInterfaceEnabled", "preview_set_opcua_interface_enabled", false)]
    [InlineData("SetOpcUaInterfaceEnabled", "set_opcua_interface_enabled", true)]
    [InlineData("PreviewDeleteOpcUaInterface", "preview_delete_opcua_interface", false)]
    [InlineData("DeleteOpcUaInterface", "delete_opcua_interface", true)]
    public void OpcUaToolsHaveMcpMetadata(string methodName, string expectedToolName, bool requiresConfirm)
    {
        var type = typeof(OpcUaTools);
        Assert.NotNull(type.GetCustomAttribute<McpServerToolTypeAttribute>());

        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
        Assert.Equal(expectedToolName, method.GetCustomAttribute<McpServerToolAttribute>()?.Name);

        var description = method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;
        if (requiresConfirm)
        {
            Assert.Contains("confirm=true", description);
        }
    }

    [Theory]
    [InlineData("ListOpcUaInterfacesAsync")]
    [InlineData("InspectOpcUaVariablesAsync")]
    [InlineData("ExportOpcUaInterfaceAsync")]
    [InlineData("GenerateOpcUaInterfaceAsync")]
    [InlineData("SetOpcUaInterfaceEnabledAsync")]
    [InlineData("DeleteOpcUaInterfaceAsync")]
    public void WorkerClientExposesOpcUaMethods(string methodName)
    {
        var method = typeof(OpennessWorkerClient).GetMethod(methodName);
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<WorkerCallResult>), method.ReturnType);
    }

    [Theory]
    [InlineData("GenerateOpcUaInterface")]
    [InlineData("SetOpcUaInterfaceEnabled")]
    [InlineData("DeleteOpcUaInterface")]
    public void OpcUaWriteToolsUseCurrentSafetyContract(string methodName)
    {
        var method = typeof(OpcUaTools).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
        var parameters = method.GetParameters();
        Assert.Contains(parameters, parameter => parameter.ParameterType == typeof(WriteSafetyService));
        Assert.Contains(parameters, parameter => parameter.Name == "confirm" && parameter.ParameterType == typeof(bool));
        Assert.Contains(parameters, parameter => parameter.Name == "safetyToken" && parameter.ParameterType == typeof(string));
    }
}

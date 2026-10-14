using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using TiaMcpServer.Tools;

namespace TiaMcpServer.Extensions;

internal static class EngineeringToolRegistration
{
    internal static IMcpServerBuilder WithEngineeringWriteTools(this IMcpServerBuilder builder)
    {
        builder.Services.AddSingleton<McpServerTool>(services => new StrictArgumentsTool(
            McpServerTool.Create(typeof(EngineeringWriteTools).GetMethod(nameof(EngineeringWriteTools.Write))!,
                target: null, options: new McpServerToolCreateOptions {
                    Services = services,
                    SchemaCreateOptions = new AIJsonSchemaCreateOptions {
                        TransformOptions = new AIJsonSchemaTransformOptions { DisallowAdditionalProperties = true }
                    }
                }),
            "engineering_write", new HashSet<string>(StringComparer.Ordinal) { "operation", "dryRun" },
            new HashSet<string>(StringComparer.Ordinal) { "dryRun" }, new Dictionary<string, string>()));
        return builder;
    }
}

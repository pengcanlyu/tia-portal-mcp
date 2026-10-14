namespace TiaMcpServer.OpennessWorker.Openness.OpcUa.SiemensGenerator;

internal static class OpcUaGenerationLog
{
    [ThreadStatic]
    private static List<string>? _messages;

    [ThreadStatic]
    private static string? _progress;

    public static string Progress
    {
        get => _progress ?? string.Empty;
        set => _progress = value;
    }

    public static IReadOnlyList<string> Messages => _messages ??= new List<string>();

    public static void Reset()
    {
        _messages = new List<string>();
        Progress = string.Empty;
    }

    public static void Publish(string message)
    {
        (_messages ??= new List<string>()).Add(message);
    }

    public static void Error(string message)
    {
        throw new InvalidOperationException(message);
    }
}

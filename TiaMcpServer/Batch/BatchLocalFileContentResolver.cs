namespace TiaMcpServer.Batch;

internal static class BatchLocalFileContentResolver
{
    private const string LocalFilePrefix = "@file:";
    private const long MaxSourceFileBytes = 16 * 1024 * 1024;

    internal static LocalFileResolutionResult Resolve(BatchOperationRequest[]? operations)
    {
        if (operations is null)
        {
            return LocalFileResolutionResult.Valid();
        }

        foreach (var operation in operations)
        {
            if (operation is null ||
                !string.Equals(operation.Operation, "update_block_logic", StringComparison.Ordinal) ||
                operation.YamlContent is null ||
                !operation.YamlContent.StartsWith(LocalFilePrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var sourcePath = operation.YamlContent[LocalFilePrefix.Length..].Trim();
            if (!Path.IsPathRooted(sourcePath))
            {
                return LocalFileResolutionResult.Invalid(
                    $"Operation '{operation.OperationId}' uses @file:, but the source path is not absolute.");
            }

            try
            {
                var normalizedPath = Path.GetFullPath(sourcePath);
                var sourceFile = new FileInfo(normalizedPath);
                if (!sourceFile.Exists)
                {
                    return LocalFileResolutionResult.Invalid(
                        $"Operation '{operation.OperationId}' @file source does not exist: {normalizedPath}");
                }

                if (sourceFile.Length > MaxSourceFileBytes)
                {
                    return LocalFileResolutionResult.Invalid(
                        $"Operation '{operation.OperationId}' @file source is larger than {MaxSourceFileBytes} bytes: {normalizedPath}");
                }

                // Expansion happens before preview/apply token validation, so the token binds to
                // the actual bytes. A file change between calls invalidates the old token.
                operation.YamlContent = File.ReadAllText(normalizedPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
            {
                return LocalFileResolutionResult.Invalid(
                    $"Operation '{operation.OperationId}' could not read its @file source: {ex.Message}");
            }
        }

        return LocalFileResolutionResult.Valid();
    }
}

internal readonly record struct LocalFileResolutionResult(bool IsValid, string? Error)
{
    internal static LocalFileResolutionResult Valid() => new(true, null);

    internal static LocalFileResolutionResult Invalid(string error) => new(false, error);
}

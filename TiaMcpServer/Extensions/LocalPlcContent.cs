using System.Security.Cryptography;
using System.Text;
using TiaMcpServer.Plc;

namespace TiaMcpServer.Extensions;

internal static class LocalPlcContent
{
    internal const int MaximumBytes = 8 * 1024 * 1024;

    // Resolve once under validation. The same immutable bytes feed plan and apply.
    internal static string? Resolve(IReadOnlyList<PlcOperationRequest> operations)
    {
        var resolved = new List<(PlcOperationRequest Operation, string Content)>();
        foreach (var op in operations)
        {
            if (op.ContentFilePath == null && op.ContentFileSha256 == null) continue;
            if (op.Operation is not ("update_block_logic" or "update_type_content"))
                return "Local content files are only supported by block/type content updates.";
            if (op.Content != null) return "Use content or contentFilePath, never both.";
            if (string.IsNullOrWhiteSpace(op.ContentFilePath) || !Path.IsPathFullyQualified(op.ContentFilePath)
                || op.ContentFilePath.StartsWith(@"\\", StringComparison.Ordinal)
                || op.ContentFilePath[2..].Contains(':'))
                return "contentFilePath must be an absolute local file (no UNC/device path or alternate stream).";
            if (op.ContentFileSha256?.Length != 64 || !op.ContentFileSha256.All(Uri.IsHexDigit))
                return "A 64-hex contentFileSha256 is required.";
            try
            {
                using var stream = new FileStream(op.ContentFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (stream.Length == 0 || stream.Length > MaximumBytes) return "Content file must be 1 byte to 8 MiB.";
                var bytes = new byte[checked((int)stream.Length)];
                stream.ReadExactly(bytes);
                if (!Convert.ToHexString(SHA256.HashData(bytes)).Equals(op.ContentFileSha256, StringComparison.OrdinalIgnoreCase))
                    return "Content file SHA256 mismatch; no operation was dispatched.";
                var content = new UTF8Encoding(false, true).GetString(bytes).TrimStart('\uFEFF');
                if (string.IsNullOrWhiteSpace(content)) return "Content file cannot be blank.";
                resolved.Add((op, content));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or DecoderFallbackException or ArgumentException)
            { return "Local content file rejected: " + e.Message; }
        }
        foreach (var item in resolved) item.Operation.Content = item.Content;
        return null;
    }
}

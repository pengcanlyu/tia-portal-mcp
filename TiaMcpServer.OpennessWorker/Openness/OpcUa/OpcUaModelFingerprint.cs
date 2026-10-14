using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace TiaMcpServer.OpennessWorker.Openness.OpcUa;

internal static class OpcUaModelFingerprint
{
    public static string Compute(string xml)
    {
        var document = XDocument.Parse(xml);
        // This root timestamp is regenerated on every preview, not read from the project.
        document.Root?.SetAttributeValue("LastModified", null);
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(document.ToString(SaveOptions.DisableFormatting)));
        return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
    }
}

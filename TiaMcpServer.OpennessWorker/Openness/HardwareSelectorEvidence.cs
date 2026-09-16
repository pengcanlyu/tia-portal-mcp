using Siemens.Engineering;
using TiaMcpServer.Contracts;

namespace TiaMcpServer.OpennessWorker.Openness;

internal static class HardwareSelectorEvidence
{
    internal static NetworkObjectDiscoveryEvidenceValue<string> ReadOptionalTypeIdentifier(
        Func<string?> read,
        string field)
    {
        try
        {
            return NetworkObjectDiscoveryEvidence.ReadOptionalStringIdentity(read(), field);
        }
        catch (EngineeringException)
        {
            return NetworkObjectDiscoveryEvidence.UnreadableString(field);
        }
    }

    internal static string MissingPathTypeIdentifierDiagnostic(
        IEnumerable<DeviceItemPathSegmentInfo> itemPath)
        => itemPath.Any(segment => string.IsNullOrEmpty(segment.TypeIdentifier))
            ? "A device item path type identifier was unavailable; non-device selectors are not available."
            : string.Empty;
}

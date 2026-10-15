using System.Security.Cryptography;
using System.Text;
using PrivacyAuditService.Domain.Model.Aggregates;

namespace PrivacyAuditService.Domain.Services;

/// <summary>
/// Tamper-evident hash chain for the append-only access log (US21 esc. 3, US23).
/// Each entry seals the previous hash; any alteration breaks verification.
/// </summary>
public static class AccessAuditHashChain
{
    public const string GenesisPrevHash = "GENESIS";

    public static string Compute(AccessAuditEntry entry)
    {
        var fields = string.Join(",", entry.Fields.OrderBy(f => f, StringComparer.OrdinalIgnoreCase));
        var payload =
            $"{entry.PrevHash}|{entry.Id}|{entry.UserId}|{entry.ResourceType}|{entry.ResourceId}" +
            $"|{fields}|{entry.Purpose}|{entry.Decision}|{entry.AccessedAt:O}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    public static bool Verify(IEnumerable<AccessAuditEntry> entriesOldestFirst)
    {
        var prevHash = GenesisPrevHash;
        foreach (var entry in entriesOldestFirst)
        {
            if (!string.Equals(entry.PrevHash, prevHash, StringComparison.Ordinal))
                return false;
            if (!string.Equals(entry.Hash, Compute(entry), StringComparison.Ordinal))
                return false;
            prevHash = entry.Hash;
        }
        return true;
    }
}

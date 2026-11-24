namespace PrivacyAuditService.Domain.Services;

/// <summary>
/// Versioned catalog of data-processing purposes (US20, scenarios 1-2).
/// A new entry supersedes the previous version; operators decide per version.
/// </summary>
public static class ProcessingPolicyCatalog
{
    public const string CurrentVersion = "v1.0";

    public static readonly IReadOnlyList<(string Code, string Description, bool IsMandatory)> Purposes =
    [
        ("fatigue-monitoring", "Continuous fatigue monitoring during the shift", true),
        ("safety-alerts", "Preventive in-cabin warnings and supervisor escalation", true),
        ("operational-analytics", "Aggregated trend analysis without identifying the operator", false)
    ];

    public static bool IsKnownVersion(string version) =>
        string.Equals(version, CurrentVersion, StringComparison.OrdinalIgnoreCase);

    public static bool IsKnownPurpose(string purpose) =>
        Purposes.Any(p => string.Equals(p.Code, purpose, StringComparison.OrdinalIgnoreCase));

    public static IEnumerable<string> MandatoryPurposes() =>
        Purposes.Where(p => p.IsMandatory).Select(p => p.Code);
}

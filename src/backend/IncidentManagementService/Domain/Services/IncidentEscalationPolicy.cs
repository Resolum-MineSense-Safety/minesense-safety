namespace IncidentManagementService.Domain.Services;

/// <summary>
/// Escalation chain for critical incidents nobody takes (US11): each time the
/// attention deadline passes without a responsible supervisor, the case moves
/// to the next authorized role.
/// </summary>
public static class IncidentEscalationPolicy
{
    public static readonly TimeSpan AttentionDeadline = TimeSpan.FromMinutes(2);

    private static readonly string[] Chain = ["ShiftManager", "SafetyManager"];

    public static int MaxLevel => Chain.Length;

    /// <summary>Roles notified once the incident reaches the given level (1-based).</summary>
    public static IReadOnlyList<string> NotifiedRolesAt(int level) =>
        Chain.Take(Math.Clamp(level, 0, Chain.Length)).ToList();
}

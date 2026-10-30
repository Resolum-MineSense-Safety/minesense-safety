namespace ReportingService.Domain.Services;

/// <summary>
/// Orders definition versions such as <c>v1.0</c>, <c>v1.10</c> and <c>2</c> numerically, so the
/// latest version of an indicator can be picked. Non-numeric versions sort before numeric ones
/// and among themselves in ordinal order.
/// </summary>
public sealed class ReportDefinitionVersionComparer : IComparer<string>
{
    public static readonly ReportDefinitionVersionComparer Instance = new();

    public int Compare(string? x, string? y)
    {
        var left = Parse(x);
        var right = Parse(y);
        if (left is not null && right is not null) return left.CompareTo(right);
        if (left is not null) return 1;
        if (right is not null) return -1;
        return StringComparer.OrdinalIgnoreCase.Compare(x, y);
    }

    private static Version? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var text = value.Trim().TrimStart('v', 'V');
        if (!text.Contains('.')) text += ".0";
        return Version.TryParse(text, out var version) ? version : null;
    }
}

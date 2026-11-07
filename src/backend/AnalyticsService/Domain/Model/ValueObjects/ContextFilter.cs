namespace AnalyticsService.Domain.Model.ValueObjects;

/// <summary>
/// Optional restriction to part of the operation (a mine, a fleet...). A null
/// value means "any"; comparisons ignore case.
/// </summary>
public record ContextFilter(string? Shift = null, string? MineSite = null, string? Location = null, string? Fleet = null)
{
    public static readonly ContextFilter All = new();

    public bool Matches(OperationalContext context) =>
        Same(Shift, context.Shift) && Same(MineSite, context.MineSite)
        && Same(Location, context.Location) && Same(Fleet, context.Fleet);

    private static bool Same(string? expected, string actual) =>
        string.IsNullOrWhiteSpace(expected) || string.Equals(expected.Trim(), actual, StringComparison.OrdinalIgnoreCase);
}

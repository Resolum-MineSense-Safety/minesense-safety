using MineSenseSafety.Shared.Domain.Model;

namespace OperationsService.Domain.Model.ValueObjects;

/// <summary>
/// Collects every missing mandatory field so the administrator is told all of them at once (US17).
/// </summary>
public static class MandatoryData
{
    public static void EnsurePresent(params (string Field, bool IsMissing)[] fields)
    {
        var missing = fields.Where(field => field.IsMissing).Select(field => field.Field).ToList();
        if (missing.Count > 0)
            throw new DomainException($"Missing mandatory data: {string.Join(", ", missing)}.");
    }
}

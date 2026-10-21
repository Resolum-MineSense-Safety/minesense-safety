using MineSenseSafety.Shared.Domain.Model;

namespace OperationsService.Interfaces.REST.Transform;

/// <summary>Parses enum names sent as text, reporting missing or unknown values as invalid requests (HTTP 400).</summary>
public static class EnumFromResourceAssembler
{
    public static TEnum Parse<TEnum>(string? value, string field) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainValidationException($"Missing mandatory data: {field}.");

        if (!Enum.TryParse<TEnum>(value.Trim(), ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed)
            || int.TryParse(value, out _))
            throw new DomainValidationException(
                $"Unknown {field} '{value}'. Allowed values: {string.Join(", ", Enum.GetNames<TEnum>())}.");

        return parsed;
    }
}

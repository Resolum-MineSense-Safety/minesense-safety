using MineSenseSafety.Shared.Domain.Model;

namespace AnalyticsService.Domain.Model.ValueObjects;

/// <summary>Where and when a fatigue alert happened: the analytical dimensions of US24.</summary>
public record OperationalContext
{
    public string Shift { get; }
    public string MineSite { get; }
    public string Location { get; }
    public string Fleet { get; }

    public OperationalContext(string shift, string mineSite, string location, string fleet)
    {
        Shift = Required(shift, nameof(Shift));
        MineSite = Required(mineSite, nameof(MineSite));
        Location = Required(location, nameof(Location));
        Fleet = Required(fleet, nameof(Fleet));
    }

    public string ValueOf(AnalysisDimension dimension) => dimension switch
    {
        AnalysisDimension.Shift => Shift,
        AnalysisDimension.MineSite => MineSite,
        AnalysisDimension.Location => Location,
        AnalysisDimension.Fleet => Fleet,
        _ => throw new DomainException($"Unknown analysis dimension '{dimension}'.")
    };

    private static string Required(string value, string name) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new DomainException($"The operational context requires {name}.")
            : value.Trim();
}

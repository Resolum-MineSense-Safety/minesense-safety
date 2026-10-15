using AnalyticsService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace AnalyticsService.Tests.Domain;

public class ValueObjectsTests
{
    [Fact]
    public void OperationalContext_TrimsValuesAndExposesEachDimension()
    {
        // Act
        var context = new OperationalContext(" Night ", "Antamina", "Pit 2", "CAT 797F");

        // Assert
        Assert.Equal("Night", context.ValueOf(AnalysisDimension.Shift));
        Assert.Equal("Antamina", context.ValueOf(AnalysisDimension.MineSite));
        Assert.Equal("Pit 2", context.ValueOf(AnalysisDimension.Location));
        Assert.Equal("CAT 797F", context.ValueOf(AnalysisDimension.Fleet));
    }

    [Fact]
    public void OperationalContext_WithMissingDimension_Throws()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => new OperationalContext("Night", " ", "Pit 2", "CAT 797F"));
    }

    [Fact]
    public void OperationalContext_WithUnknownDimension_Throws()
    {
        // Arrange
        var context = new OperationalContext("Night", "Antamina", "Pit 2", "CAT 797F");

        // Act & Assert
        Assert.Throws<DomainException>(() => context.ValueOf((AnalysisDimension)99));
    }

    [Fact]
    public void ContextFilter_MatchesIgnoringCaseAndTreatsNullAsAny()
    {
        // Arrange
        var context = new OperationalContext("Night", "Antamina", "Pit 2", "CAT 797F");

        // Act & Assert
        Assert.True(ContextFilter.All.Matches(context));
        Assert.True(new ContextFilter(Shift: "night", Fleet: "cat 797f").Matches(context));
        Assert.False(new ContextFilter(MineSite: "Cerro Verde").Matches(context));
    }

    [Fact]
    public void AnalysisPeriod_IsHalfOpenAndRejectsInvertedRanges()
    {
        // Arrange
        var from = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var period = new AnalysisPeriod(from, from.AddDays(1));

        // Act & Assert
        Assert.True(period.Contains(from));
        Assert.False(period.Contains(from.AddDays(1)));
        Assert.Equal(TimeSpan.FromDays(1), period.Length);
        Assert.Throws<DomainException>(() => new AnalysisPeriod(from, from));
    }
}

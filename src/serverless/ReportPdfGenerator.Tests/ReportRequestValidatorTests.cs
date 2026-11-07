using ReportPdfGenerator.Model;

namespace ReportPdfGenerator.Tests;

public class ReportRequestValidatorTests
{
    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        // Act & Assert
        Assert.Empty(ReportRequestValidator.Validate(ReportRequests.Valid()));
    }

    [Fact]
    public void Validate_Null_IsRejected()
    {
        // Act & Assert
        Assert.Single(ReportRequestValidator.Validate(null));
    }

    [Fact]
    public void Validate_MissingFieldsAndInvertedPeriod_ReportsEachProblem()
    {
        // Arrange
        var valid = ReportRequests.Valid();
        var request = valid with { ReportId = Guid.Empty, Title = " ", PeriodTo = valid.PeriodFrom };

        // Act
        var errors = ReportRequestValidator.Validate(request);

        // Assert
        Assert.Equal(3, errors.Count);
    }

    [Fact]
    public void Validate_RowsNotMatchingColumns_IsRejected()
    {
        // Arrange
        var request = ReportRequests.Valid() with
        {
            Sections = [new ReportSection("Tabla", null, ["A", "B"], [["solo uno"]])]
        };

        // Act & Assert
        Assert.Contains(ReportRequestValidator.Validate(request), error => error.Contains("Tabla"));
    }
}

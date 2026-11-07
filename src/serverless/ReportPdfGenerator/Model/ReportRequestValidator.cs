namespace ReportPdfGenerator.Model;

/// <summary>Rejects requests that would produce an empty or misleading report.</summary>
public static class ReportRequestValidator
{
    public static IReadOnlyList<string> Validate(ReportRequest? request)
    {
        if (request is null) return ["The message body is not a report request."];

        var errors = new List<string>();
        if (request.ReportId == Guid.Empty) errors.Add("ReportId is required.");
        if (string.IsNullOrWhiteSpace(request.Title)) errors.Add("Title is required.");
        if (request.PeriodTo <= request.PeriodFrom) errors.Add("The period must end after it starts.");
        if (request.Summary is null) errors.Add("Summary is required.");
        if (request.Sections is null) errors.Add("Sections are required (they may be empty).");

        foreach (var section in request.Sections ?? [])
        {
            if (section.Rows is null) continue;
            var width = section.Columns?.Count ?? 0;
            if (width == 0 || section.Rows.Any(row => row.Count != width))
                errors.Add($"Every row of section '{section.Heading}' must match its columns.");
        }

        return errors;
    }
}

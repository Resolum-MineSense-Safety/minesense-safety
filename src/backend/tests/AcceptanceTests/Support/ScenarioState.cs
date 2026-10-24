namespace MineSenseSafety.AcceptanceTests.Support;

/// <summary>Data shared by the steps of one scenario (Reqnroll creates a new instance per scenario).</summary>
public class ScenarioState
{
    public object? Signals { get; set; }

    public HttpResponseMessage? Response { get; set; }

    public AssessmentResult? Assessment { get; set; }

    public AlertResult? Alert { get; set; }
}

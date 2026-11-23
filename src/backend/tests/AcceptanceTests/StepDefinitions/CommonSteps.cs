using MineSenseSafety.AcceptanceTests.Support;
using Reqnroll;

namespace MineSenseSafety.AcceptanceTests.StepDefinitions;

/// <summary>Steps shared by every feature.</summary>
[Binding]
public class CommonSteps(ApiContext api)
{
    [Then("el sistema responde con el código {int}")]
    public void ThenTheSystemAnswersWith(int statusCode) => api.AssertStatus((System.Net.HttpStatusCode)statusCode);

    [Then("el mensaje de error menciona {string}")]
    public void ThenTheErrorMentions(string text) =>
        Assert.Contains(text, api.Body, StringComparison.OrdinalIgnoreCase);
}

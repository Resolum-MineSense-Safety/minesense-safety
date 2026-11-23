using Reqnroll;
using Reqnroll.UnitTestProvider;

namespace MineSenseSafety.AcceptanceTests.Support;

/// <summary>
/// Against a deployed environment, skips the scenarios tagged @solo-en-memoria: they move a fake
/// clock or start a second host with different settings, which only the in-memory host allows.
/// The CI job also filters them out (<c>Category!=solo-en-memoria</c>); this hook covers local runs.
/// </summary>
[Binding]
public sealed class EnvironmentHooks(ScenarioContext scenarioContext, IUnitTestRuntimeProvider runtimeProvider)
{
    [BeforeScenario(Order = 0)]
    public void SkipInMemoryOnlyScenariosWhenRemote()
    {
        if (!AcceptanceSettings.IsRemote) return;
        if (scenarioContext.ScenarioInfo.CombinedTags.Contains(AcceptanceSettings.InMemoryOnlyTag))
            runtimeProvider.TestIgnore($"@{AcceptanceSettings.InMemoryOnlyTag}: runs only against the in-memory APIs.");
    }
}

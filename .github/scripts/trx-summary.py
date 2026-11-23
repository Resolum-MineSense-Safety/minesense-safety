#!/usr/bin/env python3
"""Turns a .trx test report into a Markdown table of scenarios for the GitHub job summary.

Usage: trx-summary.py <report.trx> [title]
"""
import sys
import xml.etree.ElementTree as ET

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
ICON = {"Passed": "✅", "Failed": "❌", "NotExecuted": "⏸️"}

root = ET.parse(sys.argv[1]).getroot()
# xUnit reports the scenario title as testName; the feature is the class of the test method.
feature_of = {
    test.get("id"): (test.find("t:TestMethod", NS).get("className", "").split(".")[-1])
    for test in root.iterfind(".//t:UnitTest", NS)
    if test.find("t:TestMethod", NS) is not None
}
rows = []
for result in root.iterfind(".//t:UnitTestResult", NS):
    name = result.get("testName", "")
    outcome = result.get("outcome", "")
    # Scenario Outline examples append their values in parentheses (which may contain dots).
    base, paren, arguments = name.partition("(")
    feature = feature_of.get(result.get("testId"), "")
    scenario = base.strip()
    if not feature and "." in base:
        # Older reports name tests Namespace.FeatureClass.ScenarioMethod.
        feature, scenario = base.split(".")[-2:]
    if paren:
        scenario = f"{scenario} ({arguments.replace(', exampleTags: [])', ')')}"
    rows.append((feature.removesuffix("Feature"), scenario.replace("|", "\\|"), outcome))

counters = root.find(".//t:ResultSummary/t:Counters", NS)
title = sys.argv[2] if len(sys.argv) > 2 else "Acceptance scenarios"
print(f"## {title}\n")
if counters is not None:
    print(f"Total {counters.get('total')} · passed {counters.get('passed')} · "
          f"failed {counters.get('failed')} · not executed {counters.get('notExecuted')}\n")
print("| Feature | Scenario | Result |")
print("| --- | --- | --- |")
for feature, scenario, outcome in sorted(rows):
    print(f"| {feature} | {scenario} | {ICON.get(outcome, '')} {outcome} |")

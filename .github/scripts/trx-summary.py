#!/usr/bin/env python3
"""Turns a .trx test report into a Markdown table of scenarios for the GitHub job summary."""
import sys
import xml.etree.ElementTree as ET

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
ICON = {"Passed": "✅", "Failed": "❌", "NotExecuted": "⏸️"}

root = ET.parse(sys.argv[1]).getroot()
rows = []
for result in root.iterfind(".//t:UnitTestResult", NS):
    name = result.get("testName", "")
    outcome = result.get("outcome", "")
    # Reqnroll names tests Namespace.FeatureClass.ScenarioMethod; keep the last two parts.
    parts = name.split(".")
    feature, scenario = (parts[-2], parts[-1]) if len(parts) >= 2 else ("", name)
    rows.append((feature, scenario, outcome))

counters = root.find(".//t:ResultSummary/t:Counters", NS)
print("## Acceptance scenarios\n")
if counters is not None:
    print(f"Total {counters.get('total')} · passed {counters.get('passed')} · "
          f"failed {counters.get('failed')} · not executed {counters.get('notExecuted')}\n")
print("| Feature | Scenario | Result |")
print("| --- | --- | --- |")
for feature, scenario, outcome in sorted(rows):
    print(f"| {feature} | {scenario} | {ICON.get(outcome, '')} {outcome} |")

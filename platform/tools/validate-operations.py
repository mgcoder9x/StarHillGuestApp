#!/usr/bin/env python3
"""Fail-closed validation for the neutral Prometheus/Grafana operational bundle."""
from __future__ import annotations

import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
ALERTS = ROOT / "operations" / "prometheus-alerts.yml"
DASHBOARD = ROOT / "operations" / "grafana-dashboard.json"

def main() -> int:
    errors: list[str] = []
    if not ALERTS.exists():
        errors.append("operations/prometheus-alerts.yml is missing")
    else:
        text = ALERTS.read_text(encoding="utf-8")
        for alert in ("BedrockApiErrorBudgetBurning", "BedrockOutboxOldestPendingWarning", "BedrockOutboxOldestPendingCritical", "BedrockOutboxDeadLettered", "BedrockOutboxBacklogGrowing", "BedrockOutboxPublishStalled"):
            if f"alert: {alert}" not in text:
                errors.append(f"missing alert {alert}")
        if len(re.findall(r"^\s+for:\s+\S+", text, flags=re.MULTILINE)) < 6:
            errors.append("every alert must have a non-zero duration")
        for metric in ("bedrock_outbox_pending", "bedrock_outbox_oldest_pending_age_seconds", "bedrock_outbox_dead_letter_depth", "bedrock_outbox_published_total"):
            if metric not in text:
                errors.append(f"alerts missing metric {metric}")

    if not DASHBOARD.exists():
        errors.append("operations/grafana-dashboard.json is missing")
    else:
        try:
            dashboard = json.loads(DASHBOARD.read_text(encoding="utf-8"))
        except json.JSONDecodeError as exc:
            errors.append(f"dashboard is not valid JSON: {exc}")
        else:
            if not isinstance(dashboard.get("panels"), list) or len(dashboard["panels"]) < 3:
                errors.append("dashboard must contain at least three panels")
            if dashboard.get("uid") != "bedrock-platform-operations":
                errors.append("dashboard uid must remain neutral and stable")
            serialized = json.dumps(dashboard)
            for metric in ("bedrock_outbox_pending", "bedrock_outbox_dead_letter_depth", "bedrock_outbox_published_total"):
                if metric not in serialized:
                    errors.append(f"dashboard missing metric {metric}")

    if errors:
        print("VALIDATE OPERATIONS: FAIL")
        for error in errors:
            print(f"  - {error}")
        return 1
    print("VALIDATE OPERATIONS: OK (alerts + neutral dashboard)")
    return 0

if __name__ == "__main__":
    sys.exit(main())

#!/usr/bin/env python3
"""Validate release metadata and the supported-package boundary before a tag build."""
from __future__ import annotations

import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
SUPPORTED = {
    "Bedrock.Domain",
    "Bedrock.Messaging.Contracts",
    "Bedrock.Application",
    "Bedrock.Api",
    "Bedrock.Infrastructure",
    "Adapters.Messaging.RabbitMq",
}

def main() -> int:
    errors: list[str] = []
    props = (ROOT / "Directory.Build.props").read_text(encoding="utf-8")
    release = (ROOT / "RELEASE.md").read_text(encoding="utf-8") if (ROOT / "RELEASE.md").exists() else ""
    if "BedrockSupportedPackage" not in props or "PackageReadmeFile" not in props:
        errors.append("Directory.Build.props is missing supported package metadata")
    if "Semantic Versioning" not in release or "PublicApiCompatibilityTests" not in release:
        errors.append("RELEASE.md is missing SemVer/API gate policy")

    project_names = {path.stem for path in ROOT.rglob("*.csproj")}
    for package in SUPPORTED:
        if package not in project_names:
            errors.append(f"supported package project is missing: {package}")
    if "Bedrock.ReferenceHost" in SUPPORTED:
        errors.append("reference host must not be a supported package")

    version = pathlib.Path(ROOT / "global.json").read_text(encoding="utf-8")
    if not re.search(r'"version"\s*:\s*"\d+\.\d+\.\d+"', version):
        errors.append("global SDK metadata is malformed")

    if errors:
        print("VALIDATE RELEASE: FAIL")
        for error in errors:
            print(f"  - {error}")
        return 1
    print("VALIDATE RELEASE: OK (supported package boundary + release policy)")
    return 0

if __name__ == "__main__":
    sys.exit(main())

#!/usr/bin/env python3
"""Generate a disposable module with the real dotnet template and fail on missing/build-broken output."""
from __future__ import annotations

import pathlib
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as element_tree


ROOT = pathlib.Path(__file__).resolve().parents[1]
TEMPLATE = ROOT / "templates" / "bedrock-module"


def run(*args: str, cwd: pathlib.Path = ROOT) -> None:
    command = ["dotnet", *args]
    completed = subprocess.run(command, cwd=cwd, text=True)
    if completed.returncode:
        raise SystemExit(f"command failed ({completed.returncode}): {' '.join(command)}")


def main() -> int:
    if shutil.which("dotnet") is None:
        print("MODULE TEMPLATE: FAIL (dotnet is required)")
        return 2

    with tempfile.TemporaryDirectory(prefix="bedrock-template-hive-") as hive:
        output = ROOT / "src" / "Modules" / "ScaffoldProbe"
        test_output = ROOT / "tests" / "Modules" / "ScaffoldProbe.UnitTests"
        if output.exists() or test_output.exists():
            print("MODULE TEMPLATE: FAIL (ScaffoldProbe path already exists)")
            return 1
        try:
            run("new", "install", str(TEMPLATE), "--force", "--debug:custom-hive", hive)
            run(
                "new",
                "bedrock-module",
                "--name",
                "ScaffoldProbe",
                "--moduleKey",
                "scaffold-probe",
                "--output",
                str(ROOT),
                "--force",
                "--debug:custom-hive",
                hive,
            )
            projects = sorted(output.rglob("*.csproj")) + sorted(test_output.rglob("*.csproj"))
            if len(projects) != 6:
                print(f"MODULE TEMPLATE: FAIL (expected 6 projects, got {len(projects)})")
                return 1
            contracts = output / "ScaffoldProbe.Contracts" / "ScaffoldProbeModule.cs"
            if "scaffold-probe" not in contracts.read_text(encoding="utf-8"):
                print("MODULE TEMPLATE: FAIL (module key replacement missing)")
                return 1
            test_project = test_output / "ScaffoldProbe.UnitTests.csproj"
            run("restore", str(test_project))
            run("build", str(test_project), "-c", "Release", "--no-restore")
            run("test", str(test_project), "-c", "Release", "--no-build", "--nologo")

            with tempfile.TemporaryDirectory(prefix="bedrock-scaffold-root-") as sandbox:
                sandbox_root = pathlib.Path(sandbox)
                shutil.copytree(TEMPLATE, sandbox_root / "templates" / "bedrock-module")
                (sandbox_root / "Platform.slnx").write_text("<Solution />\n", encoding="utf-8")
                for relative in (
                    "src/Bedrock.Domain/Bedrock.Domain.csproj",
                    "src/Bedrock.Messaging.Contracts/Bedrock.Messaging.Contracts.csproj",
                    "src/Bedrock.Application/Bedrock.Application.csproj",
                    "src/Bedrock.Api/Bedrock.Api.csproj",
                    "src/Bedrock.Infrastructure/Bedrock.Infrastructure.csproj",
                ):
                    placeholder = sandbox_root / relative
                    placeholder.parent.mkdir(parents=True, exist_ok=True)
                    placeholder.write_text(
                        '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework>'
                        "</PropertyGroup></Project>\n",
                        encoding="utf-8",
                    )
                completed = subprocess.run(
                    [
                        sys.executable,
                        str(ROOT / "tools" / "new_module.py"),
                        "--root",
                        str(sandbox_root),
                        "--name",
                        "RegisteredProbe",
                        "--module-key",
                        "registered-probe",
                        "--skip-build",
                    ],
                    cwd=ROOT,
                    text=True,
                )
                if completed.returncode:
                    print("MODULE TEMPLATE: FAIL (cross-platform scaffolder failed)")
                    return 1
                solution_projects = element_tree.parse(sandbox_root / "Platform.slnx").findall(".//Project")
                if len(solution_projects) != 6:
                    print(f"MODULE TEMPLATE: FAIL (solution registered {len(solution_projects)} projects, expected 6)")
                    return 1
            print("MODULE TEMPLATE: OK")
            return 0
        finally:
            for path in (output, test_output):
                if path.exists():
                    shutil.rmtree(path)


if __name__ == "__main__":
    sys.exit(main())

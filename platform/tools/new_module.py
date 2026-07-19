#!/usr/bin/env python3
"""Cross-platform Bedrock module scaffolder used by the PowerShell convenience wrapper."""
from __future__ import annotations

import argparse
import pathlib
import re
import shutil
import subprocess
import sys
import tempfile


DEFAULT_ROOT = pathlib.Path(__file__).resolve().parents[1]
NAME_PATTERN = re.compile(r"^[A-Z][A-Za-z0-9]*$")
KEY_PATTERN = re.compile(r"^[a-z][a-z0-9.-]*$")


def run(*args: str, cwd: pathlib.Path) -> None:
    completed = subprocess.run(["dotnet", *args], cwd=cwd, text=True)
    if completed.returncode:
        raise RuntimeError(f"command failed ({completed.returncode}): dotnet {' '.join(args)}")


def scaffold(root: pathlib.Path, name: str, module_key: str, skip_build: bool) -> list[pathlib.Path]:
    root = root.resolve()
    solution = root / "Platform.slnx"
    template = root / "templates" / "bedrock-module"
    module_root = root / "src" / "Modules" / name
    test_root = root / "tests" / "Modules" / f"{name}.UnitTests"

    if shutil.which("dotnet") is None:
        raise RuntimeError("dotnet is required to scaffold a module")
    if not solution.exists():
        raise RuntimeError(f"platform solution is missing: {solution}")
    if not template.exists():
        raise RuntimeError(f"module template is missing: {template}")
    if module_root.exists() or test_root.exists():
        raise RuntimeError(f"module or test directory already exists for '{name}'")

    with tempfile.TemporaryDirectory(prefix="bedrock-template-hive-") as hive:
        run("new", "install", str(template), "--force", "--debug:custom-hive", hive, cwd=root)
        run(
            "new",
            "bedrock-module",
            "--name",
            name,
            "--moduleKey",
            module_key,
            "--output",
            str(root),
            "--force",
            "--debug:custom-hive",
            hive,
            cwd=root,
        )

    projects = sorted(module_root.rglob("*.csproj")) + sorted(test_root.rglob("*.csproj"))
    if len(projects) != 6:
        raise RuntimeError(f"expected five module projects plus one unit-test project, found {len(projects)}")

    # Add the graph in one invocation so dotnet does not recursively add referenced projects and then warn that later
    # explicit additions are duplicates.
    run("sln", str(solution), "add", *(str(project) for project in projects), "--include-references", "false", cwd=root)

    if not skip_build:
        run("restore", str(solution), cwd=root)
        run("build", str(solution), "-c", "Release", "--no-restore", cwd=root)

    return projects


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Generate and register a five-layer Bedrock module.")
    parser.add_argument("--name", required=True)
    parser.add_argument("--module-key")
    parser.add_argument("--root", type=pathlib.Path, default=DEFAULT_ROOT)
    parser.add_argument("--skip-build", action="store_true")
    args = parser.parse_args(argv)
    if not NAME_PATTERN.fullmatch(args.name):
        parser.error("--name must match ^[A-Z][A-Za-z0-9]*$")
    args.module_key = args.module_key or args.name.lower()
    if not KEY_PATTERN.fullmatch(args.module_key):
        parser.error("--module-key must match ^[a-z][a-z0-9.-]*$")
    return args


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv or sys.argv[1:])
    try:
        projects = scaffold(args.root, args.name, args.module_key, args.skip_build)
    except RuntimeError as error:
        print(f"MODULE SCAFFOLD: FAIL ({error})")
        return 1
    print(f"MODULE SCAFFOLD: OK ({args.name}, key={args.module_key}, projects={len(projects)})")
    return 0


if __name__ == "__main__":
    sys.exit(main())

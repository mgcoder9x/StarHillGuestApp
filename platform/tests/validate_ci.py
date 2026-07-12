#!/usr/bin/env python3
"""Fixed CI-workflow validator của BASE (command-governance AD-062: KHÔNG dùng `python -c` ad-hoc).

Kiểm bất biến của .github/workflows/ci.yml (AD-061). Sống trong platform/ (base tự-chứa) nhưng validate file CI
ở REPO-ROOT (GitHub bắt buộc workflow nằm ở <root>/.github/workflows/). Đây là NHÀ CỐ ĐỊNH cho logic validate CI:
cần thêm bất biến → sửa file NÀY. Chạy qua: platform\\scripts\\vp.cmd ci  (hoặc python platform\\tests\\validate_ci.py)

Exit code: 0 = OK, 1 = có vi phạm, 2 = thiếu tiền đề (PyYAML / file).
"""
import sys
import pathlib

try:
    import yaml
except ImportError:
    print("VALIDATE CI: FAIL (thiếu PyYAML — 'pip install pyyaml')")
    sys.exit(2)

# __file__ = <root>/platform/tests/validate_ci.py → parents[2] = <root> (chứa .github/).
REPO_ROOT = pathlib.Path(__file__).resolve().parents[2]
CI_PATH = REPO_ROOT / ".github" / "workflows" / "ci.yml"

# Bất biến kỳ vọng (AD-061). Cần siết thêm → bổ sung tại đây.
REQUIRED_PUSH_BRANCHES = ("main", "master", "develop")
REQUIRED_JOBS = ("build-test", "docker-image", "migration-bundle")


def validate(doc: dict) -> list[str]:
    errors: list[str] = []

    # YAML 1.1 quirk: khóa `on:` trần bị PyYAML đọc thành boolean True (GitHub parser thì đọc là chuỗi 'on').
    on = doc.get("on", doc.get(True))
    if not isinstance(on, dict):
        return ["không đọc được block 'on:' (trigger)"]

    # 1. Cổng anti-drift PHẢI chạy trên nhánh tích hợp thật (AD-061 — gồm develop).
    push = on.get("push") or {}
    branches = push.get("branches") or []
    for b in REQUIRED_PUSH_BRANCHES:
        if b not in branches:
            errors.append(f"push.branches thiếu '{b}' (hiện: {branches})")
    if "pull_request" not in on:
        errors.append("thiếu trigger 'pull_request'")

    # 2. concurrency cancel-in-progress (chống contention Testcontainers N-053/N-064 + tiết kiệm runner).
    conc = doc.get("concurrency") or {}
    if conc.get("cancel-in-progress") is not True:
        errors.append("concurrency.cancel-in-progress != true")
    if not conc.get("group"):
        errors.append("concurrency.group trống")

    # 3. least-privilege (tinh thần F35/AD-038).
    perms = doc.get("permissions") or {}
    if perms.get("contents") != "read":
        errors.append(f"permissions.contents != 'read' (hiện: {perms.get('contents')})")

    # 4. đủ job + mỗi job có timeout-minutes (chặn treo Docker).
    jobs = doc.get("jobs") or {}
    for j in REQUIRED_JOBS:
        if j not in jobs:
            errors.append(f"thiếu job '{j}'")
        elif "timeout-minutes" not in jobs[j]:
            errors.append(f"job '{j}' thiếu timeout-minutes")

    return errors


def main() -> int:
    if not CI_PATH.exists():
        print(f"VALIDATE CI: FAIL (không thấy {CI_PATH})")
        return 2
    doc = yaml.safe_load(CI_PATH.read_text(encoding="utf-8"))
    errors = validate(doc)
    if errors:
        print("VALIDATE CI: FAIL")
        for e in errors:
            print(f"  - {e}")
        return 1
    print("VALIDATE CI: OK (push⊇main/master/develop + pull_request; concurrency cancel-in-progress; "
          "permissions.contents=read; jobs build-test/docker-image/migration-bundle + timeouts)")
    return 0


if __name__ == "__main__":
    sys.exit(main())

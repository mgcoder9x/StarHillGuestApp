#!/usr/bin/env python3
"""Fixed CI-workflow validator của SẢN PHẨM StarHill (command-governance AD-062: KHÔNG dùng `python -c` ad-hoc).

Kiểm bất biến của .github/workflows/starhill-ci.yml (CI sản phẩm, TÁCH khỏi CI base ci.yml). Sống trong
starhill/ (sản phẩm tự-chứa) nhưng validate file CI ở REPO-ROOT (GitHub bắt buộc workflow nằm ở
<root>/.github/workflows/). Đây là NHÀ CỐ ĐỊNH cho logic validate CI sản phẩm: cần thêm bất biến → sửa file NÀY.
Chạy qua: starhill\\scripts\\vp.cmd ci  (hoặc python starhill\\tests\\validate_ci.py)

Ngoài bất biến chung (branches/concurrency/permissions/jobs+timeouts), CÒN kiểm CI này thực sự NHẮM starhill/
(không trỏ nhầm về base platform/) — chống drift QR-N-003.

Exit code: 0 = OK, 1 = có vi phạm, 2 = thiếu tiền đề (PyYAML / file).
"""
import sys
import pathlib

try:
    import yaml
except ImportError:
    print("VALIDATE CI: FAIL (thiếu PyYAML — 'pip install pyyaml')")
    sys.exit(2)

# __file__ = <root>/starhill/tests/validate_ci.py → parents[2] = <root> (chứa .github/).
REPO_ROOT = pathlib.Path(__file__).resolve().parents[2]
CI_PATH = REPO_ROOT / ".github" / "workflows" / "starhill-ci.yml"

# Bất biến kỳ vọng. Cần siết thêm → bổ sung tại đây.
REQUIRED_PUSH_BRANCHES = ("main", "master", "develop")
REQUIRED_JOBS = ("build-test", "docker-image", "migration-bundle")
# CI sản phẩm PHẢI nhắm starhill/ (chống drift trỏ nhầm về base platform/). Kiểm trên RAW text vì
# working-directory là per-step (YAML parse xong khó tổng hợp gọn) — token literal đủ mạnh & rõ.
REQUIRED_TARGET_TOKENS = ("working-directory: starhill", "global-json-file: starhill/global.json")


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
    raw = CI_PATH.read_text(encoding="utf-8")
    doc = yaml.safe_load(raw)
    errors = validate(doc)

    # Bất biến "nhắm starhill/" (chống trỏ nhầm base) — kiểm trên raw text.
    for token in REQUIRED_TARGET_TOKENS:
        if token not in raw:
            errors.append(f"CI sản phẩm không nhắm starhill/: thiếu '{token}'")

    if errors:
        print("VALIDATE CI: FAIL")
        for e in errors:
            print(f"  - {e}")
        return 1
    print("VALIDATE CI: OK (starhill-ci.yml; push⊇main/master/develop + pull_request; concurrency "
          "cancel-in-progress; permissions.contents=read; jobs build-test/docker-image/migration-bundle "
          "+ timeouts; nhắm starhill/)")
    return 0


if __name__ == "__main__":
    sys.exit(main())

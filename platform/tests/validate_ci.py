#!/usr/bin/env python3
"""Fixed CI-workflow validator của BASE (command-governance AD-062: KHÔNG dùng `python -c` ad-hoc).

Kiểm bất biến của .github/workflows/ci.yml (AD-061). Sống trong platform/ (base tự-chứa) nhưng validate file CI
ở REPO-ROOT (GitHub bắt buộc workflow nằm ở <root>/.github/workflows/). Đây là NHÀ CỐ ĐỊNH cho logic validate CI:
cần thêm bất biến → sửa file NÀY. Chạy qua: platform\\scripts\\vp.cmd ci  (hoặc python platform\\tests\\validate_ci.py)

Exit code: 0 = OK, 1 = có vi phạm, 2 = thiếu tiền đề (file).
"""
import sys
import pathlib
import re
import subprocess

try:
    import yaml
except ImportError:
    yaml = None

# __file__ = <root>/platform/tests/validate_ci.py → parents[2] = <root> (chứa .github/).
REPO_ROOT = pathlib.Path(__file__).resolve().parents[2]
CI_PATH = REPO_ROOT / ".github" / "workflows" / "ci.yml"

# Bất biến kỳ vọng (AD-061). Cần siết thêm → bổ sung tại đây.
REQUIRED_PUSH_BRANCHES = ("main", "master", "develop")
REQUIRED_JOBS = ("build-test", "docker-image", "migration-bundle")

# A-30/AD-090: KHÔNG được track build artifact (bin/obj) trong Git — .gitignore đã chặn, guard này chống ai đó
# `git add -f` lại (anti-drift permanent, review A-30 bước 4). Pattern git-pathspec.
ARTIFACT_PATHSPECS = ("**/bin/**", "**/obj/**")


def _clean_lines(raw: str) -> list[tuple[int, str]]:
    """Read the small workflow contract without requiring a third-party YAML package."""
    lines: list[tuple[int, str]] = []
    for source in raw.splitlines():
        if not source.strip() or source.lstrip().startswith("#"):
            continue
        # Workflow values used by this validator do not contain an unquoted ` #`.
        source = re.split(r"\s+#", source.rstrip(), maxsplit=1)[0]
        if not source.strip():
            continue
        indent = len(source) - len(source.lstrip(" "))
        lines.append((indent, source.strip()))
    return lines


def _children(lines: list[tuple[int, str]], index: int) -> list[tuple[int, str]]:
    parent_indent = lines[index][0]
    result: list[tuple[int, str]] = []
    for indent, text in lines[index + 1 :]:
        if indent <= parent_indent:
            break
        result.append((indent, text))
    return result


def _find_key(lines: list[tuple[int, str]], key: str, indent: int | None = None) -> int | None:
    prefix = f"{key}:"
    for index, (line_indent, text) in enumerate(lines):
        if (indent is None or line_indent == indent) and (text == prefix or text.startswith(prefix + " ")):
            return index
    return None


def _parse_inline_list(value: str) -> list[str]:
    if not value.startswith("[") or not value.endswith("]"):
        return []
    return [item.strip().strip("'\"") for item in value[1:-1].split(",") if item.strip()]


def validate_without_yaml(raw: str) -> list[str]:
    """Validate only the workflow facts this gate owns, using Python's stdlib."""
    lines = _clean_lines(raw)
    errors: list[str] = []

    on_index = _find_key(lines, "on", 0)
    if on_index is None:
        return ["không đọc được block 'on:' (trigger)"]
    on_lines = _children(lines, on_index)
    push_index = next((i for i, (indent, text) in enumerate(on_lines) if indent == 2 and text.startswith("push:")), None)
    if push_index is None:
        branches: list[str] = []
    else:
        push_value = on_lines[push_index][1].partition(":")[2].strip()
        branches = _parse_inline_list(push_value)
        if not branches:
            push_lines = on_lines[push_index + 1 :]
            branches_index = next((i for i, (indent, text) in enumerate(push_lines) if text.startswith("branches:")), None)
            if branches_index is not None:
                branch_value = push_lines[branches_index][1].partition(":")[2].strip()
                branches = _parse_inline_list(branch_value)
                if not branches:
                    branches = [text[1:].strip().strip("'\"") for indent, text in push_lines[branches_index + 1 :] if text.startswith("-")]

    for branch in REQUIRED_PUSH_BRANCHES:
        if branch not in branches:
            errors.append(f"push.branches thiếu '{branch}' (hiện: {branches})")
    if not any(indent == 2 and text.startswith("pull_request:") for indent, text in on_lines):
        errors.append("thiếu trigger 'pull_request'")

    concurrency_index = _find_key(lines, "concurrency", 0)
    concurrency_lines = _children(lines, concurrency_index) if concurrency_index is not None else []
    cancel = next((text.partition(":")[2].strip().lower() for indent, text in concurrency_lines if text.startswith("cancel-in-progress:")), "")
    group = next((text.partition(":")[2].strip() for indent, text in concurrency_lines if text.startswith("group:")), "")
    if cancel != "true":
        errors.append(f"concurrency.cancel-in-progress != true (hiện: {cancel or None})")
    if not group:
        errors.append("concurrency.group trống")

    permissions_index = _find_key(lines, "permissions", 0)
    permissions_lines = _children(lines, permissions_index) if permissions_index is not None else []
    contents = next((text.partition(":")[2].strip().strip("'\"") for indent, text in permissions_lines if text.startswith("contents:")), None)
    if contents != "read":
        errors.append(f"permissions.contents != 'read' (hiện: {contents})")

    jobs_index = _find_key(lines, "jobs", 0)
    jobs_lines = _children(lines, jobs_index) if jobs_index is not None else []
    jobs = {text.partition(":")[0] for indent, text in jobs_lines if indent == 2 and text.endswith(":")}
    for job in REQUIRED_JOBS:
        if job not in jobs:
            errors.append(f"thiếu job '{job}'")
            continue
        job_index = next(i for i, (indent, text) in enumerate(jobs_lines) if indent == 2 and text == f"{job}:")
        job_block = jobs_lines[job_index + 1 :]
        next_job = next((i for i, (indent, text) in enumerate(job_block) if indent == 2 and text.endswith(":")), len(job_block))
        if not any(text.startswith("timeout-minutes:") for indent, text in job_block[:next_job]):
            errors.append(f"job '{job}' thiếu timeout-minutes")

    return errors


def validate_no_tracked_artifacts() -> list[str]:
    """Fail nếu Git đang track file bin/obj (build artifact) — giữ repo sạch (A-30)."""
    try:
        result = subprocess.run(
            ["git", "ls-files", "--", *ARTIFACT_PATHSPECS],
            cwd=REPO_ROOT, capture_output=True, text=True, check=False,
        )
    except FileNotFoundError:
        # Không có git trong môi trường (hiếm) → không chặn cứng; CI/dev luôn có git nên guard vẫn hiệu lực ở đó.
        print("VALIDATE CI: (bỏ qua kiểm artifact — không thấy 'git' trong PATH)")
        return []

    if result.returncode != 0:
        return [f"git ls-files lỗi khi kiểm artifact: {result.stderr.strip()}"]

    tracked = [line for line in result.stdout.splitlines() if line.strip()]
    if tracked:
        sample = ", ".join(tracked[:3])
        return [f"Git đang track {len(tracked)} file build-artifact (bin/obj) — phải `git rm --cached` "
                f"(A-30). Ví dụ: {sample} ..."]
    return []


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
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
        sys.stderr.reconfigure(encoding="utf-8", errors="replace")
    if not CI_PATH.exists():
        print(f"VALIDATE CI: FAIL (không thấy {CI_PATH})")
        return 2
    raw = CI_PATH.read_text(encoding="utf-8")
    if yaml is None:
        errors = validate_without_yaml(raw)
    else:
        doc = yaml.safe_load(raw)
        errors = validate(doc)
    errors += validate_no_tracked_artifacts()
    if errors:
        print("VALIDATE CI: FAIL")
        for e in errors:
            print(f"  - {e}")
        return 1
    print("VALIDATE CI: OK (push⊇main/master/develop + pull_request; concurrency cancel-in-progress; "
          "permissions.contents=read; jobs build-test/docker-image/migration-bundle + timeouts; "
          "0 tracked bin/obj artifact)")
    return 0


if __name__ == "__main__":
    sys.exit(main())

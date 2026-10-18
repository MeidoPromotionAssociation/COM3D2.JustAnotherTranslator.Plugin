#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Function: 生成 release 正文 / Render the release body
# License: BSD-3
"""生成 release 正文。

规则 / Rules:
1. 如果 `.github/release-notes/<tag>.md` 存在，直接用它作为正文（手写正文优先，
   适合有「不兼容更改」这类需要人工措辞的版本）。
2. 否则用 `template.md` 渲染，其中 {{CHANGELOG}} 由上一个 tag 到当前 tag 的提交生成。

版本号提交（`Bump version to ...`）不会出现在更新日志里，因为提交标题里没有实际变更信息。
Version bump commits are filtered out of the changelog.
"""

import argparse
import os
import re
import subprocess
import sys

# 生成更新日志时跳过的提交标题 / Commit subjects skipped when building the changelog
SKIP_SUBJECT_PATTERNS = [
    re.compile(r"^bump version", re.IGNORECASE),
    re.compile(r"^reformat", re.IGNORECASE),
]


def git(*args):
    result = subprocess.run(["git", *args], capture_output=True, text=True, check=False)
    if result.returncode != 0:
        sys.exit(f"::error::git {' '.join(args)} failed: {result.stderr.strip()}")
    return result.stdout


def previous_tag(tag):
    """上一个 tag（按提交历史，而不是版本号排序）/ The previous tag along the history."""
    result = subprocess.run(
        ["git", "describe", "--tags", "--abbrev=0", f"{tag}^"],
        capture_output=True, text=True, check=False)
    if result.returncode != 0:
        return None
    return result.stdout.strip() or None


def changelog(tag, repo, prev):
    rev_range = f"{prev}..{tag}" if prev else tag
    raw = git("log", "--no-merges", "--reverse", "--pretty=format:%H%x1f%s", rev_range)
    lines = []
    for line in raw.splitlines():
        if not line.strip():
            continue
        sha, _, subject = line.partition("\x1f")
        subject = subject.strip()
        if any(p.search(subject) for p in SKIP_SUBJECT_PATTERNS):
            continue
        lines.append(f"- [{subject}](https://github.com/{repo}/commit/{sha})")
    if not lines:
        lines.append("- No user-visible changes in this release./本次发布没有用户可见的更改。")
    return "\n".join(lines)


def main():
    parser = argparse.ArgumentParser(description="Render the release body")
    parser.add_argument("--tag", required=True)
    parser.add_argument("--repo", required=True, help="owner/name")
    parser.add_argument("--template", default=os.path.join(".github", "release-notes", "template.md"))
    parser.add_argument("--override-dir", default=os.path.join(".github", "release-notes"))
    parser.add_argument("--output", required=True)
    args = parser.parse_args()

    override = os.path.join(args.override_dir, f"{args.tag}.md")
    if os.path.isfile(override):
        with open(override, encoding="utf-8") as f:
            body = f.read()
        print(f"using hand-written notes: {override}")
    else:
        with open(args.template, encoding="utf-8") as f:
            body = f.read()

        prev = previous_tag(args.tag)
        version = args.tag[1:] if args.tag.lower().startswith("v") else args.tag
        full_changelog = (
            f"https://github.com/{args.repo}/compare/{prev}...{args.tag}"
            if prev else f"https://github.com/{args.repo}/commits/{args.tag}"
        )
        replacements = {
            "{{REPO}}": args.repo,
            "{{TAG}}": args.tag,
            "{{VERSION}}": version,
            "{{CHANGELOG}}": changelog(args.tag, args.repo, prev),
            "{{FULL_CHANGELOG_URL}}": full_changelog,
        }
        for key, value in replacements.items():
            body = body.replace(key, value)

        leftover = re.findall(r"\{\{[A-Z_]+\}\}", body)
        if leftover:
            sys.exit(f"::error::unresolved placeholders: {sorted(set(leftover))}")
        print(f"rendered from template, previous tag: {prev or '(none)'}")

    with open(args.output, "w", encoding="utf-8", newline="\n") as f:
        f.write(body.rstrip() + "\n")
    print(f"wrote {args.output} ({len(body)} chars)")


if __name__ == "__main__":
    main()

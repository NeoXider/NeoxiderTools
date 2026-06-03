#!/usr/bin/env python3
"""Repair UTF-8 Russian text that was decoded as cp1251/latin1.

The script scans .md and .cs files, detects common mojibake tokens such as
"РџСЂ..." or "ÐŸÑ€...", and rewrites only lines where the conversion clearly
reduces mojibake markers. Output is UTF-8.
"""

from __future__ import annotations

import argparse
import os
from pathlib import Path
from typing import Iterable


DEFAULT_EXTENSIONS = {".md", ".cs"}
DEFAULT_EXCLUDE_DIRS = {
    ".git",
    ".vs",
    ".vscode",
    "Library",
    "Temp",
    "Logs",
    "obj",
    "bin",
    "Build",
    "Builds",
    "UserSettings",
    "TestResults",
}

RUSSIAN_ALPHABET = (
    "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ"
    "абвгдеёжзийклмнопрстуфхцчшщъыьэюя"
)


def build_tokens(encoding: str) -> set[str]:
    tokens: set[str] = set()
    for char in RUSSIAN_ALPHABET:
        try:
            tokens.add(char.encode("utf-8").decode(encoding))
        except UnicodeDecodeError:
            pass
    return {token for token in tokens if token != char}


CP1251_TOKENS = build_tokens("cp1251")
LATIN1_TOKENS = build_tokens("latin1")
ALL_TOKENS = CP1251_TOKENS | LATIN1_TOKENS


def mojibake_score(text: str) -> int:
    return sum(text.count(token) for token in ALL_TOKENS)


def cyrillic_count(text: str) -> int:
    return sum(1 for char in text if "\u0400" <= char <= "\u04ff")


def decode_as(text: str, encoding: str) -> str | None:
    try:
        return text.encode(encoding).decode("utf-8")
    except (UnicodeEncodeError, UnicodeDecodeError):
        return None


def repair_line_once(line: str) -> tuple[str, bool]:
    original_score = mojibake_score(line)
    if original_score == 0:
        return line, False

    original_cyrillic = cyrillic_count(line)
    best = line
    best_score = original_score
    best_cyrillic = original_cyrillic

    for encoding in ("cp1251", "latin1"):
        candidate = decode_as(line, encoding)
        if candidate is None:
            continue

        candidate_score = mojibake_score(candidate)
        candidate_cyrillic = cyrillic_count(candidate)
        improves = candidate_score < best_score
        keeps_language = candidate_cyrillic >= max(1, best_cyrillic // 2)
        if improves and keeps_language:
            best = candidate
            best_score = candidate_score
            best_cyrillic = candidate_cyrillic

    return best, best != line


def repair_text(text: str) -> tuple[str, int]:
    total_changes = 0
    lines = text.splitlines(keepends=True)
    repaired_lines: list[str] = []

    for line in lines:
        current = line
        changed_any = False
        for _ in range(3):
            repaired, changed = repair_line_once(current)
            if not changed:
                break
            current = repaired
            changed_any = True

        if changed_any:
            total_changes += 1
        repaired_lines.append(current)

    return "".join(repaired_lines), total_changes


def decode_file(raw: bytes) -> tuple[str, str]:
    try:
        return raw.decode("utf-8-sig"), "utf-8"
    except UnicodeDecodeError:
        return raw.decode("cp1251"), "cp1251"


def iter_files(root: Path, extensions: set[str], exclude_dirs: set[str]) -> Iterable[Path]:
    for current_root, dirs, files in os.walk(root):
        dirs[:] = [name for name in dirs if name not in exclude_dirs]
        current = Path(current_root)
        for name in files:
            path = current / name
            if path.suffix.lower() in extensions:
                yield path


def process_file(path: Path, dry_run: bool) -> tuple[bool, int, str]:
    raw = path.read_bytes()
    try:
        text, source_encoding = decode_file(raw)
    except UnicodeDecodeError as exc:
        return False, 0, f"skip undecodable: {exc}"

    repaired, line_changes = repair_text(text)
    encoding_change = source_encoding != "utf-8"
    changed = repaired != text or encoding_change

    if changed and not dry_run:
        path.write_text(repaired, encoding="utf-8", newline="")

    reason = []
    if line_changes:
        reason.append(f"{line_changes} line(s)")
    if encoding_change:
        reason.append(f"encoding {source_encoding}->utf-8")
    return changed, line_changes, ", ".join(reason)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("root", nargs="?", default=".", help="Repository root or folder to scan.")
    parser.add_argument("--apply", action="store_true", help="Write fixes. Without this flag, dry-run only.")
    parser.add_argument(
        "--extensions",
        nargs="*",
        default=sorted(DEFAULT_EXTENSIONS),
        help="File extensions to scan.",
    )
    parser.add_argument(
        "--exclude-dir",
        action="append",
        default=[],
        help="Additional directory name to exclude. Can be passed multiple times.",
    )
    args = parser.parse_args()

    root = Path(args.root).resolve()
    extensions = {ext if ext.startswith(".") else "." + ext for ext in args.extensions}
    exclude_dirs = DEFAULT_EXCLUDE_DIRS | set(args.exclude_dir)
    dry_run = not args.apply

    changed_files: list[tuple[Path, str]] = []
    scanned = 0
    for path in iter_files(root, extensions, exclude_dirs):
        scanned += 1
        changed, _, reason = process_file(path, dry_run)
        if changed:
            changed_files.append((path, reason))

    action = "would fix" if dry_run else "fixed"
    print(f"Scanned {scanned} file(s); {action} {len(changed_files)} file(s).")
    for path, reason in changed_files:
        print(f"{action}: {path.relative_to(root)} ({reason})")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())

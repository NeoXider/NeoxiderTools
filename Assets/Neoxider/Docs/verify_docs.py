# -*- coding: utf-8 -*-
"""
Проверка документации:
1) .md без соответствующего .cs (сироты) — по пути в тексте или по зеркалу Docs -> Scripts
2) .md без разделителя --- в первых 15 строках (для документов по скрипту)
"""
import os
import re

root_docs = os.path.dirname(os.path.abspath(__file__))
root_scripts = os.path.normpath(os.path.join(root_docs, "..", "Scripts"))
root_editor = os.path.normpath(os.path.join(root_docs, "..", "Editor"))
skip_names = {
    "README", "IMPROVEMENTS", "DEPRECATED", "NEXT_IMPROVEMENTS", "README_IMPROVEMENTS",
    "Plan_", "Roadmap", "_Plan", "Interfaces", "DOCUMENTATION", "DOCUMENTATION_GUIDELINES",
    "MarkdownRendererFork", "UsefulComponents", "ensure_doc_style", "check_doc_headers",
    "add_missing_headers", "verify_docs",
}
skip_substrings = ("README.md", "IMPROVEMENTS.md", "DEPRECATED", "NEXT_IMPROVEMENTS",
                   "README_IMPROVEMENTS", "_Plan.md", "Roadmap.md", "Interfaces.md",
                   "DOCUMENTATION.md", "DOCUMENTATION_GUIDELINES.md")

def is_skip(path_rel):
    name = os.path.splitext(os.path.basename(path_rel))[0]
    if any(name.startswith(s) or s in name for s in skip_names):
        return True
    if any(s in path_rel for s in skip_substrings):
        return True
    return False

def extract_script_path(content):
    # Scripts/.../File.cs или Editor/.../File.cs
    for prefix in ("Scripts", "Editor"):
        m = re.search(r"(?:Assets/Neoxider/)?{0}/([^\s\)\]\"']+\.cs)".format(prefix), content)
        if m:
            return m.group(1).replace("/", os.sep)
        m = re.search(r"файл[:\s]+[`\"]?(?:Assets/Neoxider/)?{0}/([^\s\)\]\"']+\.cs)".format(prefix), content, re.I)
        if m:
            return m.group(1).replace("/", os.sep)
        m = re.search(r"Путь[:\s]+[`\"]?(?:Assets/Neoxider/)?{0}/([^\s\)\]\"']+\.cs)".format(prefix), content, re.I)
        if m:
            return m.group(1).replace("/", os.sep)
    return None

def infer_script_path(md_rel):
    # Docs/Save/PlayerData.md -> Scripts/Save/PlayerData.cs (path mirror)
    base, ext = os.path.splitext(md_rel)
    if ext.lower() != ".md":
        return None
    return base + ".cs"

orphans = []
missing_sep = []
for dirpath, _, filenames in os.walk(root_docs):
    for name in filenames:
        if not name.endswith(".md"):
            continue
        path = os.path.join(dirpath, name)
        rel = os.path.relpath(path, root_docs)
        if is_skip(rel):
            continue
        try:
            with open(path, "r", encoding="utf-8", errors="replace") as f:
                lines = f.readlines()
        except Exception as e:
            orphans.append((rel, "read error: " + str(e)))
            continue
        content = "".join(lines[:30])
        script_rel = extract_script_path(content)
        if script_rel:
            # Проверяем и Scripts, и Editor (путь может быть Scripts/... или Editor/...)
            for base in (root_scripts, root_editor):
                script_full = os.path.join(base, script_rel)
                if os.path.isfile(script_full):
                    script_rel = None
                    break
            if script_rel:
                # также проверяем только по имени файла где-то под base (на случай другого подпути)
                script_name = os.path.basename(script_rel)
                found = False
                for base in (root_scripts, root_editor):
                    for _dp, _, _fnames in os.walk(base):
                        if script_name in _fnames:
                            found = True
                            break
                    if found:
                        break
                if not found:
                    orphans.append((rel, "script not found: " + script_rel))
        # Проверка разделителя --- в первых 15 строках (для документов по скрипту)
        head = "".join(lines[:15])
        if "**Что это:**" in head and "---" not in head:
            missing_sep.append(rel)

for rel, reason in sorted(orphans):
    print("ORPHAN\t{}\t{}".format(rel, reason))
for rel in sorted(missing_sep):
    print("NO_SEP\t{}".format(rel))
if orphans or missing_sep:
    print("\nTotal orphans:", len(orphans), "| Missing ---:", len(missing_sep))

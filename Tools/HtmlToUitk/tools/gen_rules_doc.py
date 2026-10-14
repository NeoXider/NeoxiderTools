#!/usr/bin/env python3
"""Generate docs/RULES.md from spec/rules.json (standard library only)."""
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SEV = {"FAIL": "Error", "WARN": "Warning", "INFO": "Info"}
FAMILIES = [
    ("D", "Dialect: elements, attributes, files"),
    ("H", "HTML recovery"),
    ("L", "Localisation"),
    ("N", "Names and classes"),
    ("C", "CSS and USS constructs"),
    ("T", "Text mapping"),
    ("U", "UXML"),
    ("R", "Project rules and transitions"),
    ("E", "Emulation failures"),
    ("J", "Multi-file jobs"),
    ("P", "Pages and navigation"),
    ("A", "Animation"),
    ("X", "Design-time checks (browser only)"),
]


def cell(text):
    return (text or "").replace("|", chr(92) + "|").replace("\n", " ")


def main():
    data = json.load(open(os.path.join(ROOT, "spec", "rules.json"), encoding="utf-8"))
    rules = data["rules"]
    out = ["# Rule catalogue", "", "Generated from `spec/rules.json` by `tools/gen_rules_doc.py`. Do not edit by hand.", "",
           "Severity is the severity when the construct is rejected. A construct handled by an emulation is reported with the emulated severity at its tier (Native, ConvertTime, Runtime). See `docs/EMULATIONS.md` section 2.", ""]
    for letter, title in FAMILIES:
        group = [r for r in rules if r["id"].startswith(letter)]
        if not group:
            continue
        out.append("## %s: %s" % (letter, title))
        out.append("")
        out.append("| Id | Severity | Tier | Emulation | Rule | Why | Rewrite |")
        out.append("|---|---|---|---|---|---|---|")
        for r in group:
            out.append("| %s | %s | %s | %s | %s | %s | %s |" % (r["id"], SEV[r["severity"]], r.get("tier") or "", r.get("emulation") or "", cell(r["title"]), cell(r["why"]), cell(r["fix"])))
        out.append("")
    with open(os.path.join(ROOT, "docs", "RULES.md"), "w", encoding="utf-8", newline="\n") as handle:
        handle.write("\n".join(out))
    print("rules: %d" % len(rules))


if __name__ == "__main__":
    sys.exit(main())

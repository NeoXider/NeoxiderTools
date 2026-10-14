#!/usr/bin/env python3
"""Compare the Python oracle, an optional C# CLI and the golden fixtures.

Usage:
  python tools/oracle_diff.py                       # oracle vs expected/ for S01..S16 (drift check)
  python tools/oracle_diff.py --cli "dotnet run --project src/HtmlToUitk.Cli --"   # also C# CLI vs oracle vs expected
  python tools/oracle_diff.py --fixtures S01 S05 --keep

The CLI is invoked as: <cli> convert <page> --config <config> --out <dir> --format json [--force]
(or validate ... --format json for fixtures in validate mode). Its JSON must contain the findings
(top level "findings" or "report.findings") using the diagnostics schema.
Exit code 0 when everything compared equal, 1 otherwise, 2 on usage errors.
"""
import argparse
import json
import os
import shlex
import shutil
import subprocess
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, os.path.join(ROOT, "oracle", "python"))

SEV = {"FAIL": "Error", "WARN": "Warning", "INFO": "Info"}
M1 = ["S%02d" % n for n in range(1, 17)]


def read(path):
    with open(path, "r", encoding="utf-8", newline="") as handle:
        return handle.read().replace("\r\n", "\n")


def tree(base):
    out = {}
    for folder, _, files in os.walk(base):
        for name in files:
            full = os.path.join(folder, name)
            rel = os.path.relpath(full, base).replace("\\", "/")
            if name.endswith((".png",)):
                continue
            out[rel] = read(full)
    return out


def diag_key(item):
    return (item["rule"], item["severity"], item["tier"], item["file"], int(item["line"]))


def relativise(path):
    path = path.replace("\\", "/")
    marker = "/input/"
    if marker in path:
        return path.split(marker, 1)[1]
    return os.path.basename(path)


def normalise_findings(findings, from_oracle=False):
    out = []
    for f in findings:
        severity = f["severity"]
        tier = f.get("tier")
        if from_oracle:
            severity = SEV.get(severity, severity)
            tier = "Reject" if severity == "Error" else "Native"
        out.append({"rule": f["rule"], "severity": severity, "tier": tier, "file": relativise(f["file"]), "line": int(f["line"])})
    return sorted(out, key=lambda d: ({"Error": 0, "Warning": 1, "Info": 2}[d["severity"]], d["file"], d["line"], d["rule"]))


def find_fixture(fid):
    base = os.path.join(ROOT, "fixtures")
    for name in sorted(os.listdir(base)):
        if name.startswith(fid + "-"):
            return os.path.join(base, name)
    return None


def run_oracle(fixture, meta, out_dir):
    from htmltouitk.config import load_config
    from htmltouitk.analyze import analyze
    from htmltouitk.pipeline import run_convert

    config = load_config(os.path.join(fixture, meta.get("config", "config.json")))
    page = os.path.join(fixture, meta["inputs"][0])
    an = analyze(page, config)
    findings = [{"rule": f.rule, "severity": f.severity, "file": f.file, "line": f.line} for f in an.report.sorted_findings()]
    if meta["mode"] == "convert":
        result = run_convert(page, config, out_dir, force=bool(meta.get("force")), lint=False)
        if result.refused:
            return findings, None
    return findings, tree(out_dir) if os.path.isdir(out_dir) else {}


def run_cli(cli, fixture, meta, out_dir):
    page = os.path.join(fixture, meta["inputs"][0])
    config = os.path.join(fixture, meta.get("config", "config.json"))
    args = shlex.split(cli) + [meta["mode"], page, "--config", config, "--format", "json"]
    if meta["mode"] == "convert":
        args += ["--out", out_dir]
        if meta.get("force"):
            args.append("--force")
    proc = subprocess.run(args, stdout=subprocess.PIPE, stderr=subprocess.PIPE, universal_newlines=True, cwd=ROOT)
    try:
        data = json.loads(proc.stdout)
    except ValueError:
        raise RuntimeError("CLI did not print JSON (exit %d):\n%s\n%s" % (proc.returncode, proc.stdout[:400], proc.stderr[:400]))
    findings = data.get("findings")
    if findings is None:
        findings = data.get("report", {}).get("findings", [])
    return findings, tree(out_dir) if os.path.isdir(out_dir) else {}


def compare_trees(label, got, want, only_status, errors):
    for rel, text in sorted(want.items()):
        if only_status is not None and only_status.get("expected/" + rel) != "oracle":
            continue
        if rel == "diagnostics.json":
            continue
        if rel not in got:
            errors.append("%s: missing %s" % (label, rel))
        elif got[rel] != text:
            errors.append("%s: %s differs" % (label, rel))
    if only_status is None:
        for rel in sorted(got):
            if rel not in want:
                errors.append("%s: unexpected file %s" % (label, rel))


def main(argv=None):
    parser = argparse.ArgumentParser()
    parser.add_argument("--fixtures", nargs="*", default=M1)
    parser.add_argument("--cli", default=None)
    parser.add_argument("--keep", action="store_true")
    args = parser.parse_args(argv)
    failures = 0
    for fid in args.fixtures:
        fixture = find_fixture(fid)
        if not fixture:
            print("%s: fixture not found" % fid)
            return 2
        meta = json.load(open(os.path.join(fixture, "fixture.json"), encoding="utf-8"))
        status = meta.get("expectedStatus", {})
        want_tree = tree(os.path.join(fixture, "expected"))
        want_diag = json.load(open(os.path.join(fixture, "expected", "diagnostics.json"), encoding="utf-8"))
        want_keys = [diag_key(d) for d in want_diag]
        errors = []
        tmp = tempfile.mkdtemp(prefix="oracle-diff-")
        try:
            o_findings, o_tree = run_oracle(fixture, meta, os.path.join(tmp, "oracle"))
            o_diag = [diag_key(d) for d in normalise_findings(o_findings, True)]
            if o_diag != want_keys:
                errors.append("oracle diagnostics differ from expected/diagnostics.json")
            if o_tree is not None:
                compare_trees("oracle vs expected", o_tree, want_tree, status, errors)
            if args.cli:
                c_findings, c_tree = run_cli(args.cli, fixture, meta, os.path.join(tmp, "cli"))
                c_diag = [diag_key(d) for d in normalise_findings(c_findings, False)]
                if c_diag != want_keys:
                    errors.append("cli diagnostics differ from expected/diagnostics.json")
                if c_diag != o_diag:
                    errors.append("cli diagnostics differ from oracle diagnostics")
                compare_trees("cli vs expected", c_tree or {}, want_tree, None if meta["mode"] == "convert" else {}, errors)
                if o_tree is not None and c_tree is not None:
                    for rel in sorted(set(o_tree) | set(c_tree)):
                        if o_tree.get(rel) != c_tree.get(rel):
                            errors.append("cli vs oracle: %s differs" % rel)
        finally:
            if args.keep:
                print("kept %s" % tmp)
            else:
                shutil.rmtree(tmp, ignore_errors=True)
        if errors:
            failures += 1
            print("FAIL %s" % fid)
            for e in errors:
                print("  " + e)
        else:
            print("ok   %s" % fid)
    print("%d of %d fixtures differ" % (failures, len(args.fixtures)))
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())

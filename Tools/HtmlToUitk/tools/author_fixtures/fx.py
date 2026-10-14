import json
import os
import shutil
import struct
import sys
import zlib

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))).replace(chr(92), "/")
sys.path.insert(0, ROOT + "/oracle/python")
sys.path.insert(0, ROOT + "/tools")

SEV = {"FAIL": "Error", "WARN": "Warning", "INFO": "Info"}


def fnv1a(text):
    h = 0x811C9DC5
    for b in text.encode("utf-8"):
        h ^= b
        h = (h * 0x01000193) & 0xFFFFFFFF
    return "%08x" % h


def png_bytes(width, height, rgba):
    raw = b""
    row = bytes(rgba) * width
    for _ in range(height):
        raw += b"\x00" + row

    def chunk(kind, data):
        body = kind + data
        return struct.pack(">I", len(data)) + body + struct.pack(">I", zlib.crc32(body) & 0xFFFFFFFF)

    return b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)) + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")


def dump(data):
    return json.dumps(data, indent=2, ensure_ascii=False) + "\n"


def write_file(path, content):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    if isinstance(content, bytes):
        with open(path, "wb") as handle:
            handle.write(content)
    else:
        with open(path, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(content)


def lines(text):
    return text.strip("\n") + "\n"


def oracle_diagnostics(an, input_root):
    from htmltouitk.rules import RULES
    out = []
    for f in an.report.sorted_findings():
        rule = RULES[f.rule]
        rel = os.path.relpath(f.file, input_root).replace("\\", "/") if os.path.isabs(f.file) or ":" in f.file else f.file
        out.append({
            "rule": f.rule,
            "severity": SEV[f.severity],
            "tier": "Reject" if f.severity == "FAIL" else "Native",
            "file": rel,
            "line": f.line,
        })
    out.sort(key=lambda d: ({"Error": 0, "Warning": 1, "Info": 2}[d["severity"]], d["file"], d["line"], d["rule"]))
    return out


def make(fid, title, proves, rules, tiers, mode, verdict, files, config=None, main=None, oracle=False,
         force=False, multi=False, caps="6000.6", patch=None, expected=None, diagnostics=None, job=None,
         notes=None, compare=None, extra=None):
    name = "%s-%s" % (fid, title)
    base = os.path.join(ROOT, "fixtures", name)
    if os.path.isdir(base):
        shutil.rmtree(base)
    os.makedirs(base)
    for rel, content in files.items():
        write_file(os.path.join(base, "input", rel), content)
    meta = {
        "id": fid,
        "title": title,
        "proves": proves,
        "rules": sorted(rules),
        "tiers": tiers,
        "mode": mode,
        "expectVerdict": verdict,
        "capabilities": caps,
        "multiFile": multi,
    }
    if patch:
        meta["capabilityPatch"] = patch
    if main:
        meta["inputs"] = ["input/" + m for m in main]
    if job is not None:
        meta["job"] = "job.json"
        write_file(os.path.join(base, "job.json"), dump(job))
    cfg = dict(config or {})
    if job is None:
        meta["config"] = "config.json"
    if cfg or job is None:
        write_file(os.path.join(base, "config.json"), dump(cfg))
    if force:
        meta["force"] = True
    if compare:
        meta["compare"] = compare
    if notes:
        meta["notes"] = notes
    if extra:
        meta.update(extra)
    status = {}
    exp_dir = os.path.join(base, "expected")
    if oracle:
        from htmltouitk.config import load_config
        from htmltouitk.analyze import analyze
        from htmltouitk.pipeline import run_convert
        c = load_config(os.path.join(base, "config.json"))
        page = os.path.join(base, "input", main[0])
        an = analyze(page, c)
        got = an.report.verdict()
        assert got == verdict, "%s: oracle verdict %s != %s\n%s" % (fid, got, verdict, an.report.to_text())
        diags = oracle_diagnostics(an, os.path.join(base, "input"))
        write_file(os.path.join(exp_dir, "diagnostics.json"), dump(diags))
        status["expected/diagnostics.json"] = "oracle"
        if mode == "convert":
            res = run_convert(page, c, exp_dir, force=force, lint=True, overwrite=False)
            assert not res.refused and not res.errors, fid
            for f in res.files:
                status["expected/" + os.path.relpath(f[0], exp_dir).replace("\\", "/")] = "oracle"
            for lf in res.lint:
                if lf.severity in ("ERROR", "MISSING", "WARN"):
                    print("LINT", fid, lf.text())
    else:
        write_file(os.path.join(exp_dir, "diagnostics.json"), dump(diagnostics or []))
        status["expected/diagnostics.json"] = "authored"
        for rel, content in (expected or {}).items():
            write_file(os.path.join(exp_dir, rel), content)
            status["expected/" + rel] = "authored"
    meta["expectedStatus"] = dict(sorted(status.items()))
    write_file(os.path.join(base, "fixture.json"), dump(meta))
    print("ok", name, verdict)
    return base

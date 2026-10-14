import glob
import os
import re
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET

from . import uitk
from .cssparse import parse_selector, parse_stylesheet, var_refs
from .loc import has_letters, key_matches
from .util import posix

URL_RE = re.compile(r"url\(\s*(?:\"([^\"]*)\"|'([^']*)'|([^)'\"]+))\s*\)")
LINT_LINE = re.compile(r"^(ERROR|MISSING|WARN|INFO)\s+(\S+?):(\d+)\s+\[([^\]]+)\]\s+(.*)$")


class LintFinding(object):
    def __init__(self, severity, file, line, check, message):
        self.severity = severity
        self.file = file
        self.line = line
        self.check = check
        self.message = message

    def as_dict(self):
        return {"severity": self.severity, "file": self.file, "line": self.line, "check": self.check, "message": self.message}

    def text(self):
        return "%-7s %s:%d [%s] %s" % (self.severity, self.file, self.line, self.check, self.message)


def _exists_any(paths):
    for path in paths:
        if os.path.exists(path):
            return True
    return False


def lint_files(an, files, out_dir):
    cfg = an.cfg
    findings = []
    uss_texts = dict((p, t) for p, t in files if p.endswith(".uss"))
    defined_vars = set()
    defined_classes = set(an.skin_classes)
    for skin in an.skins:
        defined_vars |= set(skin.vars)
    for path, text in uss_texts.items():
        sheet = parse_stylesheet(text, path)
        label = posix(os.path.relpath(path, out_dir))
        for line, kind, snippet in sheet.errors:
            findings.append(LintFinding("ERROR", label, line, "uss.syntax", "%s: %s" % (kind, snippet)))
        if "/*" in text:
            findings.append(LintFinding("ERROR", label, 1, "uss.comment", "comments are not allowed"))
        for rule in sheet.rules:
            for sel_text, line in rule.selectors:
                selector = parse_selector(sel_text)
                for _, compound in selector.parts:
                    defined_classes.update(compound.classes)
                for kind, snippet in selector.problems:
                    if kind != "type":
                        findings.append(LintFinding("ERROR", label, line, "uss.selector", "unsupported selector %s" % snippet))
            for decl in rule.decls:
                if decl.name.startswith("--"):
                    defined_vars.add(decl.name)
                elif decl.name not in uitk.USS_PROPERTIES:
                    findings.append(LintFinding("ERROR", label, decl.line, "uss.property", "unknown property %s" % decl.name))
        for match in URL_RE.finditer(text):
            target = (match.group(1) or match.group(2) or match.group(3) or "").strip()
            if target.startswith("/"):
                full = os.path.join(cfg.project_root, target.lstrip("/"))
            elif target.startswith(("project://", "resource")):
                continue
            else:
                full = os.path.join(os.path.dirname(path), target)
            if not os.path.exists(full):
                findings.append(LintFinding("ERROR", label, text.count("\n", 0, match.start()) + 1, "uss.url", "url target not found: %s" % target))
    for path, text in uss_texts.items():
        label = posix(os.path.relpath(path, out_dir))
        for name, has_fallback in var_refs(text):
            if name not in defined_vars and not has_fallback:
                findings.append(LintFinding("ERROR", label, 1, "uss.var", "%s is not defined" % name))
    key_pattern = cfg.get("localization.keyPattern")
    dynamic = re.compile(cfg.get("naming.dynamicClassPattern") or r"^$")
    dynamic_names = set(cfg.get("naming.dynamicClasses", []))
    for path, text in files:
        if not path.endswith(".uxml"):
            continue
        label = posix(os.path.relpath(path, out_dir))
        try:
            root = ET.fromstring(text)
        except ET.ParseError as exc:
            findings.append(LintFinding("ERROR", label, 1, "uxml.xml", str(exc)))
            continue
        names = {}
        for element in root.iter():
            local = element.tag.split("}")[-1]
            name = element.get("name")
            if name:
                if name in names:
                    findings.append(LintFinding("ERROR", label, 1, "uxml.duplicate-name", name))
                names[name] = True
            for cls in (element.get("class") or "").split():
                if cls not in defined_classes and not dynamic.match(cls) and cls not in dynamic_names and not cls.startswith("unity-"):
                    findings.append(LintFinding("WARN", label, 1, "uxml.class-undefined", cls))
            for attr in ("text", "label", "title", "tooltip", "placeholder-text"):
                value = element.get(attr)
                if value and has_letters(value) and not key_matches(key_pattern, value):
                    findings.append(LintFinding("ERROR", label, 1, "uxml.raw-text", "%s=\"%s\"" % (attr, value)))
            if local == "Style":
                src = element.get("src")
                candidates = [os.path.join(os.path.dirname(path), src), os.path.join(cfg.layout_dir("screens"), src)]
                if src and not _exists_any([os.path.normpath(c) for c in candidates]):
                    findings.append(LintFinding("ERROR", label, 1, "uxml.style-src", "target missing: %s" % src))
    return findings


def run_external(an, files, out_dir, spec):
    cfg = an.cfg
    stage = tempfile.mkdtemp(prefix="htmltouitk-lint-")
    try:
        project = cfg.project_root
        for pattern in spec.get("mirror", []):
            for source in glob.glob(os.path.join(project, pattern), recursive=True):
                if os.path.isfile(source):
                    dest = os.path.join(stage, os.path.relpath(source, project))
                    os.makedirs(os.path.dirname(dest), exist_ok=True)
                    shutil.copy2(source, dest)
        ui_root_rel = cfg.data["uiRoot"]
        generated = {}
        for path, text in files:
            rel = os.path.relpath(path, out_dir)
            if rel.endswith(".newkeys.json"):
                rel = rel[:-len(".json")] + ".strings.json"
            dest = os.path.join(stage, ui_root_rel, rel)
            os.makedirs(os.path.dirname(dest), exist_ok=True)
            with open(dest, "w", encoding="utf-8", newline="\n") as handle:
                handle.write(text)
            generated[posix(os.path.relpath(dest, stage))] = posix(os.path.relpath(path, out_dir))
        empty_cs = os.path.join(stage, "_empty_cs")
        os.makedirs(empty_cs, exist_ok=True)
        replacements = {"{stage}": stage, "{project}": project, "{empty}": empty_cs}
        command = []
        for part in spec["command"]:
            for key, value in replacements.items():
                part = part.replace(key, value)
            command.append(part)
        cwd = cfg.abs_path(spec["cwd"]) if spec.get("cwd") else project
        try:
            proc = subprocess.run(command, cwd=cwd, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                  universal_newlines=True, timeout=300)
        except (OSError, subprocess.SubprocessError) as exc:
            return [LintFinding("ERROR", "external-lint", 0, "external", "could not run: %s" % exc)], None
        findings = []
        for line in (proc.stdout + "\n" + proc.stderr).splitlines():
            match = LINT_LINE.match(line.strip())
            if not match:
                continue
            severity, file, number, check, message = match.groups()
            file = posix(file)
            if file in generated:
                findings.append(LintFinding(severity, generated[file], int(number), check, message))
        return findings, proc.returncode
    finally:
        shutil.rmtree(stage, ignore_errors=True)

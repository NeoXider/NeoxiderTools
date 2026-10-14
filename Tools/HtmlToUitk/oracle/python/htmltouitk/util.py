import json
import os
import re

SEVERITIES = ("FAIL", "WARN", "INFO")
SEVERITY_RANK = {"FAIL": 0, "WARN": 1, "INFO": 2}


def read_text(path):
    with open(path, "r", encoding="utf-8-sig", newline="") as handle:
        return handle.read().replace("\r\n", "\n").replace("\r", "\n")


def write_text(path, text):
    folder = os.path.dirname(os.path.abspath(path))
    os.makedirs(folder, exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(text)


def posix(path):
    return str(path).replace("\\", "/")


def relpath(path, start):
    try:
        return posix(os.path.relpath(path, start))
    except ValueError:
        return posix(path)


def line_of(text, index):
    return text.count("\n", 0, index) + 1


def number(value):
    text = ("%.4f" % value).rstrip("0").rstrip(".")
    if text in ("-0", ""):
        return "0"
    return text


def dumps(data):
    return json.dumps(data, indent=2, ensure_ascii=False, sort_keys=False) + "\n"


class Finding(object):
    __slots__ = ("rule", "severity", "file", "line", "col", "construct", "message", "why", "fix")

    def __init__(self, rule, severity, file, line, col, construct, message, why, fix):
        self.rule = rule
        self.severity = severity
        self.file = file
        self.line = line
        self.col = col
        self.construct = construct
        self.message = message
        self.why = why
        self.fix = fix

    def as_dict(self):
        return {
            "rule": self.rule,
            "severity": self.severity,
            "file": self.file,
            "line": self.line,
            "column": self.col,
            "construct": self.construct,
            "message": self.message,
            "why": self.why,
            "fix": self.fix,
        }


class Report(object):
    def __init__(self, source):
        self.source = source
        self.findings = []
        self.new_keys = []
        self.info = {}

    def add(self, finding):
        key = (finding.rule, finding.file, finding.line, finding.construct, finding.message)
        for existing in self.findings:
            if (existing.rule, existing.file, existing.line, existing.construct, existing.message) == key:
                return
        self.findings.append(finding)

    def counts(self):
        counts = {"FAIL": 0, "WARN": 0, "INFO": 0}
        for finding in self.findings:
            counts[finding.severity] += 1
        return counts

    def verdict(self):
        counts = self.counts()
        if counts["FAIL"]:
            return "FAIL"
        if counts["WARN"]:
            return "PASS WITH WARNINGS"
        return "PASS"

    def sorted_findings(self):
        return sorted(
            self.findings,
            key=lambda f: (SEVERITY_RANK[f.severity], f.file, f.line, f.col, f.rule),
        )

    def exit_code(self, strict=False):
        counts = self.counts()
        if counts["FAIL"]:
            return 1
        if strict and counts["WARN"]:
            return 1
        return 0

    def to_json(self, version):
        counts = self.counts()
        return {
            "tool": "htmltouitk",
            "version": version,
            "source": self.source,
            "verdict": self.verdict(),
            "counts": counts,
            "findings": [f.as_dict() for f in self.sorted_findings()],
            "newKeys": list(self.new_keys),
        }

    def to_text(self, verbose=True):
        lines = []
        counts = self.counts()
        lines.append("%s: %s  (FAIL %d, WARN %d, INFO %d)" % (self.source, self.verdict(), counts["FAIL"], counts["WARN"], counts["INFO"]))
        for number_, finding in enumerate(self.sorted_findings(), start=1):
            lines.append("")
            lines.append("%d. [%s] %s %s:%d:%d" % (number_, finding.severity, finding.rule, finding.file, finding.line, finding.col))
            lines.append("   construct: %s" % finding.construct)
            lines.append("   problem:   %s" % finding.message)
            if verbose and finding.why:
                lines.append("   why:       %s" % finding.why)
            if verbose and finding.fix:
                lines.append("   rewrite:   %s" % finding.fix)
        if self.new_keys:
            lines.append("")
            lines.append("new localisation keys (%d): %s" % (len(self.new_keys), ", ".join(self.new_keys)))
        return "\n".join(lines) + "\n"


KEBAB = re.compile(r"^[a-z][a-z0-9]*(-[a-z0-9]+)*$")

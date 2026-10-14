import os
from collections import OrderedDict

from .analyze import analyze
from .convert import generate
from .lint import lint_files, run_external
from .util import posix, write_text


class ConvertResult(object):
    def __init__(self):
        self.files = []
        self.analysis = None
        self.lint = []
        self.external_exit = None
        self.summary = OrderedDict()
        self.refused = False
        self.errors = []

    def lint_failed(self):
        return any(f.severity in ("ERROR", "MISSING", "WARN") for f in self.lint)


def run_validate(path, cfg, screen=None):
    return analyze(path, cfg, screen)


def guard_output(cfg, out_dir, files, overwrite):
    live = os.path.normcase(os.path.abspath(cfg.ui_root))
    target = os.path.normcase(os.path.abspath(out_dir))
    inside_live = target == live or target.startswith(live + os.sep)
    errors = []
    if inside_live and not overwrite:
        errors.append("output folder is inside the live UI root (%s); pass --overwrite to write there" % posix(cfg.ui_root))
        return errors
    for path, _ in files:
        if os.path.exists(path) and not overwrite:
            errors.append("refusing to overwrite %s (use --overwrite)" % posix(path))
    return errors


def run_convert(path, cfg, out_dir, force=False, lint=True, overwrite=False, screen=None):
    result = ConvertResult()
    an = analyze(path, cfg, screen)
    result.analysis = an
    if an.report.counts()["FAIL"] and not force:
        result.refused = True
        return result
    files, rule_count, new_tokens = generate(an, out_dir)
    errors = guard_output(cfg, out_dir, files, overwrite)
    if errors:
        result.errors = errors
        return result
    for target, text in files:
        write_text(target, text)
    result.files = [(target, len(text)) for target, text in files]
    if lint:
        if cfg.get("lint.builtin", True):
            result.lint.extend(lint_files(an, files, out_dir))
        external = cfg.get("lint.external")
        if external:
            findings, code = run_external(an, files, out_dir, external)
            result.lint.extend(findings)
            result.external_exit = code
    summary = result.summary
    summary["screen"] = an.screen
    summary["verdict"] = an.report.verdict()
    summary["elements"] = len(an.nodes)
    summary["ussRules"] = rule_count
    summary["newTokens"] = len(new_tokens)
    summary["keysUsed"] = len(an.keys)
    summary["newKeys"] = len(an.new_keys)
    summary["files"] = [posix(os.path.relpath(t, out_dir)) for t, _ in files]
    summary["lintFindings"] = len(result.lint)
    return result

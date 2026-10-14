import argparse
import json
import os
import sys

from . import __version__
from .config import ConfigError, load_config
from .pipeline import run_convert, run_validate
from .preview import build_page
from .rules import rule_table
from .util import dumps, write_text


def _config(args):
    cfg = load_config(getattr(args, "config", None))
    for problem in cfg.problems:
        sys.stderr.write("config warning: %s\n" % problem)
    return cfg


def cmd_validate(args):
    cfg = _config(args)
    an = run_validate(args.page, cfg, args.screen)
    report = an.report
    if args.format == "json":
        text = dumps(report.to_json(__version__))
    else:
        text = report.to_text()
    if args.out:
        write_text(args.out, text)
    else:
        sys.stdout.write(text)
    return report.exit_code(args.strict or cfg.get("rules.strictWarnings"))


def cmd_convert(args):
    cfg = _config(args)
    result = run_convert(args.page, cfg, args.out, force=args.force, lint=not args.no_lint, overwrite=args.overwrite, screen=args.screen)
    an = result.analysis
    out = sys.stdout
    if result.refused:
        out.write(an.report.to_text())
        out.write("\nconvert refused: the page does not validate (use --force to convert anyway)\n")
        return 1
    if result.errors:
        for error in result.errors:
            out.write("error: %s\n" % error)
        return 2
    if args.format == "json":
        data = {"summary": result.summary, "lint": [f.as_dict() for f in result.lint], "report": an.report.to_json(__version__)}
        out.write(dumps(data))
    else:
        out.write(an.report.to_text(verbose=False))
        out.write("\nconvert summary\n")
        for key, value in result.summary.items():
            if key == "files":
                out.write("  files:\n")
                for item in value:
                    out.write("    %s\n" % item)
            else:
                out.write("  %s: %s\n" % (key, value))
        if result.lint:
            out.write("lint findings:\n")
            for finding in result.lint:
                out.write("  %s\n" % finding.text())
        else:
            out.write("lint: clean\n")
    if result.lint_failed():
        return 1
    return 0


def cmd_preview(args):
    cfg = _config(args)
    fragments = {}
    for item in args.fragment or []:
        name, _, path = item.partition("=")
        fragments[name] = os.path.abspath(path)
    overrides = {}
    for item in args.text or []:
        name, _, value = item.partition("=")
        overrides[name] = value
    html_text, report = build_page(args.uxml, cfg, locale=args.locale, viewports=args.viewport, out_path=args.out, fragments=fragments, overrides=overrides)
    write_text(args.out, html_text)
    kinds = {}
    for item in report.items:
        kinds[item["kind"]] = kinds.get(item["kind"], 0) + 1
    sys.stdout.write("preview written: %s" % args.out + chr(10))
    for kind in sorted(kinds):
        sys.stdout.write("  %s: %d" % (kind, kinds[kind]) + chr(10))
    if args.report:
        write_text(args.report, dumps(report.items))
    return 0


def cmd_rules(args):
    rows = rule_table()
    if args.format == "json":
        sys.stdout.write(dumps([{"id": r[0], "severity": r[1], "title": r[2]} for r in rows]))
    elif args.format == "markdown":
        sys.stdout.write("| Id | Severity | Rule |\n|---|---|---|\n")
        for rid, severity, title in rows:
            sys.stdout.write("| %s | %s | %s |\n" % (rid, severity, title))
    else:
        for rid, severity, title in rows:
            sys.stdout.write("%s  %-4s  %s\n" % (rid, severity, title))
    return 0


def build_parser():
    parser = argparse.ArgumentParser(prog="htmltouitk", description="HTML mock to Unity UI Toolkit (UXML + USS) converter and validator")
    parser.add_argument("--version", action="version", version="htmltouitk " + __version__)
    sub = parser.add_subparsers(dest="command")
    validate = sub.add_parser("validate", help="check whether an HTML mock can be converted")
    validate.add_argument("page")
    validate.add_argument("--config")
    validate.add_argument("--screen")
    validate.add_argument("--format", choices=("text", "json"), default="text")
    validate.add_argument("--out")
    validate.add_argument("--strict", action="store_true", help="warnings also fail")
    validate.set_defaults(func=cmd_validate)
    convert = sub.add_parser("convert", help="validate, then generate UXML + USS + key stub")
    convert.add_argument("page")
    convert.add_argument("--out", required=True)
    convert.add_argument("--config")
    convert.add_argument("--screen")
    convert.add_argument("--force", action="store_true")
    convert.add_argument("--no-lint", action="store_true")
    convert.add_argument("--overwrite", action="store_true")
    convert.add_argument("--format", choices=("text", "json"), default="text")
    convert.set_defaults(func=cmd_convert)
    preview = sub.add_parser("preview", help="render UXML + USS as a standalone HTML page")
    preview.add_argument("uxml")
    preview.add_argument("--config")
    preview.add_argument("--out", required=True)
    preview.add_argument("--locale")
    preview.add_argument("--viewport", action="append")
    preview.add_argument("--fragment", action="append", help="element-name=fragment.uxml, content built by code at runtime")
    preview.add_argument("--text", action="append", help="element-name=text (or element-name#value=text for fields)")
    preview.add_argument("--report")
    preview.set_defaults(func=cmd_preview)
    rules = sub.add_parser("rules", help="list the rule catalogue")
    rules.add_argument("--format", choices=("text", "json", "markdown"), default="text")
    rules.set_defaults(func=cmd_rules)
    return parser


def main(argv=None):
    parser = build_parser()
    args = parser.parse_args(argv)
    if not getattr(args, "func", None):
        parser.print_help()
        return 2
    try:
        return args.func(args)
    except ConfigError as exc:
        sys.stderr.write("error: %s\n" % exc)
        return 2
    except FileNotFoundError as exc:
        sys.stderr.write("error: %s\n" % exc)
        return 2


if __name__ == "__main__":
    sys.exit(main())

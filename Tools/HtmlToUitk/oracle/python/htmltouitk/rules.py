from .spec import load_spec
from .util import Finding


class Rule(object):
    __slots__ = ("id", "severity", "title", "why", "fix")

    def __init__(self, id, severity, title, why, fix):
        self.id = id
        self.severity = severity
        self.title = title
        self.why = why
        self.fix = fix


def _load():
    table = {}
    for item in load_spec("rules")["rules"]:
        table[item["id"]] = Rule(item["id"], item["severity"], item["title"], item["why"], item["fix"])
    return table


RULES = _load()


def advice_for(prop):
    spec = load_spec("uss")
    if prop in spec["propertyAdvice"]:
        return spec["propertyAdvice"][prop]
    for item in spec["prefixAdvice"]:
        if prop.startswith(item["prefix"]):
            return item["text"]
    return spec["defaultAdvice"]


def make_finding(report, rule_id, file, line, col, construct, message=None, severity=None, why=None, fix=None):
    rule = RULES[rule_id]
    finding = Finding(
        rule_id,
        severity or rule.severity,
        file,
        line,
        col,
        construct,
        message or rule.title,
        why if why is not None else rule.why,
        fix if fix is not None else rule.fix,
    )
    report.add(finding)
    return finding


def rule_table():
    return [(rule.id, rule.severity, rule.title) for _, rule in sorted(RULES.items())]

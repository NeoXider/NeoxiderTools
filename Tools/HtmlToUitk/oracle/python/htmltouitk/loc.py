import json
import os
import re

from .util import read_text


class LocTable(object):
    def __init__(self):
        self.locales = {}
        self.sources = []

    def merge(self, locale, mapping):
        target = self.locales.setdefault(locale, {})
        for key, value in mapping.items():
            if isinstance(value, str):
                target[key] = value

    def has(self, key):
        return any(key in table for table in self.locales.values())

    def get(self, key, locale=None):
        if locale and key in self.locales.get(locale, {}):
            return self.locales[locale][key]
        for table in self.locales.values():
            if key in table:
                return table[key]
        return None

    def keys(self):
        found = set()
        for table in self.locales.values():
            found.update(table)
        return sorted(found)

    def locale_names(self):
        return sorted(self.locales)


def load_file(table, path, fmt="locale-map", locale_hint=None):
    text = read_text(path)
    data = json.loads(text)
    table.sources.append(path)
    if fmt == "flat":
        locale = locale_hint or os.path.splitext(os.path.basename(path))[0]
        table.merge(locale, data)
        return
    for locale, mapping in data.items():
        if locale == "version" or not isinstance(mapping, dict):
            continue
        table.merge(locale, mapping)


def load_table(paths, fmt="locale-map"):
    table = LocTable()
    for path in paths:
        if path and os.path.isfile(path):
            load_file(table, path, fmt)
    return table


def key_matches(pattern, key):
    return re.match(pattern, key) is not None


def has_letters(text):
    return any(ch.isalpha() for ch in text)


def normalise_text(text):
    return re.sub(r"\s+", " ", text).strip()

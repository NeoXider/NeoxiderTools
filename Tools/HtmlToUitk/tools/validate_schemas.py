#!/usr/bin/env python3
"""Validate the data files of the repository against spec/schemas (standard library only).

Usage: python tools/validate_schemas.py [--root <repo>] [--verbose]
Exit code 0 when every checked file validates, 1 otherwise.
"""
import argparse
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)


def load_json(path):
    with open(path, "r", encoding="utf-8-sig") as handle:
        return json.load(handle)


class Validator(object):
    def __init__(self, schema_dir):
        self.schema_dir = schema_dir
        self.cache = {}

    def schema_file(self, name):
        if name not in self.cache:
            self.cache[name] = load_json(os.path.join(self.schema_dir, name))
        return self.cache[name]

    def resolve(self, ref, root_schema):
        file_part, _, fragment = ref.partition("#")
        target_root = self.schema_file(file_part) if file_part else root_schema
        node = target_root
        if fragment:
            for part in fragment.strip("/").split("/"):
                node = node[part]
        return node, target_root

    def validate(self, instance, schema, root_schema, path, errors):
        if "$ref" in schema:
            node, new_root = self.resolve(schema["$ref"], root_schema)
            self.validate(instance, node, new_root, path, errors)
            return
        expected = schema.get("type")
        if expected is not None:
            types = expected if isinstance(expected, list) else [expected]
            if not any(self.is_type(instance, t) for t in types):
                errors.append("%s: expected %s, got %s" % (path, "/".join(types), type(instance).__name__))
                return
        if "const" in schema and instance != schema["const"]:
            errors.append("%s: expected constant %r" % (path, schema["const"]))
        if "enum" in schema and instance not in schema["enum"]:
            errors.append("%s: %r is not one of %s" % (path, instance, schema["enum"]))
        if "oneOf" in schema:
            matches = 0
            for option in schema["oneOf"]:
                sub = []
                self.validate(instance, option, root_schema, path, sub)
                if not sub:
                    matches += 1
            if matches != 1:
                errors.append("%s: must match exactly one alternative (matched %d)" % (path, matches))
        if isinstance(instance, str):
            if "minLength" in schema and len(instance) < schema["minLength"]:
                errors.append("%s: shorter than %d" % (path, schema["minLength"]))
            if "pattern" in schema and re.search(schema["pattern"], instance) is None:
                errors.append("%s: %r does not match %s" % (path, instance, schema["pattern"]))
        if self.is_number(instance):
            if "minimum" in schema and instance < schema["minimum"]:
                errors.append("%s: below minimum %s" % (path, schema["minimum"]))
            if "maximum" in schema and instance > schema["maximum"]:
                errors.append("%s: above maximum %s" % (path, schema["maximum"]))
            if "exclusiveMinimum" in schema and instance <= schema["exclusiveMinimum"]:
                errors.append("%s: must be greater than %s" % (path, schema["exclusiveMinimum"]))
        if isinstance(instance, list):
            if "minItems" in schema and len(instance) < schema["minItems"]:
                errors.append("%s: fewer than %d items" % (path, schema["minItems"]))
            if "maxItems" in schema and len(instance) > schema["maxItems"]:
                errors.append("%s: more than %d items" % (path, schema["maxItems"]))
            if schema.get("uniqueItems") and len(set(json.dumps(i, sort_keys=True) for i in instance)) != len(instance):
                errors.append("%s: items are not unique" % path)
            if "items" in schema:
                for index, item in enumerate(instance):
                    self.validate(item, schema["items"], root_schema, "%s[%d]" % (path, index), errors)
        if isinstance(instance, dict):
            if "minProperties" in schema and len(instance) < schema["minProperties"]:
                errors.append("%s: fewer than %d properties" % (path, schema["minProperties"]))
            for key in schema.get("required", []):
                if key not in instance:
                    errors.append("%s: missing required property %r" % (path, key))
            properties = schema.get("properties", {})
            additional = schema.get("additionalProperties", True)
            for key, value in instance.items():
                child = "%s.%s" % (path, key)
                if key in properties:
                    self.validate(value, properties[key], root_schema, child, errors)
                elif additional is False:
                    errors.append("%s: unknown property %r" % (path, key))
                elif isinstance(additional, dict):
                    self.validate(value, additional, root_schema, child, errors)

    @staticmethod
    def is_number(value):
        return isinstance(value, (int, float)) and not isinstance(value, bool)

    def is_type(self, value, name):
        if name == "object":
            return isinstance(value, dict)
        if name == "array":
            return isinstance(value, list)
        if name == "string":
            return isinstance(value, str)
        if name == "boolean":
            return isinstance(value, bool)
        if name == "null":
            return value is None
        if name == "integer":
            return isinstance(value, int) and not isinstance(value, bool)
        if name == "number":
            return self.is_number(value)
        return False

    def check(self, instance, schema_name):
        schema = self.schema_file(schema_name)
        errors = []
        self.validate(instance, schema, schema, "$", errors)
        return errors


SUFFIX_SCHEMAS = [
    (".anim.json", "anim.schema.json"),
    (".nav.json", "nav.schema.json"),
    (".png.expect.json", "png-expect.schema.json"),
    (".mq.json", "mq.schema.json"),
    (".runtime.json", "runtime.schema.json"),
    (".htujob.json", "job.schema.json"),
]


def schema_for(rel):
    name = os.path.basename(rel)
    if name == "fixture.json":
        return "fixture.schema.json"
    if name == "diagnostics.json" and "/expected/" in "/" + rel:
        return "expected-diagnostics.schema.json"
    if name == "diagnostics.json" or name.endswith(".diagnostics.json"):
        return "diagnostics.schema.json"
    if name.startswith("capabilities.") and name.endswith(".json") and "overrides" not in name:
        return "capabilities.schema.json"
    if name in ("htmltouitk.config.json", "config.json"):
        return "config.schema.json"
    if name == "job.json":
        return "job.schema.json"
    for suffix, schema in SUFFIX_SCHEMAS:
        if name.endswith(suffix):
            return schema
    return None


def collect(root):
    skip = {"oracle", ".git", "node_modules", "bin", "obj"}
    found = []
    for base, dirs, files in os.walk(root):
        dirs[:] = [d for d in sorted(dirs) if d not in skip]
        for name in sorted(files):
            if not name.endswith(".json"):
                continue
            full = os.path.join(base, name)
            rel = os.path.relpath(full, root).replace("\\", "/")
            if rel.startswith("spec/schemas/") or rel.startswith("spec/rules") or rel.startswith("spec/dialect") or rel.startswith("spec/uss"):
                continue
            if "/input/" in "/" + rel:
                continue
            schema = schema_for(rel)
            if schema:
                found.append((rel, schema))
    return found


def main(argv=None):
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", default=ROOT)
    parser.add_argument("--verbose", action="store_true")
    args = parser.parse_args(argv)
    validator = Validator(os.path.join(args.root, "spec", "schemas"))
    failed = 0
    checked = 0
    for rel, schema in collect(args.root):
        try:
            instance = load_json(os.path.join(args.root, rel))
        except ValueError as exc:
            print("FAIL %s: not valid JSON (%s)" % (rel, exc))
            failed += 1
            continue
        errors = validator.check(instance, schema)
        checked += 1
        if errors:
            failed += 1
            print("FAIL %s (%s)" % (rel, schema))
            for error in errors:
                print("  " + error)
        elif args.verbose:
            print("ok   %s (%s)" % (rel, schema))
    print("checked %d files, %d failed" % (checked, failed))
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())

# -*- coding: utf-8 -*-
"""
Проверка: все атрибуты [NeoDoc("path")] в .cs ссылаются на существующие .md в Docs.
Путь в атрибуте — относительно Assets/Neoxider/Docs (например Tools/Input/SwipeController.md).
"""
import os
import re

root_docs = os.path.dirname(os.path.abspath(__file__))
root_neoxider = os.path.normpath(os.path.join(root_docs, ".."))
pattern = re.compile(r'NeoDoc\s*\(\s*"([^"]+)"\s*\)', re.IGNORECASE)


def collect_neodoc_paths():
    paths_by_file = []
    for dirpath, _, filenames in os.walk(root_neoxider):
        for name in filenames:
            if not name.endswith(".cs"):
                continue
            path = os.path.join(dirpath, name)
            try:
                with open(path, "r", encoding="utf-8", errors="replace") as f:
                    content = f.read()
            except Exception as e:
                print("READ_ERR\t{}\t{}".format(os.path.relpath(path, root_neoxider), e))
                continue
            for m in pattern.finditer(content):
                doc_rel = m.group(1).strip().replace("\\", "/")
                if not doc_rel.endswith(".md"):
                    doc_rel = doc_rel + ".md" if "." not in os.path.basename(doc_rel) else doc_rel
                cs_rel = os.path.relpath(path, root_neoxider)
                paths_by_file.append((cs_rel, doc_rel))
    return paths_by_file


def main():
    collected = collect_neodoc_paths()
    missing = []
    for cs_rel, doc_rel in collected:
        doc_full = os.path.normpath(os.path.join(root_docs, doc_rel))
        if not os.path.isfile(doc_full):
            missing.append((cs_rel, doc_rel))
    for cs_rel, doc_rel in sorted(missing, key=lambda x: (x[1], x[0])):
        print("MISSING\t{}\t->\t{}".format(cs_rel, doc_rel))
    if missing:
        print("\nTotal: {} broken NeoDoc link(s)".format(len(missing)))
        return 1
    print("OK: all {} NeoDoc link(s) point to existing .md".format(len(collected)))
    return 0


if __name__ == "__main__":
    exit(main())

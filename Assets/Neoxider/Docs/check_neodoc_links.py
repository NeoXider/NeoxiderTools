# -*- coding: utf-8 -*-
"""
Проверка: все атрибуты [NeoDoc("path")] в .cs ссылаются на существующие .md в Docs.
Путь в атрибуте — относительно Assets/Neoxider/Docs (например Tools/Input/SwipeController.md).

Дополнительно проверяется наличие зеркального EN-файла в DocsEn/ для каждой RU-ссылки.
"""
import os
import re

root_docs = os.path.dirname(os.path.abspath(__file__))
root_neoxider = os.path.normpath(os.path.join(root_docs, ".."))
root_docs_en = os.path.normpath(os.path.join(root_neoxider, "DocsEn"))
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
    missing_ru = []
    missing_en = []
    for cs_rel, doc_rel in collected:
        doc_full = os.path.normpath(os.path.join(root_docs, doc_rel))
        if not os.path.isfile(doc_full):
            missing_ru.append((cs_rel, doc_rel))
        doc_en_full = os.path.normpath(os.path.join(root_docs_en, doc_rel))
        if not os.path.isfile(doc_en_full):
            missing_en.append((cs_rel, doc_rel))

    for cs_rel, doc_rel in sorted(missing_ru, key=lambda x: (x[1], x[0])):
        print("MISSING_RU\t{}\t->\t{}".format(cs_rel, doc_rel))
    for cs_rel, doc_rel in sorted(missing_en, key=lambda x: (x[1], x[0])):
        print("MISSING_EN\t{}\t->\tDocsEn/{}".format(cs_rel, doc_rel))

    total_missing = len(missing_ru) + len(missing_en)
    if total_missing:
        if missing_ru:
            print("\nTotal RU broken: {}".format(len(missing_ru)))
        if missing_en:
            print("Total EN missing: {}".format(len(missing_en)))
        return 1
    print("OK: all {} NeoDoc link(s) point to existing RU and EN .md".format(len(collected)))
    return 0


if __name__ == "__main__":
    exit(main())

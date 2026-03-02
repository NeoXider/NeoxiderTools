# -*- coding: utf-8 -*-
"""Проверка: в каждом .md в начале есть **Что это:** или **Оглавление:** или **Навигация:** или **Как пользоваться:**"""
import os
import sys

root = os.path.dirname(os.path.abspath(__file__))
required = (
    "**Что это:**",
    "**Оглавление:**",
    "**Навигация:**",
    "**Как пользоваться:**",
    "**Как использовать:**",
)
missing = []
for dirpath, _, filenames in os.walk(root):
    for name in filenames:
        if not name.endswith(".md"):
            continue
        path = os.path.join(dirpath, name)
        try:
            with open(path, "r", encoding="utf-8", errors="replace") as f:
                head = "".join(f.readline() for _ in range(12))
        except Exception as e:
            missing.append((path.replace(root + os.sep, ""), str(e)))
            continue
        if not any(r in head for r in required):
            missing.append((path.replace(root + os.sep, ""), "no standard header"))
for rel, reason in sorted(missing):
    print(f"{rel}\t{reason}")
sys.exit(1 if missing else 0)

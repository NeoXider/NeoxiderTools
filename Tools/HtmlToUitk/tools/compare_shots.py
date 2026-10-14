#!/usr/bin/env python3
"""Compare two screenshots (for example a Unity Game View capture and a browser render).

Writes an overlay PNG (the first image dimmed, differing regions in red, region numbers) and a
JSON/text list of regions whose difference exceeds a threshold, so layout fidelity can be checked
without looking at the pictures. Pillow is required (pip install pillow); everything else is the
standard library.

Usage:
  python tools/compare_shots.py unity.png browser.png --out overlay.png [--json regions.json]
         [--threshold 40] [--cell 24] [--min-ratio 0.25] [--min-cells 2] [--crop x0,y0,x1,y1 ...]
Exit code: 0 when no region exceeds the limits, 1 otherwise, 2 on usage errors.
"""
import argparse
import json
import sys

try:
    from PIL import Image, ImageChops, ImageDraw, ImageFilter
except ImportError:
    sys.stderr.write("compare_shots.py needs Pillow: pip install pillow\n")
    sys.exit(2)


def load(path, size=None, crop=None):
    image = Image.open(path).convert("RGB")
    if crop:
        image = image.crop(crop)
    if size and image.size != size:
        image = image.resize(size, Image.LANCZOS)
    return image


def regions_from_grid(flags, cols, rows):
    seen = set()
    result = []
    for r in range(rows):
        for c in range(cols):
            if not flags[r][c] or (c, r) in seen:
                continue
            stack = [(c, r)]
            cells = []
            seen.add((c, r))
            while stack:
                x, y = stack.pop()
                cells.append((x, y))
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < cols and 0 <= ny < rows and flags[ny][nx] and (nx, ny) not in seen:
                        seen.add((nx, ny))
                        stack.append((nx, ny))
            result.append(cells)
    return result


def compare(a, b, threshold, cell, min_ratio, min_cells):
    diff = ImageChops.difference(a, b).convert("L").filter(ImageFilter.GaussianBlur(1.2))
    mask = diff.point(lambda v: 255 if v > threshold else 0)
    width, height = a.size
    cols = (width + cell - 1) // cell
    rows = (height + cell - 1) // cell
    small = mask.resize((cols, rows), Image.BOX)
    pixels = small.load()
    flags = [[(pixels[c, r] / 255.0) >= min_ratio for c in range(cols)] for r in range(rows)]
    regions = []
    for cells in regions_from_grid(flags, cols, rows):
        if len(cells) < min_cells:
            continue
        xs = [c for c, _ in cells]
        ys = [r for _, r in cells]
        box = (min(xs) * cell, min(ys) * cell, min(width, (max(xs) + 1) * cell), min(height, (max(ys) + 1) * cell))
        mean = sum(pixels[c, r] for c, r in cells) / (255.0 * len(cells))
        regions.append({"box": list(box), "cells": len(cells), "areaPx": (box[2] - box[0]) * (box[3] - box[1]), "meanDiff": round(mean, 3)})
    regions.sort(key=lambda item: -item["cells"])
    total = sum(1 for row in flags for flag in row)
    flagged = sum(1 for row in flags for flag in row if flag)
    return diff, mask, regions, flagged / float(total or 1)


def overlay(a, mask, regions):
    base = Image.blend(a, Image.new("RGB", a.size, (0, 0, 0)), 0.55)
    red = Image.new("RGB", a.size, (255, 40, 40))
    base.paste(red, (0, 0), mask.point(lambda v: 150 if v else 0))
    draw = ImageDraw.Draw(base)
    for index, region in enumerate(regions, start=1):
        draw.rectangle(region["box"], outline=(255, 220, 0), width=2)
        draw.text((region["box"][0] + 3, region["box"][1] + 2), str(index), fill=(255, 255, 0))
    return base


def main(argv=None):
    parser = argparse.ArgumentParser()
    parser.add_argument("first")
    parser.add_argument("second")
    parser.add_argument("--out", required=True)
    parser.add_argument("--json")
    parser.add_argument("--threshold", type=int, default=40)
    parser.add_argument("--cell", type=int, default=24)
    parser.add_argument("--min-ratio", type=float, default=0.25)
    parser.add_argument("--min-cells", type=int, default=2)
    parser.add_argument("--crop-first")
    parser.add_argument("--crop-second")
    parser.add_argument("--max-regions", type=int, default=0, help="fail when more regions than this (0 = any region fails)")
    args = parser.parse_args(argv)
    crop_a = tuple(int(v) for v in args.crop_first.split(",")) if args.crop_first else None
    crop_b = tuple(int(v) for v in args.crop_second.split(",")) if args.crop_second else None
    a = load(args.first, None, crop_a)
    b = load(args.second, a.size, crop_b)
    diff, mask, regions, ratio = compare(a, b, args.threshold, args.cell, args.min_ratio, args.min_cells)
    overlay(a, mask, regions).save(args.out)
    report = {"size": list(a.size), "threshold": args.threshold, "cell": args.cell, "flaggedCellRatio": round(ratio, 4), "regions": regions}
    if args.json:
        with open(args.json, "w", encoding="utf-8") as handle:
            json.dump(report, handle, indent=2)
    print("size %dx%d, %.1f%% of cells differ, %d regions" % (a.size[0], a.size[1], ratio * 100, len(regions)))
    for index, region in enumerate(regions[:20], start=1):
        print("  %2d box=%s cells=%d mean=%.2f" % (index, region["box"], region["cells"], region["meanDiff"]))
    return 1 if len(regions) > args.max_regions else 0


if __name__ == "__main__":
    sys.exit(main())

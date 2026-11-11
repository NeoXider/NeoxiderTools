"""Shrink package source assets without changing what Unity imports.

PNG: a source larger than the biggest maxTextureSize in its .meta is downscaled to that size,
which is what Unity imports anyway. The sprite pixels-per-unit, borders and sheet rects are scaled
by the same factor, so sprites keep their world size and slicing. Files whose sprites carry custom
outlines, physics shapes or meshes are skipped.

WAV: re-encoded to Ogg Vorbis (ffmpeg, quality 6) under the same .meta, so the GUID and every
reference survive. Unity re-encodes audio to Vorbis for players anyway.

Usage: python scripts/optimize_package_assets.py [--root DIR ...] [--dry-run] [--no-audio] [--recompress-kb N] [--min-psnr DB] [--cap NAME=PX]
"""
import argparse
import os
import re
import shutil
import subprocess
import sys

from PIL import Image

NUMBER = r"-?\d+(?:\.\d+)?(?:e-?\d+)?"


def fmt(value):
    text = f"{value:.4f}".rstrip("0").rstrip(".")
    return text if text not in ("", "-0") else "0"


def scale_fields(text, pattern, factor):
    def repl(match):
        return match.group(1) + fmt(float(match.group(2)) * factor)
    return re.sub(pattern, repl, text)


def scale_border(text, factor):
    def repl(match):
        values = [fmt(float(v) * factor) for v in match.groups()[1:]]
        return f"{match.group(1)}{{x: {values[0]}, y: {values[1]}, z: {values[2]}, w: {values[3]}}}"
    return re.sub(rf"((?:spriteBorder|border): )\{{x: ({NUMBER}), y: ({NUMBER}), z: ({NUMBER}), w: ({NUMBER})\}}",
                  repl, text)


def has_custom_geometry(meta):
    return re.search(r"\n\s+(outline|physicsShape|vertices|bones|edges|weights):\s*\n\s+- ", meta) is not None


def import_limit(meta):
    sizes = [int(v) for v in re.findall(r"maxTextureSize: (\d+)", meta)]
    return max(sizes) if sizes else 2048


def shrink_png(path, dry_run, cap=None):
    meta_path = path + ".meta"
    if not os.path.exists(meta_path):
        return 0, "no meta"
    with open(meta_path, encoding="utf-8", newline="") as handle:
        meta = handle.read()
    with Image.open(path) as image:
        width, height = image.size
        limit = min(import_limit(meta), cap) if cap else import_limit(meta)
        if max(width, height) <= limit * 1.05:
            return 0, None
        if has_custom_geometry(meta):
            return 0, "custom sprite geometry, kept"
        factor = limit / max(width, height)
        size = (max(1, round(width * factor)), max(1, round(height * factor)))
        if dry_run:
            return os.path.getsize(path), f"{width}x{height} -> {size[0]}x{size[1]} (dry run)"
        resized = image.convert("RGBA" if image.mode in ("P", "LA", "RGBA") else image.mode)
        resized = resized.resize(size, Image.LANCZOS)
        icc = image.info.get("icc_profile")
    before = os.path.getsize(path)
    resized.save(path, format="PNG", optimize=True, **({"icc_profile": icc} if icc else {}))
    meta = scale_fields(meta, rf"(spritePixelsToUnits: )({NUMBER})", factor)
    meta = scale_border(meta, factor)
    meta = scale_fields(meta, rf"(\n\s+(?:x|y|width|height): )({NUMBER})(?=\r?\n)", factor) \
        if "sprites:\n    - " in meta or "sprites:\r\n    - " in meta else meta
    with open(meta_path, "w", encoding="utf-8", newline="") as handle:
        handle.write(meta)
    return before - os.path.getsize(path), f"{width}x{height} -> {size[0]}x{size[1]}"


def psnr(first, second):
    from PIL import ImageChops, ImageStat
    diff = ImageStat.Stat(ImageChops.difference(first, second))
    mse = sum(value * value for value in diff.rms) / len(diff.rms)
    return 99.0 if mse == 0 else 10 * __import__("math").log10(255 * 255 / mse)


def is_plain_color_texture(meta):
    texture_type = re.search(r"textureType: (\d+)", meta)
    return texture_type is not None and texture_type.group(1) in ("0", "8") and "sRGBTexture: 1" in meta


def recompress_large_png(path, dry_run, min_kb, min_psnr=40.0):
    """Opaque art becomes JPEG; art with alpha becomes a 256-colour palette PNG when it stays faithful."""
    if os.path.getsize(path) < min_kb * 1024 or not os.path.exists(path + ".meta"):
        return 0, None
    with open(path + ".meta", encoding="utf-8") as handle:
        if not is_plain_color_texture(handle.read()):
            return 0, None
    before = os.path.getsize(path)
    with Image.open(path) as source:
        image = source.convert("RGBA")
    opaque = image.getchannel("A").getextrema()[0] == 255
    import io
    buffer = io.BytesIO()
    if opaque:
        candidate = image.convert("RGB")
        candidate.save(buffer, format="JPEG", quality=90, optimize=True, progressive=False, subsampling=0)
        decoded = Image.open(io.BytesIO(buffer.getvalue())).convert("RGB")
        score = psnr(candidate, decoded)
        target = path[:-4] + ".jpg"
    else:
        candidate = image.quantize(256, method=Image.Quantize.FASTOCTREE, kmeans=4, dither=Image.Dither.FLOYDSTEINBERG)
        candidate.save(buffer, format="PNG", optimize=True)
        decoded = Image.open(io.BytesIO(buffer.getvalue())).convert("RGBA")
        score = psnr(image, decoded)
        target = path
    size = buffer.tell()
    if size > before * 0.6 or score < min_psnr:
        return 0, f"kept (psnr {score:.1f}, {size * 100 // before}%)"
    note = f"{'jpg' if opaque else 'palette'} psnr {score:.1f}, {size * 100 // before}%"
    if dry_run:
        return before - size, note + " (dry run)"
    with open(target, "wb") as handle:
        handle.write(buffer.getvalue())
    if target != path:
        shutil.move(path + ".meta", target + ".meta")
        os.remove(path)
    return before - size, note


def wav_to_ogg(path, dry_run):
    target = path[:-4] + ".ogg"
    if os.path.exists(target) or not os.path.exists(path + ".meta"):
        return 0, "ogg exists or no meta"
    before = os.path.getsize(path)
    if dry_run:
        return before, "-> .ogg (dry run)"
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", path, "-c:a", "libvorbis", "-q:a", "6", target],
                   check=True)
    shutil.move(path + ".meta", target + ".meta")
    os.remove(path)
    return before - os.path.getsize(target), "-> .ogg"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", nargs="+", default=["Assets/Neoxider"])
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--no-audio", action="store_true")
    parser.add_argument("--recompress-kb", type=int, default=0,
                        help="also recompress PNGs at least this large (0 = off); see recompress_large_png")
    parser.add_argument("--min-psnr", type=float, default=40.0,
                        help="lowest PSNR (dB) a recompressed PNG may have; 38 still reads clean on UI art")
    parser.add_argument("--cap", action="append", default=[], metavar="FILE=PX",
                        help="downscale this file name to at most PX even below its import size")
    args = parser.parse_args()
    caps = {item.rsplit("=", 1)[0]: int(item.rsplit("=", 1)[1]) for item in args.cap}
    saved = 0
    walk = [entry for root in args.root for entry in
            ([(os.path.dirname(root), [], [os.path.basename(root)])] if os.path.isfile(root) else os.walk(root))]
    for folder, _, files in walk:
        for name in sorted(files):
            path = os.path.join(folder, name).replace("\\", "/")
            lower = name.lower()
            if lower.endswith(".png"):
                delta, note = shrink_png(path, args.dry_run, caps.get(name))
                if args.recompress_kb and not args.dry_run or args.recompress_kb and not note:
                    extra, extra_note = recompress_large_png(path, args.dry_run, args.recompress_kb, args.min_psnr)
                    delta += extra
                    note = "; ".join(n for n in (note, extra_note) if n) or None
            elif lower.endswith(".wav") and not args.no_audio:
                delta, note = wav_to_ogg(path, args.dry_run)
            else:
                continue
            if note:
                print(f"{delta / 1024:8.0f} KB  {path}  {note}")
            saved += delta
    print(f"saved {saved / 1048576:.2f} MB")
    return 0


if __name__ == "__main__":
    sys.exit(main())

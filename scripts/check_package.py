#!/usr/bin/env python3
"""Validate package text, manifests, sample layout and Unity metadata without Unity.

Run from any directory: python scripts/check_package.py
Runtime behaviour and compilation still require the Unity EditMode/PlayMode suites.
"""

from __future__ import annotations

import json
import re
import subprocess
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
PACKAGE_ROOT = REPO_ROOT / "Assets" / "Neoxider"
TEXT_EXTENSIONS = {".cs", ".md", ".json", ".asmdef", ".asmref", ".txt", ".uss", ".uxml", ".meta"}
JSON_EXTENSIONS = {".json", ".asmdef", ".asmref"}
GUID_PATTERN = re.compile(r"^guid: ([0-9a-fA-F]{32})$", re.MULTILINE)
CONFLICT_PATTERN = re.compile(r"^(?:<<<<<<< |>>>>>>> )", re.MULTILINE)
ID_PATTERN = re.compile(r"\.(?:GetInstanceID\s*\(|objectReferenceInstanceIDValue\b)")
ID_GATEWAYS = {
    "Scripts/Extensions/NeoDiagnostics.cs",
    "Editor/PropertyAttribute/Internal/NeoComponentHealth.cs",
}


def main() -> int:
    errors: list[str] = []
    texts: dict[Path, str] = {}
    guids: dict[str, Path] = {}
    assemblies: dict[str, Path] = {}
    files = sorted(path for path in PACKAGE_ROOT.rglob("*") if path.is_file())

    for path in files:
        relative = path.relative_to(PACKAGE_ROOT).as_posix()
        if path.suffix.lower() in TEXT_EXTENSIONS:
            try:
                text = path.read_text(encoding="utf-8-sig")
            except UnicodeDecodeError:
                errors.append(f"{relative}: invalid UTF-8")
                continue
            texts[path] = text
            if CONFLICT_PATTERN.search(text):
                errors.append(f"{relative}: unresolved merge conflict")
            if path.suffix.lower() in JSON_EXTENSIONS:
                try:
                    data = json.loads(text)
                except json.JSONDecodeError as exc:
                    errors.append(f"{relative}: invalid JSON: {exc}")
                    continue
                if path.suffix == ".asmdef":
                    name = data.get("name") if isinstance(data, dict) else None
                    if not isinstance(name, str) or not name:
                        errors.append(f"{relative}: missing assembly name")
                    elif name in assemblies:
                        errors.append(f"{relative}: duplicate assembly name {name}")
                    else:
                        assemblies[name] = path

            if path.suffix == ".meta":
                match = GUID_PATTERN.search(text)
                if match is None:
                    errors.append(f"{relative}: missing Unity GUID")
                else:
                    guid = match.group(1).lower()
                    if guid in guids:
                        errors.append(f"{relative}: duplicate GUID from {guids[guid].relative_to(PACKAGE_ROOT)}")
                    guids[guid] = path
                asset = path.with_suffix("")
                if not asset.exists() and "folderAsset: yes" not in text:
                    errors.append(f"{relative}: metadata has no asset")

            if (path.suffix == ".cs" and relative not in ID_GATEWAYS
                    and relative.startswith(("Scripts/", "Editor/"))):
                # Comments can describe migration APIs; only inspect executable lines.
                code = re.sub(r"/\*.*?\*/|//[^\n]*", "", text, flags=re.DOTALL)
                if ID_PATTERN.search(code):
                    errors.append(f"{relative}: use the shared StableId compatibility gateway")

        if (path.suffix != ".meta" and not path.name.startswith(".")
                and not Path(str(path) + ".meta").is_file()):
            errors.append(f"{relative}: missing Unity metadata")

    # Share the exact encoding markers enforced by the package's EditMode contract.
    principles = PACKAGE_ROOT / "Tests" / "Edit" / "Core" / "ModulePrinciplesTests.cs"
    principles_text = texts.get(principles, "")
    marker_block = re.search(r"MojibakeMarkers\s*=\s*\{(.*?)\};", principles_text, re.DOTALL)
    if marker_block is None:
        errors.append("ModulePrinciplesTests: encoding marker contract is missing")
    else:
        markers = [bytes(value, "ascii").decode("unicode_escape")
                   for value in re.findall(r'"((?:\\u[0-9a-fA-F]{4})+)"', marker_block.group(1))]
        if not markers:
            errors.append("ModulePrinciplesTests: encoding marker contract is empty")
        for path, text in texts.items():
            if any(marker in text for marker in markers):
                errors.append(f"{path.relative_to(PACKAGE_ROOT)}: known encoding corruption marker")

    manifest_path = PACKAGE_ROOT / "package.json"
    try:
        manifest = json.loads(texts.get(manifest_path, ""))
        if manifest.get("name") != "com.neoxider.tools":
            errors.append("package.json: unexpected package name")
        if not re.fullmatch(r"\d+\.\d+\.\d+(?:-[\w.-]+)?", manifest.get("version", "")):
            errors.append("package.json: invalid version")
        sample_roots = [root for root in ("Samples", "Samples~") if (PACKAGE_ROOT / root).is_dir()]
        if len(sample_roots) != 1:
            errors.append("package.json: expected exactly one active Samples or Samples~ root")
        for sample in manifest.get("samples", []):
            sample_path = sample.get("path", "")
            target = (PACKAGE_ROOT / sample_path).resolve()
            if (not sample_path or not target.is_relative_to(PACKAGE_ROOT)
                    or not target.is_dir()):
                errors.append(f"package.json: sample directory does not exist inside the package: {sample_path}")
            elif sample_roots and sample_path.split("/")[0] != sample_roots[0]:
                errors.append(f"package.json: sample path does not match the active layout: {sample_path}")
    except (json.JSONDecodeError, AttributeError, TypeError):
        errors.append("package.json: invalid package manifest")

    diff = subprocess.run(["git", "diff", "--check"], cwd=REPO_ROOT, capture_output=True, text=True)
    if diff.returncode:
        errors.append(diff.stdout.strip() or diff.stderr.strip() or "git diff --check failed")

    print(f"Checked {len(files)} package files, {len(assemblies)} assemblies and {len(guids)} GUIDs.")
    for error in errors:
        print(f"ERROR: {error}")
    print(f"Package validation: {'FAILED' if errors else 'PASSED'} ({len(errors)} errors)")
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())

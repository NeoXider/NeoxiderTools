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
from urllib.parse import unquote, urlsplit

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

ENTRY_DOCUMENTS = (
    "README.md", "Assets/Neoxider/README.md", "Assets/Neoxider/Docs/README.md",
    "Assets/Neoxider/Docs/GettingStarted.md", "Assets/Neoxider/Docs/PackageCompatibility.md",
    "Assets/Neoxider/Docs/Samples.md", "Assets/Neoxider/Docs/Migration11.md",
)
CANONICAL_TOPICS = {
    "Abilities/README.md": ("Abilities/Editor.md",),
    "Audio/README.md": ("Audio/Editor.md",),
    "Cards/README.md": ("Cards/Editor.md",),
    "Core/LevelCurveDefinition.md": ("Core/Level/Data/CurveEvaluation.md",),
    "Extensions/README.md": ("Extensions/NumericEdgeCases.md", "Extensions/LifecycleAndOwnership.md"),
    "Network/README.md": ("Network/Contracts.md",),
    "Rpg/README.md": ("Rpg/ConditionBridge.md", "Rpg/Combat/README.md", "Rpg/Network.md"),
    "Shop/README.md": ("Shop/Bridges.md",),
    "StateMachine/README.md": ("StateMachine/Editor.md",),
    "Tools/InteractableObject/README.md": ("Tools/InteractableObject/Core/README.md",),
    "UI/UIMeshRig.md": ("UI/Editor.md",),
    "UI/HtmlToUitk.md": ("UI/HtmlToUitkEditor.md", "UI/HtmlToUitkTesting.md"),
    "README.md": ("Migration11.md", "Testing.md", "PlayModeTesting.md"),
}


def prose(text: str) -> str:
    """Exclude fenced examples from the small entry-document contract."""
    return re.sub(r"(?ms)^(```+|~~~+)[^\n]*\n.*?^\1[^\n]*$", "", text)


def document_links(text: str) -> list[str]:
    content = prose(text)
    markdown = re.findall(r"!?\[[^\]]*\]\((<[^>]+>|[^\s)]+)(?:\s+\"[^\"]*\")?\)", content)
    html = re.findall(r"(?:href|src)\s*=\s*[\"']([^\"']+)[\"']", content, re.IGNORECASE)
    return [value[1:-1] if value.startswith("<") and value.endswith(">") else value
            for value in markdown + html]


def heading_anchors(text: str) -> set[str]:
    anchors = set(re.findall(r"<(?:a|[a-z][a-z0-9]*)\b[^>]*(?:id|name)=[\"']([^\"']+)",
                             prose(text), re.IGNORECASE))
    duplicates: dict[str, int] = {}
    for heading in re.findall(r"^#{1,6}\s+(.+?)(?:\s+#+)?\s*$", prose(text), re.MULTILINE):
        plain = re.sub(r"<[^>]+>", "", heading).replace("`", "")
        slug = re.sub(r"[^\w\s-]", "", plain.lower()).replace(" ", "-")
        duplicate = duplicates.get(slug, 0)
        duplicates[slug] = duplicate + 1
        anchors.add(slug if duplicate == 0 else f"{slug}-{duplicate}")
    return anchors


def check_entry_documents(manifest: dict, errors: list[str]) -> None:
    """Check current entry points and module navigation, without fetching remote URLs."""
    docs_root = PACKAGE_ROOT / "Docs"
    version = manifest.get("version", "")
    health_path = PACKAGE_ROOT / "Editor/PackageHealthCheck.cs"
    health = health_path.read_text(encoding="utf-8-sig")
    mentions = re.findall(r'new VersionMention\(\s*(Root\s*\+\s*)?"([^"]+)",\s*"([^"]+)"', health)
    if not mentions:
        errors.append("PackageHealthCheck: version-mention contract is missing")
    for prefix, relative, template in mentions:
        path = PACKAGE_ROOT / relative.lstrip("/") if prefix else REPO_ROOT / relative
        if not path.is_file() or template.replace("{0}", version) not in path.read_text(encoding="utf-8-sig"):
            errors.append(f"{path.relative_to(REPO_ROOT)}: current version declaration differs from package.json")

    for relative in ENTRY_DOCUMENTS:
        path = REPO_ROOT / relative
        if not path.is_file():
            errors.append(f"{relative}: missing canonical entry document")
            continue
        text = path.read_text(encoding="utf-8-sig")
        if path.name != "Migration11.md":
            for imported_version in re.findall(r"Assets/Samples/NeoxiderTools/(\d+\.\d+\.\d+)/", text):
                if imported_version != version:
                    errors.append(f"{relative}: current imported-sample example uses {imported_version}, expected {version}")
        for link in document_links(text):
            parsed = urlsplit(link)
            if parsed.scheme or parsed.netloc:
                continue
            target = (path.parent / unquote(parsed.path)).resolve() if parsed.path else path.resolve()
            if path.is_relative_to(PACKAGE_ROOT) and not target.is_relative_to(PACKAGE_ROOT):
                errors.append(f"{relative}: local link leaves the UPM payload: {link}")
                continue
            if not target.exists():
                errors.append(f"{relative}: missing local link target: {link}")
            elif parsed.fragment and target.suffix.lower() == ".md":
                fragment = unquote(parsed.fragment)
                if fragment not in heading_anchors(target.read_text(encoding="utf-8-sig")):
                    errors.append(f"{relative}: missing Markdown heading anchor: {link}")

    sample_guide = docs_root / "Samples.md"
    sample_text = sample_guide.read_text(encoding="utf-8-sig") if sample_guide.is_file() else ""
    for sample in manifest.get("samples", []):
        if sample.get("displayName", "") not in sample_text or sample.get("path", "") not in sample_text:
            errors.append(f"Docs/Samples.md: missing manifest sample name/path for {sample.get('displayName')}")
    for parent, topics in CANONICAL_TOPICS.items():
        path = docs_root / parent
        if not path.is_file():
            errors.append(f"Docs/{parent}: missing canonical module entry")
            continue
        targets = set()
        for link in document_links(path.read_text(encoding="utf-8-sig")):
            parsed = urlsplit(link)
            if not parsed.scheme and not parsed.netloc:
                targets.add((path.parent / unquote(parsed.path)).resolve())
        for topic in topics:
            if (docs_root / topic).resolve() not in targets:
                errors.append(f"Docs/{parent}: missing canonical topic link to {topic}")


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
                matches = list(GUID_PATTERN.finditer(text))
                if len(matches) > 1:
                    errors.append(f"{relative}: metadata contains multiple GUID records")
                if len(re.findall(r"^fileFormatVersion:[^\n]*$", text, re.MULTILINE)) != 1:
                    errors.append(f"{relative}: metadata must contain exactly one fileFormatVersion record")
                match = matches[0] if matches else None
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
                and not any(part.endswith("~") for part in path.relative_to(PACKAGE_ROOT).parts[:-1])
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
        check_entry_documents(manifest, errors)
    except (json.JSONDecodeError, AttributeError, TypeError):
        errors.append("package.json: invalid package manifest")

    for arguments in (["git", "diff", "--check"], ["git", "diff", "--cached", "--check"]):
        diff = subprocess.run(arguments, cwd=REPO_ROOT, capture_output=True, text=True)
        if diff.returncode:
            errors.append(diff.stdout.strip() or diff.stderr.strip() or "git diff whitespace check failed")

    print(f"Checked {len(files)} package files, {len(assemblies)} assemblies and {len(guids)} GUIDs.")
    for error in errors:
        print(f"ERROR: {error}")
    print(f"Package validation: {'FAILED' if errors else 'PASSED'} ({len(errors)} errors)")
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())

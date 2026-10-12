"""Runtime hot-path audit for the NeoxiderTools package.

Finds non-obvious per-frame cost in Assets/Neoxider/Scripts: physics queries,
scene searches, component lookups, runtime Instantiate/Destroy, allocations and
string building reachable from Update / LateUpdate / FixedUpdate.

Why a script instead of grep: the expensive call usually does NOT sit in the
body of Update. InteractiveObject.Update calls UpdateMouseTargetRaycast ->
TryGetCurrentMouseHit, and only the last one calls Physics.RaycastNonAlloc.
A direct-body grep reports that file as clean. This tool builds a per-file
method call graph and walks it from every frame method.

Usage:
    python scripts/audit_runtime_hotpaths.py [--root <Scripts dir>] [--json <out>]

Exits 0 when the scan completes, 1 only on a hard failure (bad root, IO error).
The exit code is deliberately independent of how many findings there are:
this is a reporting tool, not a lint gate.

Findings are heuristics, not profiler measurements. A reachable call may run
only in response to input, and some overloads reuse caller-owned buffers.
Confirm allocation and frequency with the Unity Profiler before optimizing.
"""

from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys
from collections import defaultdict

REPO_ROOT = pathlib.Path(__file__).resolve().parent.parent
DEFAULT_ROOT = REPO_ROOT / "Assets" / "Neoxider" / "Scripts"

FRAME_METHODS = (
    "Update",
    "LateUpdate",
    "FixedUpdate",
    "OnGUI",
    "OnRenderObject",
    "OnPostRender",
    "OnPreRender",
    "OnPreCull",
    "OnWillRenderObject",
)

# label -> (weight, allocates_managed, pattern)
# `allocates_managed` flags a potential managed allocation, not a measurement.
# Physics2D.OverlapCircle/OverlapBox/OverlapPoint return a single Collider2D and
# do NOT allocate; the allocating variants are the *All suffixed ones.
SIGNALS: tuple[tuple[str, int, bool, str], ...] = (
    (
        "alloc_ray",
        5,
        True,
        r"Physics\.\s*(?:RaycastAll|OverlapSphere(?!NonAlloc)|OverlapBox(?!NonAlloc)"
        r"|OverlapCapsule(?!NonAlloc)|SphereCastAll|CapsuleCastAll|BoxCastAll)\s*\("
        r"|Physics2D\.\s*(?:RaycastAll|OverlapCircleAll|OverlapBoxAll|OverlapPointAll"
        r"|CircleCastAll|BoxCastAll|CapsuleCastAll|GetRayIntersectionAll)\s*\(",
    ),
    # Unity spells the singular form FindGameObjectWithTag; both are matched so a
    # typo in either direction cannot silently hide a full-scene search.
    ("scene_search", 5, True,
     r"FindObject[s]?OfType|FindAnyObjectByType|FindFirstObjectByType"
     r"|FindGameObject[s]?WithTag|FindWithTag|GameObject\s*\.\s*Find\s*\("),
    ("instantiate", 5, True, r"\bInstantiate\s*(?:<[^>]*>)?\s*\("),
    ("resources_load", 5, True, r"Resources\s*\.\s*Load\s*\("),
    ("pixel_readback", 5, True, r"\.GetPixels\s*\(|\.SetPixels\s*\(|\.ReadPixels\s*\("),
    ("material_clone", 4, True, r"\.material\b(?!\s*=)"),
    ("get_components_many", 4, True, r"GetComponentsInChildren\s*<|GetComponentsInParent\s*<"),
    ("camera_main", 4, False, r"Camera\s*\.\s*main"),
    ("destroy", 4, False, r"\bDestroy\s*(?:Immediate)?\s*\("),
    ("get_component", 3, False, r"\.GetComponent(?:InChildren|InParent)?\s*(?:<[^>]*>)?\s*\("),
    ("new_array", 3, True, r"\bnew\s+(?:[\w<>.,]+\s*)?\[[^\]\n]*\]"),
    ("to_list_array", 3, True, r"\.(?:ToList|ToArray|ToHashSet|ToDictionary)\s*\("),
    ("linq", 3, True,
     r"\.(?:Where|Select|OrderBy|OrderByDescending|ThenBy|FirstOrDefault|SingleOrDefault"
     r"|ToList|ToArray|ToHashSet|ToDictionary|GroupBy|SelectMany|Aggregate|Last|OfType|Cast)\s*(?:\(|<)"),
    ("nonalloc_ray", 2, False,
     r"Physics\.\s*(?:Raycast|OverlapSphere|OverlapBox|OverlapCapsule|OverlapPoint|OverlapSphereNonAlloc)\s*\("
     r"|Physics2D\.\s*(?:Raycast|OverlapCircle|OverlapBox|OverlapPoint|GetRayIntersectionNonAlloc)\s*\("),
    ("new_collection", 2, True, r"\bnew\s+(?:List|Dictionary|HashSet|Queue|Stack|LinkedList)\s*<"),
    ("string_build", 2, True, r"\$\"|string\s*\.\s*(?:Format|Concat|Join)\s*\("),
    ("wait_alloc", 3, True, r"new\s+WaitForSeconds|new\s+WaitUntil|new\s+WaitWhile"),
    ("animator_get", 2, False, r"animator\s*\.\s*Get(?:Bool|Float|Integer|CurrentAnimatorStateInfo)"),
)

COMPILED = tuple((label, weight, alloc, re.compile(pattern))
                 for label, weight, alloc, pattern in SIGNALS)

# Tokens that are calls but must not be walked into as project methods.
KEYWORDS = frozenset("""
if for foreach while switch catch return new typeof nameof lock using fixed
get set sizeof checked unchecked do else stackalloc
""".split())

EDITOR_PATH = re.compile(r"\\(?:Editor|EditorTests|Tests|Play|Edit)\\|/Editor/")

METHOD_START = re.compile(
    r"^\s*(?:\[[^\]]*\]\s*)*"
    r"(?:(?:public|private|protected|internal|static|sealed|override|virtual|new"
    r"|abstract|partial|extern|unsafe)\s+)*"
    r"(?:[\w<>\[\],\.\?]+\s+)+"
    r"([A-Za-z_]\w*)\s*\("
)

CALL_TOKEN = re.compile(r"\b([A-Za-z_]\w*)\s*\(")


def strip_comments_and_strings(text: str) -> str:
    """Blank out comments and string literals so the scanners cannot match prose."""
    out: list[str] = []
    i = 0
    n = len(text)
    quote: str | None = None
    in_line_comment = False
    in_block_comment = False
    while i < n:
        ch = text[i]
        nxt = text[i + 1] if i + 1 < n else ""
        if in_line_comment:
            if ch == "\n":
                in_line_comment = False
                out.append("\n")
            i += 1
            continue
        if in_block_comment:
            if ch == "*" and nxt == "/":
                in_block_comment = False
                i += 2
                continue
            if ch == "\n":
                out.append("\n")
            i += 1
            continue
        if quote is not None:
            if ch == "\\":
                i += 2
                continue
            if ch == quote:
                quote = None
            i += 1
            continue
        if ch == "/" and nxt == "/":
            in_line_comment = True
            i += 2
            continue
        if ch == "/" and nxt == "*":
            in_block_comment = True
            i += 2
            continue
        if ch in "\"'":
            quote = ch
            i += 1
            continue
        out.append(ch)
        i += 1
    return "".join(out)


def find_method_blocks(lines: list[str]) -> list[tuple[str, int, int]]:
    """Return (name, start_index, end_index_exclusive) for every method-like block."""
    blocks: list[tuple[str, int, int]] = []
    for i, line in enumerate(lines):
        match = METHOD_START.match(line)
        if not match:
            continue
        depth = 0
        started = False
        j = i
        while j < len(lines):
            for ch in lines[j]:
                if ch == "{":
                    depth += 1
                    started = True
                elif ch == "}":
                    depth -= 1
            if started and depth <= 0:
                break
            if not started and ";" in lines[j] and "{" not in lines[j]:
                break
            j += 1
        blocks.append((match.group(1), i, min(j + 1, len(lines))))
    return blocks


def read_unit(path: pathlib.Path) -> dict:
    raw = path.read_text(encoding="utf-8", errors="replace")
    clean = strip_comments_and_strings(raw)
    clean_lines = clean.splitlines()
    raw_lines = raw.splitlines()
    return {"raw": raw_lines, "clean": clean_lines, "blocks": find_method_blocks(clean_lines)}


def is_editor_path(relative: str) -> bool:
    return bool(EDITOR_PATH.search(relative.replace("/", "\\")))


def scan(root: pathlib.Path, repo: pathlib.Path) -> list[dict]:
    units: dict[pathlib.Path, dict] = {}
    for path in sorted(root.rglob("*.cs")):
        units[path] = read_unit(path)

    # per-file call graph, restricted to methods actually declared in that file
    call_graph: dict[tuple[pathlib.Path, str], set[str]] = {}
    for path, unit in units.items():
        for name, start, end in unit["blocks"]:
            body = "\n".join(unit["clean"][start:end])
            names = {
                m.group(1)
                for m in CALL_TOKEN.finditer(body)
                if m.group(1) not in KEYWORDS
            }
            call_graph[(path, name)] = names

    rows: list[dict] = []
    for path, unit in units.items():
        relative = str(path.relative_to(repo))
        if is_editor_path(relative):
            continue
        frame_blocks = [b for b in unit["blocks"] if b[0] in FRAME_METHODS]
        if not frame_blocks:
            continue
        if any(
            re.search(r"#if\s+UNITY_EDITOR", "\n".join(unit["clean"][s:e]))
            for _, s, e in frame_blocks
        ):
            continue

        for frame_name, _, _ in frame_blocks:
            seen = {frame_name}
            frontier = [(frame_name, 0)]
            best: dict[tuple[str, int], dict] = {}

            while frontier:
                name, hop = frontier.pop()
                for block_name, start, end in unit["blocks"]:
                    if block_name != name:
                        continue
                    for index in range(start, end):
                        line = unit["clean"][index]
                        for label, weight, alloc, rx in COMPILED:
                            if not rx.search(line):
                                continue
                            key = (label, index + 1)
                            if key not in best or hop < best[key]["hop"]:
                                best[key] = {
                                    "file": relative,
                                    "frame": frame_name,
                                    "signal": label,
                                    "weight": weight,
                                    "allocates": alloc,
                                    "line": index + 1,
                                    "code": unit["raw"][index].strip(),
                                    "via": name,
                                    "hop": hop,
                                }
                if hop < 2:
                    for callee in call_graph.get((path, name), ()):
                        if callee in seen or callee in FRAME_METHODS:
                            continue
                        if callee in ("Invoke", "ToString", "Equals", "GetHashCode"):
                            continue
                        seen.add(callee)
                        frontier.append((callee, hop + 1))

            if not best:
                continue
            findings = sorted(best.values(), key=lambda f: (f["signal"], f["line"]))
            rows.append({
                "file": relative,
                "frame": frame_name,
                "severity": sum(f["weight"] for f in findings)
                + (2 if any(f["allocates"] for f in findings) else 0),
                "allocates": any(f["allocates"] for f in findings),
                "findings": findings,
            })

    rows.sort(key=lambda r: (-r["severity"], r["file"]))
    return rows


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=pathlib.Path, default=DEFAULT_ROOT)
    parser.add_argument("--json", type=pathlib.Path, default=None)
    parser.add_argument("--min-severity", type=int, default=2)
    args = parser.parse_args()

    root: pathlib.Path = args.root.resolve()
    if not root.is_dir():
        print(f"error: scripts root not found: {root}", file=sys.stderr)
        return 1

    try:
        rows = scan(root, REPO_ROOT)
    except OSError as exc:
        print(f"error: failed to scan {root}: {exc}", file=sys.stderr)
        return 1

    reported = [r for r in rows if r["severity"] >= args.min_severity]
    total_findings = sum(len(r["findings"]) for r in reported)
    allocating = [r for r in reported if r["allocates"]]

    print(f"scanned      : {len(list(root.rglob('*.cs')))} .cs files under {root}")
    print(f"frame methods: {sum(1 for r in rows)} with reachable per-frame cost")
    print(f"reported     : {len(reported)} components, {total_findings} sites"
          f" (severity >= {args.min_severity})")
    print(f"allocating   : {len(allocating)} components reach a potential managed allocation")
    print()

    for row in reported:
        flag = "POSSIBLE ALLOC" if row["allocates"] else "no alloc signal"
        print(f"[sev {row['severity']:>3}] {row['file']} :: {row['frame']}()  ({flag})")
        grouped: dict[str, list[dict]] = defaultdict(list)
        for finding in row["findings"]:
            grouped[finding["signal"]].append(finding)
        for label in sorted(grouped, key=lambda k: -grouped[k][0]["weight"]):
            sites = grouped[label]
            alloc = "alloc" if sites[0]["allocates"] else "no-alloc"
            lines = ", ".join(str(s["line"]) for s in sites[:8])
            print(f"    {label} (w{sites[0]['weight']}, {alloc}) x{len(sites)}: {lines}")
            for site in sites[:2]:
                print(f"        L{site['line']}: {site['code'][:110]}"
                      f"   [via {site['via']} hop{site['hop']}]")
        print()

    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(
            json.dumps(rows, ensure_ascii=False, indent=1), encoding="utf-8"
        )
        print(f"json written: {args.json}")

    return 0


if __name__ == "__main__":
    sys.exit(main())

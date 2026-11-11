"""Build the NeoxiderTools distributions from a committed revision (never from the working tree).

  upm            Write the package (Assets/Neoxider without Tests/ and Images/) as the root of the
                 `upm` branch: one new commit on top of the previous one. Unity installs it with
                 https://github.com/NeoXider/NeoxiderTools.git#upm and fetches only that snapshot
                 (`git fetch --depth=1`), so the download is the package alone.
  unitypackages  Write NeoxiderTools.v<ver>.unitypackage (the same package files) and one
                 .unitypackage per demo tree under Assets/NeoxiderDemos, plus SHA256SUMS.txt.

Usage:
  python scripts/build_distribution.py upm [--ref HEAD] [--branch upm]
  python scripts/build_distribution.py unitypackages [--ref HEAD] [--out Build/Release]
"""
import argparse
import gzip
import hashlib
import io
import json
import os
import re
import subprocess
import sys
import tarfile
import tempfile

PACKAGE_ROOT = "Assets/Neoxider"
DEMOS_ROOT = "Assets/NeoxiderDemos"
PACKAGE_EXCLUDES = ("Tests", "Images")
DEMO_ARCHIVES = {
    "Demo": "NeoxiderTools.Demo",
    "NeoxiderPages": "NeoxiderPages",
    "CharacterMovementFundamentals": "NeoxiderTools.CharacterMovementDemo",
}


def git(*args, data=None, env=None):
    result = subprocess.run(["git", *args], input=data, capture_output=True, check=True,
                            env={**os.environ, **(env or {})})
    return result.stdout


def tree_entries(ref, root):
    """(mode, sha, path) for every blob under root at ref."""
    out = git("ls-tree", "-r", "-z", "--full-tree", ref, "--", root)
    entries = []
    for record in out.split(b"\0"):
        if not record:
            continue
        meta, path = record.split(b"\t", 1)
        mode, kind, sha = meta.decode().split()
        if kind == "blob":
            entries.append((mode, sha, path.decode("utf-8")))
    return entries


def package_entries(ref):
    prefix = PACKAGE_ROOT + "/"
    keep = []
    for mode, sha, path in tree_entries(ref, PACKAGE_ROOT):
        relative = path[len(prefix):]
        top = relative.split("/", 1)[0]
        if top in PACKAGE_EXCLUDES or top in (name + ".meta" for name in PACKAGE_EXCLUDES):
            continue
        keep.append((mode, sha, path))
    return keep


def package_version(ref):
    return json.loads(git("show", f"{ref}:{PACKAGE_ROOT}/package.json"))["version"]


def pack_size(commit):
    """Bytes of the pack a depth-1 fetch of this commit receives (commit, trees and blobs, delta-compressed)."""
    listing = git("rev-list", "--objects", "--no-walk", commit)
    return len(git("pack-objects", "--stdout", "-q", data=listing))


def build_upm(ref, branch):
    version = package_version(ref)
    source = git("rev-parse", "--short", ref).decode().strip()
    prefix = len(PACKAGE_ROOT) + 1
    with tempfile.TemporaryDirectory() as folder:
        env = {"GIT_INDEX_FILE": os.path.join(folder, "index")}
        lines = "".join(f"{mode} {sha}\t{path[prefix:]}\n" for mode, sha, path in package_entries(ref))
        git("update-index", "--add", "--index-info", data=lines.encode("utf-8"), env=env)
        tree = git("write-tree", env=env).decode().strip()
    parent = subprocess.run(["git", "rev-parse", "-q", "--verify", f"refs/heads/{branch}"],
                            capture_output=True, text=True).stdout.strip()
    if parent and git("rev-parse", f"{parent}^{{tree}}").decode().strip() == tree:
        print(f"{branch} already holds this package ({parent[:8]})")
        return parent
    args = ["commit-tree", tree, "-m", f"upm: {version} (from {source})"]
    if parent:
        args[2:2] = ["-p", parent]
    commit = git(*args).decode().strip()
    git("update-ref", f"refs/heads/{branch}", commit)
    print(f"{branch} -> {commit[:8]}  version {version}; depth-1 fetch {pack_size(commit) / 1048576:.2f} MB")
    return commit


class BlobReader:
    """One `git cat-file --batch` process for every blob of an archive instead of a process per file."""

    def __init__(self):
        self._process = subprocess.Popen(["git", "cat-file", "--batch"], stdin=subprocess.PIPE,
                                         stdout=subprocess.PIPE)

    def read(self, sha):
        self._process.stdin.write(sha.encode() + b"\n")
        self._process.stdin.flush()
        header = self._process.stdout.readline().split()
        size = int(header[2])
        data = self._process.stdout.read(size)
        self._process.stdout.read(1)
        return data

    def close(self):
        self._process.stdin.close()
        self._process.wait()


def guid_of(meta_bytes):
    match = re.search(rb"^guid: ([0-9a-f]{32})", meta_bytes, re.M)
    return match.group(1).decode() if match else None


def write_unitypackage(path, entries):
    """entries: (asset path, asset sha or None for a folder, meta sha)."""
    buffer = io.BytesIO()
    blobs = BlobReader()
    with tarfile.open(fileobj=buffer, mode="w", format=tarfile.GNU_FORMAT) as archive:
        def add(name, payload):
            info = tarfile.TarInfo(name)
            info.size = len(payload)
            info.mode = 0o644
            archive.addfile(info, io.BytesIO(payload))
        for asset_path, asset_sha, meta_sha in sorted(entries):
            meta = blobs.read(meta_sha)
            guid = guid_of(meta)
            if guid is None:
                raise SystemExit(f"no guid in {asset_path}.meta")
            if asset_sha:
                add(f"{guid}/asset", blobs.read(asset_sha))
            add(f"{guid}/asset.meta", meta)
            add(f"{guid}/pathname", asset_path.encode("utf-8"))
    blobs.close()
    with open(path, "wb") as handle:
        handle.write(gzip.compress(buffer.getvalue(), compresslevel=9, mtime=0))


def unity_entries(blobs):
    """Pairs each asset (file or folder) with its .meta; folders come from parent paths."""
    files = {path: sha for _, sha, path in blobs}
    folders = {path.rsplit("/", depth)[0] for path in files for depth in range(1, path.count("/") + 1)}
    entries = []
    for path, sha in files.items():
        if not path.endswith(".meta"):
            continue
        asset = path[:-5]
        if asset in files:
            entries.append((asset, files[asset], sha))
        elif asset in folders:
            entries.append((asset, None, sha))
    missing = [p for p in files if not p.endswith(".meta") and p + ".meta" not in files]
    if missing:
        print(f"  warning: {len(missing)} files without .meta skipped, e.g. {missing[:3]}")
    return entries


def build_unitypackages(ref, out):
    version = package_version(ref)
    os.makedirs(out, exist_ok=True)
    root_meta = tree_entries(ref, "Assets")
    root_metas = {path: sha for _, sha, path in root_meta if path.count("/") == 1 and path.endswith(".meta")}
    outputs = []

    core = package_entries(ref) + [("100644", root_metas[PACKAGE_ROOT + ".meta"], PACKAGE_ROOT + ".meta")]
    name = os.path.join(out, f"NeoxiderTools.v{version}.unitypackage")
    write_unitypackage(name, unity_entries(core))
    outputs.append(name)

    for folder, archive in DEMO_ARCHIVES.items():
        blobs = tree_entries(ref, f"{DEMOS_ROOT}/{folder}")
        blobs += [e for e in tree_entries(ref, DEMOS_ROOT) if e[2] == f"{DEMOS_ROOT}/{folder}.meta"]
        blobs.append(("100644", root_metas[DEMOS_ROOT + ".meta"], DEMOS_ROOT + ".meta"))
        name = os.path.join(out, f"{archive}.v{version}.unitypackage")
        write_unitypackage(name, unity_entries(blobs))
        outputs.append(name)

    with open(os.path.join(out, "SHA256SUMS.txt"), "w", encoding="utf-8", newline="\n") as sums:
        for name in outputs:
            with open(name, "rb") as handle:
                digest = hashlib.sha256(handle.read()).hexdigest()
            sums.write(f"{digest}  {os.path.basename(name)}\n")
            print(f"{os.path.getsize(name) / 1048576:7.2f} MB  {name}")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("target", choices=("upm", "unitypackages"))
    parser.add_argument("--ref", default="HEAD")
    parser.add_argument("--branch", default="upm")
    parser.add_argument("--out", default="Build/Release")
    args = parser.parse_args()
    if args.target == "upm":
        build_upm(args.ref, args.branch)
    else:
        build_unitypackages(args.ref, args.out)
    return 0


if __name__ == "__main__":
    sys.exit(main())

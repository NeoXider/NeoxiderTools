# NI64 — Windows inventory stat correction

**SOURCE-CLOSED; no confirmed residual in this one-line delta.** Read-only review of `Local/Audits/assembly-2026-10-23/unity/maintenance-source/asset-schema-v2/prepare_schema_maintenance.py`245 lines, current SHA256 `4bdb71501fb20540c96a1d3360f58671f40ef3b987be14832b4579b4db57223d`. Only this report was written; no preparer/import/probe/parser/checker/Unity/process or product operation was executed by this reviewer.

Line63 replaces the single `entry.stat(follow_symlinks=False)` with `Path(entry.path).lstat()`. Reversing exactly that occurrence restores byte-identical NI63 source, SHA `392d8c97e99dad1edd525dc73ead4c6add1022dee50b093d9c0a6e1c7385f86c`. No inventory allowlist, authority, threshold or other source behavior changed.

The fresh non-following entry stat remains BEFORE link/reparse admission64, case/identity handling67, directory descent69–70 and the regular-file/`st_nlink==1` guard72. Root-to-leaf parent checks still run at60. There is no special acceptance of zero link counts, hardlink exception or fallback that follows the entry.

Root reports the actual first preparer attempt stopped on sealed `.editorconfig` because installed Windows Python3.14 DirEntry.stat returned nlink0 while Path.stat/lstat returned1, before controller/project creation or Unity. Root also reports invoking the ACTUAL imported `exact_inventory` with main uncalled: an owned ordinary-file positive passed, injected unexpected.cs was rejected, and two actual os.link files with nlink2 were rejected. These are root-attributed no-Unity operator results, not reviewer executions or native metadata/product passes. The retained owned probe directories were enumerated read-only; this reviewer did not rerun their checks.

NI63's other source closure remains bounded and unchanged. Root must preserve the failed attempt/old request/seal, then bind a NEW current seal/request/review to this new preparer SHA rather than overwrite old authority. Native/helper/preparation acceptance is not supplied by this source correction. Closing source hash was rechecked unchanged.

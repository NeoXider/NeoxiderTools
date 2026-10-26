# Neo.Audio.Editor: audit

Status: source audit and scoped changes complete; Unity compilation and test execution pending centrally.

- Date: 2026-10-23. Base revision: `ca198556` (package 10.19.0).
- Assigned model/reasoning: `gpt-6.1-sol` / `max`.
- Assembly: `Assets/Neoxider/Scripts/Audio/Editor/Neo.Audio.Editor.asmdef`.
- Ownership: all three manifest C# files, `Assets/Neoxider/Docs/Audio/Editor.md`, this report, and new
  `Audit20261023NeoAudioEditor` regression files. No nested assembly source is included.
- User restrictions observed: no Unity/MCP, compile/build/test runner, installs, child agents, shared
  configuration edits, scenes/prefabs/sample changes, commits or other Git mutations.

## Full source review

All 898 original source lines in the manifest inventory were read, including every method. This is a
source audit, not a claim that Unity imported or executed the changes.

| Owned file | Review and result |
|---|---|
| `Assets/Neoxider/Scripts/Audio/Editor/AMEditor.cs` (293 original lines) | Reviewed base-editor integration, the deliberately empty attribute-assignment pass, property selection, list drop event routing, non-audio filtering, reset defaults after Unity array duplication, immediate serialized apply, duplicate/empty/placeholder warnings, runtime status and Next Track gating. Reset includes ids, clips, trims, pitch, volume, channel, spatial blend, mode, fade and placeholder state. Existing serialized/public contracts preserved. Four implicit local types replaced with explicit types; no behavior refactor here. |
| `Assets/Neoxider/Scripts/Audio/Editor/AudioEntryDrawer.cs` (467 original lines) | Reviewed collapsed and expanded height accounting, foldout state, header assignment/clear, drag/drop versus per-slot replacement, null clips, all entry-specific fields, sparse trim defaults/growth/clamping, coordinated deletion, and event consumption. Fixed orphaned trim reuse on every new-slot path. Existing slot replacement retains its trim. |
| `Assets/Neoxider/Scripts/Audio/Editor/AudioIdDrawer.cs` (138 original lines) | Reviewed non-string fallback, absent-manager/empty-id fallback, sound/music selection, duplicate/empty-id filtering, missing-id preservation, label/value mapping, serialized changes and static lookup cache. Fixed the empty-choice/id collision and uncached manager misses; all three implicit local types are now explicit. |

Context inspected read-only: the editor/runtime asmdefs; `AudioEntry`, `SoundEntry`, `MusicEntry` and
`AudioIdAttribute`; AM entry accessors, setters, lifecycle and ordinal id lookup; `CustomEditorBase`
property hooks and GUI lifecycle; canonical Audio README and AM setup/API documentation; existing audio
entry tests and editor test conventions. Other modules were not audited as owned source.

### Contracts, lifecycle and compatibility

- This Editor-only assembly references `Neo.Audio`, `Neo.Editor` and `Neo.Tools.Managers`. No assembly
  definitions, dependency versions, serialized fields or public signatures were changed.
- Entry edits still use `SerializedProperty`; caller/editor code retains control of applying changes
  and Undo. No scene saving, asset generation, new subscriptions, runtime services or owned disposable
  editor resources were introduced.
- Empty clip arrays, null clips, optional ids and placeholders retain their documented behavior.
  Existing runtime index-keyed trim storage remains intact; cleanup occurs only when the drawer adds a
  new slot. Dropping multiple clips uses the same append path repeatedly.
- Missing managers/ids continue to allow manual input. Missing ids are displayed without mutation.
  Global manager resolution and the one-second discovery delay remain explicit limitations.
- Runtime status only draws during Play Mode. Inspector diagnostics remain help boxes; the fixes add
  no runtime logs, scene searches in runtime code or reflection-driven gameplay.
- No owned source uses `GetInstanceID`, `objectReferenceInstanceIDValue`, or version-specific entity-id
  calls. No scattered Unity-version conditional was added. Actual Unity 6.0–6.4 / 6.5+ compilation has
  not been performed by this worker.
- Reuse check: read the NeoxiderTools module reference and searched the module/editor reference for
  serialization/drawer helpers. No existing scoped append/popup-model helper covered these defects.
  Added two small private helpers inside the existing drawers, reusing Unity serialization and the
  existing runtime entry contract. Existing `NeoxiderEditorGUI` pill controls remain in use.

## Findings and completed fixes

### F1 — P2: a newly added clip can inherit an orphaned volume trim

**Trigger:** construct an entry with clips A/B, set B's trim to `0`, then shorten the public `Clips`
array to A. The runtime setter leaves trims keyed by index. Add C through the inspector header, drag a
clip onto the entry, or add an empty slot using **+** and assign C.

**Before:** each new-slot path grew only `_clips`. The old trim at index 1 still applied to C, so a
freshly assigned sound could be completely silent. The same problem occurred at index 0 after emptying
an entry. This follows directly from the public `Clips` setter, `SetClipVolume` and `ClipVolume` accessors;
it does not require malformed YAML or reflection to create the input.

**Source:** original `AudioEntryDrawer.cs:159`, `:302`, `:460`; dependency
`Scripts/Audio/AudioSimple/AudioEntry.cs:95` and `:144` (read-only).

**Fix:** `AudioEntryDrawer.AppendClip` (`:412`) removes trims beyond the previous clip count before
growing the clip array. The empty-entry picker (`:161`), **+** (`:308`) and header drop (`:478`) all use
it. Missing trims naturally resolve to `1`; existing clips keep their authored levels. Ordinary
replacement of an existing slot remains unchanged. No serialized format migration is needed.

**Regressions:** eight parameterized cases use actual Unity serialized properties and assert the
resulting public `ClipCount`, `ClipAt` / `NextClip`, `ClipVolume` and `LastClipVolume` values for sound and
music entries. They cover direct append, adding an empty slot then assigning it, an emptied entry, and
sparse trims that must retain neutral defaults.

### F2 — P2: a valid `<none>` id selects the empty option

**Trigger:** configure a sound/music entry whose id is literally `<none>` and reference that id from a
field marked with `AudioIdAttribute`. The runtime accepts this nonempty id and resolves it ordinally.

**Before:** the drawer prepended its `<none>` label and then searched that combined display list using
`IndexOf(current)`. It always selected index 0 for this id, so the field displayed the empty choice;
the empty action could not be distinguished reliably from the configured id. Serialization also used
display labels as values, making any added display decoration unsafe.

**Source:** original `AudioIdDrawer.cs:52–69`; runtime `AM.FindById` accepts this id (read-only).

**Fix:** `BuildOptions` (`AudioIdDrawer.cs:68`) searches only the raw ids and offsets configured choices
by one. The real `<none>` id is labelled with an `(id)` suffix. `OnGUI` (`:57`) stores raw ids, stores
empty string for index 0, and ignores the display-only missing option. A display suffix cannot enter
serialized data through this write path.

**Regressions:** three option-output tests cover a real `<none>` id, empty string in the same catalog,
and a missing id whose decorated label is itself another legal id. They check selected indices and
user-visible labels rather than source text.

### F3 — P3: empty manager lookups bypass the advertised cache

**Trigger:** draw one or more `AudioIdAttribute` fields in a scene without AM.

**Before:** the cache-hit condition required `_cachedManager != null`. Every field/event repeated
`FindFirstObjectByType<AM>` even when the immediately preceding lookup found nothing. Cost multiplied
with inspector repaints and id fields in precisely the fallback workflow the drawer supports.

**Source:** original `AudioIdDrawer.cs:88`.

**Fix:** `AudioIdDrawer.cs:66`, `:99` initialize the timestamp to negative infinity and cache results by
elapsed time, including null. The first call still searches immediately; subsequent misses reuse the
result for one second. Newly available managers are discovered after expiry. Destroyed references
remain safe because `CollectIds` uses Unity's null semantics.

**Regression:** one cache-lifecycle case establishes an unexpired null result, verifies it remains
null despite a newly available real manager, expires the timestamp, and verifies discovery. It restores
both static fields and destroys its temporary GameObject in `finally`.

### Style correction

Seven existing `var` declarations were replaced with concrete types in the two touched editor files.
This enforces AGENTS.md; it is not counted as a runtime defect and has no separate tautological test.

## Regression files and documentation

- New `Assets/Neoxider/Tests/Edit/Audio/Audit20261023NeoAudioEditorTests.cs`: seven test methods,
  twelve NUnit cases in total. It binds only private editor boundaries through reflection because the
  shared test asmdef has no `Neo.Audio.Editor` reference. Serialization assertions use real entries,
  clips and `SerializedObject`; the GUI option tests inspect output choices. No source-text assertions,
  replacement implementation, no-throw-only assertion or test asmdef edit was added.
- Tests exercise the serialization/model boundaries used by the GUI, not synthetic mouse events.
  Actual IMGUI routing, Undo/Redo and visual interaction remain central verification items.
- New owned topic `Assets/Neoxider/Docs/Audio/Editor.md` documents entry authoring, header/list drop
  targets, trim maintenance, optional/missing ids, fallback/cache behavior, UnityEvent components,
  explicit-manager C# usage, and the Play Mode readout. The topic was absent before this audit.
- Both new package files have unique GUID `.meta` files. No folder metadata was needed.
- Coordinator action: link this narrow topic from `Audio/README.md` or `AM.md`; those entry points are
  owned by the runtime worker and were not edited here. No additional shared test reference is required
  for the reflection-bound regression fixture.

## Substantial improvement proposal — contextual audio authoring catalog

**Proposal only; not implemented.**

**Problem:** the current single global AM cache is unrelated to the inspected object's scene or prefab
stage. With additive scenes it can offer another scene's ids. A prefab asset can see ids from a loaded
scene that will not exist where the prefab is instantiated. Every drawer also rebuilds its id list on
each draw. Fixing the negative cache reduces unnecessary searches but does not give authoring a clear
context or a directly testable option/value API.

**Architecture/public API:** add a small Editor-only `AudioAuthoringContext` with
`bool TryResolveManager(UnityEngine.Object inspectedTarget, out AM manager)` and a plain immutable
`AudioIdOptions` model built from `(IReadOnlyList<string> ids, string currentId)`. Each option exposes its
raw `Value`, display `Label`, and `IsMissing` / `IsEmpty` flags. Expose `SelectedIndex` and a selection
method that returns a raw id only for editable options. Both AM validation and the attribute drawer can
reuse a typed catalog snapshot; the runtime `AM` API remains the playback contract.

Resolution should first inspect an explicit manager field when the host supports one, then resolve
within the target scene/root; ambiguous multiple managers should show a hint and retain text input.
Prefab assets should fall back to text unless an explicit authoring catalog is supplied. Cache by
scene/stage context and invalidate on hierarchy changes, Undo/Redo, scene closing and play-mode state
changes. Register invalidation hooks once and release context references on invalidation/reload. Reuse
`NeoComponentHealth.StableId` if Unity-object keys are needed rather than adding new instance-id
version switches. An optional ScriptableObject catalog may contain clip/data assets only, never scene
references.

**Compatibility/migration:** keep `AudioIdAttribute`, all component field names, serialized strings and
runtime `AM` APIs unchanged. Introduce contextual resolution only in the editor; preserve manual text
fallback so ids supplied by dynamically created managers remain authorable. Existing global lookup can
remain a documented legacy fallback for objects with no scene context. Replace the private option
builder behind the same drawer without rewriting user data. Coordinate any shared test-asmdef
reference needed for direct tests of the new public editor model.

**Acceptance scenarios:**

1. Additive scenes A/B with distinct AM ids offer only the inspected target's catalog, independent of
   selection order. Two possible managers produce an explicit ambiguity hint.
2. A prefab edited alone does not acquire main-scene ids; unresolved existing strings survive opening,
   repainting, selecting the missing option and closing without modification.
3. `<none>`, ids containing display-suffix text, empty strings and duplicate ids have distinct stable
   choices and round-trip raw strings through selection and Undo/Redo.
4. Repainting 100 fields without AM performs one lookup per context until invalidation/expiry. Adding,
   deleting or renaming manager entries updates the relevant snapshot without a scene-wide per-field
   search; closing a scene releases cached references.
5. Compile and execute the catalog/editor regressions on Unity 6.0–6.4 and 6.5+, including prefab stages
   and entering/exiting Play Mode with domain reload disabled.

**Effort/risk:** approximately 2–3 developer days including focused editor tests and two-version
validation. Medium risk lies in prefab-stage resolution and cache invalidation; the pure option model
is low risk. Do not broaden this into a runtime service locator or require scene references in assets.

## Verification and remaining risks

Actual checks run against the final source/test/documentation batch:

- Manifest inventory comparison: exactly all three owned C# files; no extra/nested source added.
- Strict UTF-8 decoding of all eight owned files; no replacement characters or project
  `SYMBOL_MOJIBAKE_TOKENS`; final newlines and no trailing whitespace, including untracked files.
- Comment/string-aware delimiter balance and explicit-type scan: all four C# files passed. This is
  structural consistency checking, not C# compilation.
- Public/protected declarations and serialized relative-property names compared with `ca198556`:
  unchanged in all three owned sources.
- Owned documentation links resolve; static NUnit inventory confirms seven methods / twelve cases.
- Both new metadata GUIDs validated and found exactly once across `Assets/Neoxider` using a
  CRLF-aware search. The first scanner used an LF-only end anchor; correcting that scanner required no
  source or GUID change.
- `git diff --check --` with all eight owned paths: exit 0. Untracked content was covered separately by
  the UTF-8/newline/whitespace checks above.
- Read-only recheck of the concurrently changed `AudioEntry.cs` diff: the other worker's `NextClip`
  fallback fix does not change the `Clips` / trim contract used here; it was preserved untouched.

No Unity execution, compilation, runtime audio validation or independent QA is claimed. The user
explicitly reserved all Unity/build/test runners for the coordinator; no attempt was made to start
them. The worker performed local source review and static validation only.

Central follow-up: run the new fixture after import, exercise header/list drop and **+** / **-**, verify
single-slot replacement versus addition, confirm prefab overrides and Undo/Redo, and compile both
supported Unity API families. The reflection binding names are covered by test discovery/execution,
not by a compiler reference from the shared test asmdef. Runtime callers that replace/reorder `Clips`
remain responsible for index-keyed trims outside subsequent inspector additions.

Кратко: проверены все три исходника сборки; исправлены наследование старого trim новым клипом,
конфликт id `<none>` с пустым выбором и повторные поиски отсутствующего AM. Тесты добавлены, но не
запускались: Unity и test runners зарезервированы за координатором.

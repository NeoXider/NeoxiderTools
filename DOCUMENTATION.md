# Neoxider Documentation Standard

A single standard for every `.md` under `Assets/Neoxider/Docs` and the root READMEs. Goal: the top of every page immediately answers "what is this and how do I use it", in one consistent style.

---

## 1. Language and tone

- **Language:** English. Entry pages may add a short `По-русски` paragraph; write it in real UTF-8 Cyrillic, never in an escaped or mojibake form.
- **Tone:** dry and to the point. No marketing phrases and no obvious filler.
- **Terms:** script/class/component by context; file paths relative to `Assets/Neoxider/Scripts` or absolute.
- **Code samples:** they must compile against the current API and follow the package code style: explicit types instead of `var`, English comments.
- **Scope:** `Docs/` holds user documentation only. Plans, TODO lists, audits and maintainer guides live in the repository's `dev-docs/` folder.

---

## 2. Two document types

### 2.1. Script/component page (one .md per class or tightly related group)

**Mandatory opening** — right after the `# ClassName` heading:

```markdown
# ClassName

**What it is:** [1–2 sentences: kind (MonoBehaviour/ScriptableObject/class), purpose, file path, namespace when relevant.]

**How to use:**
1. [Step 1.]
2. [Step 2.]
…

---
```

After the `---` add sections as needed: **Fields**, **Methods**, **Events**, **Examples**, **See also**.

- **What it is** must state: what the entity is, what it is for, and the `.cs` path (short form is fine: `Scripts/Quest/QuestConfig.cs`).
- **How to use** — concrete steps (add to an object, assign a field, call a method, subscribe to an event). For pure-API pages a short "Get via X, call Y" is enough.

### 2.2. Module README (section index and description)

**Mandatory opening:**

```markdown
# Module name

**What it is:** [1–2 sentences: what the module covers, what it contains, where the scripts live.]

**Contents:** [Table or link list to the section's pages. Optionally a 2–3 point "How to work with it".]

---
```

Further subsections (Data flow, Code structure, etc.) as needed. A module with a demo scene adds a **Demo scene** section: the scene path relative to the Demo Scenes sample, the objects to inspect, and the equivalent C# calls. The scenes are saved complete; see [Samples](Docs/Samples.md#how-demo-scenes-are-made).

### 2.3. Upgrade pages

Behaviour changes, obsolete APIs and removals that an existing project can notice go into `CHANGELOG.md` **and** the current upgrade page (`Docs/Migration12.md` for 11.x → 12). Historical upgrade pages (`Docs/Migration11.md`) are kept as records; add a pointer instead of rewriting them.

---

## 3. Section structure (script page)

After the "What it is" / "How to use" block and the `---` divider:

| Section     | Content |
|-------------|---------|
| **Fields**  | Table: field name, type, purpose. Group by Header when useful. |
| **Methods** | Table or list: signature, return value, short description. |
| **Events**  | UnityEvents and C# events: when they fire, parameters. |
| **Examples**| Minimal code or a usage scenario. |
| **See also**| Links to related pages. |

Section headings are `## Fields`, `## Methods`, etc. Use `### …` subheadings when needed.

---

## 4. File and heading naming

- Script page file: `ClassName.md`, or a short meaningful name (e.g. `QuestBridge.md` for two classes).
- H1 heading: `# ClassName` or a short title (e.g. `# Quest NoCode Action`).
- Module README: `README.md` with `# Module name` or `# Module name — short description`.

---

## 5. Navigation and links

- **Root index:** [Docs/README.md](Docs/README.md) — documentation entry point; links to every module and Tools submodule.
- **Module README (inside a Docs subfolder):** after the "What it is" block add a **Navigation:** line linking to the parent README, e.g. `**Navigation:** [← Docs](../README.md)`, then the contents table.
- **Links in the Docs/README table:** always spell out the submodule README path, e.g. `[Tools/Inventory/README.md](./Tools/Inventory/README.md)`, not `[Tools/Inventory](./Tools/Inventory)`.
- End a script page with a **See also** block when related pages exist.
- Inline links use `[text](path/to/File.md)`.

---

## 6. Checklist when adding/editing an .md

- The page opens with **What it is** (and, for a script page, **How to use**; for a README, **Contents**).
- The script path is stated in **What it is**.
- The style matches the rest of the documentation (English, dry, no filler).
- A module README contains a **Contents** list with links.
- Relative links resolve (the `Neoxider > Health Check` menu reports dead links inside `Docs/`).
- New or changed public API, behaviour or obsolete members are reflected in the module page, `CHANGELOG.md` and, when users must act, the current upgrade page.

---

*Folder organization and the update process live in `DOCUMENTATION_GUIDELINES.md`.*

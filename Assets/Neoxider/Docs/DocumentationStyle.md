# Documentation Style

Documentation style rules for NeoxiderTools: how to describe components so they are convenient both when configuring in the Inspector (No-Code) and when using from code. Complements [DOCUMENTATION_GUIDELINES.md](../DOCUMENTATION_GUIDELINES.md) (folder structure, writing process).

---

## 1. Three Documentation Layers

Documentation is split into three layers. Do not duplicate long texts between them: each layer has its own level of brevity.

| Layer | Where to write | Language | Audience | What to describe |
|------|------------|------|----------|----------------|
| **Inspector** | `[Header]`, `[Tooltip]` attributes in C# | **English** | No-Code, scene setup | Briefly: what the field/section is, units, default behavior. |
| **XML in code** | `///` comments above classes, methods, properties | **English** | Developers (IntelliSense) | Summary; param/returns/remarks as needed; example/seealso for complex cases. |
| **.md file** | `Docs/` (Russian) and `Docs/` (English), `[NeoDoc("...")]` link → Docs | **Russian** / **English** | Everyone | Purpose, relationships, fields, API, No-Code and code examples, "See also". |

---

## 2. XML Documentation in Code

- **Language**: **English** (everywhere in code: summary, param, returns, remarks, example, seealso).
- **Class/component**: `/// <summary>` is mandatory — why the component exists, what it relates to; add `/// <remarks>` if needed (when to use it, limitations).
- **Public methods**: `/// <summary>`, plus `/// <param name="name">` for each parameter, and `/// <returns>` when a value is returned.
- **Public properties**: `/// <summary>` or `/// <value>`.
- **Enums / constants**: one `/// <summary>` per member.
- **Optional**: `/// <seealso cref="TypeOrMember"/>`, `/// <example>` — only for non-obvious cases.

Example:

```csharp
/// <summary>
///     Convenient driver for Animator parameters: trigger, bool, float, int. Use from code or wire to UnityEvent.
/// </summary>
public sealed class AnimatorParameterDriver : MonoBehaviour
{
    /// <summary>Fires the trigger by name.</summary>
    /// <param name="triggerName">Parameter name in the Animator.</param>
    public void SetTrigger(string triggerName) { ... }
}
```

---

## 3. Inspector Attributes (Tooltip, Header)

- **Language**: **English** (all texts in `[Header]` and `[Tooltip]`).
- **Every serialized field** visible in the Inspector must have a `[Tooltip("...")]`: what it is, units/range, what happens if left empty.
- Grouping: `[Header("Section name")]` for blocks (e.g., "Settings", "Events", "Save").
- The Tooltip text should be one short sentence.

Example:

```csharp
[Header("Parameter names (for methods without name argument)")]
[SerializeField]
[Tooltip("Trigger parameter name for SetTrigger() when called with no argument.")]
private string triggerParameterName;
```

---

## 4. Component .md Page Template

Use this structure for every component that has its own .md file.

### 4.1. Required Blocks

1. **Title** — component name (H1).
2. **Purpose** — 1–3 sentences: why the component exists, what problem it solves.
3. **Fields (Inspector)** — table: field name (as in the Inspector), type/units, short description. Group by `[Header]` sections.
4. **API** — public methods and properties convenient to call from code and from UnityEvent; return values.
5. **Unity Events** — when each event fires, what parameters it passes (important for No-Code).
6. **Examples** — at least two: scene/UnityEvent setup (No-Code) and a short C# snippet (code).
7. **See also** — links to related documents and a back link to the section (e.g., "← [Tools/Components](README.md)").

### 4.2. Optional Blocks

- **Relationships and dependencies** — what it works with (Animator, SaveProvider, etc.), required components, links to related .md files.
- **Installation/setup** — if a prefab, asmdef, or special first-run steps are needed.

### 4.3. Short Form for Simple Components

For simple scripts, this is enough: **Purpose** → **Fields** (table) → **API** (table) → **Examples** (No-Code + code) → **See also**.

### 4.4. Example Fields Table

```markdown
## Fields

| Field | Description |
|------|----------|
| **Animator** | Target Animator. If not set, taken from this object. |
| **Trigger Parameter Name** | Trigger name for the SetTrigger() method with no argument. |
```

### 4.5. Example API Table

```markdown
## API

| Method | Description |
|-------|----------|
| **SetTrigger()** | Fire the trigger from the Trigger Parameter Name field. |
| **SetBool(string parameterName, bool value)** | Set a bool parameter by name. |
```

---

## 5. Language and Tone

- **In code (XML, Inspector)**: **English** only — `/// summary`/param/returns, `[Tooltip]`, `[Header]`. C# type and method names are in English.
- **In .md files**: **Russian** in `Docs/` (main documentation, linked from `[NeoDoc]`); **English** in `Docs/` (same paths as in `Docs/`, for the English-speaking audience).
- **Tone**: brief and to the point; do not omit anything important for choosing between "configure in Inspector" and "call from code".

---

## 6. Linking Code and .md

- In the component class, specify the path to the **Russian** .md via the attribute: `[NeoDoc("Tools/Components/AnimatorParameterDriver.md")]`.
- The path is relative to the `Assets/Neoxider/Docs/` folder (no leading slash). The Neoxider inspector shows a documentation block with a preview and a button to open this .md.
- Put the **English** pages in `Assets/Neoxider/Docs/` with the same relative path (e.g., `Docs/Tools/Components/AnimatorParameterDriver.md`). The folder structure mirrors `Docs/`. See [Docs/README.md](../Docs/README.md).

---

## 7. Pre-Commit Checklist

- [ ] All fields visible in the Inspector have a `[Tooltip]`.
- [ ] Sections are grouped with `[Header]`.
- [ ] The class has a `/// <summary>` (and `/// <remarks>` if needed).
- [ ] Public methods have a `/// <summary>`, parameters have `/// <param>`, return values have `/// <returns>`.
- [ ] There is an .md file with the purpose, fields, API, events, examples, and "See also".
- [ ] The class has `[NeoDoc("...")]` with the path to that .md.

---

## 8. Module Readiness (Definition of Done)

A module is considered fully documented when all **four kinds** are complete:

| Kind | Description |
|-----|----------|
| **XML (EN)** | All public types/members in the module have XML comments in English. |
| **Inspector (EN)** | All serialized fields have `[Tooltip]` and, where needed, `[Header]` in English. |
| **Docs (RU)** | `Docs/` contains .md files following the [template](#4-component-md-page-template) in Russian. |
| **Docs (EN)** | `Docs/` has the same paths and structure, with text in English (adapted translation or 1:1). |

Track module readiness in the team's working checklist (issue/tracker). This file is removed once all four kinds of documentation are complete for every module in the project.

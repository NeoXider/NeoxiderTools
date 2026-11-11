# Documentation Guidelines

Standards and process for maintaining the package documentation. The **mandatory structure of every .md** ("what it is", "how to use") is defined in **[DOCUMENTATION.md](./DOCUMENTATION.md)** — check it whenever you add or edit a page.

Detailed **formatting rules** (XML docs in code, Tooltip/Header, the .md page template, examples) live in the repository's [dev-docs/DocumentationStyle.md](https://github.com/NeoXider/NeoxiderTools/blob/main/dev-docs/DocumentationStyle.md) (not part of the UPM package).

## 1. General principles

- **Language in `.md` (Docs/)**: English — user-facing documentation for the Neoxider inspector and the repository. Entry pages may add a short `По-русски` paragraph in real UTF-8 text.
- **Language in code (C#)**: XML `///`, `[Tooltip]`, `[Header]`, and `WHY:` / `TODO:` / `HACK:` comments — **English only** (IntelliSense, one style). See the layer table and examples in [dev-docs/DocumentationStyle.md](https://github.com/NeoXider/NeoxiderTools/blob/main/dev-docs/DocumentationStyle.md).
- **Goal**: the top of every page makes **what it is** and **how to use it** immediately clear; fields, methods, events, and examples follow.
- **Style**: dry, to the point. No filler and no marketing phrasing.
- **Two layers**: describe both the runtime C# API and the scene / NoCode workflow of a module.
- **User vs maintainer docs**: `Docs/` is for package users. Plans, TODO lists, audit notes and maintainer guides go into the repository's `dev-docs/`.

## 2. Structure and file organization

- **Documentation** lives in `Assets/Neoxider/Docs`. Paths relative to this folder go into the `[NeoDoc("...")]` attribute (opened from the Inspector).

- **Every module** gets a **folder** with the same name under `Docs` and one canonical entry, `README.md`. A single-class module may use its class page as the entry instead (`Docs/Haptics/Haptics.md`).
    - Inside that folder create:
        1. `README.md` with a short section overview and links to every script's page. If the module has subfolders, the README must also contain a contents list linking to those submodules.
        2. A separate `.md` file for **every** public script in the module.
    - *Example*: `Assets/Neoxider/Scripts/Extensions` -> `Assets/Neoxider/Docs/Extensions/`
    - Tools submodules follow the same rule one level down: `Scripts/Tools/Inventory` -> `Docs/Tools/Inventory/README.md`.

- **Legacy overview pages**: a few modules also keep a single overview file next to their folder (`Docs/Audio.md`, `Docs/Bonus.md`, `Docs/Editor.md`, `Docs/GridSystem.md`, `Docs/Save.md`, `Docs/Shop.md`). Keep them consistent with the folder README and link to it; do not add new ones.

- **Package-level pages** (`Docs/README.md`, `GettingStarted.md`, `Samples.md`, the `MigrationNN.md` upgrade guides, `PackageCompatibility.md`, ...) sit directly in `Docs/`.

## 3. Page content

**Mandatory:** every `.md` opens (right after the H1) with **What it is** and **How to use** (script page) or **What it is** and **Contents** (module README). Full template: [DOCUMENTATION.md](./DOCUMENTATION.md).

### 3.1. Class descriptions

For each key class in a module provide:

1. **Class name** (as a heading, e.g. `### ClassName`).
2. **Namespace** and **file path**.
3. **Short description**: 1–2 sentences on the class purpose.
4. **Key features (optional)**: list of main capabilities.
5. **Public properties and fields**: every public member with type and purpose.
6. **Public methods**: signature and short description. **Always** state the return type and what the returned value means (e.g. `Spend(float count)` returns `bool` — `true` on success).
7. **Unity Events**: **always** describe every public `UnityEvent` — when it fires and what parameters it passes.

## 4. Workflow

1. **Analyze the folder**: study the target folder in `Assets/Neoxider/Scripts` to understand its structure.
2. **Read one file at a time**: read scripts **one by one** — critical for describing each script accurately.
3. **Write the documentation** following section 3.
4. **Save** to the matching `.md` file.

## 5. Keeping documentation in sync

When scripts are moved or renamed:

1. **Update paths**: if a script moved to another folder, update the file path inside its page.
    - *Example*: if `ScoreManager.cs` moved from `Tools/Managers/` to `Tools/Components/`, update the path in the corresponding `.md`.
2. **Update links**: check and fix every link to the moved script in other pages (READMEs, related components).
3. **Move the page**: if the script moved to another subfolder, move its `.md` to the matching folder as well, and update the `[NeoDoc]` path in the script.
    - *Example*: a script moved from `Managers/` to `Components/` means its page moves from `Docs/Tools/Managers/` to `Docs/Tools/Components/`.
4. **Check the links**: run **Neoxider > Health Check**. It verifies that every `[NeoDoc]` path resolves, every public component links a page, relative links inside `Docs/` are not dead, and the version mentioned in the entry pages matches `package.json`.

When behaviour or public API changes, update the module page in the same change, add a `CHANGELOG.md` line, and list user-visible changes in the current upgrade page (`Docs/Migration12.md`).

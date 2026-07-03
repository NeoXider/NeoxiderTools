# MarkdownRenderer Fork (NeoXider)

**What it is:** Instructions for finishing the [NeoXider/MarkdownRenderer](https://github.com/NeoXider/MarkdownRenderer) fork: fixing errors, renaming the package, and an improvement plan before integrating it into NeoxiderTools.

**How to use:** see the sections below.

---


Instructions for finishing the [NeoXider/MarkdownRenderer](https://github.com/NeoXider/MarkdownRenderer) fork: fixing errors, renaming the package, and an improvement plan before integrating it into NeoxiderTools.

**Supported Unity version:** 2022.3 and above.

---

## 1. Error CS0246: UxmlElement / UxmlElementAttribute

**Cause:** `[UxmlElement]` and `partial class` are Unity 6 APIs. Unity 2022.3+ uses the **UxmlFactory** + **UxmlTraits** pattern.

**Files in the fork** (fix both if present): `Editor/VideoElement/VideoPlayerElement.cs` and `MarkdownRenderer/Editor/VideoElement/VideoPlayerElement.cs`

**Was:**
```csharp
[UxmlElement]
public partial class VideoPlayerElement : VisualElement
{
```

**Replace with:**
```csharp
public class VideoPlayerElement : VisualElement
{
    public new class UxmlFactory : UxmlFactory<VideoPlayerElement, UxmlTraits> { }
    public new class UxmlTraits : VisualElement.UxmlTraits { }
```

The class should no longer be `partial`. After the fix, the project builds in Unity 2022.3 and above.

---

## 2. Renaming the Package to com.neoxider.markdownrenderer

To make the package appear in the project as your own (Neoxider), the package name in the fork must be changed.

### 2.1 package.json

In `package.json` at the root of the fork repository, replace:

- **Was:** `"name": "com.rtl.markdownrenderer"`
- **Becomes:** `"name": "com.neoxider.markdownrenderer"`

Optionally update `displayName`, for example: `"Markdown Renderer (Neoxider)"`.

### 2.2 Package folder (if used)

When installed via Git URL, Unity places the package in `Library/PackageCache` under the name from `package.json` plus a hash. After changing `name` to `com.neoxider.markdownrenderer`, the next time the package is updated the cache folder will look like `com.neoxider.markdownrenderer@<hash>`. Renaming folders in the repository separately is not required: the structure can stay as is (for example, the `MarkdownRenderer/` subfolder, etc.).

### 2.3 Installation in a Project (NeoxiderTools and others)

**Recommended method:** Package Manager → **+** → Add package from git URL → paste:

```
https://github.com/NeoXider/MarkdownRenderer.git
```

If the package in the fork is renamed to `com.neoxider.markdownrenderer`, the dependency in `Packages/manifest.json` will look like:

- `"com.neoxider.markdownrenderer": "https://github.com/NeoXider/MarkdownRenderer.git"`

(previously it may have been `"com.rtl.markdownrenderer": "https://github.com/NeoXider/MarkdownRenderer.git"`).

Unity will substitute the new name in `packages-lock.json` on the next package resolution by itself (or you can delete the lock file and reopen the project).

### 2.4 Asmdef (optional)

The assembly names in the package are currently `Rtl.MarkdownRenderer.Editor` and so on. For consistency they can be renamed to `Neoxider.MarkdownRenderer.Editor` with the asmdef file names updated accordingly, but this is not required for it to work: references from NeoxiderTools go by package name, not by assembly name.

---

## 3. Fork Improvements Before Integration

Before embedding documentation into the NeoxiderTools inspector, it makes sense to add the following to the fork:

### 3.1 Convenient .md search (Assets and Packages)

- **Search by name/extension** in the Markdown viewer window (or in a separate window): search for `.md` files both in `Assets/` and in `Packages/` (including `Packages/com.neoxider.tools/...` when Neoxider is installed as a package).
- Use `AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets", "Packages" })` with an extension filter, or search by GUID in the relevant folders, so the document path does not depend on the installation method (Git in Assets or UPM in Packages).

### 3.2 Button and Navigation

- An explicit "Open in window" button (or equivalent) in the custom .md inspector or in the document list, to open the selected file in the Markdown Doc View window.
- The ability to open an .md by a project-relative path (`Assets/...` or `Packages/...`) from code (as in the NeoxiderTools integration plan: "Open in window" from a component's inspector).

### 3.3 Nice Rendering

- Styles/USS for readability (headings, code, links) in the viewer window.
- Support for relative paths to images and links relative to the current .md (as in the [MarkdownRenderer documentation](https://github.com/UnityGuillaume/MarkdownRenderer) — relative path from the file's location), so that Neoxider docs with images and cross-references look correct both in Assets and in Packages.

After these improvements, the fork can be hooked up in NeoxiderTools and the "Documentation" block used in the inspector, with an "Open in window" button and correct paths.

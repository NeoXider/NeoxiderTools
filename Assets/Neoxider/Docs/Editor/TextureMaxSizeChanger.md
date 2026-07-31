# Texture Max Size Changer Utility

**What it is:** This tool provides a simple Unity editor window for bulk-changing the `Max Size` setting of textures. This is especially useful for project optimization, when you need to quickly reduce the maxi...

**How to use:** see the sections below.

---


## 1. Introduction

This tool provides a simple Unity editor window for bulk-changing the `Max Size` setting of textures. This is especially useful for project optimization, when you need to quickly reduce the maximum resolution for a large number of textures to cut memory usage.

The utility lets you select a texture type (for example, `Sprite`, `Default`, `Normal map`) and apply a new `Max Size` value to all textures of that type in the project.

---

## 2. Tool Description

### TextureMaxSizeChanger
- **Namespace**: `Neo`
- **File path**: `Assets/Neoxider/Editor/TextureMaxSizeChanger.cs`
- **GUI class**: `TextureMaxSizeChangerGUI` (`Assets/Neoxider/Editor/GUI/TextureMaxSizeChangerGUI.cs`)
- **Menu access**: `Neoxider/Tools/Texture Max Size`

**Description**
Creates a Unity editor window for bulk-changing the maximum texture size. Uses an architecture that separates logic from GUI rendering.

**Key features**
- **Bulk editing**: Lets you change import settings for hundreds of textures in a single operation.
- **Filtering by type**: Changes can be applied only to a specific texture type (`TextureImporterType`).
- **Simple interface**: The window contains just two fields (size and type) and an "Apply" button.
- **Progress bar**: Shows the texture processing progress.
- **Confirmation**: Asks for confirmation before applying changes.
- **Professional architecture**: GUI rendering is extracted into a separate class, `TextureMaxSizeChangerGUI`.

**Public methods**
- `ShowWindow()`: Static method that opens the tool window. Invoked via `MenuItem`.

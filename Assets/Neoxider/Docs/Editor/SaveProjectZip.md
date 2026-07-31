# Save Project Zip Utility

**What it is:** This is a simple but extremely useful tool that adds a single command to the Unity menu for creating a ZIP archive of the entire project. It's a convenient way to quickly make a backup or share the proj...

**How to use:** see the sections below.

---


## 1. Introduction

This is a simple but extremely useful tool that adds a single command to the Unity menu for creating a ZIP archive of the entire project. It's a convenient way to quickly make a backup or share the project with someone.

The utility automatically finds the key project folders (`Assets`, `ProjectSettings`, `Packages`) and packs them into a single ZIP file.

---

## 2. Tool Description

### SaveProjectZip
- **Namespace**: `(global)`
- **File path**: `Assets/Neoxider/Editor/SaveProjectZip.cs`
- **Menu access**: `Neoxider/Tools/Save Project Zip`

**Description**
A static class that adds project-to-ZIP archiving functionality to the editor menu.

**Key features**
- **Simplicity**: One menu button performs the whole process.
- **Key folders**: Archives the most important folders of any Unity project: `Assets`, `ProjectSettings`, and `Packages`.
- **Standard dialog**: Uses the standard file save window, letting you pick the archive's name and location.

**Public methods**
- This class has no public methods intended to be called from other scripts. All logic is internal and invoked via `MenuItem`.

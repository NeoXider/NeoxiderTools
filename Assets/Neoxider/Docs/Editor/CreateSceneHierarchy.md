# Create Scene Hierarchy Utility

**What it is:** This tool is designed to create and maintain a clean, organized scene hierarchy. It lets you create, with a single click, a standard set of empty `GameObject`s that serve as separators ...

**How to use:** see the sections below.

---


## 1. Introduction

This tool is designed to create and maintain a clean, organized scene hierarchy. It lets you create, with a single click, a standard set of empty `GameObject`s that serve as separators and containers for other objects (for example, `--System--`, `--UI--`, `--Environment--`).

Using such a structure greatly simplifies scene navigation, especially in large projects.

---

## 2. Tool Description

### CreateSceneHierarchy
- **Namespace**: `Neo`
- **File path**: `Assets/Neoxider/Editor/Main/CreateSceneHierarchy.cs`
- **Menu access**: 
  - `GameObject/Neoxider/Btn/Create Scene Hierarchy`
  - `GameObject/Neoxider/Btn/Sort Hierarchy Objects`

**Description**
An editor script for creating and sorting a standard object hierarchy in the scene.

**Key features**
- **Structure creation**: Adds a predefined list of empty separator objects to the scene.
- **Sorting**: Can sort the created separator objects alphabetically.
- **Customizable**: The list of objects to create and their separators (`--`) are configured via `NeoxiderSettingsWindow`.

**Public methods**
- `CreateHierarchy()`: Creates all objects from the list in the scene. Returns `void`.

**Usage**
- **Create Scene Hierarchy**: Creates the set of empty objects in the current scene according to the settings.
- **Sort Hierarchy Objects**: Sorts already created container objects alphabetically.

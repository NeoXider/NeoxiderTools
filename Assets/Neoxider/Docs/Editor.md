
# Editor Tools (Editor)

**What it is:** an overview of the Editor module (utilities, windows, inspector). Script navigation is in [Editor/README](Editor/README.md).

**How to use:** see the sections below or [Editor/README](Editor/README.md).

---


The `Editor` module contains a set of utilities and extensions for the Unity editor, created to speed up development, automate routine tasks, and keep the project organized.

## Key Features

The key functions you will interact with are described below.

### 1. Utility Windows

All utilities are accessed through the top-level **Neoxider/** menu (windows under **Neoxider/Windows/**, utilities under **Neoxider/Tools/**).

- **Settings**: Opens the main Neoxider settings window, where you can configure the project folder structure, scene hierarchy parameters, and other global options.
- **Find & Remove Missing Scripts**: A powerful tool for finding and removing missing scripts across the entire project, including all scenes and prefabs.
- **Texture Max Size**: Lets you bulk-change the maximum texture size for all assets of a specific type (for example, all sprites or all normal maps).
- **Scene Saver Settings**: Opens the settings for the background scene auto-save utility. Helps prevent loss of work.
- **Save Project Zip**: Packs the key project folders (`Assets`, `ProjectSettings`, `Packages`) into a single ZIP archive. Convenient for creating backups.

### 2. Object Creation

The **GameObject/Neoxider/** menu (also available by right-clicking in the Hierarchy) includes items for quickly creating standard objects and hierarchies:

- **Create Neoxider Object...**: Opens a searchable window listing every component marked with `[CreateFromMenu]` (for example, `TimeReward`, `WheelFortune`, `Money`, etc.). Also dockable via **Neoxider/Windows/Create Neoxider Object**.
- **Presets**: Ready-made prefabs — System Root, First Person Controller, Simple Weapon, Bullet, Interactive Sphere, Toggle Interactive, Trigger Cube.
- **Create Scene Hierarchy** / **Sort Scene Hierarchy**: Creates (and sorts) the standard set of empty container objects in the scene (`--System--`, `--Environment--`, etc.) to keep things organized. The list is configured in the Neoxider settings window.

### 3. Custom Attributes

The editor automatically extends the inspector functionality for **all** `MonoBehaviour` components.

- **`[Button]`**: Turns any method in your code into a clickable button in the inspector. Supports parameters.
- **Auto-fill attributes**: Attributes such as `[GetComponent]`, `[FindInScene]`, `[LoadFromResources]` and their plural versions (`[GetComponents]`, etc.) automatically find and assign references to components and assets in your script's fields. This removes the need to do it manually.

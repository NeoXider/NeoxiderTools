
# Editor Tools (Editor)

**What it is:** an overview of the Editor module (utilities, windows, inspector). Script navigation is in [Editor/README](Editor/README.md).

**How to use:** see the sections below or [Editor/README](Editor/README.md).

---


The `Editor` module contains a set of utilities and extensions for the Unity editor, created to speed up development, automate routine tasks, and keep the project organized.

## Key Features

The key functions you will interact with are described below.

### 1. Utility Windows

Most utilities are accessed through the **Tools/Neoxider/** menu.

- **Settings**: Opens the main Neoxider settings window, where you can configure the project folder structure, scene hierarchy parameters, and other global options.
- **Find & Remove Missing Scripts**: A powerful tool for finding and removing missing scripts across the entire project, including all scenes and prefabs.
- **Change Texture Max Size**: Lets you bulk-change the maximum texture size for all assets of a specific type (for example, all sprites or all normal maps).
- **Scene Saver Settings**: Opens the settings for the background scene auto-save utility. Helps prevent loss of work.
- **Save Project Zip**: Packs the key project folders (`Assets`, `ProjectSettings`, `Packages`) into a single ZIP archive. Convenient for creating backups.

### 2. Object Creation

The **GameObject/Neoxider/** menu includes items for quickly creating standard objects and hierarchies:

- **Create Scene Hierarchy**: Creates a standard set of empty container objects in the scene (`---System---`, `---Environment---`, etc.) to keep things organized.
- **Other items**: The menu also contains options for quickly creating ready-made prefabs or components from the Neoxider set (for example, `TimeReward`, `WheelFortune`, `Money`, etc.).

### 3. Custom Attributes

The editor automatically extends the inspector functionality for **all** `MonoBehaviour` components.

- **`[Button]`**: Turns any method in your code into a clickable button in the inspector. Supports parameters.
- **Auto-fill attributes**: Attributes such as `[GetComponent]`, `[FindInScene]`, `[LoadFromResources]` and their plural versions (`[GetComponents]`, etc.) automatically find and assign references to components and assets in your script's fields. This removes the need to do it manually.

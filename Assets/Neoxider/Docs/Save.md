
# Save System (Save)

**What it is:** an overview of the Save module (SaveManager, providers, GlobalSave). Class navigation is in [Save/README](Save/README.md).

**How to use:** see the sections below or [Save/README](Save/README.md).

---


## Description

The **Save** module provides several flexible mechanisms for saving and loading game data. It is built on top of `PlayerPrefs` but offers more structured and automated approaches for different tasks: from saving individual fields in components to managing global game state.

## 1. Core System (SaveManager)

This is the most powerful and recommended part of the module. It lets you automatically save and load fields in any components.

### How It Works

The system is built on three key elements:
1.  **`SaveManager`**: A singleton manager that drives the whole process. It finds all saveable objects, loads their state on startup, and saves it on exit.
2.  **`ISaveableComponent`**: An interface you mark any `MonoBehaviour` with so that `SaveManager` starts working with it.
3.  **`[SaveField]`**: An attribute used to mark the specific fields inside a component that you want to save.

### How to Use

1.  **Add a `SaveManager`**: Make sure your startup scene contains an object with the `SaveManager` component. If it doesn't exist, it will create itself on first access.
2.  **Mark the component**: In the script whose data needs to be saved, implement the `ISaveableComponent` interface.
3.  **Mark the fields**: In the same script, put the `[SaveField("UniqueKey")]` attribute above each variable that should be saved.
4.  **(Optional) Inherit from `SaveableBehaviour`**: To have your component automatically register with `SaveManager` even when created at runtime, inherit it from `SaveableBehaviour` instead of `MonoBehaviour`.

### Key Classes and Attributes

#### `SaveManager`
- **Description**: The central manager. It is a "lazy" singleton. It automatically loads data on initialization and saves it on application quit.
- **Public methods**:
  - `Save()`: Forces a save of the state of **all** registered objects. Useful for checkpoints.
  - `Save(MonoBehaviour monoObj)`: Forces a save of the state of **one** specific object.
  - `Load(MonoBehaviour monoObj)`: Forces a load of the state for **one** specific object.

#### `ISaveableComponent`
- **Description**: A marker interface. Simply add it to your class so that `SaveManager` can find it.
- **Methods**:
  - `void OnDataLoaded()`: This method is called on your component **immediately after** `SaveManager` loads all saved data into it. Use it to apply the loaded data (for example, to update the object's position, health, etc.).

#### `[SaveField]`
- **Description**: An attribute for marking fields that should be saved.
- **Parameters**:
  - `key` (`string`): Required. A unique key for this field.
  - `autoSaveOnQuit` (`bool`): `true` by default. Whether to save the field on quit.
  - `autoLoadOnAwake` (`bool`): `true` by default. Whether to load the field on startup.

#### `SaveableBehaviour`
- **Description**: An abstract base class you can inherit from instead of `MonoBehaviour`. It already implements `ISaveableComponent` and automatically registers/unregisters the component with `SaveManager` when it is enabled/disabled in the scene. This is the most reliable way to work with the system.

--- 

## 2. Global Save (GlobalSave)

This is a simpler system for storing a single global data object, accessible from anywhere in the code.

### How It Works

- **`GlobalSave`**: A static class that does not need to exist in the scene. It manages saving and loading a single instance of the `GlobalData` class.
- **`GlobalData`**: A container class for your data. It is initially empty; you add the fields you need yourself (for example, `public int coins;`).

### How to Use

1.  Add the fields you need to the `GlobalData.cs` class.
2.  To access the data from any script, use `GlobalSave.data`.
3.  Assigning a new value with `GlobalSave.data = myData;` automatically saves the data.



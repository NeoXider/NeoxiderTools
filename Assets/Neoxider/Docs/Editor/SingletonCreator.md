# Singleton Creator Utility

**What it is:** The template already contains all the necessary boilerplate for a class inherited from `Singleton<T>`, letting you focus on writing the singleton's logic rather than repetitive code.

**How to use:** see the sections below.

---


## 1. Introduction

`SingletonCreator` is a code generation tool that helps you quickly create new singletons in your project. It adds a new item to the `Assets -> Create` menu that automates creating a C# script from a predefined template.

The template already contains all the necessary boilerplate for a class inherited from `Singleton<T>`, letting you focus on writing the singleton's logic rather than repetitive code.

---

## 2. Tool Description

### SingletonCreator
- **Namespace**: `Neo.Editor`
- **File path**: `Assets/Neoxider/Editor/Create/SingletonCreator.cs`
- **Menu access**: `Assets/Create/Neoxider/Singleton`

**Description**
Adds an option to the `Assets/Create` menu for quickly creating a C# script for a new singleton.

**Key features**
- **Template-based generation**: Creates a new script with a ready-made structure for a singleton class.
- **Interactive dialog**: Before the file is created, a dialog window appears asking you to enter the new class name.
- **Contextual creation**: The file is created in the folder currently selected in the `Project` window.

**Public methods**
- `CreateSingletonTemplate()`: A static method that starts the script creation process. Invoked via `MenuItem`.

Cancel or close the name dialog to leave the project untouched. Empty or punctuation-only input also creates no script. Names retain letters, digits and underscores; their first letter is capitalized and a leading digit gains an underscore (`123 Hero` becomes `_123Hero`). The filename, class declaration and `Singleton<T>` type argument use the same normalized name.

The generated script exposes the existing typed `Singleton<T>` extension points, including `Init()` and `OnInstanceCreated()`. Its runtime use does not depend on this Editor menu or a NoCode setup.

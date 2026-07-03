# CustomEditorBase Base Class

**What it is:** It also provides a convenient interface for setting method parameters right in the inspector before invoking them. This class is the foundation of `NeoCustomEditor`.

**How to use:** see the sections below.

---


## 1. Introduction

`CustomEditorBase` is an abstract base class for Unity editors that implements one very useful feature: the ability to turn methods of your class into inspector buttons using the `[Button]` attribute.

It also provides a convenient interface for setting method parameters right in the inspector before invoking them. This class is the foundation of `NeoCustomEditor`.

---

## 2. Class Description

### CustomEditorBase
- **Namespace**: `Neo.Editor`
- **File path**: `Assets/Neoxider/Editor/PropertyAttribute/CustomEditorBase.cs`

**Description**
An abstract editor class that adds inspector button rendering for methods marked with the `[Button]` attribute.

**Key features**
- **Button methods**: Any public or private method with the `[Button]` attribute is displayed in the inspector as a button.
- **Editable parameters**: If a method has parameters (e.g. `int`, `float`, `string`, `bool`, `GameObject`), they are shown in a dropdown below the button and can be modified before invocation.
- **Default value support**: Initial parameter values are taken from the default values specified in the method signature.

**Public methods**
- This class has no public methods intended to be called from other scripts. It extends the functionality of the Unity inspector.

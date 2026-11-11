# Getting Started — NeoxiderTools

**What it is:** the quick start: get from "package installed" to a working C# runtime model or a first audio scene in about five minutes.

**How to use:** check the [requirements](#requirements), [install](#installation) the package and its two
dependencies, then follow either [Start with a C# runtime model](#start-with-a-c-runtime-model) or
[Your first scene in 5 minutes](#your-first-scene-in-5-minutes-audio-manager-am).

For recorded test environments, results and limits, see the [verification record](../Verification11.md).

**Navigation:** [← To Docs](./README.md) · [Upgrading from 11.x](./Migration12.md) · [Migration from 10.x](./Migration11.md) · [Testing and isolation](./Testing.md)

**По-русски:** [C# без сцены](#start-with-a-c-runtime-model), [первый аудиокомпонент](#your-first-scene-in-5-minutes-audio-manager-am) или [NoCode](./NoCode/GettingStarted.md). Существующий проект сначала сверьте с [обновлением до 12](./Migration12.md) (с 11.x) или [миграцией на 11](./Migration11.md) (с 10.x).

---

## Requirements

| Requirement | Version |
|-------------|---------|
| Unity | **6000.0** (Unity 6) or newer — see [PackageCompatibility](./PackageCompatibility.md) |
| uGUI / TextMeshPro | `com.unity.ugui` 2.0.0+ (UPM dependency); TextMeshPro ships inside it on Unity 6 |
| AI Navigation | `com.unity.ai.navigation` 2.0.16+ (UPM dependency) |
| Input System | `com.unity.inputsystem` 1.20.1+ (UPM dependency) |
| UniTask | **required** — install manually (see below) |
| DOTween | **required** by tween-based runtime assemblies in the installed package; install separately |
| TMP Essential Resources | needed by the text-based samples; **Window → Text Mesh Pro → Import TMP Essential Resources** |

> UniTask and DOTween are not listed in `package.json` dependencies and must be installed into the host project separately.

---

## Installation

### Option A — UPM via Git URL (recommended)

1. Open **Window → Package Manager**.
2. Click **+** → **Add package from git URL…**
3. Paste:
   ```
   https://github.com/NeoXider/NeoxiderTools.git#upm
   ```
4. Click **Add** and wait for the import to finish.

The `upm` branch holds only the package, so Unity downloads a few megabytes instead of the whole repository (the older `?path=Assets/Neoxider` URL still works but fetches every file of the repository). To pin a version, replace `upm` with the commit listed in the release notes. For local development, use the local route below.

### Option B — Local path (for development)

Copy the `Assets/Neoxider` folder into your project or reference it as a local package in `manifest.json`:
```json
"com.neoxider.tools": "file:../path/to/Assets/Neoxider"
```

### Install UniTask (required)

Open `Packages/manifest.json` and add to `dependencies`:
```json
"com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.11"
```

`2.5.11` is the version the package is validated with; see [Dependencies](./Dependencies.md).
Or download the `.unitypackage` from [UniTask releases](https://github.com/Cysharp/UniTask/releases).

### Install DOTween (required)

Install [DOTween (HOTween v2)](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676) from the Asset Store, then run **Tools → Demigiant → DOTween Utility Panel** to complete its setup. DOTween Pro is optional.

## Choose your starting point

| Workflow | Start with |
|----------|------------|
| **C# runtime API** | The resource model below, then [Merge](./Merge/README.md) or [StateMachine](./StateMachine/README.md) for more runtime contracts. Your code owns these objects. |
| **Scene components** | The [AM audio scene](#your-first-scene-in-5-minutes-audio-manager-am) below. Add the component and assign its dependencies. |
| **Inspector / NoCode** | [NoCode Getting Started](./NoCode/GettingStarted.md), [Condition](./Condition/README.md), and a module sample. Scene wrappers expose the corresponding C# API. |
| **Interaction** | [Interaction contracts and components](./Tools/InteractableObject/README.md) for custom C# input or scene hover/click and physics events. |

The package's `Prefabs/-System--.prefab` is available for the existing manager bootstrap. Add only the managers your chosen scene workflow needs. Runtime models can be used without this prefab or a scene singleton.

## Start with a C# runtime model

`ResourcePoolModel` and `ResourcePoolEntry` in `Neo.Core.Resources` are ordinary C# objects with no `UnityEngine` dependency. This complete example creates a mana pool and spends 25; the expected remaining value is 75:

```csharp
using System;
using Neo.Core.Resources;

public static class ManaExample
{
    public static float SpendMana()
    {
        ResourcePoolModel resources = new ResourcePoolModel();
        ResourcePoolEntry mana = new ResourcePoolEntry
        {
            Current = 100f,
            Max = 100f,
            MaxDecreaseAmount = -1f,
            MaxIncreaseAmount = -1f
        };
        resources.AddPool("mana", mana);

        if (!resources.TrySpend("mana", 25f, out string failureReason))
        {
            throw new InvalidOperationException(failureReason);
        }

        return resources.GetCurrent("mana");
    }
}
```

The two `Max*Amount` values are per-operation limits; `-1` removes the limit. Keep the model in your owning service for persistent runtime state. Call `Tick(deltaTime)` from that owner if you configure regeneration, and subscribe to `OnResourceChanged` to update a view. For a scene wrapper, see [HealthComponent](./Core/Resources/Components/HealthComponent.md).

`TrySpend` checks both the available balance and the decrease limit. A cost above that limit fails without a partial payment. See the [Core entry](./Core/README.md) and [migration guide](./Migration11.md) for resource-cost integration details.

---

## Demo scenes

Demos are not part of the package. Download the ones you want from the [releases page](https://github.com/NeoXider/NeoxiderTools/releases) and import them after the package (**Assets → Import Package → Custom Package…**):

| Download | What it contains |
|----------|-----------------|
| `NeoxiderTools.Demo.v<ver>.unitypackage` | A demo scene per module (`AM`, `GridSystem`, `Shop`, `NoCode`, `Condition`, `StateMachine`, `Abilities`, and more), example games and `SurvivorDemo` |
| `NeoxiderPages.v<ver>.unitypackage` | Optional UI page-navigation module (`PM`, `UIPage`, `BtnChangePage`) |
| `NeoxiderTools.CharacterMovementDemo.v<ver>.unitypackage` | Character-controller demos for slopes, stairs, moving platforms, gravity, and a top-down arena |

They import into `Assets/NeoxiderDemos/<name>/`. For example, the audio scene is `Assets/NeoxiderDemos/Demo/Scenes/Audio/AudioDemo.unity`.

Each demo scene is saved with its UI, configured components and UnityEvent wiring. Open it, select a button and look at its **On Click** list to see which module API it calls, then press Play. The collapsible info card in the scene names the components to inspect and the equivalent C# calls. If you change a demo scene and want it back, use **Neoxider > Demo > Rebuild Active Demo Scene** ([how demo scenes are made](./Samples.md#how-demo-scenes-are-made)).

Samples imported from 12.1 and earlier stay in `Assets/Samples/NeoxiderTools/<version>/`. Delete that copy before importing a demo download: both carry the same asset GUIDs. See [Demo scenes](./Samples.md) for the scene index.

---

## Your first scene in 5 minutes: Audio Manager (AM)

For a small scene example, add **AM** and play a sound. AM creates its music/effects AudioSources during initialization. Use either the C# or NoCode playback step below after creating the manager.

### Step 1 — Create a GameObject with the AM component

Create an empty GameObject and use **Add Component → Neoxider → Audio → AM**. You can also use **Neoxider → Windows → Create Neoxider Object** and select AM.

This creates an `AM` object in the scene with the component already attached. AudioSources for effects and music are created automatically.

### Step 2 — Assign an audio clip

1. Select the `AM` object in the Hierarchy.
2. In the Inspector's **Sounds** section, add one entry to **Sound Entries**.
3. Set its **Id** to `ui_click`.
4. Add one element to the entry's **Clips** array and assign an `AudioClip`.

### Step 3 — Play a sound from code

Create a MonoBehaviour with an explicit AM reference and call its typed API:

```csharp
using Neo.Audio;
using UnityEngine;

public sealed class SoundTest : MonoBehaviour
{
    [SerializeField] private AM _audio;

    private void Start()
    {
        PlaySound();
    }

    public void PlaySound()
    {
        _audio.Play("ui_click");
    }
}
```

Attach this script to a GameObject and drag the scene's AM into its **Audio** field. `PlaySound()` can also be called from your own code or a UI event. For projects deliberately using the singleton entry point, `AM.I.Play("ui_click")` is available after AM initialization.

### Step 4 — Play a sound without code (No-Code)

If you prefer not to write a script, use the **PlayAudio** component:

1. Select any GameObject.
2. **Add Component → Neoxider → Audio → PlayAudio**.
3. Set **Sound Id** to `ui_click`, using the entry created on AM.
4. Enable **Play On Awake** to play the sound on scene start. Keep the AM object in the scene.

Alternatively, wire the `AudioPlay()` method of the `PlayAudio` component to any `UnityEvent` in the Inspector — zero lines of code.

### Step 5 — Press Play

Run the scene. Your selected C# or NoCode playback component plays the sound automatically.

> **Tip:** named sound IDs survive reordering the entry list. Index-based calls such as `AM.I.Play(0)` remain available for existing setups.

---

## Where to next

| What to explore | Document |
|-----------------|----------|
| Full audio manager API | [Audio/AM](./Audio/AM.md) |
| Volume and mute settings | [Audio/AMSettings](./Audio/AMSettings.md) |
| Button-triggered sounds | [Audio/PlayAudioBtn](./Audio/PlayAudioBtn.md) |
| No-code conditions and events | [Condition](./Condition/README.md) |
| Scene and global save | [Save](./Save/README.md) |
| Grid games (Dice, Match3, 2048) | [GridSystem](./GridSystem/README.md) |
| Shop and currency | [Shop](./Shop/README.md) |
| Movement and input | [Tools](./Tools/README.md) |
| Interaction contracts, hover/click and physics events | [Interaction](./Tools/InteractableObject/README.md) |
| RPG characters and combat | [Rpg](./Rpg/README.md) |
| Ready-to-run example scenes | [Sample scenes](./Samples.md) |
| Injectable time and exact odds for testable rules | [Clocks](./Tools/Time/Clocks.md), [IntegerChanceTable](./Tools/Random/IntegerChanceTable.md) |
| Package compatibility | [PackageCompatibility](./PackageCompatibility.md) |
| Upgrading an 11.x project | [Upgrading to 12](./Migration12.md) |
| Upgrading a 10.x project | [Migration to 11](./Migration11.md) |
| EditMode contracts and isolated test hosts | [Testing](./Testing.md) |
| PlayMode lifecycle and integration checks | [PlayMode testing](./PlayModeTesting.md) |
| Full module index | [README](./README.md) |

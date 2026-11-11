# Getting Started — NeoxiderTools

**What it is:** the installation guide and quick start. It takes you from an empty Unity 6 project to a working C# runtime model or a first audio scene in about five minutes.

**How to use:**
1. Check the [requirements](#requirements).
2. [Install](#installation) NeoxiderTools, UniTask and DOTween.
3. Pick a [starting point](#choose-your-starting-point): a [C# runtime model](#start-with-a-c-runtime-model), a [first scene](#your-first-scene-in-5-minutes-audio-manager-am) or [NoCode](./NoCode/GettingStarted.md).
4. Optionally import a demo: [Demo scenes](./Samples.md).

**Navigation:** [← To Docs](./README.md) · [Upgrading from 11.x](./Migration12.md) · [Migration from 10.x](./Migration11.md)

**По-русски:** установите пакет, UniTask и DOTween по разделу [Installation](#installation), затем выберите [C# без сцены](#start-with-a-c-runtime-model), [первый аудиокомпонент](#your-first-scene-in-5-minutes-audio-manager-am) или [NoCode](./NoCode/GettingStarted.md). Существующий проект сначала сверьте с [обновлением до 12](./Migration12.md) (с 11.x) или [миграцией на 11](./Migration11.md) (с 10.x).

---

## Requirements

| Requirement | Version |
|-------------|---------|
| Unity | **6000.0** (Unity 6) or newer; see [PackageCompatibility](./PackageCompatibility.md) |
| uGUI / TextMeshPro | `com.unity.ugui` 2.0.0+, installed by UPM. TextMeshPro ships inside it on Unity 6. |
| AI Navigation | `com.unity.ai.navigation` 2.0.16+, installed by UPM |
| Input System | `com.unity.inputsystem` 1.20.1+, installed by UPM |
| UniTask | **Required.** Install it yourself, see below. |
| DOTween | **Required** by the tween-based assemblies. Install it yourself, see below. |
| TMP Essential Resources | For the text-based demos: **Window → Text Mesh Pro → Import TMP Essential Resources** |

UniTask and DOTween are not in the `package.json` dependencies. Optional integrations (Mirror, DOTween Pro, the haptics backend, URP) are listed in [Package compatibility](./PackageCompatibility.md#package-dependencies).

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

The `upm` branch holds only the package, so Unity downloads a few megabytes. The older `?path=Assets/Neoxider` URL still works, but it fetches every file of the repository. To pin a version, replace `upm` with the commit listed in the release notes.

### Option B — Local path (for development)

Copy the `Assets/Neoxider` folder into your project, or reference it as a local package in `manifest.json`:
```json
"com.neoxider.tools": "file:../path/to/Assets/Neoxider"
```

You can also import `NeoxiderTools.v<ver>.unitypackage` from the [releases page](https://github.com/NeoXider/NeoxiderTools/releases). It holds the same files as the `upm` branch ([release archives](./Samples.md#release-archives)).

### Install UniTask (required)

Open `Packages/manifest.json` and add to `dependencies`:
```json
"com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.11"
```

The package is validated with `2.5.11`; see [Dependencies](./Dependencies.md). You can also download the `.unitypackage` from [UniTask releases](https://github.com/Cysharp/UniTask/releases).

### Install DOTween (required)

Install [DOTween (HOTween v2)](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676) from the Asset Store. Then run **Tools → Demigiant → DOTween Utility Panel** to finish its setup. DOTween Pro is optional.

## Choose your starting point

| Workflow | Start with |
|----------|------------|
| **C# runtime API** | The resource model below, then [Merge](./Merge/README.md) or [StateMachine](./StateMachine/README.md). Your code owns these objects. |
| **Scene components** | The [AM audio scene](#your-first-scene-in-5-minutes-audio-manager-am) below. Add the component and assign its dependencies. |
| **Inspector / NoCode** | [NoCode Getting Started](./NoCode/GettingStarted.md), [Condition](./Condition/README.md) and a module demo. Scene wrappers expose the matching C# API. |
| **Interaction** | [Interaction contracts and components](./Tools/InteractableObject/README.md) for custom C# input, scene hover/click and physics events. |

The package's `Prefabs/-System--.prefab` holds the existing manager bootstrap. Add only the managers your scene needs. Runtime models work without this prefab and without a scene singleton.

## Start with a C# runtime model

`ResourcePoolModel` and `ResourcePoolEntry` in `Neo.Core.Resources` are plain C# objects with no `UnityEngine` dependency. This example creates a mana pool and spends 25; the method returns 75:

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

- The two `Max*Amount` values are per-operation limits; `-1` removes the limit.
- `TrySpend` checks both the balance and the decrease limit. A cost above the limit fails without a partial payment.
- Keep the model in the service that owns the state. Call `Tick(deltaTime)` from that owner if you configure regeneration, and subscribe to `OnResourceChanged` to update a view.

For a scene wrapper, see [HealthComponent](./Core/Resources/Components/HealthComponent.md). Resource-cost integration is described in the [Core entry](./Core/README.md) and the [migration guide](./Migration11.md).

---

## Demo scenes

Demos are separate downloads, imported after the package into `Assets/NeoxiderDemos/<name>/`. [Demo scenes](./Samples.md) lists the downloads, their prerequisites and every scene.

---

## Your first scene in 5 minutes: Audio Manager (AM)

Add **AM** to a scene and play a sound from C# or without code. AM creates its music and effects AudioSources during initialization.

### Step 1 — Create a GameObject with the AM component

Create an empty GameObject and use **Add Component → Neoxider → Audio → AM**. You can also use **Neoxider → Windows → Create Neoxider Object** and select AM.

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

Attach the script to a GameObject and drag the scene's AM into its **Audio** field. `PlaySound()` can also be called from your code or a UI event. If you use the singleton entry point, `AM.I.Play("ui_click")` works after AM initialization.

### Step 4 — Play a sound without code (No-Code)

1. Select any GameObject.
2. **Add Component → Neoxider → Audio → PlayAudio**.
3. Set **Sound Id** to `ui_click`, the entry created on AM.
4. Enable **Play On Awake** to play the sound on scene start. Keep the AM object in the scene.

You can also wire the `AudioPlay()` method of `PlayAudio` to any `UnityEvent` in the Inspector.

### Step 5 — Press Play

Run the scene. The C# or NoCode component you added plays the sound.

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
| Ready-to-run example scenes | [Demo scenes](./Samples.md) |
| Injectable time and exact odds for testable rules | [Clocks](./Tools/Time/Clocks.md), [IntegerChanceTable](./Tools/Random/IntegerChanceTable.md) |
| Package compatibility | [PackageCompatibility](./PackageCompatibility.md) |
| Running and writing tests | [Testing](./Testing.md) |
| Full module index | [README](./README.md) |

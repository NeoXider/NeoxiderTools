# Getting Started — NeoxiderTools

**Goal:** get from "package installed" to a working C# runtime model or a first audio scene in about five minutes.

**11.0.0 release preparation — UNVERIFIED:** final compilation, execution of these examples, and package/sample verification are pending completion of the source changes.

**Navigation:** [← To Docs](./README.md) · [Migration from 10.x](./Migration11.md) · [Testing and isolation](./Testing.md)

**По-русски:** [C# без сцены](#start-with-a-c-runtime-model), [первый аудиокомпонент](#your-first-scene-in-5-minutes-audio-manager-am) или [NoCode](./NoCode/GettingStarted.md). Существующий проект сначала сверьте с [миграцией на 11](./Migration11.md).

---

## Requirements

| Requirement | Version |
|-------------|---------|
| Unity | **6000.0** (Unity 6) or newer — see [PackageCompatibility](./PackageCompatibility.md) |
| TextMeshPro | ships inside `com.unity.ugui` on Unity 6 |
| AI Navigation | 1.1.7+ (resolved as UPM dependency) |
| Input System | 1.14.2+ (resolved as UPM dependency) |
| UniTask | **required** — install manually (see below) |
| DOTween | **required** by tween-based runtime assemblies in the installed package; install separately |

> UniTask and DOTween are not listed in `package.json` dependencies and must be installed into the host project separately.

---

## Installation

### Option A — UPM via Git URL (recommended)

1. Open **Window → Package Manager**.
2. Click **+** → **Add package from git URL…**
3. Paste:
   ```
   https://github.com/NeoXider/NeoxiderTools.git?path=Assets/Neoxider
   ```
4. Click **Add** and wait for the import to finish.

The Git URL follows the remote default branch. To work with the current 11.0.0 preparation checkout, use the local route below.

### Option B — Local path (for development)

Copy the `Assets/Neoxider` folder into your project or reference it as a local package in `manifest.json`:
```json
"com.neoxider.tools": "file:../path/to/Assets/Neoxider"
```

### Install UniTask (required)

Open `Packages/manifest.json` and add to `dependencies`:
```json
"com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
```
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

## Importing Samples

Samples are not copied automatically after installation. To import them:

1. Open **Window → Package Manager**, find **NeoxiderTools**.
2. Go to the **Samples** tab.
3. Click **Import** next to the sample you want:

| Sample | What it contains |
|--------|-----------------|
| **Demo Scenes** | Example scenes for `AM`, `GridSystem`, `Shop`, `NoCode`, `Condition`, `StateMachine`, and more |
| **NeoxiderPages** | Optional UI page-navigation module (`PM`, `UIPage`, `BtnChangePage`) |
| **Character Movement Fundamentals** | Character-controller demos for slopes, stairs, moving platforms, gravity, and a top-down arena |

After import, files appear in `Assets/Samples/NeoxiderTools/11.0.0/<sample name>/`. For example, the audio scene is `Assets/Samples/NeoxiderTools/11.0.0/Demo Scenes/Scenes/Audio/AudioDemo.unity`.

The release sources live in `Assets/Neoxider/Samples~/`; this hidden folder is not the imported project copy. During sample development, `Assets/Neoxider/Samples/` is the alternative active source root. See [Sample scenes](./Samples.md) for the layout and scene index.

Imported copies from earlier versions remain separate. Compare customized content using [Migration to 11](./Migration11.md).

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
| Package compatibility | [PackageCompatibility](./PackageCompatibility.md) |
| Upgrading a 10.x project | [Migration to 11](./Migration11.md) |
| EditMode contracts and isolated test hosts | [Testing](./Testing.md) |
| PlayMode lifecycle and integration checks | [PlayMode testing](./PlayModeTesting.md) |
| Full module index | [README](./README.md) |

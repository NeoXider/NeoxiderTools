# Getting Started — NeoxiderTools

**Goal:** get from "package installed" to "first working scene" in ~5 minutes.

**Navigation:** [← To DocsEn](./README.md) · [Russian version](../Docs/GettingStarted.md)

---

## Requirements

| Requirement | Version |
|-------------|---------|
| Unity | **2022.1** or newer |
| TextMeshPro | 3.0.6+ (resolved as UPM dependency) |
| AI Navigation | 1.1.7+ (resolved as UPM dependency) |
| Input System | 1.14.2+ (resolved as UPM dependency) |
| UniTask | **required** — install manually (see below) |
| DOTween | required by `Cards`, `UI`, `Tools/View`, `Tools/Text` modules; install if you use those |

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

After import, files appear in `Assets/Samples/NeoxiderTools/<version>/`.

---

## Your first scene in 5 minutes: Audio Manager (AM)

The fastest way to verify the package works is to add the central **AM** audio manager and play a sound. AM is a singleton with no extra dependencies.

### Step 1 — Create a GameObject with the AM component

In the Hierarchy, right-click → **Neoxider → Audio → AM** (or use the menu **GameObject → Neoxider → Audio → AM**).

This creates an `AM` object in the scene with the component already attached. AudioSources for effects and music are created automatically.

### Step 2 — Assign an audio clip

1. Select the `AM` object in the Hierarchy.
2. In the Inspector, find the **Sounds** array.
3. Increase the size by 1 (press **+**).
4. Drag any `AudioClip` from your assets into **Element 0 → Clip**.

### Step 3 — Play a sound from code

Create a MonoBehaviour and call `AM.I.Play(0)`:

```csharp
using Neo.Audio;
using UnityEngine;

public class SoundTest : MonoBehaviour
{
    private void Start()
    {
        // Play sound at index 0 from the Sounds array
        AM.I.Play(0);
    }

    private void Update()
    {
        // Play sound on Space key press
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AM.I.Play(0);
        }
    }
}
```

Attach this script to any GameObject in the scene.

### Step 4 — Play a sound without code (No-Code)

If you prefer not to write a script, use the **PlayAudio** component:

1. Select any GameObject.
2. **Add Component → Neoxider → Audio → PlayAudio**.
3. Drag an `AudioClip` into the **Clips** array.
4. Enable **Play On Awake** to play the sound on scene start.

Alternatively, wire the `AudioPlay()` method of the `PlayAudio` component to any `UnityEvent` in the Inspector — zero lines of code.

### Step 5 — Press Play

Run the scene. The sound plays automatically (or on Space if you used `SoundTest`).

> **Tip:** in the AM Inspector you can click the **Play(int id)** buttons directly in Edit mode — useful for previewing clips without entering Play mode (buttons appear thanks to the `[Button]` attribute).

---

## Where to next

| What to explore | Document |
|-----------------|----------|
| Full audio manager API | [Audio/AM](../Docs/Audio/AM.md) |
| Volume and mute settings | [Audio/AMSettings](../Docs/Audio/AMSettings.md) |
| Button-triggered sounds | [Audio/PlayAudioBtn](../Docs/Audio/PlayAudioBtn.md) |
| No-code conditions and events | [Condition](./Condition/README.md) |
| Scene and global save | [Save](./Save/README.md) |
| Grid games (Dice, Match3, 2048) | [GridSystem](./GridSystem/README.md) |
| Shop and currency | [Shop](./Shop/README.md) |
| Movement and input | [Tools](./Tools/README.md) |
| RPG characters and combat | [Rpg](./Rpg/README.md) |
| Ready-to-run example scenes | [Sample scenes](./Samples.md) |
| Package compatibility | [PackageCompatibility](./PackageCompatibility.md) |
| Full module index | [README](./README.md) |

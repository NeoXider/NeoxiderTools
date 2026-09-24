# Mobile casual game on NeoxiderTools — assembly notes

How the package pieces fit together in a shipped portrait puzzle game (Loading scene → Game scene with
~15 pages: menu, game, win, lose, gift, shop, settings, daily, free play, city map, rate, age gate).
Use it as the checklist of *which Neo piece does what*, and which small pieces stay in the game.

## Scene and page flow

- **Two scenes:** `Loading` (index 0 in build settings) and `Game`. Loading shows a progress bar, then
  the age question if enabled, then activates the preloaded Game scene. Preload with
  `SceneManager.LoadSceneAsync(name)` + `allowSceneActivation = false`, and report
  `min(real progress / 0.9, elapsed / minimumSeconds)` so the bar never lies and never flashes past.
  `FakeLoad` (NeoxiderPages) is for mock loading only — do not drive a real scene switch from it.
  Start the async load one frame after `OnEnable` (a synchronous scene activation can flush pending
  async operations).
- **Pages:** one `PM` root, every `UIPage` below it, `PageId` assets per page, `PM.I.ChangePage(id)`.
  Popups (`UIPage` marked popup) stack over the exclusive page and close on the next exclusive switch
  (`closePopupsOnExclusivePageChange`). Read `PM.I.currentUiPage.PageId` for Back handling.
- **Back (Android):** one handler maps the current page to the same action as its on-screen back/close
  button; result screens ignore Back; the root menu minimises the app. With the Input System, check
  Escape on every `Keyboard` device and treat `Application.wantsToQuit` as Back on Android, handled
  once per frame (recipe: `Docs/Cookbook.md` → "Android Back over PM pages").

### NeoxiderPages sample versions (gotcha)

NeoxiderPages is a **sample**, copied into the game at import time:
`Assets/Samples/NeoxiderTools/<version at import>/NeoxiderPages/`. Bumping the package pin does **not**
update that copy. After a package fix in the sample (for example 10.16.1: `PM` editor preview no longer
runs in Play Mode), re-import the sample from Package Manager or copy the changed files — and expect the
folder to keep the *old* version name, so compare contents (ignoring BOM/line endings), not the folder
name, when checking which fix a project has.

## Audio — `AM`

- Music per context (menu / gameplay) with crossfade, plus the **ambience layer** (10.16.0):
  `AM.I.PlayAmbience(clip)`, `AM.I.AmbienceVolume = 0.3f` — a night-lake bed keeps playing under
  both music tracks and follows the music volume and mute.
- **One tap sound for every button:** hook all `Button`s under the `PM` root once at start
  (`GetComponentsInChildren<Button>(true)` includes inactive pages) instead of a `PlayAudioBtn` per
  button.
- **Pause in the background and during rewarded ads** is a game-level owner of `AudioListener.pause`
  (OR of "ad playing" and "app in background"; created `BeforeSceneLoad`, `DontDestroyOnLoad`). `AM`
  keeps its clip positions, so music resumes where it stopped. Do not toggle `AudioListener.pause`
  from two places.

## Haptics — `Neo.Haptics`

Install `com.tsyk5.mobilehapticfeedback` for real iOS/Android feels (without it only coarse
`Handheld.Vibrate` fallbacks fire). Bind the settings toggle once:
`Haptics.EnabledProvider = () => settings.Vibration;` then `Haptics.Play(HapticType.Success)` on a
solved board, `Selection` on a grab, `Warning` on a locked tap. The module throttles repeats.

## Save

Small flags go through `SaveProvider` (`GetInt`/`SetInt`/`Save`, namespace `Neo.Save`) — for example
the age-gate answer read by the Loading scene before `Game` exists. Keep the game's profile (wallet,
progress, hints per board) in one serialised model with repair-on-load for corrupted or old saves.

## Reward flights — `AnimationFly`

`AnimationFly` does the motion; the game decides *when* and *where*:

```csharp
int from = balanceAfter - amount;               // the save already holds balanceAfter
showCounter(from);
int arrived = 0, items = Mathf.Clamp(amount, 1, 12);
AnimationFly.I.Play(new AnimationFly.AnimationFlyRequest
{
    Sprite = coinSprite, Count = items,
    StartTransform = source, EndTransform = counterIcon,
    StartSpace = AnimationFlyCoordinateSpace.Canvas, EndSpace = AnimationFlyCoordinateSpace.Canvas,
    SpawnSpace = AnimationFlySpawnSpace.Canvas, Parent = overlayLayer,
    MotionPreset = AnimationFlyMotionPreset.FountainMagnet,
    EndScaleMultiplier = 0.3f, DelayBetweenItems = 0.05f,
    RewardTiming = AnimationFlyRewardTiming.Manual, CompletionMode = AnimationFlyCompletionMode.Destroy,
    OnItemArrived = _ => { arrived++; showCounter(from + Mathf.RoundToInt(amount * (arrived / (float)items))); },
    OnAllArrived = () => showCounter(balanceAfter),
});
```

- A reward earned on a popup that is about to close (Win, Gift) is **queued** and flown when the
  destination page has opened and finished its entrance (~0.35 s) — into *that* page's counter.
- Rewards collected on the page that shows the counter (map pins, shop) fly immediately, from the
  collected object.
- The counter starts at the old balance and lands exactly on the real one; the save is written first.

## Motion that is not in the package (keep it in the game)

These are small, game-level components; write them once per project:

- **Press feedback on every button** (scale 0.93 on down, `OutBack` back to the rest scale taken in
  `Awake`; release on up and on exit; restore on disable) — injected at start by walking the `PM`
  root, skipping objects that are not real buttons.
- **Staggered page entrance** (alpha + scale per item, `blocksRaycasts` off until each item starts
  to appear, `SetUpdate(true)`, `SetLink`).
- **Result-screen reveal order**: headline → earned → progress → rewarded offer (with an
  unscaled-time shine) → the free exit after a tunable delay.

Raise DOTween capacity before the first burst: `DOTween.SetTweensCapacity(500, 200)` in a
`RuntimeInitializeOnLoadMethod(BeforeSceneLoad)`.

## Economy config

Keep prices, rewards, unlock levels, timers and pack contents in one `ScriptableObject` (defaults in
code, overridable in the asset), and cover the spec values with one EditMode test. Check pack pricing
makes larger packs cheaper per unit — a 5-pack priced like a 1-pack shipped once and was caught only by
the owner.

## Editor workflow with MCP

- `[Button(PlayModeOnly = true)]` for cheats (add coins, unlock map, complete level) instead of
  custom editors.
- Save scenes after every scripted edit and check `isDirty == false`: a dirty scene raises a modal save
  dialog on the next play/test/scene switch and blocks every MCP call.

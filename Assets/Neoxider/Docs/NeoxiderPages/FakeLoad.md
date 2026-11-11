# FakeLoad (NeoxiderPages)

**What it is:** fake loading for a loading page: simulates progress over a random duration and fires progress events. Script `Assets/NeoxiderDemos/NeoxiderPages/Runtime/Scripts/Page/FakeLoad.cs`, namespace `Neo.Pages`. This is the Pages sample component; the package's own `Neo.UI.FakeLoad` is documented in [UI/FakeLoad](../UI/FakeLoad.md).

**How to use:**
1. **Add Component → Neoxider → Pages → FakeLoad** on the loading page.
2. Bind **On Change** (`float`) to a slider or fill image and **On Finis Load** to opening the next page.
3. Keep **Load On Awake** on, or call `Load()` yourself.

---

## Fields

- **Time Load** — minimum and maximum duration of the "loading" (sec).
- **Load On Awake** — start on Awake.
- **Is Load One** — show the fake progress only once per application session; later `Load()` calls complete on the next frame (outside Play Mode at once). The final ticks and **On Finis Load** still fire, so a loading page waiting for it closes. The one frame matters when the loading page is PM's startup page: `Load()` then runs from `Awake` inside `PM.Start`, and a page switch from **On Finis Load** at that moment would be undone by PM's own startup switch.
- **Use Unscaled Time** — on by default: progress ignores `Time.timeScale`, so a pause left over from the previous scene does not freeze the loader. Turn it off for scaled time. Same as `Neo.UI.FakeLoad`.

A `Load()` while a load is running restarts it, so **On Finis Load** fires once. The one-shot flag resets for every Play
session, also with Enter Play Mode Options (no domain reload).

A load pauses while its GameObject is inactive and continues where it stopped when the object is active again (`PM`
hides every page before it shows the startup page, which used to kill the load for good). `Load()` on an inactive
object starts the load on activation.

## Events

- **On Start** — fired when loading begins.
- **On Change** (`float`) — normalized progress `0..1`; a final `1` tick fires on completion.
- **On Change Percent** (`int`) — progress `0..100`; a final `100` tick fires on completion.
- **On Finis Load** — fired when loading completes (also invoked by a manual `EndLoad()`).

## API

- **Load()** — starts the fake load (completes on the next frame when **Is Load One** already ran this session).
- **EndLoad()** — stops a running load and completes immediately, emitting the final full-progress ticks and **On Finis Load**.
- **IsLoading** — true from `Load()` until completion, including the one-frame wait of a one-shot load and a load paused on an inactive object.
- **UseUnscaledTime** — same as the field.

## See also

- [NeoxiderPages README](./README.md)
- [PM](./PM.md)

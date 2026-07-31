# FakeLoad (NeoxiderPages)

**What it is:** Fake loading: simulates progress over a given time range and fires events (for example, for a loading screen).

**How to use:** see the sections below.

---


Fake loading: simulates progress over a given time range and fires events (for example, for a loading screen).

**Add:** Neoxider → Pages → FakeLoad.

## Fields

- **Time Load** — minimum and maximum duration of the "loading" (sec).
- **Load On Awake** — start on Awake.
- **Is Load One** — once per session.

## Events

- **On Start** — fired when loading begins.
- **On Change** (`float`) — normalized progress `0..1`; a final `1` tick fires on completion.
- **On Change Percent** (`int`) — progress `0..100`; a final `100` tick fires on completion.
- **On Finis Load** — fired when loading completes (also invoked by a manual `EndLoad()`).

## API

- **Load()** — starts the fake load (skipped when **Is Load One** already ran this session).
- **EndLoad()** — completes immediately, emitting the final full-progress ticks and **On Finis Load**.

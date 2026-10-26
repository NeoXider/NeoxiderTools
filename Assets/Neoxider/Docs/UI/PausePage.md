# PausePage

`Neo.Tools.PausePage` applies pause behavior while its GameObject is active. It can change
`Time.timeScale`, call `GM.Pause`/`Resume`, and unlock/show the cursor. Active pages share ownership of
each channel, so overlapping screens can close in either order without restoring another page's snapshot.

## Setup

1. Add `Neoxider > Tools > PausePage` to the pause screen and keep that screen inactive initially.
2. Enable `_useTimeScale` for time-scale control and choose `_timeScaleOnPause` (`0` for a full pause).
3. Enable `_sendPause` when the scene's `GM` should receive pause/resume calls.
4. Enable `_controlCursor` to unlock/show the cursor while the screen is open. Choose whether closing
   restores the previous cursor state or forces a locked, hidden cursor.

The same workflow works from C# and UnityEvents:

```csharp
pauseScreen.SetActive(true);  // Join the shared page pause/cursor requests.
pauseScreen.SetActive(false); // Release this page; the last owner restores the baseline.
```

There is no separate `PausePage.Pause()` API. The optional same-object Animator is configured to use
unscaled time during Inspector validation so its pause-screen animation can run at time scale zero.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `AfterPauseCursorBehavior` | After Pause Cursor Behavior. |
| `_afterPauseCursor` | Restore the first cursor baseline, or request a final locked/hidden cursor. |
| `_controlCursor` | Join cursor ownership; active cursor owners keep it visible and unlocked. |
| `_useTimeScale` | Join shared time ownership. |
| `_sendPause` | Join the shared GM pause request. Only the exact acquired transition can be resumed. |
| `_timeScaleOnPause` | Nonnegative requested scale; the lowest active page request wins. A time-controlling GM pause keeps scale zero. |

## Ownership

No coordinator component or additional scene wiring is required. The first page requesting a channel
captures its baseline; later pages retain that same baseline. Closing one page recalculates the remaining
requests. Time and cursor ownership are independent: a cursor-only page does not delay restoration of time.

The first GM requester attempts to pause the available manager. GM receives the original time scale even
if another page already holds time at zero. The last GM requester resumes only the same live/current manager,
only if this group actually entered Pause and that transition is still current. An already paused manager,
a replacement manager, or a later external resume/re-pause does not receive the old group's Resume. An owned
GM is resumed before the final page time baseline is restored, preserving the GM's resume-observer order.

While any cursor owner remains, its visible/unlocked request takes precedence over an earlier page's
`ForceLockedHidden` exit. An explicit force-lock request is remembered until the final cursor owner closes;
then the cursor is locked/hidden. If none requested that policy, the first cursor baseline is restored.

```csharp
pauseScreen.SetActive(true);
settingsScreen.SetActive(true); // Each screen can carry its own PausePage.
pauseScreen.SetActive(false);   // settingsScreen still owns its requested channels.
settingsScreen.SetActive(false);
```

Options and requested values are captured when a page is enabled and are released even if Inspector/code
changes the fields while it is open. Re-enable the page to apply new options. Shared synchronization handles
pages opened/closed by GM observers, with bounded repeated reconciliation and at most one shared frame update.

## 11.0 compatibility

The serialized names, enum values and defaults are unchanged. Existing scenes automatically receive shared
first/last ownership instead of independent snapshots. A page no longer resumes somebody else's GM pause,
and an early force-lock exit no longer hides the cursor under another active cursor page.

This is scoped page ownership, not an arbitration service for arbitrary direct `Time.timeScale`/cursor
writers. Avoid mixing raw time writes or `SceneFlowController.Pause(bool)` with a page-owned pause. Cursor
controllers and other global owners retain their own contracts. No ScriptableObject scene references or
UnityEvent-only game rules are needed; the active-object workflow above is the public C# and scene path.

## See Also

- [Module Root](../README.md)

# TimerObject

**Namespace:** `Neo`  
**File:** `Assets/Neoxider/Scripts/Tools/Time/TimerObject.cs`

## Purpose

MonoBehaviour-based timer with Unity events, optional UI auto-update (Image fillAmount, TMP_Text), save/load support, and milestones. Counts up or down, supports looping, unscaled time, and random duration.

## Fields (Inspector)

| Section | Key fields |
|---------|------------|
| **Timer Settings** | duration, updateInterval, countUp, useUnscaledTime, pauseOnTimeScaleZero, looping, infiniteDuration |
| **Random Duration** | useRandomDuration, randomDurationMin, randomDurationMax |
| **Initial State** | autoStart, initialProgress, isActive, currentTime |
| **UI Auto Update** | progressImage, timeText, timeFormat, fillImageNormal |
| **Visual Feedback** | enableStartAnimation, startAnimationScale, startAnimationDuration |
| **Events** | OnTimerStarted, OnTimerPaused, OnTimerResumed, OnTimerStopped, Time (ReactivePropertyFloat), OnProgressChanged, OnProgressPercentChanged, OnTimerCompleted |
| **Progress Milestones** | enableMilestones, milestonePercentages, OnMilestoneReached |
| **Save** | saveProgress, saveMode (Seconds/RealTime), saveKey |

## API

- **Play()**, **Stop()**, **Pause(bool)**, **TogglePause()**, **Reset()**
- **StartTimer(float newDuration, float newUpdateInterval)** — optional new duration, then reset + play
- **SetDuration(float newDuration)** — change **`duration`** and **preserve** the progress ratio (same as the two-argument form with `keepProgress: true`).
- **SetDuration(float newDuration, bool keepProgress)** — same with an explicit flag: **true** preserves ratio; **false** only changes the cap and clamps `currentTime` to `[0, duration]`. Negative values clamp to **0**. **`SetProgress`** only moves along the current duration; use **SetDuration** or **StartTimer** to change total length.
- **SetTime(float)**, **AddTime(float)**, **SetProgress(float)**
- **GetProgress()**, **GetCurrentTime()**, **GetRemainingTime()**
- **TimeValue** — current time in seconds (for NeoCondition/reflection)

## See also

- [Timer](Timer.md)
- [Bonus/TimeReward/CooldownReward](../../Bonus/README.md)
- [Save](../../Save/README.md)
- [README](README.md)

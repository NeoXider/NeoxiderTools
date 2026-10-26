# ScoreManager

`Neo.Tools.ScoreManager` is a scene score source built on `Singleton<ScoreManager>`. It exposes current
score, persistent best score, target, progress, and stars as reactive properties.

## C# API

```csharp
ScoreManager score = ScoreManager.I;
score.TargetScoreValue = 200;
score.Set(50, updateBestScore: false);
float progress = score.ProgressValue;  // 0.25
int stars = score.CountStarsValue;     // 1 with the default thresholds
score.Add(25);
score.ResetScore();
```

`Set(int, bool)` and `Add(int, bool)` optionally update the best score; the default is true.
`SetBestScore(int?)` raises the record using its argument or the current score. `ResetBestScore()`
sets the record to zero, deletes its save key, and refreshes both reactive output and configured labels.
`ResetScore()` affects only current score and its derived output.

`Score`, `BestScore`, `TargetScore`, `Progress`, and `CountStarsReactive` support code subscriptions:
use `Subscribe` and dispose the handle when the consumer's lifetime ends. The serialized target is
published to `TargetScore` during initialization. Setting `TargetScoreValue` refreshes both progress
and stars immediately, without requiring another score change. Mutate through the manager API;
writing reactive output fields directly does not update the manager's backing fields or labels.

## Inspector and scene workflow

Add **Neoxider / Tools / ScoreManager**, choose a unique best-score save key, and set the target.
Configure `textScores`/`textBestScores` for plain TMP labels or `setTextScore`/`setTextBestScores` for
formatted [SetText](../Text/SetText.md) views. Missing arrays and null entries are ignored.
[TextScore](TextScore.md) is an alternative self-binding label with an explicit source override.

With **Use Progress** enabled, `starScores` contains ascending thresholds in normalized progress;
the defaults are 0.25, 0.5, and 0.75. Otherwise thresholds are absolute score values. Evaluation stops
at the first unmet threshold, so keep the array ordered. A null or empty array awards zero stars.
Progress is clamped to 0–1, and progress-based stars require a positive target.

`GetCountStars(float[], bool, int?)` and `IsStar(float, bool, int?)` evaluate thresholds without changing
the score. `CountStars` can also be assigned explicitly; later score/target changes recompute it.

See [Components](README.md) and [TextScore](TextScore.md).

# TypewriterEffect and TypewriterEffectComponent

**What it is:** `TypewriterEffect` is a serializable plain C# playback object (UniTask) that reveals text character by
character; `TypewriterEffectComponent` is the `MonoBehaviour` that adapts it to a TMP label and UnityEvents. Scripts:
`Scripts/Tools/Components/TypewriterEffect.cs` and `TypewriterEffectComponent.cs`, namespace `Neo.Tools`, assembly
`Neo.Tools.Components`.

**How to use:**
1. Scene: add **Neoxider / Tools / TypewriterEffectComponent** next to a TMP label, set **Reveal Mode**
   (`MaxVisibleCharacters` recommended for long text) and leave **Auto Start** on, or call `Play(text)`.
2. Code: create a `TypewriterEffect` and await `PlayAsync` (string callback) or `PlayRevealAsync` (visible count).
3. Subscribe to `OnCharacterTyped` / `OnComplete` (C# events) or the component's UnityEvents.

---

## C# playback

```csharp
TypewriterEffect effect = new TypewriterEffect(50f, useUnscaledTime: true);
await effect.PlayAsync("<b>Hello</b>", text => label.text = text, cancellationToken);
```

Allocation-free reveal for TMP labels: assign the full text once and raise `maxVisibleCharacters`.

```csharp
label.text = line;
label.maxVisibleCharacters = 0;
await effect.PlayRevealAsync(line, count => label.maxVisibleCharacters = count, cancellationToken);
label.maxVisibleCharacters = 99999; // TMP default: show everything (covers sprites and <br>)
```

`PlayAsync` builds a new prefix string for every step; `PlayRevealAsync` reports the number of revealed visible
(non-tag) characters instead, so typing costs no allocation per character and words never jump between lines
while they are typed. Both replace the current run. `Stop()` cancels playback
without completing it and preserves current text. `Complete()` reveals full text, emits progress and
completion, and returns the completed text. `Reset()` stops and clears internal state.

Read `IsTyping`, `CurrentText`, `FullText`, and `Progress`. Subscribe to `OnStart`, `OnCharacterTyped`,
`OnProgressChanged`, and `OnComplete` as needed. Natural completion reports `IsTyping == false` to
completion handlers. Callbacks can stop, reset, complete, or start another run: the replaced run stops
producing further callbacks after that callback returns. `Complete()` returns its original text even
if a completion handler starts another run. Explicit `Complete()` emits completion even when idle;
use the scene wrapper's `TrySkip()` when completion should require active typing.

External cancellation is an expected stop. Exceptions from user callbacks fault the returned task and
release playback resources. Observe or await the returned UniTask. Use the Unity main thread for
label/audio callbacks.

## Scene and UnityEvent workflow

1. Add **Neoxider / Tools / TypewriterEffectComponent** and assign **Target Text**, or let it resolve
   a TMP component on the same object.
2. Configure **Auto Start Text**, or leave it empty to capture the authored label text during `Awake`.
   Authored text remains visible until playback starts.
3. **Auto Start** plays once in `Start`. **Play On Enable** plays on the first start and on subsequent
   enables, including when Auto Start is off. Enabling both settings does not double-start the first run.
4. Wire the wrapper's `OnStart`, `OnCharacterTyped`, and `OnComplete` UnityEvents to scene behavior.
   `Progress` is reactive, and `ProgressValue` is available to bindings.

`Play(string)` starts explicit text; an empty argument uses the current label text. `PlayAutoText()`
uses the captured/configured auto text. `Complete()` updates the label before notifying completion,
so a handler may inspect the full text and safely start the next line. `TrySkip()` completes an active
run and otherwise returns false. `Clear()` resets the label, effect, and reactive progress to zero.
Disabling stops playback; destruction also detaches event forwarding from an externally retained effect.

`PlayAutoText`, `Stop`, and `Clear` have Play Mode-only Inspector buttons.

### Reveal mode

| Mode | Behavior |
|------|----------|
| `RebuildText` (default) | Assigns the typed prefix as a new string every character (the classic behavior; layout reflows while typing). |
| `MaxVisibleCharacters` | Assigns the full text once and raises `TMP_Text.maxVisibleCharacters` through `PlayRevealAsync`: no per-character string, no word-wrap jumps. |

After a `MaxVisibleCharacters` run, natural completion, `Complete()`, `Clear()` and the next `RebuildText` run
restore `maxVisibleCharacters` to TMP's unlimited default (99999); `Stop()` (and disabling) keeps the current
reveal. The component restores whatever limit it set itself, whatever `RevealMode` is now, so switching the mode
during a run never leaves the label truncated. The reveal count skips rich-text tags, so inline sprites and `<br>`
(one TMP character each) can appear one step early or late; completion always shows everything. Set the mode from
code with `RevealMode`; it applies to the next `Play` call.

## Timing, rich text, and audio

Configure characters per second, scaled/unscaled time, and punctuation pauses through the effect's
properties or serialized Inspector fields. `SetPunctuationPause`, `RemovePunctuationPause`,
`SetPunctuationPauses`, and `ClearPunctuationPauses` update the pause map. Call `RebuildPauseMap()` after
editing `PunctuationPauses` directly; `PlayAsync` / `PlayRevealAsync` also rebuild it.

The rate is kept over time, not per frame: each wait resumes on the first frame at or past its due time, and that
overshoot is spent on the next characters. When a character's delay is shorter than a frame, several characters
are typed in one frame, so 70 characters per second stays 70 at 60 fps (before 12.0 the run typed at most one
character per frame, about 60 per second). A delay below 0.5 ms does not wait at all, so very high rates still
complete synchronously. In Edit Mode, where the frame clock may not advance, each character falls back to one
plain delay.

The catch-up is capped at 0.1 s. That keeps the configured rate down to 10 fps, but one long frame (a loading
hitch, a GC spike, an app resume with unscaled time) types at most 0.1 s worth of extra characters, about 7 at
70 per second, instead of revealing the rest of the line at once; it also shortens a punctuation pause by at most
0.1 s. The overshoot is measured on the double-precision frame clock (`Time.timeAsDouble` or
`Time.unscaledTimeAsDouble`), so the pace stays exact in long sessions.

The current tokenizer treats angle-bracket sequences as tags and counts other UTF-16 characters for
progress/audio cadence. It does not provide grapheme-aware reveal or full TMP tag semantics.
Assign a typing audio source and optional clip, enable typing sound, and choose the character cadence.

See [Components](README.md) and [Dialogue](../Dialogue/README.md).

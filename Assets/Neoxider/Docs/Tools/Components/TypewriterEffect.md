# TypewriterEffect and TypewriterEffectComponent

`Neo.Tools.TypewriterEffect` is a serializable C# playback object using UniTask.
`TypewriterEffectComponent` adapts it to a TMP label and UnityEvents.

## C# playback

```csharp
TypewriterEffect effect = new TypewriterEffect(50f, useUnscaledTime: true);
await effect.PlayAsync("<b>Hello</b>", text => label.text = text, cancellationToken);
```

`PlayAsync` replaces the current run and reports text through its callback. `Stop()` cancels playback
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

## Timing, rich text, and audio

Configure characters per second, scaled/unscaled time, and punctuation pauses through the effect's
properties or serialized Inspector fields. `SetPunctuationPause`, `RemovePunctuationPause`,
`SetPunctuationPauses`, and `ClearPunctuationPauses` update the pause map. Call `RebuildPauseMap()` after
editing `PunctuationPauses` directly; `PlayAsync` also rebuilds it.

The current tokenizer treats angle-bracket sequences as tags and counts other UTF-16 characters for
progress/audio cadence. It does not provide grapheme-aware reveal or full TMP tag semantics.
Assign a typing audio source and optional clip, enable typing sound, and choose the character cadence.

See [Components](README.md) and [Dialogue](../Dialogue/README.md).

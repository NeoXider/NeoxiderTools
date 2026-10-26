# Box

Exchanges one positive **Max Progress** threshold for a locked item from `Collection.I`.

## Scene setup

Assign a unique **Save Name**, progress amounts, and any optional bar, text, closed/open box sprites,
prize image, and prize animation object. A `Collection` must be available to grant items.
Wire gameplay rewards to `AddProgress()` or `AddProgress(float)`, and the claim button to `TakePrize()`.

## C# API

```csharp
box.AddProgress(100f);
if (box.CheckProgress)
{
    box.TakePrize();
}
```

`progress` is the persisted value (`<Save Name>progress`). Assigning it updates `Progress`, the optional
bar, and text immediately. `AddProgress` and `ChangeProgress` also emit `OnChangeProgress(bool)` and
the corresponding `OnProgressReached` / `OnProgressNotReached` event. These events describe the state
after each change; they are not limited to threshold crossings. `ProgressValue` exposes the reactive value.

A nested progress command from a save-key observer supersedes the outer setter's pending reactive
and visual publication, including same-value writes and changes that return to the earlier value.
For example, taking a threshold of 10 from progress 20 while a one-shot progress-key observer adds 5
leaves raw, saved, and reactive progress at 15 before collection observers run. The price is still 10;
the nested credit is an independent command. Later ordinary progress commands publish normally.

For a positive threshold, `CheckProgress` and `TakePrize` require a strictly smaller finite stored float
candidate using the same clamped subtraction as payment. At progress 100,000,000, a threshold of
1 rounds to no decrease, so readiness is false and no prize, save, or progress notification occurs.
A threshold of 5 at that magnitude stores a decrease of 8 and remains valid; payment is not required to
equal the nominal threshold exactly. Ordinary fractional thresholds remain supported. Authored zero
thresholds retain their legacy behavior: a valid item can be granted without consuming progress, including
when progress is zero. Use a positive threshold for a paid box.

`TakePrize()` consumes one threshold only after an item is actually granted. An exhausted/missing
collection leaves progress unchanged. Recursive takes from collection or prize callbacks are ignored;
a later explicit take can consume another threshold. `OnTakePrize` fires after progress and visuals update.

In 11.0, an accepted unlock and the clamped remaining progress are recorded before Collection
UnityEvents run. Progress observers therefore run before `OnItemAdded`, completion events, and
`OnGetItem`. A throwing observer propagates its exception and can stop later notifications/animation;
it does not restore the accepted prize or its price. The take guard is always released, so a retry needs
another full threshold. Even a save-key observer throwing after accepting the unlock still settles its
progress cost. Save-provider key notifications are individual writes, not an atomic multi-key transaction.

В 11.0 открытый предмет и потраченный прогресс сохраняются до событий коллекции. Исключение
подписчика не возвращает оплату; если доступного приза нет, прогресс не списывается.
Вложенная запись прогресса не затирается внешней публикацией. Положительный порог должен реально
уменьшать сохранённое значение `float`; нулевой порог сохраняет прежнее поведение без оплаты.

## See Also

- [Module Root](../README.md)

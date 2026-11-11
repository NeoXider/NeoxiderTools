# BoxProgress

**What it is:** a plain C# "piggy bank" core (`Neo.Bonus.BoxProgress`, `Scripts/Bonus/Collection/BoxProgress.cs`) with integer progress (for example cents), a claim threshold and idempotent claims, plus the shared `BoxProgressRules` used by the [Box](Box.md) component. A claim id that was already processed returns its original receipt instead of consuming progress again, so retried requests and replayed saves cannot pay twice.

**How to use:**
1. Create it with a threshold: `new BoxProgress(threshold: 500)`.
2. Credit progress with `Add(amount)`; read `Progress`, `CanClaim` and `FillRatio` for UI.
3. Claim with `TryClaim(claimId, out BoxClaimReceipt receipt)`; pass a stable id (purchase id, run id) to make retries safe.
4. Persist `Capture()` (a JsonUtility-friendly `BoxProgressState`) and restore it with `Restore(state)` after a restart.

---

## BoxProgress members

| Member | Description |
|--------|-------------|
| `BoxProgress(long threshold, long progress = 0, int rememberedClaims = 32)` | Threshold 0 makes every claim free. `rememberedClaims` receipts are kept for idempotency. |
| `long Progress` | Current progress, never negative. |
| `long Threshold` | Progress consumed by one claim (clamped to 0 or more). |
| `long ClaimSequence` | Number of claims so far. |
| `bool CanClaim`, `float FillRatio` | Readiness and bar fill 0..1. |
| `long Add(long amount)` | Adds (negative removes), saturates at `long.MaxValue`, never below 0. |
| `void SetProgress(long value)` | Sets progress directly. |
| `bool TryClaim(out BoxClaimReceipt)` / `bool TryClaim(string claimId, out BoxClaimReceipt)` | Claims once; a known id returns its receipt with `IsReplay = true` and changes nothing. A null or empty id makes an automatic claim that is never treated as a replay. |
| `bool HasProcessedClaim(string)`, `bool TryGetReceipt(string, out BoxClaimReceipt)` | Idempotency queries. |
| `BoxProgressState Capture()`, `void Restore(BoxProgressState)` | Save snapshot (progress, sequence, recent receipts). `Restore` raises no events. |

Events: `ProgressChanged(long previous, long current)` and `Claimed(BoxClaimReceipt)`. Progress is consumed before `Claimed` runs; replays raise nothing.

`BoxClaimReceipt` fields: `ClaimId`, `Sequence`, `ConsumedAmount`, `RemainingProgress`, `IsReplay`. `ClaimId` is
null for an automatic claim, which is identified by `Sequence` only, so a caller id such as `"2"` never collides
with automatic claim number 2.

## BoxProgressRules

Static rules shared by `Box` (float) and `BoxProgress` (long): `CanClaim`, `GetProgressAfterClaim` and `GetFillRatio`. A non-positive threshold is a free claim. The float overload also refuses a claim whose threshold is lost to float precision (progress would not decrease), so a huge progress value cannot be farmed forever.

## Example

```csharp
BoxProgress bank = new(threshold: 1000);
bank.Claimed += receipt => wallet.Add(receipt.ConsumedAmount);

bank.Add(coinsEarnedThisRun);
if (bank.CanClaim && bank.TryClaim("run-" + runId, out BoxClaimReceipt receipt) && !receipt.IsReplay)
{
    SaveProvider.SetString("PiggyBank", JsonUtility.ToJson(bank.Capture()));
}
```

## See also

- [Box](Box.md) — scene/UI component with float progress.
- [Collection README](README.md)

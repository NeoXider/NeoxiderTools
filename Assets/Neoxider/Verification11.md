# 11.0.0 verification record

This records the Windows validation performed on `demo` on 26–27 October 2026 (receipt timestamps use UTC). It distinguishes
full-suite baselines, current affected tests, capability-specific runs and manual observations.
The groups overlap and must not be added together as one fresh all-pass run.

## Tested environments

- Unity `6000.3.14f1` and `6000.6.0f1`: both branches of the shared object-identity compatibility helpers.
- Mirror `96.11.3`, UniTask `2.5.11`, Input System `1.20.1`, AI Navigation `2.0.16`, uGUI `2.0.0`.
- Mirror present and physically absent; legacy/Both and Input System Package (New) input profiles.
- A true embedded UPM package with all three samples imported through Unity's public Sample API,
  and DOTween Pro physically absent.
- Separate Windows server, client A and late client B using the production Telepathy transport.

Unity's declared package minimum remains `6000.0`. These checks do not certify every Unity 6 version,
IL2CPP, mobile hardware, consoles, HDRP or audible device output. See [dependency pins](Docs/Dependencies.md).

## Automated evidence

| Scope | Recorded result on each tested SDK |
| --- | --- |
| Full Edit Mode baseline | 4596 positive cases; remaining native cursor controls verified separately in visible editors. One Unity 6.6 Burst transport failure was retained and the affected five-test group passed on retry. |
| Full Play Mode baseline | 726 positive cases, four native cursor controls verified separately, one explicit HDRP/Lit capability skip. |
| Visible editor controls | 35 Edit Mode cases including end-of-frame rendering, and four Play Mode cursor/lifecycle cases passed. |
| Mirror absent | Six selected Solo/scene contracts passed; discovery recorded the absent integration. |
| New input backend | 17 Edit Mode and 92 batch Play Mode cases passed. The remaining strict mouse/UI case passed in a focused Game View with an actual pointer, real rendered graphic and `legacy=False`. |
| Fresh imported UPM samples without Pro | All three samples imported; 28 Edit Mode cases (P30) and 20 Play Mode cases (P31) passed with no skips. GUID/resource checks include the legacy integration collection. |
| Cards regressions | 149 Edit Mode cases passed per SDK (P27). The 46-case Cards/sample Play Mode group passed per SDK (P29), covering Canvas scaling, world-space preservation, hover/transfer and reentrant deck mutation. |
| Current sample presentation | Ten native sample cases passed per SDK (P31), including Inventory/Abilities button flows, viewport bounds, non-null Filled Image sprites and an actual CardGame EventSystem raycast/click that starts exactly one round. |
| Separate OS peers | Three distinct Windows player processes completed the matrix, including authority/admission, state/RPC delivery, late join, reconnect, restart and owned cleanup. |
| HTML → UITK .NET graph | Historical 2852 passed / 30 NotExecuted retained by exact identity of 752 selected source/support files, 37 compile-glob members, 182 SDK DLLs, 16 resolved dependency manifests and seven raw TRX files. This is qualification, not a new .NET execution. |

Raw failures were retained. Batch-mode cursor failures were not changed to ignores or counted as
passes: the original controls were run in the required interactive context. The HDRP case remains
explicitly unsupported by the selected URP compatibility projects. The .NET `net9.0` target used the
recorded .NET 10 runtime with process-local major roll-forward; an exact .NET 9 runtime was unavailable.

An isolated Unity 6.6 discovery process completed its helper receipt but stalled during shutdown;
that exact owned process was stopped and its exit was not reported as a natural success. The
subsequent six actual no-Mirror tests completed successfully. Transient registry/Burst/bootstrap
failures remain operational history, separate from the successful retries.

## Final HTML → UITK acceptance for 11.0.0

A fresh focused round on 27 October covers the separate converter and Unity adapters.
The complete converter solution passed **2573**, retaining **30 NotExecuted** legacy cases;
the portable pure-logic project passed **291/291**. The complete native HTML assembly passed
**370/370 without skips on each of Unity 6000.3.14f1 and 6000.6.0f1**. Actual published external
CLI conversion/import/render/navigation/lifecycle and native gradient pixels are included.
A separate OS-mouse pass exercised Settings, Video, Back, modal Cancel and exactly one Save
after document replacement. The standalone published CLI also ran outside the repository.

The [focused report](Docs/UI/HtmlToUitkVerification11.md) includes actual captures, repaired
output collisions, projection callback ownership and localization, plus exact scope/limits.
These groups overlap earlier full baselines; they are not an additional package-wide full run.
Hosted package integrity passed; hosted Unity jobs still require GameCI secrets and are not
presented as successful CI execution. Release Core/Complete archives are separate installation
variants; their actual member/import checks are retained with the release asset manifest.

## Manual sample observations

Manual play took place after source authoring and automated acceptance. Findings caused focused
sample corrections and repeat checks. This is selected coverage, not a claim that every sample scene
or every possible game has been manually completed.

| Scene | Actual interaction and observation |
| --- | --- |
| InventoryUseDemo | Deposited two same-type unique specimens, selected a physical slot with a button and key 2, placed/recalled the same UID, used E to consume exactly one CELL, observed HP 45 → 70 and count 3 → 2, and reset the scenario. |
| AbilitiesNoCodeDemo | Clicked authored damage/heal buttons, observed HP 100 → 80 → 95, cast Zap for 12 damage and then 30 at level 3, and reset. Corrected clipped guide layout and missing progress sprites; HP/cooldown bars now change visibly. |
| NoCodeActionsDemo | Gained a level through the authored action, accepted a quest and completed its objective. |
| NoCodeBindingDemo | Changed the Counter from 50 to 60 and observed both text and actual progress fill; corrected its missing Filled Image sprite. |
| SaveDemo | Saved 100 coins, changed to unsaved 110, loaded and observed restoration to 100. |
| QuestDemo | Accepted Wolf Hunt, sent three kill events, observed Completed, 1/1 objectives and 25 gold. |
| GridSystemTicTacToeDemo | Played alternating moves to a visible X victory. |
| GridSystemDiceMergeDemo | Tap rotation was confirmed. OS drag/drop through the automation gesture remained inconclusive; it is not reported as a manual placement pass. |
| AudioDemo | Beep and music ON/OFF controls and feedback were exercised. This does not establish audible output quality. |
| SurvivorDemo | Observed auto-combat, kills, defeat after a short run and a successful new-attempt restart. Sustained movement was not certified by short injected key presses. |
| CardGame | Played three rounds through actual mouse clicks. Corrected Canvas card creation/scale, centered piles, tutorial click interception and hand padding. Inspected Free Aspect, 16:9 and portrait 1080 × 1920; constrained the decorative dealer to the right edge with aspect preserved so it does not cover the lower pile. |

## CardGame acceptance and scope

The reported defect was reproduced: UI cards created outside a scaled Canvas acquired an inverse
Canvas scale when transferred to a hand. UI cards now start under their Canvas spawn point;
non-Canvas/world-space creation keeps its existing contract. Deck revision and post-callback
checks stop stale visual work after reentrant changes. The native regression groups and actual
manual rounds above passed. Sample-only dealer anchors preserve visibility in portrait.

The 28-case imported Edit Mode result qualifies unchanged GUID/resource inputs from P30; the
current 20-case imported Play Mode result is P31. The final dealer-anchor adjustment changes no
script, GUID, sprite reference or click binding. Final source/receipt identity and affected scene
acceptance are recorded in the root integration record. This record does not create a tag.

## По-русски

Основные автоматические проверки выполнены в изолированных проектах Unity 6.3 и 6.6. Результаты
полных прогонов, повторных проверок изменённых областей и проверок реальных устройств/окон описаны
отдельно: пересекающиеся группы нельзя складывать и выдавать за один новый полный прогон без ошибок.

Ручная проверка подтвердила перенос и использование конкретных UID инвентаря, лечение/урон/уровни
способностей, сохранение и загрузку, завершение квеста, победу в крестиках-ноликах и перезапуск Survivor.
В абилках и NoCode исправлены обрезание интерфейса и спрайты шкал. Для Dice подтверждён поворот;
надёжный drag/drop через инструмент ручного управления пока не подтверждён. Проверка Audio касается
управления и состояния, а не прослушивания. В CardGame исправлены масштаб карт, отступы колод и
перехват кликов обучением: нативные проверки прошли на обеих SDK, вручную сыграны три раунда.
Декоративный персонаж ограничен правым краем с сохранением пропорций для портретного экрана.

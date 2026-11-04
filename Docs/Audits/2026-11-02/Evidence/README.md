# Библиотечные native evidence

`LibraryNativeFirst-20261102.zip` сохраняет первые реальные Unity 6000.3.14f1 / D3D11 результаты этого прохода, включая failures, skips и прерванный до первого теста OOM. Состав и SHA-256 — в `LibraryNativeFirst-20261102.receipt.json`; каждый payload имеет размер/хеш в `archive-members.json`. Root повторно прочитал CRC и все member hashes.

- Main EditMode: 4235 = 4201 PASS / 11 cursor-capability FAIL / 23 SKIP.
- Main PlayMode: 748 = 718 PASS / 6 FAIL / 24 SKIP; четыре cursor controls и два новых physics stay cases остаются failed в этом исходном запуске.
- StateMachine constructor regression: 12/12 RED на предсказанном CurrentState assertion, затем 78/78 PASS после проверенного исправления.
- Verification/Inventory fixture: 82/82 PASS, в том числе clean-cache positive/sentinel.
- Попытка Cards/policy/corrected-physics: native OOM при bootstrap, **0 executed / no XML**, без доказательства red/green для этих сценариев.

Архив содержит исходный source tree и exact per-run deltas, manifests и raw XML/logs; последующие версии operator scripts явно отделены от фактических launch arguments ранних запусков. Private configuration/prefs и memory dump исключены. Legal notices приложены отдельно. Общий зелёный assembly не заменяет per-action mapping, а source hashes не доказывают runtime-путь. Другие SDK, Player/IL2CPP, реальные import/network/performance/manual contexts требуют своих доказательств.

`LibraryActionWitnesses-20261102.zip` сохраняет пять предложенных per-action maps: **45 + 56 + 55 + 27 + 63 = 246 уникальных IDs**. SHA-256: `e7460316e3979b077de14d733d673c10e85a8b4b1b405be266990dea6f20108f`; 10 payloads плюс manifest, CRC/member hashes проверены root. Карты связаны с raw первого архива и не создают новых test passes. Исходный mapping05 сохранён вместе с отдельной operator amendment после двух prose-only sample-description правок; corrected mapping03 сохраняет реальные skip reasons и три NONCOUNTED handoffs. Статусы остаются ограниченными зафиксированным profile: карты не означают финальное закрытие всех багов.

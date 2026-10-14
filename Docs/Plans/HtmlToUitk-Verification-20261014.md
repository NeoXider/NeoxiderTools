# HtmlToUitk: проверка ближайшей поставки

Дата: 2026-10-14. FeatureId: `feature-htmltouitk-toolkit-20261014`; решение владельца IB:705–706. Объём и ограничения — [план](HtmlToUitk-20261014.md), оставшиеся приоритеты — [TODO](HtmlToUitk-TODO.md). **Проверенное ближайшее заявленное подмножество принято; расширенный TODO открыт, Git publication Pending. SourceReady ≠ Accepted ≠ Released.** Portable actual XML/completions/pixels/captures и tested harness: [proof](../QA/HtmlToUitk-20261014/README.md).

Этот переносимый отчёт содержит реальные counts, receipt names/digests и воспроизводимые команды. Пути исходников ниже относятся к корню NeoxiderTools. XML/captures — внешние QA artifacts, не runtime dependencies; их basenames и SHA фиксируют evidence независимо от локальной папки хранения. Наличие receipts не заменяет перечисленные открытые гейты.

## Ревизия и source review

| Артефакт | SHA256 / проверка |
|---|---|
| CLI [manifest](../../Tools/HtmlToUitk/docs/source-final.json) | `24337a5ed949d4a9ced9cae7064e810627d2b4fff4d262907eaf62fc23ff21f1`; 706/706 hashes совпали |
| Published runtime/test/meta [manifest](../../Assets/Neoxider/Tests/Edit/HtmlToUitk/Verification~/source-final.json) | `a9c0cd0ee76ffd70a9a926444026e38785192cde0a36f3c0667f53b13edb1043`;163/163 worktree/index blob SHA совпали |
| Actual native input identity | `e5ed8df321f38a6e6572f808d59db313f4dc32968167b88e153af6910bec6f25`; native XML ниже относятся к этому input. A9C сохраняет полное163-file raw→LF mapping:60CRLF→LF,0non-line-ending/GUID drift; отдельного native rerun после lossless normalization не было |
| `cli-final-receipt.json` | `e7e83fea6388e5f312df9f8f42570f1942d4701f33e8447f040f6e047ee5a27b`; CLI exit0, suite2559 pass /32 historical skip /0 fail; production19/0/0 |
| Frozen CLI outputs | 20/20 actual output hashes совпали с receipt; источник не менялся после P1 fixes |
| Final targeted re-review | Три прежних отрицательных CLI inputs дают exit1; homogeneous fixed /fixed pixel stops /fluid percentage stops /ordered media —4/4 exit0; повтор4/4 с полным output drift0 |

Четыре прежних blockers исправлены: неоднородные effective gradient размеры и динамические absolute stops получают явный отказ; противоречивые media bounds не создают unusable runtime spec; PNG importer всегда требует exact manifest/hash, включая config. Config может только сузить policy. Референсы/лицензии и границы собственного кода: [Provenance](../../Tools/HtmlToUitk/docs/Provenance.md).

Последняя runtime ревизия меняет только closure fixture (method group через InvocationCounter вместо захвата локального int) и whitespace новых meta; production runtime реализации сохранены. Из97 отформатированных meta95 входят в163-file manifest: независимое сравнение этих95 с cold tree `3b57e8185c5af86af087f76417f54435b42dbb39` после удаления trailing whitespace показало0 изменений значений/GUID. Остальные2 принадлежат Root metadata wave вне этого manifest.

`git diff --cached --check` после Root restage: exit0. До restage ошибка относилась к старому staged whitespace, не к финальному worktree. Root actual `python scripts/check_package.py`:5358 text files /68 assemblies /2825 GUIDs /0 errors. Единственное изменение правила missing-meta исключает файлы под каталогами с суффиксом `~`, которые Unity не импортирует (включая `Verification~` и его ignored bin/obj); encoding/JSON/conflict/GUID/assembly guards сохранены. Это package structure PASS, не runtime acceptance.

## Реальная native проверка и история

| Срез | Фактический результат | Что ещё открыто |
|---|---|---|
| Unity6000.6 native compile | 0 C# errors | Не заменяет EditMode/PlayMode/render |
| Unity6000.6 старые EditMode | Первый238/236/2; затем239/238/1 | История до closure correction, не текущий PASS |
| Unity6000.3.14 native compile/EditMode на старой AC6E ревизии | Compile0 C# errors, Edit239/238/1; NRE fixture | Исправленный прогон E5ED ожидается |
| Unity6000.6 старые PlayMode |7/4/3 и black captures | История QA host, не актуальный visual FAIL новой render-host версии |
| Unity6000.6 renderhost1 | **7 total /6 pass /1 fail /0 skip**, native exit2 | Один action-тест: modal перекрывает кнопку в configureScreen=false fixture. Исправляется только внешний QA driver; suite ещё не PASS |
| Final E5ED native6000.6 Edit |239/239 pass,0fail/0skip; exit0 | [actual public XML](../QA/HtmlToUitk-20261014/editmode-6000.6-final.xml) |
| Final E5ED native6000.6 Play |7/7 pass,0fail/0skip; exit0 | [actual public XML](../QA/HtmlToUitk-20261014/playmode-6000.6-final.xml) |
| Final E5ED native6000.3.14 Edit |239/239 pass,0fail/0skip; exit0 | [actual public XML](../QA/HtmlToUitk-20261014/editmode-6000.3-final.xml) |

`playmode-6000.6-renderhost-1.xml` SHA256 `d230e05881e20f27bd6a80645612c8f03e444e81a5dfaf083cf332aceff7b431` содержит Passed: already-loaded UIDocument, native PanelRenderer reload, gradient authored geometry/render, nav/modal/tabs/focus/cancel, resize media classes, scheduled animation/reduced motion/dispose. Failed: `PointerAndNavigationActionsHaveOneOwnerAcrossDetachReattachAndRootReload` (expected1 click, actual0). Причина перекрытия кнопки передана оператором/Root, не объявлена новым production defect без следующего actual run.

Historical XML digests: `editmode-6000.3-1.xml` — `731b8e46783e7d4de4ac92fcdf47459f49316600ecc73ecc09015a490aacdbde`; `editmode-6000.6-2.xml` — `d0c88626359abc9f2084c85dda0801b8139b12e4c8018eec12ff39402f36cd06`. Их fail не скрыты и не переносятся на новую source-final ревизию как текущий итог.

## Render evidence

Final native gradient-тест проверил authored bounds и sampled pixels при1280×720,1920×1080,3440×1440. [Final PNG/pixel proof](../QA/HtmlToUitk-20261014/README.md) скопирован побайтно. Root просмотрел final home1920/modal-closed1280/settings-video1280: native controls и правильные nav состояния присутствуют. Старые black captures и misleading gallery filenames относятся к истории прежнего host.

| Capture basename | SHA256 |
|---|---|
| [gradients-1280x720.png](../QA/HtmlToUitk-20261014/captures/gradients-1280x720.png) | `877bb7f6c1e650dd7d0ca91194ce7239de64e691616963ffb1e34bbb2f6b49b1` |
| [gradients-1920x1080.png](../QA/HtmlToUitk-20261014/captures/gradients-1920x1080.png) | `da3d5199faf25285a7d30681b2122000d7886f543f61b7c97e4500b644c1872c` |
| [gradients-3440x1440.png](../QA/HtmlToUitk-20261014/captures/gradients-3440x1440.png) | `dc41fd8cd2fc2eb78aa34a56cc53a08f60a968a0cc2230f35fb8700893afe611` |

В прежнем renderhost1 home filenames действительно показывали gallery без native control theme; это история, не итог final run. Final behavioral/native controls/render presence PASS подтверждён actual tests и просмотром Root. ILocalizer в fixture не назначен, поэтому видны app.home.* keys. Black-on-black contrast и partial animation не выдаются за product UX polish/full browser fidelity; удобный localized/styled landscape sample и independent HTML/Unity comparison остаются TODO.

## Cold/source/package и повторение

Root cold staged tree `3b57e8185c5af86af087f76417f54435b42dbb39`: actual solution build0 errors /1 legacy CS0219 warning /9.90s и actual production CLI tests19pass/0fail/0skip. Наличие source build definitions и новый CLI fixture в archive проверены. Полный cold Unity UPM/runtime/player smoke, mobile/performance и версии вне native matrix остаются по [TODO](HtmlToUitk-TODO.md); source archive build/test их не заменяют.

```powershell
dotnet build Tools/HtmlToUitk/HtmlToUitk.sln
dotnet test Tools/HtmlToUitk/HtmlToUitk.sln
dotnet publish Tools/HtmlToUitk/src/HtmlToUitk.Cli/HtmlToUitk.Cli.csproj -c Release -o Tools/HtmlToUitk/bin
dotnet Tools/HtmlToUitk/bin/htmltouitk.dll run Tools/HtmlToUitk/fixtures/production-neutral/neutral.htujob.json
python scripts/check_package.py
git diff --cached --check
```

Native editor tests: импортировать текущий пакет в установленную Unity6000.3.14f1/6000.6.0f1 и запустить `Neo.HtmlToUitk.Tests` через Test Runner единственным оператором, сохранив actual XML и source hash до/после. Runtime/render fixture дополнительно проверяет реальные pointer/keyboard действия, detach/reload/teardown, PanelRenderer и gallery pixels в landscape matrix. Typecheck не заменяет этот шаг. Publish gate: reviewed final diff/version/notices, commit от NeoXider без coauthors, push exit0 и local/remote SHA equality; пока Pending.

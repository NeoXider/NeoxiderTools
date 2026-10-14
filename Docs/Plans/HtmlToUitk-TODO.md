# HtmlToUitk: ближайшая поставка и оставшийся TODO

2026-10-14. Источник владельца IB:706: «доделывай ближайшее остальное туду распиши и запушь». FeatureId `feature-htmltouitk-toolkit-20261014`. Это приоритеты реализации и проверки, не обещание browser parity и не закрытие непроверенных гейтов.

## Ближайший объём

Поставляется заявленное HTML/CSS-подмножество: реальный CLI → UXML/USS/specs/gradient PNG; width/height/aspect media для поддержанных selectors; nav/modal/tabs/animations и C# actions с одним владельцем lifecycle. Static angled/radial/conic/repeating/absolute-stop gradients требуют однородной fixedpx geometry; текучие horizontal/vertical gradients поддерживают нормализованные stops. Unsupported CSS/JS и C# preview/compare получают честный nonzero, Python oracle остаётся отдельным маршрутом. Игровые JS callbacks автоматически не переводятся.

Source baseline main `4ca6f50f0e4de5740411c6a495a15f85ebe2b4f4`; CLI manifest `24337a5ed949d4a9ced9cae7064e810627d2b4fff4d262907eaf62fc23ff21f1` совпал706/706. Публикуемый runtime manifest `a9c0cd0ee76ffd70a9a926444026e38785192cde0a36f3c0667f53b13edb1043` совпал163/163 worktree/index; actual native input E5ED8D… сохранён отдельно с полным raw→LF mapping60files/0semantic-or-GUID drift. Native normalization rerun не заявляется. CLI2559pass/32historicalskip/0fail,production19/0/0; re-review3negative exit1,4positive+4repeat exit0/drift0. Runtime importer всегда требует manifest/hash. Переносимые results и proof: [Verification](HtmlToUitk-Verification-20261014.md).

**Ближайшее заявленное подмножество принято и опубликовано в `main` (коммит с этим файлом); расширенный TODO открыт.** Actual final native input E5ED:6000.3.14 Edit239/239,6000.6 Edit239/239 иPlay7/7, всеexit0/0fail/0skip. Gradient pixels/bounds landscape matrix и final native controls/nav/render presence PASS; Root просмотрел соответствующие home/modal/settings states. Старые black/3fail/renderhost1 6/7 — история. Fixture без ILocalizer показывает keys, имеет неполированный contrast/partial animation; это не finished UX demo или browser fidelity.

Root cold staged archive tree `3b57e8185c5af86af087f76417f54435b42dbb39`: solution build0 errors/1 legacy CS0219 warning/9.90s; cold build definitions gate пройден. Formatting97 новых meta завершён;95 manifest meta независимо сравнивались с cold tree, value/GUID drift0. После restage staged diff-check exit0. Package checker PASS5358 text files/68 assemblies/2825GUID/0errors; missing-meta исключает Unity-nonimport каталоги `~`, остальные guards сохранены. Это не cold Unity package/runtime/player PASS.

## Приоритеты и критерии закрытия

| ID / приоритет | Работа | Критерий закрытия / текущий статус |
|---|---|---|
| HT-P0-01 | Завершить текущий native/render гейт | Закрыто для ближайшего scope: actual Edit239/239 обеих версий,Play7/7,action/lifecycle/nav/render/pixels PASS; native input E5ED отделён от publishedA9C newline mapping. Failed attempts исторически сохранены. |
| HT-P0-02 | Targeted cold archive/source проверка ближайшей версии | Cold source solution build0errors/1legacy warning и productionCLI19/0/0 PASS; actual native import/Edit matrix и importer ownership tests PASS. Универсальный UPM/platform/player cold smoke остаётся HT-P1-04/P2, не заявляется выполненным. |
| HT-P0-03 | Root checkpoint и push | Закрыто 2026-10-14: root повторил `dotnet test HtmlToUitk.sln -c Release` (2559 passed / 32 historical skip / 0 failed), `scripts/check_package.py` (5358 файлов, 68 сборок, 2825 GUID, 0 ошибок), сверку XML (6000.3 Edit 239/239, 6000.6 Edit 239/239, 6000.6 Play 7/7) и source-final: runtime 163/163, CLI 706 файлов, из них 47 отличаются только CRLF/LF. Пользовательский Physics2DSettings остаётся в stash, вне коммита. Коммит от Neoxider без соавторов. |
| HT-P1-01 | Dynamic gradient geometry backend | Angled/radial/conic/repeating/absolute stops при resize и heterogeneous cascade имеют корректные render pixels во всей landscape matrix; до этого unsupported cases остаются nonzero. TODO, не часть обещанного текущего backend. |
| HT-P1-02 | Реализовать C# CLI preview/compare | Actual entrypoint создаёт preview/diff/metrics и сообщает meaningful nonzero при отсутствии renderer/input; сравнение независимых HTML/Unity shots, не self-diff. Сейчас nonzero unsupported + документированный Python route. |
| HT-P1-03 | Расширить CSS/media selector coverage | Каждый объявленный selector/query/property имеет positive и negative tests, cascade/cross-page и finite/range checks; неподдержанное не теряется при PASS. Complex/descendant media и broader CSS остаются TODO. |
| HT-P1-04 | Автоматизировать portable cold-package smoke | Повторяемый CI cold consumer из опубликованной версии и переносимых путей; CLI discovery и package docs проверены на поддержанных host configurations. Дополняет targeted P0 архивный гейт, не заменяет его. |
| HT-P1-05 | Удобный styled landscape sample | Ready theme/contrast, localizer и table binding, практический C# Button example с disposal; controls и localized text читаются во всей matrix. Human UX review и tests нужны; current fixture с raw keys не polished demo. |
| HT-P1-06 | Independent HTML/Unity visual comparison | Независимые screenshots одного supported sample с matching viewport/theme/fonts, tolerances и loss report; self-diff и render presence не считать fidelity proof. TODO, не исправление game style в этом checkpoint. |
| HT-P2-01 | Text gradients через TextCore/native API | Живой локализуемый текст, font fallback/resize и pixel fidelity; строки не растеризуются. Пока TODO/NotVerified. |
| HT-P2-02 | Performance/GC budgets и mobile Player QA | Заранее фиксированные бюджеты, профили CPU/GC/VRAM/resize, teardown leaks и Android/iOS реальные player evidence. Пока NotRun; desktop compile не считается mobile PASS. |
| HT-P2-03 | Неустановленные Unity версии и browser/Unity fidelity | Отдельные compile/runtime/render captures на реально установленной версии; независимые screenshot comparisons с tolerances и перечислением unsupported features. Полный browser parity не обещается. TODO. |

Динамический backend, TextCore и дополнительные providers/runtime dependencies не добавляются без отдельного exact scope/плана. Licenses/provenance остаются обязательными; FUI без лицензии не копируется. По каждой задаче сохраняются revision/hashes/команда/exit/действительный результат; SourceReady ≠ Accepted ≠ Released.

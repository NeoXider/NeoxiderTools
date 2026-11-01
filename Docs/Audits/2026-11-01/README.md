# NeoxiderTools: сверка закрытия и HTML runtime — 1 ноября 2026

Рабочая ветка `demo`, база `d2715311`: актуальный `main` 11.0.1 влит без конфликтов. Для текущего HTML-среза выполнены нативные проверки с границами ниже. Это не глобальное закрытие всех аудитов или реализация browser backend; исторический `source-final` и готовые C# контракты не доказывают перенос произвольного сайта.

## Текущее состояние

| Область | Результат | Что осталось |
| --- | --- | --- |
| Аудиты модулей | [Ledger](ClosureLedger.md) связывает 72 владельца и 257 действий с отчётами и текущими файлами. Root независимо проверил уникальность IDs и SHA-256 исторических документов всех 72 владельцев. | Это не новая семантическая проверка всех runtime-путей; независимые source observations ограничены указанными в JSON 16 действиями. Raw XML/TRX/manifest из `Local/Audits` отсутствуют. |
| Существенные улучшения | В handoff выделены 64 отложенных предложения и 8 частично реализованных областей. | Это классификация планов, а не 64 подтверждённых бага или доказательство отсутствия любой похожей функции. См. точные scopes и решения в ledger. |
| Runtime Web API | Standalone **65/65 PASS** и первый фактический Unity6.3 Mono EditMode **65/65 PASS**, без пропусков. Старые failures сохранены; результаты независимо проверены. | Это чистые options/URL contracts, не browser provider/content inspector, Player/IL2CPP или визуальная приёмка. |
| Окно конвертера | После узкого исправления фикстуры и независимого source review фактический Unity6.3 batch прошёл **301/301**, включая все **23 draft**, 10 replacement, 2 watch, 2 runner и 8 pipeline случаев; остальное — pure logic. | Первый запуск88=79PASS/9FAIL сохранён. Исправлены наблюдение сериализованного пути и предпосылка прикреплённой панели в тесте; production-код от этого не менялся. Внешний вид вкладок и физический ввод ещё не проверены. |
| CTA и Velora PlayMode | После исправления framebuffer фикстуры целевые CTA дали **2/2 PASS**, затем весь fixture — **16/16 PASS**, без пропусков, exit0. Проверены сохранённые desktop/mobile PNG, фон/текст/контраст, synthetic pointer/modal, страницы, формы, Pricing и прокрутка. | Два исходных failed запуска сохранены. Причина в этом profile — depth0 target не обеспечивал depth/stencil для масок; actual исправленный формат `D32_SFloat_S8_UInt`. Product-стили, пороги и scheduling не менялись. Обычный Game View, физический ввод, motion/video и browser parity остаются открытыми. |
| Готовый WebView | [Статический gree-анализ](GreeBackendInspection.md) и [offline WebViewToolkit inspection](WebViewToolkitBinaryInspection.md) завершены. Для Toolkit release/UPM DLL идентичны ожидаемым bytes/hash, AMD64 и все 23 P/Invoke names найдены. | Known source lifetime/AOT/resize gaps остаются. Root принял только проверку identity/entry names; import/adoption/исполнение непатченной DLL не одобрены. Наличие binary не закрывает runtime safety, renderer или все требования backend. |
| Целостность пакета | Актуальный `check_package.py`: **5889 файлов / 71 сборка / 3209 GUID / 0 ошибок**. | Первые две ошибки текущих imported-sample путей в GettingStarted после main11.0.1 исправлены; первый failed log сохранён. Это статическая проверка, не Unity sample import. |
| API-компиляция HTML-модуля | Оба профиля managed API **6000.3.14f1 / 6000.6.0f1** собрали 33 runtime + 23 Editor исходника и две новые fixture: C#9/netstandard2.1, **0 ошибок / 1 сохранённое CS0649 на профиль**. Исходники, ссылки и outputs проверены независимо. | Это компиляторная проверка с реальными DLL, а не запуск Unity/import/USS/test execution. Neo.Extensions/TestRunner использованы precompiled; 1701/1702 подавлены по существующему recipe. Отдельная ветка 6.5 не покрыта. |

## Проверки и исполнение

После завершения source writers и независимых ревью root выполнил standalone .NET-host: SDK 10.0.301, target net8.0, NUnit 4.6.1 / adapter 4.6.0. Первый запуск **57/61 PASS, четыре FAIL, без skip**, exit1, сохранён. После узкой правки и повторного source review второй запуск дал **65/65 PASS, без skip**, exit0. До/после обоих запусков проверены хеши семи compiled inputs. Все прежние 61 имени testcase присутствуют во втором TRX. Точная версия testhost runtime отдельно не трассировалась. Сырые TRX, manifests, receipts, lockfile и исходники сохранены в [проверенном архиве Evidence](Evidence/README.md); run1-восстановление исходников явно квалифицировано, run2-копии сделаны до исполнения. Позднее выполнены отдельные native Unity6.3 группы: Web65, Editor301 и Velora PlayMode16 с результатами выше. Обычный Game View, физический ввод и настоящие браузерное/Unity видео остаются отдельными критериями; Player/IL2CPP не проверены.

Владелец разрешил отдельные batch-проверки NeoxiderTools при открытом другом проекте. Второй процесс на том же NeoxiderTools не запускается; GUI другого проекта не управляется. Перед процессными проверками требуется свежая проверка свободного commit не менее 2.5 GiB. Чужие приложения и проекты не закрываются и не меняются.

Root использовал скрытый D3D11 batch без `-nographics` только для NeoxiderTools; чужой Editor не управлялся. Первый native результат сохранён целиком. Для Editor-r2, CTA-r1/r2/r3 и полного Velora fixture предварительно копировались 332 HTML/showcase input files; до/после каждого запуска их хеши совпали, 12 пользовательских путей не изменились. Это конкретные snapshots до последующих разрешённых правок, а не обещание неизменности дерева навсегда. Исходный HTML EditorPrefs prefix восстановлен после проверки names/types/bytes; приватные snapshots не публикуются. В [Evidence](Evidence/README.md) сохранены XML/logs, exact captures, manifests и failed-versus-passed сравнение. GUI/физический ввод таким разрешением не подтверждены.

Root сохраняет старые неудачные прогоны и skips как историю. Текущие passes нельзя вывести только из числа файлов, хешей документов или чужого success-сообщения. Состояние нового выполнения будет записано здесь после фактического запуска.

## Доступность старых доказательств

`Local/Audits/assembly-2026-10-23` отсутствует. Read-only запрос release `v11.0.0` показал пять assets: CLI ZIP, Core/Complete unitypackage, `release-input-manifest.json`, `SHA256SUMS.txt`; отдельного raw test-evidence archive среди assets нет. Это не доказывает отсутствие других копий на компьютере или во внешнем хранилище. Старые заявленные результаты остаются историческими claims с указанными границами.

Пользовательские 12 pre-existing paths сверяются по внешнему `pre-main-merge.json` и не включаются автоматически в новые исправления. Worktree не создавались. Исходный `D:/AI/chicken-road` не изменялся.

## Отчёты

- [Ledger всех владельцев и действий](ClosureLedger.md), [машиночитаемая запись](ClosureLedger.json), [независимая проверка закрытия](NativeClosureReview.md).
- [Runtime contracts](RuntimeWebContracts.md), [независимое runtime review](NativeRuntimeReview.md), [gree inspection](GreeBackendInspection.md).
- [Editor UX](EditorWindowUX.md), [независимое Editor/demo review](NativeDemoEditorReview.md), [CTA readback](CTAReadback.md).
- [Доступность native toolchain и план spike](WebBackendFeasibility.md), [сравнение готовых OSS WebView](WebViewAlternatives.md). Ни один browser backend здесь не принят как готовый.
- [Историческая приёмка 11.0.0](../../../Assets/Neoxider/Verification11.md), [текущий HTML runtime TODO](../../../Assets/Neoxider/Docs/UI/HtmlToUitkRuntimeTODO.md).

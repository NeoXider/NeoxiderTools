# README: подготовка интеграции 11.0.0

## Контекст и границы

Исходный контекст сохранился. Это продолжение той же сессии `/root/audit_readme`, выполнившей `README-review.md` и `README-review.plan.md`, с прежними шестью entry documents. Сохранены решения о модульной C# библиотеке, необязательных scene/NoCode wrappers, SVG hero/layers/four-ways, HTML/CSS-разделе и пользовательском `DocsEn.meta`.

Перечитаны оба собственных отчёта, `V11-RELEASE-PLAN.md`, `review-actions.json`, `INTERACTION-QUERY-DESIGN.md`, текущие входы, package metadata и нужные canonical guides. Ветка подготовки — `demo`, цель — **11.0.0**. В прочитанном `package.json` пока **10.19.0**: это текущие метаданные, а не уже выпущенная v11. Аудиты и исторические verification receipts сохраняют исходную версию и дату.

Сейчас создан только этот файл. Public docs, source, config и предыдущие отчёты не меняются; tests/build/validators/HTTP checks не запускаются. Публикационные формулировки ниже применяются после общего подтверждения и завершения соответствующих source actions. Статус `pending`/`awaiting confirmation` из review-actions не превращается в обещание выпущенной функции.

## Точные изменения шести entry documents

| Файл | Изменение после общего подтверждения |
|---|---|
| `README.md` | Badge `11.0.0`; короткая ссылка «Upgrading from 10.x / Обновление с 10.x»; сохранить визуальную структуру и четыре маршрута. В NeoCondition-примере описать bool pair и ровно одну true/false ветку, учитывая режим dispatch, вместо двусмысленного обещания четырёх событий одновременно (`CONDITION-DOC-PARITY`). Tests-раздел направить к canonical testing guides. Imported sample examples обновить до `11.0.0`. |
| `Assets/Neoxider/README.md` | Тот же version/migration вход; Condition в module map ведёт к `Docs/Condition/README.md`, а per-component guide остаётся дополнительной ссылкой. Сохранить C#/scene/NoCode quick start, корректные DOTween/UniTask prerequisites и bundled CMF. Добавить короткий путь к interaction guide и testing guides. Обновить imported sample version. |
| `Assets/Neoxider/Docs/README.md` | Версия `v11.0.0`; короткая RU-ориентация рядом с основным английским входом; матрица маршрутов ниже. Прямые ссылки на migration, `Testing.md`, `PlayModeTesting.md` и interaction entry. В module map сохранить один canonical entry на область; nested/editor topics открываются через этот entry. |
| `Assets/Neoxider/Docs/GettingStarted.md` | Сохранить рабочие `ResourcePoolModel` и named-ID AM примеры; после source barrier сверить финальные signatures, cap semantics и lifecycle. Новый additive preflight не становится обязательным для простого примера. Добавить RU краткий маршрут и ссылку на migration; imported paths — `11.0.0`. Не навязывать bootstrap singleton или новые scene registrations. |
| `Assets/Neoxider/Docs/PackageCompatibility.md` | Package version `11.0.0`; Unity minimum брать из manifest, не повышать из-за major bump. Разделить metadata/source review, реальные compiler results и player coverage. Таблица результатов появляется только по фактическим финальным запускам; сохранить centralized StableId gates и требование компиляции обеих веток. |
| `Assets/Neoxider/Docs/Samples.md` | Версия и imported roots `Assets/Samples/NeoxiderTools/11.0.0/<sample name>/`; те же три manifest samples, если координатор не меняет manifest. Сохранить release `Samples~`, альтернативный developer `Samples` и отдельный imported copy. Ветка `demo` сама по себе не требует rename. Обновление imported examples не удаляет пользовательские копии 10.x. |

Координатор синхронно ведёт `Assets/Neoxider/package.json`, `CHANGELOG.md`, `PROJECT_SUMMARY.md`, root `AGENTS.md`, bundled `Skill/neoxider-tools/SKILL.md` и существующие version-mention guards. Глобальный replace `10.19.0` запрещён по смыслу: historical changelog/audits/receipts и происхождение Abilities из v10 остаются историей.

Обычный Git URL без ref ведёт к опубликованному default branch и не гарантирует локальную v11. Пока нет опубликованного ref, проверять подготовку из local checkout/local package. Не выдавать `#demo` или выдуманный tag `#11.0.0` за уже опубликованный релиз. После публикации координатор указывает реально существующий ref.

## Единственный migration entry

Предлагаемый точный package-local путь: **`Assets/Neoxider/Docs/Migration11.md`**. На момент чтения такого файла нет; создаёт координатор либо назначенный им владелец с уникальной `.meta`, после финальных решений. Root/package READMEs и Docs index ссылаются на него, а он — на canonical module docs. `Docs/Audits/...` остаётся evidence, не основным пользовательским входом.

В migration включить следующие разделы; неподтверждённые схемы не публиковать как принятые:

| Раздел | Конкретный пользовательский контракт и источник решения |
|---|---|
| Shared interaction queries | `INTERACTION-SHARED-QUERY`: одинаковый физический контекст делит полный raw hit result в пределах epoch; число прогретых queries зависит от K контекстов, а не N targets. Сохранить Mouse/Center/Both, masks, tags, trigger policy, range/LOS, UnityEvents и serialized settings. `obstacleLayers` остаётся LOS-маской. Разные направления/2D/3D/камеры не объявлять одним raycast. Публичные invalidation/fresh-query имена брать из финального API владельца, не придумывать заранее. |
| Покупка и уведомления | `SHOP-OBSERVER-REFUND-GRANT`: до commit ошибка mutation/persistence означает rollback/refund; после commit observer fault изолируется/сообщается без refund, bundle children продолжаются согласно финальному контракту. Перечислить изменение для игр, использовавших exception из observer как отмену покупки. Не обещать транзакцию с произвольными callbacks, inventory grants и несколькими disk keys. |
| Resources / ability costs | `CORE-CANSPEND`, `ABIL-CAPPED-PAYMENT`, `ABIL-OPERATION-RECEIPT-DELTA`: точный cap-aware preflight/payment; стоимость 10 не оплачивается 1 из-за decrease limit. Реальный delta берётся из operation receipt, не из позднего баланса после reentrant callback. Сохранить существующие interfaces; реальные additive signatures документирует владелец. |
| Огромный catch-up и clocks | Принятый bounded RPG policy: максимум 1024 delivered ticks на entry/call, лишний целый backlog отбрасывается, дробный остаток сохраняется; normal cadence и последующий normal tick восстанавливаются. Nonfinite inputs не поддерживаются. `BONUS-CLOCK-BOUNDARY`: CooldownReward читает/пишет trusted UTC, обычный TimerObject сохраняет default clock и existing save keys. |
| Числа и ownership | Сослаться на `Extensions/NumericEdgeCases.md` и `LifecycleAndOwnership.md`: near-plane-relative depth, проверка потребителей ScreenPositioner, cancellation через `CoroutineHandle.Stop()`, граница наблюдения native StopCoroutine, borrowed/generated previews. Не переписывать эти контракты независимо в README. |
| Save / serialized compatibility | `SAVE-ENCRYPTION-CONFIG`: неверная включённая encryption config должна давать явный отказ без plaintext overwrite и с сохранением прежнего provider. `RPG-BASE-MAX-CONTRACT`/v3 presence flag и `LEADERBOARD-PERSISTENT-IDENTITY` требуют отдельного подтверждения: описывать только реально принятый формат и путь старых данных; никаких name-based guesses или массового ресейва ассетов. |
| Network / NoCode / lifecycle | Ссылки на финальные module contracts для reject unknown policy, reflection validation, opt-out ingress, late bindings, state-machine reentry и pause ownership. Отделить исправление поведения от изменения пользовательского API; не предписывать новые обязательные компоненты или Mirror для solo workflow. |

Каждый раздел содержит «что изменилось → кого касается → действие C# caller → действие scene/NoCode author → сохранённые данные/настройки → фактическая проверка». Обычные bug fixes без ручного перехода обозначаются как такие, без искусственной миграции всех сцен.

## Новые canonical links и владельцы

Все пути в таблице относительны **`Assets/Neoxider/Docs/`**. Target-файлы уже найдены в checkout, кроме нового `Migration11.md`. Module entries меняют исходные владельцы/координатор; этот README worker после разрешения меняет только свои шесть входов.

| Canonical entry / существующий legacy pointer | Добавляемая или уточняемая цель |
|---|---|
| `README.md` | `Migration11.md`, `Testing.md`, `PlayModeTesting.md`, `Tools/InteractableObject/README.md` |
| `Abilities/README.md`, `Abilities/AbilityDesignerWindow.md` | `Abilities/Editor.md`; согласовать radius rule (`ABIL-EDITOR-DOC`) |
| `Audio/README.md` | `Audio/Editor.md` |
| `Cards/README.md`, `Cards/DeckConfig.md` | `Cards/Editor.md` |
| `Core/LevelCurveDefinition.md` | `Core/Level/Data/CurveEvaluation.md` |
| `Extensions/README.md` | `Extensions/NumericEdgeCases.md`, `Extensions/LifecycleAndOwnership.md` |
| `Network/README.md` | `Network/Contracts.md` |
| `Rpg/README.md`; `Rpg/RpgConditionAdapter.md` | `Rpg/ConditionBridge.md`, `Rpg/Combat/README.md`, `Rpg/Network.md`; старый adapter guide делегирует семантику bridge guide |
| `Tools/Components/AttackSystem/README.md` | `Rpg/Combat/README.md`; не дублировать исправленный Timer status |
| `Shop/README.md`; `Tools/Inventory/ShopInventoryGrantBridge.md` | `Shop/Bridges.md`; legacy page остаётся compatibility pointer |
| `StateMachine/README.md` | `StateMachine/Editor.md` |
| `Tools/InteractableObject/README.md` | `Tools/InteractableObject/Core/README.md`; provider guide уточняется после реализации shared query |
| `UI/UIMeshRig.md` | `UI/Editor.md`; runtime safety status обновить после исправления и проверки |
| `UI/HtmlToUitk.md` | `UI/HtmlToUitkEditor.md`, `UI/HtmlToUitkTesting.md` |

Root/module tables направляют в canonical entries, а не в исторические audit/plan files. Новые folder metadata для Core/Combat и прочих module docs создаются один раз центральным владельцем, без смены существующих GUID.

## RU/EN route matrix

Один набор ссылок и C#-примеров обслуживает обе языковые ориентации. Основной английский текст сохраняется, RU summary добавляется в Docs index и Getting Started; отдельный `DocsEn` tree не создаётся. Существующие heading anchors не ломаются.

| RU / EN маршрут | Точная цель внутри package Docs | Дальнейший маршрут |
|---|---|---|
| C# API без сцены / Runtime API | `GettingStarted.md#start-with-a-c-runtime-model` | `Core/README.md`, `Merge/README.md`, `StateMachine/README.md` |
| Первый компонент / First component | `GettingStarted.md#your-first-scene-in-5-minutes-audio-manager-am` | `Audio/README.md`, затем `Audio/Editor.md` |
| Inspector / NoCode | `NoCode/GettingStarted.md` | `Condition/README.md`, typed API выбранного модуля |
| Взаимодействия / Interaction | `Tools/InteractableObject/README.md` | `Core/README.md` в той же module directory и `InteractionRayProvider.md` |
| Обновление с 10.x / Upgrade from 10.x | `Migration11.md` | Canonical contracts из migration table |
| Примеры / Samples | `Samples.md` | Три manifest samples и imported root версии 11.0.0 |
| Проверки / Verification | `Testing.md`, `PlayModeTesting.md`, `PackageCompatibility.md` | Изолированный host, prerequisites выбранной suite, реально выполненный scope |

Tests-разделы должны явно различать optional runtime integration и зависимости broad test assemblies: текущие Edit/Play suites ссылаются на Mirror. `testables` делает tests видимыми, но не доказывает готовность host и поддержку произвольного UPM test layout. Сослаться на canonical guides об owned saves/scenes/static resets, не обещать безопасный запуск всей suite внутри рабочего проекта.

## Минимальная docs validation после общего source barrier

Root выбрал ограниченное расширение **существующего `scripts/check_package.py`**. JSON/C# DocumentationContract framework, новый loader, snippets registry, новые assemblies и массовая генерация prose из прежнего плана — optional future work, не prerequisite v11.

После окончания всех writers координатор/назначенный владелец checker выполняет ограниченные read-only проверки:

1. Version badges/current-version declarations и imported-version examples шести страниц соответствуют `package.json`; существующий `PackageHealthCheck.VersionMentions` остаётся источником Editor parity, исторические receipts исключены из «current version».
2. UPM dependencies и все sample displayName/path берутся из manifest. Проверить три таблицы и Getting Started list, выбранный source root и примеры Audio/Survivor/UI Mesh Rig; не требовать наличия всех layout одновременно.
3. Для шести входов плюс migration проверить используемые Markdown/HTML ссылки, percent-encoded пути и существующие heading anchors, исключая code blocks. Relative package links остаются внутри payload; repository-only изображения/CLI/report используют явные repository URL. HTTP availability учитывается отдельно.
4. Проверить UTF-8, existing mojibake markers, whitespace и `.meta` новых public docs через existing package checks. Пройти всю canonical link table и обе языковые строки, не добавляя полноценный новый Markdown framework.
5. Финально сверить C# snippets с замороженными APIs и результатами релевантных Core/Audio checks. Наличие API-тестов не называть буквальной компиляцией Markdown snippet, если именно этот текст не компилировался. Чётко записать остаток проверки.

Команды `python scripts/check_package.py`, scoped `git diff --check`, Unity compiler checks и любые Edit/Play tests здесь только перечислены для будущего общего этапа: в этой подготовке они **не запускались**. Compatibility evidence требует фактической Unity 6.0–6.4 и фактической 6.5+ компиляции; shared query acceptance отдельно требует counters для 1/100/1000 и parity механики. HTML → UITK исторические 239/239 и 7/7 остаются датированным slice; новые counts/status появляются только после реального финального запуска.

Готовность README-интеграции: root подтвердил финальные actions/API/migration; центральный version bump согласован; все новые targets существуют и связаны; stale promises удалены; реальные pass/fail/skip отделены от source review. После общего подтверждения эта же сессия продолжит правки шести entry documents.

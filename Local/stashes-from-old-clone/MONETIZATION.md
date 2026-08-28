# NeoxiderTools — Стратегия монетизации и выхода на Unity Asset Store

> Market-researched go-to-market deliverable для `com.neoxider.tools` (v9.2.9).
> Дата: 2026-06-22. Все рыночные данные — 2025–2026, с источниками (см. раздел 10).
> Продуктовые/маркетинговые термины и имена ассетов оставлены на английском намеренно.

---

## 1. Executive Summary (TL;DR)

**Три зафиксированных решения (подтверждены рынком, не пересматриваются):**

1. **Позиционирование:** «no-code фреймворк игровых систем», а не «набор тулзов». Рынок наказывает grab-bag паки («quality varies») и вознаграждает focused single-system ассеты с поддержкой. NeoxiderTools должен продаваться как **готовые системы** (RPG, Cards, Merge, Grid, Slot) поверх **NeoCondition + UnityEvent**, а не как 150 утилит.
2. **License fork:** custom royalty-лицензия (0.5% выше $10k) **юридически несовместима** с Asset Store — Unity продаёт лицензии по фиксированной EULA без роялти. Free Core остаётся на OpenUPM/GitHub под royalty-лицензией; платные Pro-SKU идут на Asset Store под стандартной EULA. Это санкционированный самим OpenUPM dual-license паттерн.
3. **Модель:** Free Core (OpenUPM, воронка) + платные Pro-пакеты на Asset Store. Цены — **скорректированы вверх** по данным рынка (см. §5).

**Единственное самое важное следующее действие:**
> **Закрыть P0-блокеры перед листингом — и первым из них исправить отсутствие версионирования сейвов (save schema versioning).** Платная save-система без миграции схемы ломает сейв покупателя при первом же его изменении схемы — это #1 риск доверия и прямая причина 1★-отзывов. Без него нельзя продавать ни один SKU, где фигурирует Save. Лицензия и версия (9.2.9 vs README 9.2.7) — формальные блокеры, но save-versioning — продуктовый.

**Честный прогноз:** первые 90 дней = валидация, не доход. Год 1: $50–300/мес. 12–24 мес при 50+ отзывах и 2–3 SKU: $500–2000+/мес. Главные рычаги — YouTube-туториалы, документация/поддержка и launch-скидка (см. §9).

---

## 2. Рыночное исследование (Market Research)

### 2.1 Comparables — RPG / combat / архитектурные фреймворки

| Продукт | Издатель | Цена (list) | Отзывы / ★ | Вывод |
|---|---|---|---|---|
| **RPG Builder** | Blink | **$175** | ~367 оценок; 3 607 favorites | No-code полный RPG-редактор (combat, AI, talent trees, pets). Хвалят мощь/полноту; жалобы на темп обновлений и поддержку. Верхняя планка RPG-рынка. |
| **Dialogue System for Unity** | Pixel Crushers | **~$95** | ~528 отзывов; ~5★ | Лидер категории (Disco Elysium, Citizen Sleeper). **#1 причина успеха — легендарная долгосрочная поддержка** и десятки интеграций. |
| **Emerald AI 2025** | Black Horizon Studios | **~$60** (часто ~$30 в −50%) | ~49 оценок | Модульный AI/combat фреймворк, 24 example-сцены, бесплатные мажорные апдейты. Хвалят breadth + lifetime updates. |
| **Inventory Engine** | More Mountains | **$25** | ~16 оценок; 854 favorites | Минималистичная гибкая инвентарь-система; обкатана в Corgi/TopDown Engine. Focused single-system + Discord-поддержка. |
| **Odin Inspector & Serializer** | Sirenix | **$55** ($27.50 в −50%); per-seat | **~783 оценки; 5★** | Эталон архитектурного тула. Важно: **своя EULA с лимитом выручки <$200k** — прецедент revenue-gated лицензии на самом Asset Store. Трение — per-seat, не scope. |
| **Quantum Console** | QFSW | **~$40** (~$20 в −50%) | ~120 оценок; 5★; 1 608 fav | In-game консоль через `[Command]`. Focused, best-in-class. Жалоба — цена против бесплатных аналогов. |
| **MyBox** (GitHub, MIT) | Deadcows | **Free** | без формального рейтинга | Авторски названный «grab bag». Хвалят `ConditionalField`/`MustBeAssigned`, но прямо отмечают «some good, some less» — классический grab-bag-компромисс. Живёт бесплатно на GitHub, а не за деньги. |

### 2.2 Comparables — Casual / Slot / Merge / Match-3 / Grid / Board

| Продукт | Издатель | Цена (list) | Отзывы / ★ | Вывод |
|---|---|---|---|---|
| **Candy Match 3 Kit** | gamevanilla | **$99.99** (list до $199.99, −50%) | ~26 оценок; 1 150+ fav | Эталон match-3, «самое полное решение». Хвалят visual level editor, no-code reskin, IAP/ads. **Постоянная жалоба — слабая документация.** |
| **Fruit Swipe Match 3 Kit** | gamevanilla | **$199.99** | ~17 оценок; 513 fav | Connect/line match-3 от того же издателя; премиум-полный кит. |
| **Clicker-Idle Game Template** | (Packs) | ~$33 в бандлах | в целом позитивные | Хвалят presentation + commented code + гайд; код тяжело расширять за рамки правок. |
| **BIZNIZ Idle-Clicker Template** | Jimbob Games | mid-tier (точно не подтв.) | давний листинг | ScriptableObject-driven, offline collect, Easy Save 3 + Rewired. Сильный breadth для бэнда. |
| **MK Casino Kit (Slot Machine)** | Master Key | **~€46 (~$50)** | ~9 оценок | Reels, paylines, paytable, bonus game, jackpot, lobby UI. Mid-band casino-темплейт. |
| **Slots Creator Pro** | Brad | **~$70** | ~29 отзывов; 431 fav | «Premiere slot machine creator», **но последнее обновление ~2021 (эра Unity 4.6)** — стареющий, окно для дифференциации. |
| **Merge Toolkit** | Awessets | **~$60** | новый (2024) | Field+evolution редакторы, инвентарь, валюты, energy/XP. Использует UniTask. Полноценный merge-стартер. |
| **2048 Merge** | techjuego | **€18.39 (~$20)** | мало оценок | Дешёвый одно-механиковый пак — нижняя граница merge-бэнда. |
| **Turn Based Strategy Framework (TBSF)** | Crooked Head | **$45** | **238 оценок; 3 777 fav** | **Лидер grid/board.** Hex+square 2D/3D, pathfinding, AI, online MP; в реальных Steam-тайтлах; активно обновляется (v4.2, март 2026). Доказательство: один well-maintained grid-фреймворк доминирует. |
| **Gridr - Turn Based Framework** | Carl Hinas | **$29.99** | 7 оценок; 134 fav | Designer-friendly grid (chess→4X). Честная оговорка в листинге: «not a template, you should know how to code». |

### 2.3 Ключевой рыночный сигнал — grab-bag vs focused

Рынок **последовательно вознаграждает focused, well-supported single-system ассеты** (Dialogue System, Quantum Console, Inventory Engine, TBSF) — они копят высокие рейтинги вокруг «одна работа сделана отлично + отзывчивая поддержка». **Grab-bag/utility-коллекции** (MyBox) получают язык «mixed bag / quality varies» **даже будучи бесплатными** и обычно живут на GitHub free, а не за премиум. Премиальное «grab-bag»-пространство занято узкими архитектурными тулами (Odin), не рыхлыми утилити-паками.

**Прямое следствие для NeoxiderTools:** 534 скрипта «навалом» — это профиль MyBox (бесплатный GitHub), а не профиль платного ассета. Деньги — в **выделенных системах** с демкой и поддержкой. Отсюда и решение об SKU-распиле (§5).

### 2.4 Профиль аудитории

- **Ядро покупателя — solo indie devs и хоббисты среднего уровня** (по гайдам Unity для издателей); спектр beginner→expert.
- **Главный мотив покупки — экономия времени:** ускорить сроки, получить фичи, которые тяжело написать с нуля. Покупки project/genre-specific.
- **География смещена в US** (~43.8% Unity gamedev-юзеров; India ~10.4%; France ~9.8% — по широкой базе Unity, не строго по покупателям кода).
- **Авторитетная демография (Unity «Tastes & Trends 2025») закрыта** в publisher-портале — доступна только после принятия первого ассета. Публичные цифры — SEO/AI-агрегаты, осторожно.
- **Code/tooling-покупатели ждут:** чистый документированный модульный код + отзывчивую поддержку + обширные доки. **Template-покупатели особенно наказывают слабую документацию** (постоянная жалоба на Candy Match 3).

### 2.5 Каналы (что реально конвертит)

- **YouTube-туториалы — проверенный маховик.** Unity сама приводит в пример RvR Gaming (Kevin Penhoat → Patreon+Discord), More Mountains (TopDown Engine/Feel), The Messy Coder. Туториал показывает тул в деле → канал → комьюнити.
- **Reddit:** r/Unity3D (самый релевантный), r/gamedev, r/Unity2D, r/IndieDev, r/madewithunity — но строгие правила self-promo: чистый промо конвертит плохо, работает участие + showcase-треды.
- **Discord:** Unity Developer Community (есть каналы листинга ассетов, джемы с призами для asset-creator'ов), The Dev Mafia, IGDA (~10k). Серверы с #asset/#showcase конвертят лучше общего чата.
- **Реальная воронка:** главный конверсионный рычаг — **встроенность в Editor + аудитория Asset Store (3.3M девелоперов)**. Внешние каналы строят воронку, продаёт сам стор.
- **Launch-скидка двигает выручку:** одноразовый New Release Discount (10/30/50% на 1–2 недели, ассеты ≥$15) в среднем приносит *больше*, чем отказ от неё, и повышает шанс быть приглашённым в официальные распродажи Unity.
- **«Делай больше ассетов»** — cross-discovery между своими SKU; editor-tools/systems держат лучшую sales persistence и pricing power, чем арт.

---

## 3. Позиционирование (Positioning)

**Решение (locked):** «**no-code фреймворк игровых систем**», лид — `NeoCondition` + готовые системы (RPG, Cards, Merge, Grid, Slot).

**Почему это бьёт «tools pack» — на данных рынка:**

- **Grab-bag не продаётся за деньги.** MyBox — функционально близкий «grab bag» атрибутов/утилит — бесплатен и собирает «quality varies». Если NeoxiderTools выйдет как «150+ компонентов», он конкурирует с бесплатным MyBox и проигрывает.
- **Focused системы — продаются и копят рейтинг.** TBSF ($45, 238 оценок) — один grid-фреймворк доминирует категорию. Dialogue System (~$95, 528 отзывов) — одна система + поддержка = лидер. Это шаблон, который NeoxiderTools должен повторить **по каждому SKU отдельно**.
- **NeoCondition — реальный дифференциатор.** No-code условный слой + UnityEvent — это «glue», которого нет у focused-конкурентов (они дают систему, но не no-code-связку между системами). Это то, что превращает «набор систем» в «фреймворк»: один язык склейки поверх RPG/Cards/Merge/Grid.
- **Каждый SKU = «одна работа отлично».** Покупателю продаётся не «весь Neoxider», а «RPG Kit, который делает RPG-бой/стат/баффы/Mirror-синк» — с демо-сценой и доками. Breadth Core остаётся бесплатной воронкой, а не товаром.

**Формула листинга-заголовка (каждый SKU):**
`<Система> Kit — no-code <жанр> systems (NeoCondition + UnityEvent)` — система впереди, no-code как механизм, фреймворк-язык как «почему вместе».

---

## 4. Лицензия (License Fork) — Блокер #1

### 4.1 Проблема (подтверждена источниками)

Текущая `Assets/Neoxider/LICENSE.md` — **custom royalty**: бесплатно до $10k выручки, затем 0.5% + обязательное уведомление автора. Asset Store работает иначе:

- Покупатель получает **лицензию по фиксированной Asset Store EULA**, единоразово; Unity **запрещает** перепродажу/роялти-схемы со стороны покупателя и не поддерживает revenue-share модель на стороне buyer→author.
- Модель стора — **license, not sale**, без ongoing-роялти с покупателя. Royalty-условие из текущей лицензии **нельзя навязать** покупателю Asset Store.

Вывод: **royalty-лицензия несовместима с продажей на Asset Store**. Нельзя «прилепить» её к SKU.

### 4.2 Прецедент (санкционирован OpenUPM)

OpenUPM прямо документирует нужный паттерн: **держать open-source Core на OpenUPM/GitHub, а более полную платную версию — на Asset Store под её EULA** (доп. фичи, демо-проект, поддержка). Дополнительно: **Odin** уже продаёт на Asset Store под своей EULA с revenue-cap <$200k — то есть кастомные «entity revenue» лицензии на сторе существуют (но это лимит доступа, а не per-sale роялти; роялти всё равно нельзя).

⚠️ **Важное ограничение Unity Package Guidelines:** в пакете на OpenUPM/в Editor **нельзя рекламировать/продвигать коммерческие продукты** изнутри Editor. Free Core не должен содержать «купи Pro»-баннеров в инспекторе — только нейтральные ссылки на доки/репозиторий.

### 4.3 Конкретные шаги и где какой файл

| Файл / место | Лицензия | Действие |
|---|---|---|
| `Assets/Neoxider/LICENSE.md` (GitHub/OpenUPM Core) | Текущая royalty (или заменить на стандартную OSS — MIT/Apache-2.0 для чистоты OpenUPM) | Оставить royalty можно, **но** для OpenUPM рекомендуется SPDX-лицензия; royalty усложняет adoption воронки. Решение владельца: либо royalty (контроль), либо MIT (рост воронки). Для максимизации funnel — **MIT на Core**. |
| Каждый Pro-SKU (Asset Store) | **Стандартная Unity Asset Store EULA** | Никаких royalty-/threshold-пунктов. Не прикладывать royalty `LICENSE.md` в Pro-пакет. |
| Pro-пакет: `Third-Party Notices.md` | — | Перечислить UniTask/DOTween и их лицензии (см. §6 dependency gating). |
| Core-пакет в Editor | — | Убрать любую коммерческую рекламу Pro изнутри Editor (Unity Guidelines). |

**Чек-лист лицензии (P0):**
- [ ] Решить Core-лицензию: MIT (рост) vs royalty (контроль). Рекомендация — **MIT на Core**.
- [ ] Подготовить чистую копию пакета Pro-SKU **без** `LICENSE.md` с royalty.
- [ ] Удалить любые in-Editor промо-ссылки на платное из Core.
- [ ] Добавить `Third-Party Notices` в Pro-SKU.

---

## 5. Ценообразование и SKU (Pricing & Packaging)

### 5.1 Валидация цен владельца против рынка

| SKU | Цена владельца | Рыночные якоря | Вердикт |
|---|---|---|---|
| **RPG Kit** | $49 | RPG Builder $175; Emerald AI ~$60; Dialogue System ~$95; Inventory Engine $25 | **$49 ОК, можно $49–59.** RpgCharacter (combat+stats+buffs+NPC+Mirror) шире Inventory Engine, но уже RPG Builder. $49 — честно и конкурентно; при сильной демке/доках поднять до $59. |
| **Casual Kit** (Slot/Wheel/Merge/bonus) | $39 | Полные casual-киты $50–100 (Candy Match3 $99, Merge Toolkit $60, MK Casino ~$50); Slots Creator Pro ~$70 (устарел 2021) | **Поднять до $49–59.** Полные casual-киты на рынке дороже $39. Стареющий Slots Creator Pro — окно: современный slot/wheel/merge с no-code оправдывает $49+. |
| **Grid & Board** | $34 | **TBSF $45 (238 оценок)**; Gridr $30; Merge Toolkit $60 | **Поднять до $44–49.** Флагман категории (TBSF) — $45 при 238 оценках. $34 продаёт ценность ниже рынка и якорит вниз. |
| **Bundle** | $89–99 | Сумма SKU $49+$49+$44 = $142 list | **$99–119.** Бандл из 3 Pro-SKU при суммарном list ~$140 удобно встаёт в **$99 (−30%)** или **$119**. $89 — слишком дёшево относительно сумм. |

**Итоговая рекомендация по ценам (скорректировано вверх по данным):**
- RPG Kit — **$49** (опц. $59 при premium-демке)
- Casual Kit — **$49** (вместо $39)
- Grid & Board Kit — **$44** (вместо $34)
- Bundle (3 SKU) — **$99** (вместо $89)

> Это меняет одно из зафиксированных чисел: **Casual и Grid недооценены**. Рынок полных casual-китов ($50–100) и флагман-grid ($45) поддерживает более высокие цены. Поднятие цен также улучшает экономику launch-скидки.

### 5.2 Поведение распродаж (planning)

- Asset Store-сейлы обычно **−50% циклами ~14 дней**; list-цена — правильный якорь (Odin $55→$27.50, Quantum $40→$20).
- **New Release Discount** (одноразовый, 10/30/50% на 1–2 нед, ассет ≥$15) — использовать на старте каждого SKU.
- Закладывать, что фактическая средняя цена продажи ниже list из-за сейлов — поэтому list нельзя ставить впритык к нижней границе.

### 5.3 Что в Free Core vs Pro vs Bundle

**Free Core (OpenUPM/GitHub) — воронка, не товар:**
- `NeoCondition` + UnityEvent-слой, Reactive properties, State Machine, DI/inject, Singletons, Object Pooling, базовые Tools/UI-хелперы, Save (базовый — **только после фикса versioning**), документация.
- Цель: чтобы человек уже строил на NeoCondition и захотел готовую систему.

**Pro SKU (Asset Store):**
- **RPG Kit ($49):** RpgCharacter, combat (`RpgAttackController/Definition/Projectile`), buffs/statuses, regen, NPC (navigation/patrol/chase/animator), evade, target selectors, UI bindings, Mirror-синк, no-code RPG-bridges, **демо-сцена RPG-арены**.
- **Casual Kit ($49):** Slot/reels/paylines, Wheel-of-Fortune, daily/bonus games, Merge-движок (Neo.Merge/GridMergeResolver), Dice, прогрессия/валюты-хуки, **демо-сцены slot + wheel + merge**.
- **Grid & Board Kit ($44):** FieldGenerator, multi-cell placement, pathfinding, GridMerge, Match3, TicTacToe, SlidingMerge, DiceBoardService, **демо board-сцена**.
- **Bundle ($99):** все три + (опц.) приоритетная поддержка / Discord-роль.

**Принцип распила:** Core даёт «язык» (NeoCondition), Pro даёт «готовую игру минус контент». Каждый Pro-SKU **самодостаточен** (тащит нужный кусок Core внутрь или зависит от free Core по UPM — решить технически; для Asset Store проще **self-contained**, т.к. покупатель ждёт «импортировал и работает»).

---

## 6. Полный аудит готовности (Full Readiness Audit)

Сгруппировано по влиянию на продаваемость. Технические находки — уже верифицированы в этой сессии.

### 6.1 P0 — MUST FIX перед листингом (без этого нельзя продавать)

| # | Находка | Файл | Почему блокер продаж |
|---|---|---|---|
| P0-1 | **Save: нет schema versioning/migration** | Save-модуль | **#1 риск доверия.** Сейв, записанный в v9, ломается при следующем изменении схемы у покупателя → 1★ «потерял прогресс». Платная save-система обязана иметь миграцию. **Фиксить первым.** |
| P0-2 | **License fork** | `LICENSE.md` + Pro-пакеты | Royalty-лицензия несовместима с Asset Store EULA (см. §4). Без чистой EULA-копии SKU отклонят/незаконно. |
| P0-3 | **Dependency gating (UniTask/DOTween)** | package/asmdef | Asset Store **отклоняет** пакеты, которые не компилируются «из коробки» из-за внешних зависимостей. Нужен graceful gating (`#if` define + ясная инструкция) или включение/документирование зависимостей, иначе review-rejection. |
| P0-4 | **Version sync** | `package.json` 9.2.9 vs `README.md` badge 9.2.7 | Несоответствие версии — непрофессионально и ловится ревью/покупателем. Тривиально, но обязательно. |
| P0-5 | **RpgProjectile alloc/кадр** | `RpgProjectile.cs:87/101` | `SphereCastAll`/`CircleCastAll` аллоцируют каждый кадр на каждый снаряд → GC-спайки в бою. Для платного **RPG Kit** это видимый перф-дефект → плохие отзывы. |
| P0-6 | **MagneticField alloc/кадр** | `MagneticField.cs:334-356` | `OverlapSphere` + `new HashSet` + `new List` каждый кадр → GC-мусор. Если попадает в Casual/Grid SKU — тот же риск. |

### 6.2 SHOULD FIX (HIGH) — до листинга или сразу после, влияет на рейтинг

| Находка | Файл | Риск |
|---|---|---|
| **Material leak (нет OnDestroy)** | `Drawer.cs` | Утечка памяти в рантайме покупателя. |
| **UniTask.Forget() без CancellationToken** → `MissingReferenceException` при смене сцены | `DrunkardGame` / `BoardComponent` / `HandView` (Cards) | Краш-репорты при обычном flow (смена сцены) — прямой источник 1★ для Casual/Cards. |
| **GetComponent / Camera.main каждый кадр** | `InteractiveObject` | Перф-дефект, легко правится кешированием. |

### 6.3 POLISH (MEDIUM + docs/tests)

| Находка | Деталь |
|---|---|
| **Singleton full-scene scan** | `Singleton.cs`: `_searchFailed` пишется, но не читается → `FindObjectsByType` (полный обход сцены) на каждый доступ `I` при отсутствии инстанса. |
| **API-несогласованность** | public camelCase props (`AM.startVolumeEfx`) нарушают PascalCase; Events vs UnityEvents без принципа; `AM.SetVolume(float,bool)` — boolean-trap. |
| **God-classes** | RpgCharacter 76KB, Selector 64KB, SpinController 62KB — рефакторинг для поддерживаемости (не блокер продаж, но удешевляет поддержку = ключевой фактор успеха по рынку). |
| **Сомнительные Singletons** | `AnimationFly`, `MouseInputManager`. |
| **Editor scene-dirtying — ИСПРАВЛЕНО в этой сессии** | `ParallaxLayer`, `CameraAspectRatioScaler`, `AM` чинили (вечный `*`). НЕ баг (проверено): WheelFortune, Row, SpinController (`_setSpace` default false), Selector — идемпотентны. Вечный `*` — типичный повод для возврата/жалоб, хорошо что закрыт. |
| **Тест-гэпы** | Payline-математика `AM` и `SpinController` не покрыта — а это ядро Slot/Casual SKU; нужны тесты перед продажей Casual Kit. |
| **Доки EN отстают от RU** | Quest −4 стр., Tools −8 стр.; NeoDoc проверяет только RU-пути. Asset Store — **English-first** (US ~44% аудитории), EN-доки критичны для конверсии. |

### 6.4 Привязка к продаваемости (итог)

Рынок прямо говорит: **#1 жалоба покупателей топ-ассетов — документация и поддержка** (Candy Match 3, RPG Builder). Поэтому P0/HIGH (краши, утечки, перф, save-миграция) + EN-доки — это не «техдолг», а **прямые драйверы рейтинга**, от которого зависит вся экономика (§9).

---

## 7. Как оформить листинг (Asset Store Listing)

**English-first.** US ~44% аудитории; EN-доки и EN-листинг обязательны.

- **Title:** система впереди + ключевик жанра. Пример: `RPG Kit — No-Code Combat, Stats, Buffs & Mirror Multiplayer`. Не «NeoxiderTools RPG» (бренд покупателю неизвестен) — продавать решение, не бренд.
- **Keywords:** жанр + механика + «no-code» + «template/kit» + «Mirror/multiplayer» (для RPG). Для Casual — «slot, wheel of fortune, merge, idle, casino». Покупатели ищут по механике.
- **Первый скриншот = демо-геймплей, не диаграмма.** Top-sellers (TBSF, Candy Match3) ведут визуальным геймплеем/редактором. Показать **работающую демо-сцену** SKU.
- **Видео-трейлер (обязательно):** 30–60 сек, реальный геймплей демо-сцены + 2–3 сек на no-code инспектор (NeoCondition) как дифференциатор. Видео — главный конверсионный актив после первого скриншота.
- **Description-структура:** (1) одна строка-обещание → (2) bullet-список систем → (3) «no-code NeoCondition» как USP → (4) что в демо-сцене → (5) зависимости (UniTask/DOTween) честно и явно → (6) ссылка на EN-доки и поддержку.
- **Документация и поддержка на витрине:** ссылка на EN-доки + канал поддержки (Discord/GitHub Issues). По рынку это #1 фактор удержания рейтинга.
- **Demo-сцена per SKU** — must (locked pre-sale решение). Покупатель ждёт «импортировал → запустил демо → понял».
- **Launch:** включить New Release Discount (−30/−50%) на 1–2 недели на старте.

---

## 8. Пошаговый план выхода на прибыль (Checklist)

### Этап 0 — Pre-sale фиксы (недели 1–4) — БЕЗ ЭТОГО НЕ ЛИСТИТЬ
- [ ] **P0-1: Save schema versioning + migration** (первым; ядро доверия).
- [ ] **P0-2: License fork** — Core-лицензия (рекоменд. MIT), чистые Pro-копии без royalty, убрать in-Editor промо (§4).
- [ ] **P0-3: Dependency gating** UniTask/DOTween (`#if`-defines + инструкция) — иначе reject.
- [ ] **P0-4: Version sync** 9.2.9 везде (README badge → 9.2.9).
- [ ] **P0-5/6: Перф-фиксы** RpgProjectile + MagneticField (убрать аллокации/кадр).
- [ ] HIGH: Drawer material-leak, Cards UniTask CancellationToken, InteractiveObject кеш.

### Этап 1 — Первый SKU (недели 4–7) — выбрать ОДИН
- [ ] Выбрать **RPG Kit** как флагман (наибольший scope/дифференциация; Mirror-синк — редкий USP) ИЛИ **Casual Kit** (быстрее демонстрируется, стареющие конкуренты).
- [ ] Собрать **self-contained Pro-пакет** этого SKU + демо-сцена.
- [ ] **EN-доки** для SKU (English-first), Third-Party Notices.
- [ ] Тесты на ядро SKU (для Casual — payline-математика AM/SpinController).
- [ ] Landing + 30–60 сек трейлер.

### Этап 2 — Сабмит и валидация (недели 7–10)
- [ ] Пройти Asset Store validation tooling **до** сабмита (избежать reject-цикла).
- [ ] Submit → ревью **~10 рабочих дней** (апдейты потом ~2 дня).
- [ ] Параллельно: выложить **Free Core на OpenUPM** (воронка), без промо в Editor.
- [ ] Включить **New Release Discount** на старте.

### Этап 3 — Воронка и первые продажи (недели 10–16)
- [ ] **YouTube-туториал** по SKU (показать тул в деле) — проверенный маховик.
- [ ] Showcase-посты в r/Unity3D / r/gamedev (по правилам self-promo) + Discord (#showcase-каналы).
- [ ] Собрать **первые отзывы** — активно просить у early-покупателей (рейтинг = всё).
- [ ] Быстро отвечать на Issues/поддержку (#1 фактор успеха по рынку).

### Этап 4 — Второй и третий SKU + bundle (мес. 4–9)
- [ ] Выпустить второй SKU → cross-discovery с первым.
- [ ] После 2–3 SKU собрать **Bundle ($99)**.
- [ ] Подавать заявки/попадать в официальные Unity-сейлы (помогает сильный non-sale + launch).

### Этап 5 — Масштабирование (мес. 9–24)
- [ ] Третий SKU + регулярные апдейты (free major updates — хвалят у Emerald AI).
- [ ] Растить YouTube/Discord-комьюнити (снижает стоимость поддержки, растит affiliate).
- [ ] Боюсти/Sponsors-донаты как вторичный поток; itch/Gumroad — вторично; **Fab — пропустить** (см. §9).

---

## 9. Честный прогноз заработка (Honest Earnings)

**Привязка к comparables.** Рейтинги топ-фокус-ассетов: TBSF 238 оценок ($45), Dialogue System 528 ($95), Odin 783 ($55); а свежие/нишевые — единицы-десятки (Emerald AI 49, MK Casino 9, Gridr 7). То есть **большинство SKU стартуют с единиц отзывов**, и только годы поддержки дают сотни.

- **Месяцы 0–3:** валидация, не доход. Реалистично **$0–100/мес** на первом SKU. Постмортемы фиксируют истории «продал 2 копии за $5».
- **Год 1 (1–2 SKU, launch-скидки, первые отзывы):** **$50–300/мес**.
- **12–24 мес (2–3 SKU + bundle, 50+ отзывов, активная поддержка/YouTube):** **$500–2000+/мес**. Mid-tier $1k–5k/мес достижим для committed-издателей, но это путь **6–18 мес** от Hobbyist к Active Publisher (long-tail: малая доля издателей берёт большую часть выручки).

**Рычаги по убыванию влияния:**
1. **Рейтинг/отзывы** — экспоненциальный множитель конверсии. Достигается качеством (P0/HIGH фиксы) + быстрой поддержкой + просьбой об отзывах.
2. **Документация + поддержка** — #1 жалоба рынка; прямой драйвер удержания рейтинга. EN-доки обязательны.
3. **YouTube-туториалы** — проверенный маховик воронки.
4. **Количество SKU** — cross-discovery; «делай больше ассетов».
5. **Launch-скидка + попадание в Unity-сейлы** — в среднем даёт больше, чем отказ.
6. **Цена** — скорректированная вверх (§5) улучшает экономику без потери конкурентности.

**Что НЕ рычаг сейчас:** Fab. Он art/3D- и Unreal-центричен, **нет ясного пути для standalone Unity C#-tool/плагинов**; Unity C#-тулинг остаётся нативным для Asset Store. (Заметка на будущее: Fab-сплит 88/12 vs Unity 70/30 — повод пересмотреть, *если* Epic откроет полноценный path для Unity-кода.)

---

## 10. Источники (Sources)

**Revenue split / EULA / Provider Agreement**
- Asset Store Provider Agreement (70% издателю) — https://unity.com/legal/provider
- Asset Store Terms of Service & EULA — https://unity.com/legal/as-terms
- EULA FAQ — https://assetstore.unity.com/browse/eula-faq
- Revenue guide (70/30 подтверждение, 2026) — https://generalistprogrammer.com/tutorials/unity-asset-store-selling-guide-revenue
- Asset Store revenue manual — https://docs.unity3d.com/6000.3/Documentation/Manual/asset-store-revenue.html

**Review timeline / submission / promotion**
- How long to approve (~10 раб. дней / ~2 на апдейт) — https://support.unity.com/hc/en-us/articles/210569723-How-long-will-it-take-for-my-Asset-to-be-approved
- Submit an asset package — https://docs.unity3d.com/6000.2/Documentation/Manual/AssetStoreSubmit.html
- Submission Guidelines — https://assetstore.unity.com/publishing/submission-guidelines
- Promotion / New Release Discount — https://docs.unity3d.com/Manual//AssetStorePromotion.html

**OpenUPM / dual-license**
- OpenUPM FAQ (dual-license, commercial vs OSS split) — https://openupm.com/docs/faq.html
- OpenUPM main — https://openupm.com/

**Fab**
- Fab launch (Epic) — https://www.unrealengine.com/en-US/blog/fab-epics-new-unified-content-marketplace-launches-today
- Fab Tools & Plugins — https://www.fab.com/category/tool-and-plugin?lang=en
- Fab updates / Unity content — https://gamefromscratch.com/august-2025-fab-giveaway-new-bundles-unity-giveaway/

**Comparables — RPG / frameworks**
- RPG Builder (Blink) — https://assetstore.unity.com/packages/templates/systems/rpg-builder-177657
- Dialogue System for Unity (Pixel Crushers) — https://assetstore.unity.com/packages/tools/behavior-ai/dialogue-system-for-unity-11672
- Emerald AI 2025 — https://assetstore.unity.com/packages/tools/ai/emerald-ai-2025-303012
- Inventory Engine (More Mountains) — https://assetstore.unity.com/packages/tools/game-toolkits/inventory-engine-7100
- Odin Inspector & Serializer (Sirenix) — https://assetstore.unity.com/packages/tools/utilities/odin-inspector-and-serializer-89041 · pricing/EULA https://odininspector.com/pricing
- Quantum Console (QFSW) — https://assetstore.unity.com/packages/tools/utilities/quantum-console-211046
- MyBox (Deadcows) — https://github.com/Deadcows/MyBox

**Comparables — casual / merge / grid**
- Candy Match 3 Kit (gamevanilla) — https://assetstore.unity.com/packages/templates/systems/candy-match-3-kit-111083 · deal https://gamecontentshopper.com/asset/all-assets/candy-match-3-kit-10/2025/04/17/
- Fruit Swipe Match 3 Kit — https://assetstore.unity.com/packages/templates/systems/fruit-swipe-match-3-kit-140660
- Clicker-Idle Game Template — https://assetstore.unity.com/packages/templates/packs/clicker-idle-game-template-134752
- BIZNIZ Idle-Clicker Template — https://assetstore.unity.com/packages/templates/systems/bizniz-idle-clicker-game-template-110627
- MK Casino Kit (Slot Machine) — https://assetstore.unity.com/packages/templates/systems/mk-casino-kit-realistic-slot-machine-template-157533
- Slots Creator Pro — https://forum.unity.com/threads/slots-creator-pro-slot-machine-maker.237410/
- Merge Toolkit (Awessets) — https://assetstore.unity.com/packages/templates/systems/merge-toolkit-casual-game-template-283444
- 2048 Merge (techjuego) — https://assetstore.unity.com/packages/templates/packs/2048-merge-212641
- Turn Based Strategy Framework (Crooked Head) — https://assetstore.unity.com/packages/templates/systems/turn-based-strategy-framework-50282
- Gridr - Turn Based Framework — https://assetstore.unity.com/packages/tools/game-toolkits/gridr-turn-based-framework-231887

**Audience / channels / earnings**
- Unity community-building resources — https://assetstore.unity.com/publishing/community-building-resources
- Unity «Finding the right price» — https://assetstore.unity.com/publishing/finding-the-right-price
- Asset Store publisher earnings (forum) — https://discussions.unity.com/t/how-much-asset-store-publishers-earn/628535
- Postmortem (Medium) — https://medium.com/@dynamogeeks/how-i-made-100-by-selling-assets-on-unity-asset-store-1b1db9e88eaa
- Discord/Reddit gamedev research — https://gamedesignskills.com/game-design/discord-servers/

> Оговорки: цены/рейтинги — снимки из поиска и deal-трекеров (мид-сейл цифры пересчитаны в list); проверьте на живых страницах перед публикацией листинга. Демография «Tastes & Trends 2025» — за publisher-paywall.

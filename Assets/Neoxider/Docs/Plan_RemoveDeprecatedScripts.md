# План: удаление устаревших скриптов (выполнить позже)

Этот план описывает шаги по удалению устаревших скриптов **TimeReward**, **AiNavigation**, **WheelFortune**, **UIReady** и обновлению префабов/сцен, чтобы не осталось Missing Script. Выполнять **после** перехода на реактивные поля (7.0.0).

---

## 1. Удалить компоненты в префабах и сценах (сначала)

Перед удалением .cs нужно убрать ссылки на скрипты с объектов, иначе в Unity появятся Missing Script.

### TimeReward (guid: `84aec033cfc961449928c2a86e63aece`)

- **Префаб:** `Assets/Neoxider/Prefabs/Bonus/TimeReward.prefab`  
  Либо удалить компонент TimeReward с префаба, либо заменить на CooldownReward, либо удалить сам префаб.

### WheelFortune (guid: `0a88b1e25429f4748918c9172538abc0`)

- **Префаб:** `Assets/Neoxider/Prefabs/Bonus/WheelFortune.prefab`  
  Удалить компонент или заменить на WheelFortuneImproved.
- **Сцены:** проверить и обновить:
  - `Assets/Scenes/DemoNeo.unity`
  - `Assets/Scenes/AutoSaves/DemoNeo_AutoSave.unity`
  - `Assets/Neoxider/Samples~/Demo/Scenes/Bonuses/WheelFortuneNew.unity`
  - `Assets/Neoxider/Samples~/Demo/Scenes/Bonuses/WheelFortuneExample.unity`
  - `Assets/Scenes/AutoSaves/WheelFortuneExample_AutoSave.unity`
  - `Assets/Scenes/AutoSaves/WheelFortune_AutoSave.unity`  
  На объектах с компонентом WheelFortune — удалить компонент или заменить на WheelFortuneImproved.

### UIReady (guid: `97c57a1e8a32a1743ad637ad53ef1fec`)

- **Сцены:**
  - `Assets/Scenes/AutoSaves/WheelFortuneExample_AutoSave.unity` — удалить компонент UIReady с объекта.
  - `Assets/Neoxider/Samples~/Demo/Scenes/_ExampleGame/Shooter2D.unity` — удалить компонент UIReady с объекта.

### AiNavigation (guid: `c98d85d231be4794ba78c975aa4e68c3`)

- **Префаб:** `Assets/Neoxider/Prefabs/Tools/AI/OLD AI NPC.prefab`  
  Удалить компонент AiNavigation или заменить на альтернативу (например NpcNavigation).
- **Сцены:**
  - `Assets/Neoxider/Samples~/Demo/Scenes/Ai/AiMoveExample.unity`
  - `Assets/Scenes/AutoSaves/AiMoveExample_AutoSave.unity`
  - `Assets/Scenes/AutoSaves/AiMove_AutoSave.unity`  
  На объектах с компонентом AiNavigation — удалить компонент или заменить на NpcNavigation/другую альтернативу.

---

## 2. Удалить файлы скриптов и .meta (после обновления префабов/сцен)

После того как во всех префабах и сценах компоненты убраны или заменены:

1. **TimeReward**  
   - `Assets/Neoxider/Scripts/Bonus/TimeReward/TimeReward.cs`  
   - `Assets/Neoxider/Scripts/Bonus/TimeReward/TimeReward.cs.meta`

2. **AiNavigation**  
   - `Assets/Neoxider/Scripts/Tools/Other/AiNavigation.cs`  
   - `Assets/Neoxider/Scripts/Tools/Other/AiNavigation.cs.meta`

3. **WheelFortune**  
   - `Assets/Neoxider/Scripts/Bonus/WheelFortune/WheelFortune.cs`  
   - `Assets/Neoxider/Scripts/Bonus/WheelFortune/WheelFortune.cs.meta`

4. **UIReady**  
   - `Assets/Neoxider/Scripts/UI/UIReady.cs`  
   - `Assets/Neoxider/Scripts/UI/UIReady.cs.meta`

---

## Порядок выполнения

1. Открыть каждый перечисленный префаб и сцену в Unity.
2. Найти объекты с компонентами TimeReward, WheelFortune, UIReady, AiNavigation (по нужному guid или по имени скрипта).
3. Удалить компонент или заменить на указанную альтернативу.
4. Сохранить префабы и сцены.
5. Удалить перечисленные .cs и .meta файлы из проекта.

После этого в проекте не должно остаться ссылок на удалённые скрипты.

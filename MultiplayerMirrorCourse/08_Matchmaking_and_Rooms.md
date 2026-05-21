# Урок 8: лобби, Ready flow и NetworkRoomManager

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · базовый трек · урок 8/15 · Mirror `96.x`

| Ключевые слова | `NetworkRoomManager`, Room Player, Game Player, ready, scene change |
|----------------|----------------------------------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | Lobby/room flow, room player, game player. |
| Кто владеет state | Server/room manager решает ready/start; backend может искать match. |
| Как проверить | Два clients проходят lobby -> ready -> game scene. |
| Артефакт | `ROOM_FLOW.md` со схемой переходов и late join policy. |

---

## Что должно получиться

Вы понимаете разницу между лобби и матчем. У вас есть схема перехода: подключение -> room player -> ready -> gameplay scene -> game player.

---

## Проблема

Новички часто пытаются хранить всё состояние в одном player prefab. Потом оказывается, что выбор персонажа, ready, загрузка сцены и боевой state смешаны.

---

## Теория коротко

`NetworkRoomManager` помогает разделить:

| Сущность | Где живёт | Что хранит |
|----------|-----------|------------|
| Room Player | Лобби | ready, выбор класса, цвет, слот. |
| Game Player | Матч | HP, движение, инвентарь матча. |

Если вы пишете своё лобби без `NetworkRoomManager`, роли всё равно полезно сохранить в архитектуре.

---

## Практика

1. Создайте scene `Lobby`.
2. Создайте scene `Game`.
3. Создайте `RoomPlayer` prefab с `NetworkRoomPlayer`.
4. Создайте `GamePlayer` prefab с movement/identity.
5. Настройте `Room Scene` и `Gameplay Scene`.
6. Добавьте кнопку Ready.
7. Проверьте переход двумя клиентами.

Для выбора персонажа храните стабильный ID:

```csharp
public sealed class MyRoomPlayer : NetworkRoomPlayer
{
    [SyncVar] public int selectedClassId;

    [Command]
    public void CmdSelectClass(int classId)
    {
        if (!ClassCatalog.Exists(classId)) return;
        selectedClassId = classId;
    }
}
```

Перенос в Game Player делайте на сервере в flow создания game player, а не через UI.

---

## Проверка себя

- Два клиента видят ready state друг друга.
- Gameplay scene стартует только после ready.
- Выбор класса переносится в матч.
- Disconnect в лобби чистит UI и room state.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| Игра стартует у одного клиента | Scene change запускается server-side и синхронизируется manager? |
| Ready можно подделать | Ready request проходит через Command/server validation. |
| Late join ломает матч | Политика late join явно записана: allow, spectator, deny. |
| Данные lobby потерялись | Отличаете room player data от game player state? |

---

## Частые ошибки

- Боевой HP хранится в Room Player.
- Scene загружается через обычный `LoadScene`, минуя сетевой flow.
- Ready можно спамить без debounce.
- Нет timeout для зависшего игрока.
- UI держит ссылки на destroyed room objects после смены сцены.

---

## Лайфхаки

- Нарисуйте state machine лобби до кода.
- У выбора персонажа должен быть ID, а не ссылка на prefab из UI.
- Сразу решите, что происходит при cancel search и disconnect.
- Для маленького проекта можно написать custom lobby, но не смешивайте room и match state.

---

## Профессиональный минимум

- Matchmaking не хранит gameplay truth.
- Room metadata не содержит секретов и авторитетного HP/score.
- Ready имеет debounce/rate limit.
- Переход room -> game покрыт smoke-тестом с disconnect/reconnect.

---

## Домашнее задание

Сделайте `ROOM_FLOW.md`:

- состояния: disconnected, lobby, ready, loading, in match;
- кто инициирует переход;
- какие данные переносятся из Room Player в Game Player;
- что происходит при disconnect на каждом этапе.

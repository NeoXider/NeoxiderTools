# Урок 3: движение, физика и NetworkTransform

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · базовый трек · урок 3/15 · Mirror `96.x`

| Ключевые слова | `NetworkTransform`, `isLocalPlayer`, `isOwned`, Rigidbody, prediction |
|----------------|------------------------------------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | Player prefab, movement script, камера локального игрока. |
| Кто владеет state | Input читает client-owner; позицию синхронизирует выбранная authority-схема. |
| Как проверить | Два клиента двигают только свои player object; remote copies не читают input. |
| Артефакт | `MOVEMENT_NET.md` с authority, `NetworkTransform` variant, Send Rate/buffer. |

---

## Что должно получиться

Игрок двигает только свой объект, а остальные клиенты видят движение как удалённые копии. Вы понимаете, когда брать server authority, client authority и почему движение не надо отправлять вручную каждый кадр без причины.

---

## Проблема

Самая частая ошибка: один и тот же `Update()` двигает все копии игрока на всех клиентах. На host это иногда выглядит нормально, а отдельный client сразу показывает хаос.

---

## Теория коротко

Есть две разные задачи:

| Задача | Где решается |
|--------|--------------|
| Читать ввод локального игрока | Только owner/local player. |
| Передать позицию другим | Через `NetworkTransform` или свою сетевую схему. |

Типовой guard:

```csharp
void Update()
{
    if (!isLocalPlayer) return;

    float x = Input.GetAxisRaw("Horizontal");
    float z = Input.GetAxisRaw("Vertical");
    Move(x, z);
}
```

Для owned objects, которые не являются player prefab, может быть уместнее `isOwned`. Для первого player movement используйте `isLocalPlayer`, пока не поймёте разницу.

---

## Выбор подхода

| Подход | Когда подходит |
|--------|----------------|
| Client authority + проверки | Кооп, прототипы, нестрогая честность. |
| Server authority | PvP, экономика позиции, важная физика. |
| Server authority + prediction | PvP action, где задержка ломает ощущение. |
| NetworkTransform | Базовая синхронизация transform без ручного протокола. |

В Mirror `96.x` есть разные варианты NetworkTransform. Не выбирайте их по названию: измеряйте поведение под задержкой и потерями.

По документации Mirror `NetworkTransform` бывает reliable и unreliable. Reliable экономнее по bandwidth, unreliable обычно лучше для низкой задержки движения. В новых версиях настройка частоты может жить на `NetworkManager` как `Send Rate`, поэтому не ищите старый `syncInterval`, если inspector уже изменился.

Буферизация движения - это не баг, а способ сгладить latency/loss/jitter. Чем хуже сеть, тем выше нужен buffer, но тем больше визуальная задержка.

---

## Практика

1. На player prefab добавьте `NetworkTransform` подходящего типа вашей версии.
2. Отключите sync лишних осей, если scale/rotation не нужны.
3. В movement script добавьте guard `if (!isLocalPlayer) return;`.
4. Камеру и input включайте в `OnStartLocalPlayer`.
5. Запустите Host + отдельный Client.

Пример локальной камеры:

```csharp
public sealed class LocalPlayerSetup : NetworkBehaviour
{
    [SerializeField] Camera playerCamera;
    [SerializeField] AudioListener audioListener;

    public override void OnStartLocalPlayer()
    {
        playerCamera.enabled = true;
        audioListener.enabled = true;
    }

    public override void OnStartClient()
    {
        if (isLocalPlayer) return;

        playerCamera.enabled = false;
        audioListener.enabled = false;
    }
}
```

---

## Проверка себя

- Нажатие WASD в первом клиенте двигает только его игрока.
- Второй клиент видит движение первого, но своим вводом его не контролирует.
- В сцене активна только камера локального игрока.
- При 150 ms latency движение остаётся понятным, даже если не идеально.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| Все игроки двигаются от одного input | Нет guard `isLocalPlayer` или `isOwned`. |
| Две камеры активны | Камера включается только в `OnStartLocalPlayer`. |
| Remote movement дёргается | Send Rate, buffer multiplier, reliable/unreliable variant, packet loss. |
| PvP speedhack возможен | Server не проверяет скорость, cooldown и допустимую позицию. |

---

## Частые ошибки

- Нет `isLocalPlayer`/`isOwned` guard.
- Две активные камеры и два `AudioListener`.
- Синхронизация scale "на всякий случай".
- Физика считается на клиенте, а сервер никак не проверяет speedhack.
- Попытка решить PvP movement одним `NetworkTransform` без prediction/валидации.

---

## Лайфхаки

- В `OnStartLocalPlayer` меняйте цвет/маркер локального игрока. Новичкам так проще видеть ownership.
- Для платформ, дверей и лифтов начинайте с server authority.
- Для Rigidbody не смешивайте ручной transform movement и физические силы без ясной схемы.
- Решение по движению документируйте в `MOVEMENT_NET.md`.

---

## Профессиональный минимум

- Movement не отправляется вручную каждый кадр без измеренной причины.
- Для PvP есть server validation или отдельный план prediction/reconciliation.
- У `NetworkTransform` отключены лишние axes.
- Решение проверено с latency/loss simulation, а не только на localhost.

---

## Домашнее задание

Сделайте две сущности:

1. Игрок с локальным вводом и сетевой синхронизацией.
2. Движущаяся платформа, которую двигает только сервер.

В `MOVEMENT_NET.md` запишите, кто имеет authority и какой компонент синхронизации используется.

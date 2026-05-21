# Урок 20: сетевой тик, Update и FixedUpdate

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · продвинутый трек · урок 5/15 · Mirror `96.x`

| Ключевые слова | tick, `Update`, `FixedUpdate`, send rate, simulation |
|----------------|------------------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | Timing model: input, physics, network send, server simulation. |
| Кто владеет state | Server simulation owns authoritative gameplay time. |
| Как проверить | Logs show update/fixed/network send assumptions under different frame rates. |
| Артефакт | Timing table in `MOVEMENT_NET.md` or `NET_TIMING.md`. |

---

## Что должно получиться

Вы понимаете, какие системы обновляются каждый кадр, какие - физическим шагом, а какие - с сетевой частотой.

---

## Проблема

Если читать ввод, двигать Rigidbody, отправлять команды и симулировать сервер "как попало", поведение будет зависеть от FPS, лагов и порядка callbacks.

---

## Три времени

| Время | Для чего |
|-------|----------|
| `Update` | Ввод, камера, локальный visual. |
| `FixedUpdate` | Физика Unity. |
| Network/send interval | Частота отправки state по сети. |

Сетевой тик не обязан совпадать с FPS или physics tick.

---

## Практика

Сделайте таблицу систем:

| Система | Где обновляется | Почему |
|---------|-----------------|--------|
| Input | `Update` | Не пропускать кадры ввода. |
| Rigidbody movement | `FixedUpdate` | Физика. |
| Weapon cooldown | Серверное время | Честность. |
| HP SyncVar | По изменению | Не каждый кадр. |
| HUD | Event/hook | Только при изменении. |

---

## Проверка себя

- Нет `Command` каждый кадр без лимита.
- Физика не зависит от FPS клиента.
- Серверные cooldown используют серверное время.
- Send interval выбран осознанно и измерен.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| Движение зависит от FPS | Movement использует frame delta/physics неправильно. |
| Physics разъезжается | Rigidbody меняется в `Update`, а не согласованно с `FixedUpdate`. |
| Network flood | Send Rate слишком высокий или manual sends каждый frame. |
| Prediction ломается | Нет единого tick/id для input и reconciliation. |

---

## Частые ошибки

- Двигать Rigidbody через `transform.position` в `Update`.
- Отправлять input как RPC каждый кадр.
- Считать `FixedUpdate` "сетевым тиком".
- Менять `Time.timeScale` и ломать сетевую логику.

---

## Лайфхаки

- Для server build задавайте target frame rate осознанно.
- Все cooldown, влияющие на честность, считайте на сервере.
- Для movement смотрите уроки 3 и 10 вместе.
- В `MOVEMENT_NET.md` фиксируйте, что где обновляется.

---

## Профессиональный минимум

- Input, physics, network send и server simulation разделены в документации.
- Важные gameplay решения не зависят от client FPS.
- Network rate не подменяет design of simulation tick.
- Timing проверен на low FPS/high latency cases.

---

## Домашнее задание

Заполните таблицу для вашей игры: movement, shooting, UI, AI, pickups, inventory, scene loading. Для каждой системы укажите update-loop и сетевую частоту.

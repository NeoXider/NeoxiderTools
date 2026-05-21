# Урок 26: кастомные NetworkMessage и протокол

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · продвинутый трек · урок 11/15 · Mirror `96.x`

| Ключевые слова | `NetworkMessage`, protocol, version, serializer, payload |
|----------------|----------------------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | Protocol messages before/without spawned objects. |
| Кто владеет state | Messages carry requests/data; server still validates. |
| Как проверить | Valid/invalid message handled before player spawn, with size/rate limits. |
| Артефакт | `NETWORK_PROTOCOL.md` with message schema and version. |

---

## Что должно получиться

Вы понимаете, когда RPC неудобен, и умеете описать сетевое сообщение без привязки к конкретному spawned object.

---

## Проблема

RPC привязан к `NetworkBehaviour`. Для auth, handshake, matchmaking, telemetry или системных сообщений это часто лишняя зависимость.

---

## Когда использовать NetworkMessage

| Сценарий | Почему message |
|----------|----------------|
| Handshake/version | До spawn объекта. |
| Auth payload | Не относится к player object. |
| Match list | Глобальный протокол. |
| Server notice | Системное сообщение. |
| Telemetry/dev diagnostics | Не gameplay state. |

---

## Практика

```csharp
using Mirror;

public struct ClientHelloMessage : NetworkMessage
{
    public int protocolVersion;
    public string clientVersion;
    public string token;
}

public struct ServerRejectMessage : NetworkMessage
{
    public string reason;
}
```

Правила:

- у сообщения есть версия протокола;
- payload ограничен по размеру;
- сервер валидирует все поля;
- секреты не пишутся в лог.

---

## Проверка себя

- Сообщение можно обработать без spawned player.
- Есть ограничение размера и частоты.
- Есть version field.
- Ошибка протокола ведёт к понятному disconnect/reject.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| Message handler падает до spawn | Null checks, auth state, connection lifecycle. |
| Protocol mismatch | Version field и reject reason. |
| Memory/traffic spike | Payload size limit, no giant JSON, rate limit. |
| Gameplay state расходится | Вы используете message там, где нужен Sync/Command flow. |

---

## Частые ошибки

- Делать gameplay state через messages вместо SyncVar/Commands без причины.
- Не версионировать протокол.
- Отправлять большие JSON payload.
- Доверять message от клиента.

---

## Лайфхаки

- Держите список сообщений в `NETWORK_PROTOCOL.md`.
- Для нестандартных типов используйте явные Reader/Writer.
- Если message влияет на матч, он всё равно проходит server validation.

---

## Профессиональный минимум

- Каждое message имеет direction, version, max size и handler owner.
- Client messages валидируются как недоверенные.
- Protocol changes проходят migration/backward compatibility decision.
- Auth/protocol logs не раскрывают secrets.

---

## Домашнее задание

Опишите три сообщения вашей игры:

| Message | Направление | Поля | Лимит | Что проверяет сервер |
|---------|-------------|------|-------|----------------------|

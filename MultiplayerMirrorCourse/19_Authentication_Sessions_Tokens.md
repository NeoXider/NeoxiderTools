# Урок 19: аутентификация, сессии и токены

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · продвинутый трек · урок 4/15 · Mirror `96.x`

| Ключевые слова | `NetworkAuthenticator`, token, session, timeout, version |
|----------------|----------------------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | Authenticator/handshake, token validation, session mapping. |
| Кто владеет state | Backend issues token; server validates before spawning player. |
| Как проверить | Valid token connects, invalid/expired/version mismatch gets rejected. |
| Артефакт | `AUTH_FLOW.md` with success and failure paths. |

---

## Что должно получиться

Подключение игрока проходит через явный auth-flow. Неверный token, версия или сессия получают отказ до полноценного spawn игрока.

---

## Проблема

Если любой подключившийся клиент сразу попадает в матч, сервер не контролирует аккаунт, версию клиента, повторный вход и забаненных игроков.

---

## State machine

```text
Disconnected -> Connecting -> Authenticating -> InLobby -> Loading -> InMatch -> Disconnecting
```

На каждом переходе нужны:

- timeout;
- причина отказа;
- лог;
- UI-сообщение.

---

## Практика

1. Клиент получает token у backend/platform.
2. При подключении отправляет token через authenticator/handshake.
3. Сервер проверяет token, версию и match id.
4. При успехе допускает игрока.
5. При отказе закрывает соединение с причиной.

Псевдологика:

```csharp
bool ValidateJoin(string token, string clientVersion, string matchId)
{
    if (!VersionAllowed(clientVersion)) return false;
    if (!TokenValid(token)) return false;
    if (!MatchAcceptsPlayer(matchId, token)) return false;
    return true;
}
```

---

## Проверка себя

- Неверный token не создаёт player object.
- Старая версия клиента получает понятную ошибку.
- Auth timeout отключает молчащий client.
- Серверный лог содержит причину отказа.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| Player spawned до auth | Spawn происходит до successful authenticator/validation. |
| Token работает бесконечно | Нет expiry, nonce/session binding или revocation. |
| В логах утёк token | Логируйте token id/hash, не raw token. |
| Reconnect создаёт дубль | Session mapping не очищается/не обновляется. |

---

## Частые ошибки

- Проверять auth только в UI.
- Хранить server secret в клиенте.
- Не иметь timeout.
- Пускать игрока в gameplay до окончания auth.
- Не отличать reconnect от второго входа.

---

## Лайфхаки

- Token должен быть короткоживущим.
- Version mismatch лучше ловить до загрузки gameplay scene.
- Для dev/staging/prod используйте разные ключи.
- Не пишите raw token в лог.

---

## Профессиональный минимум

- Auth failure не создаёт gameplay objects.
- Token привязан к account/session/build version и живёт ограниченное время.
- Raw secrets не попадают в client logs, server logs и lobby metadata.
- Reconnect и disconnect имеют отдельные сценарии в `AUTH_FLOW.md`.

---

## Домашнее задание

Опишите `AUTH_FLOW.md`:

- откуда берётся token;
- как сервер его проверяет;
- какие есть причины отказа;
- какие timeout;
- какой текст увидит игрок.

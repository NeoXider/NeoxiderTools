# Урок 29: сетевая безопасность, лимиты и секреты

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · продвинутый трек · урок 14/15 · Mirror `96.x`

| Ключевые слова | rate limit, secrets, firewall, admin, encryption, validation |
|----------------|--------------------------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | Public test security checklist and threat model. |
| Кто владеет state | Server/backend protect truth; client is untrusted. |
| Как проверить | Try invalid token, command spam, wrong version, blocked port. |
| Артефакт | `SECURITY_CHECKLIST.md` and one-page threat model. |

---

## Что должно получиться

У вас есть базовый security checklist перед публичным тестом.

---

## Проблема

Публичный сервер получает не только игроков, но и сканеры, спам, модифицированные клиенты, старые версии, случайные подключения и утечки токенов.

---

## Минимальные слои защиты

| Слой | Что сделать |
|------|-------------|
| Network | Открыты только нужные порты. |
| Auth | Token, timeout, version check. |
| Commands | Validation + rate limit. |
| Secrets | Только server/CI secrets, не client build. |
| Logs | Причины отказов без секретов. |
| Admin | VPN/IP allowlist, не публичная админка. |

---

## Rate limit

Для каждой команды:

| Command | Лимит | Реакция |
|---------|-------|---------|
| Attack | cooldown оружия | ignore/log. |
| Buy | несколько раз/сек | reject/log. |
| Chat | окно сообщений | mute/kick. |
| Ready | debounce | ignore. |

---

## Проверка себя

- Нет секретов в репозитории и client build.
- У опасных commands есть лимиты.
- Admin endpoint не открыт всему интернету.
- Disconnect/reject причины логируются.
- Encryption transport не считается заменой validation.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| Secret оказался в build | Search in project/build logs, move to backend/env/CI secret store. |
| Command spam влияет на матч | Rate limit/cooldown/server validation. |
| Старый client входит | Version check before spawn. |
| Admin доступен извне | Firewall/VPN/IP allowlist/separate port. |

---

## Частые ошибки

- Ключ backend в клиенте.
- RCON/admin без VPN.
- Один порт для игры и админки.
- Нет лимитов на команды.
- Логируются raw tokens.

---

## Лайфхаки

- Dev/staging/prod имеют разные ключи.
- Секреты регулярно ротируются.
- Любой public endpoint должен иметь owner и лимит.
- Перед beta сделайте threat model на одну страницу.

---

## Профессиональный минимум

- Threat model покрывает modified client, spam, old version, leaked token.
- Secrets never ship with client.
- Admin surface isolated from gameplay surface.
- Incident path exists: who responds, how to revoke, how to rollback.

---

## Домашнее задание

Создайте `SECURITY_CHECKLIST.md` из 10 пунктов:

- commands;
- auth;
- ports;
- secrets;
- logs;
- admin;
- client version;
- rate limits;
- backup/rollback;
- incident contact.

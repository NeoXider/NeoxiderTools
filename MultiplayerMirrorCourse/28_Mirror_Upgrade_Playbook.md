# Урок 28: playbook обновления Mirror

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · продвинутый трек · урок 13/15 · Mirror `96.x`

| Ключевые слова | upgrade, changelog, rollback, smoke, package version |
|----------------|------------------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | Mirror package update branch and PR checklist. |
| Кто владеет state | Upgrade PR owns dependency change only, not gameplay features. |
| Как проверить | Client/server builds + smoke matrix before merge. |
| Артефакт | `MIRROR_UPGRADE.md` and PR template old -> new. |

---

## Что должно получиться

Вы обновляете Mirror отдельной задачей, а не "между делом" вместе с gameplay-фичей.

---

## Проблема

Mirror влияет на transport, spawn, serialization, editor tooling и callbacks. Обновление может сломать код, который в обычном singleplayer test даже не запускается.

---

## Правильный процесс

1. Создать отдельную ветку.
2. Зафиксировать текущую версию Mirror.
3. Прочитать GitHub Releases между старой и новой версией.
4. Обновить пакет.
5. Собрать client и server.
6. Прогнать smoke.
7. Обновить docs/changelog проекта.
8. Иметь rollback.

---

## Что проверять

| Зона | Проверка |
|------|----------|
| Spawn | player, dynamic prefab, destroy. |
| SyncVar | initial state, hooks, owner-only. |
| Commands/RPC | authority, requiresAuthority. |
| Transport | connect/disconnect, WebGL if нужен. |
| Scene loading | lobby/gameplay/additive. |
| Dedicated | server build and READY log. |

---

## Проверка себя

- PR обновления Mirror не содержит gameplay-фич.
- В описании PR есть old -> new version.
- Есть список пройденных smoke-сценариев.
- Rollback понятен.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| После upgrade broken commands | Authority/default changes, `requiresAuthority`, sender validation. |
| Spawn сломался | Prefab registration, NetworkIdentity changes, scene objects. |
| WebGL шумит errors | Transport release notes and platform-specific fixes. |
| Нельзя откатиться | No branch/tag/artifact before upgrade. |

---

## Частые ошибки

- Обновлять Mirror и сразу менять gameplay.
- Читать только старый GitBook, игнорируя GitHub Releases.
- Не собирать dedicated/server target.
- Не проверять WebGL/Steam path, если они есть.

---

## Лайфхаки

- В проекте держите `MIRROR_UPGRADE.md`.
- Для больших проектов используйте staged rollout: internal -> closed test -> public.
- Если API поменялся, обновляйте учебные docs сразу.

---

## Профессиональный минимум

- Upgrade is isolated, documented and reversible.
- Smoke matrix includes platform-specific transports.
- Release notes are linked in PR.
- Course/docs updated in same dependency task when behavior changes.

---

## Домашнее задание

Сделайте шаблон PR:

```text
Mirror upgrade X -> Y
Sources:
Smoke passed:
Risks:
Rollback:
```

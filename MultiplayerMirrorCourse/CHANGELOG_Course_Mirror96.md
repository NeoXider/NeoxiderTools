# Версии Mirror и актуальность курса

Курс ориентирован на Mirror `96.x`. В этом проекте локально установлен Mirror `96.0.1` (`Assets/Mirror/version.txt`).

Последняя сверенная версия в GitHub Releases: `96.10.0` от 2026-04-02. Это не значит, что проект уже обновлён. Это значит, что курс помечает места, которые нужно проверить перед апдейтом с `96.0.1`.

Этот файл нужен не как полный changelog Mirror, а как учебная карта:

- какие темы курса зависят от версии;
- какие документы проверять;
- какие smoke-тесты запускать после обновления;
- какие изменения стоит объяснить новичку отдельно.

---

## Источники

- [Mirror documentation](https://mirror-networking.gitbook.io/docs/)
- [Mirror GitHub Releases](https://github.com/MirrorNetworking/Mirror/releases)
- [Mirror repository README](https://github.com/MirrorNetworking/Mirror)
- [Mirror Network Identity docs](https://mirror-networking.gitbook.io/docs/manual/components/network-identity)
- [Mirror Synchronization docs](https://mirror-networking.gitbook.io/docs/manual/guides/synchronization)
- [Mirror Remote Actions docs](https://mirror-networking.gitbook.io/docs/manual/guides/communications/remote-actions)
- [Mirror Network Transform docs](https://mirror-networking.gitbook.io/docs/manual/components/network-transform)
- [Mirror Interest Management docs](https://mirror-networking.gitbook.io/docs/manual/interest-management)
- [Unity Dedicated Server build docs](https://docs.unity3d.com/Manual/dedicated-server-build.html)
- [Unity Multiplayer Play Mode package docs](https://docs.unity3d.com/Packages/com.unity.multiplayer.playmode@1.6/manual/index.html)

---

## Что важно для курса

| Тема | Уроки | Комментарий |
|------|-------|-------------|
| `NetworkIdentity` на root, nested identities не поддерживаются | 02 | Проверять каждый network prefab. |
| Dynamic spawn через `NetworkServer.Spawn` | 02 | Клиент просит, сервер спавнит. |
| `netId` назначается runtime instance | 02, 04 | Prefab asset с `netId = 0` - нормально. |
| `SyncVar` идёт server -> clients | 04 | Клиент меняет state через `Command`. |
| Sync-коллекции для списков и словарей | 05 | Не отправлять JSON всего списка без причины. |
| `Command` требует authority по умолчанию | 06 | `requiresAuthority = false` требует строгой валидации. |
| Interest Management управляет observers | 07 | Это не замена античиту, но снижает трафик и утечки данных. |
| `NetworkTransform` использует snapshot/interpolation подход | 03, 10, 20 | Проверять Sync Direction, reliable/unreliable variant, Send Rate и buffer. |
| Dedicated server требует headless bootstrap | 14, 23, 25 | UI-кнопки не подходят. |
| Custom `NetworkMessage` полезны для protocol/auth | 19, 26 | Версионировать и лимитировать. |
| Multiplayer Play Mode полезен для локальной проверки | 01, 22 | Не заменяет отдельный build и dedicated smoke. |
| WebGL требует подходящий transport | 01, 16, 17 | Browser client не работает через KCP-only. |

---

## Учтённые релизы после 96.0.1

| Версия | Дата | Важно |
|--------|------|-------|
| `96.9.16` | 2026-02-26 | SimpleWebTransport memory allocation fixes. |
| `96.9.19` | 2026-03-01 | NetworkIdentity unreliable baseline fix. |
| `96.9.21` | 2026-03-26 | `GetStableHashCode16` вместо усечения хеша. |
| `96.9.22` | 2026-03-26 | `DestroyOwnedObjects` performance. |
| `96.9.23` | 2026-03-26 | WebGL unreliable EntityStateMessages noise fix. |
| `96.10.0` | 2026-04-02 | `SetHostVisibility` затрагивает Canvas renderers; Vector X Byte structs. |

---

## Учебные решения по Mirror 96.x

| Решение | Почему так |
|---------|------------|
| Базовый transport для desktop: KCP | Mirror docs называют KCP default transport; он UDP и требует открыть UDP port. |
| Sync state через `SyncVar`/SyncCollections | Это проще и безопаснее для новичка, чем самописный протокол. |
| Event через RPC, long-lived state через Sync | Так меньше десинхронов после late join/reconnect. |
| Movement сначала через `NetworkTransform` | Это даёт быстрый baseline; prediction включается только по реальной необходимости. |
| Interest Management не с первого урока | Новичку сначала нужен connect/spawn/state, а не оптимизация видимости. |
| Dedicated отдельно от Host | Host скрывает server/client ошибки и не показывает headless проблемы. |

---

## Как обновлять Mirror

1. Отдельная ветка.
2. Записать old/new version.
3. Прочитать GitHub Releases между версиями.
4. Обновить package.
5. Собрать client и dedicated server.
6. Прогнать smoke tests.
7. Обновить этот файл и уроки, если API/поведение изменились.
8. Подготовить rollback.

Минимальный smoke после обновления:

- Host + Client connect.
- Dedicated + Client connect.
- Player spawn/despawn.
- `SyncVar` hook.
- `Command` с валидным и невалидным request.
- `NetworkTransform` под latency/loss simulation.
- Dynamic prefab spawn/destroy.
- Custom `NetworkMessage`, если используется.

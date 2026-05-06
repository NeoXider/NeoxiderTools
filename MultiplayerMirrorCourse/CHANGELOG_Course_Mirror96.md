# Соответствие курса линии Mirror v96.x

**Год актуализации материалов: 2026.** В URL GitBook фигурирует сегмент **`2025-change-log`** — это **официальное имя** страницы документации (крупный свод на дату выхода **v96.0.1** в Asset Store, **2025-02-27**). Все **последующие** патчи и миноры линии `v96.x` за **2026** смотрите в [GitHub Releases](https://github.com/MirrorNetworking/Mirror/releases).

Источники (проверяйте оба — после **v96.0.1** новые фиксы чаще всего сначала появляются на GitHub):

- [2025 Change Log (Asset Store / GitBook)](https://mirror-networking.gitbook.io/docs/manual/general/changelog/2025-change-log) — структурированный список фич для **v96.0.1** (дата публикации в Store — 2025-02-27).
- [Releases на GitHub](https://github.com/MirrorNetworking/Mirror/releases) — **v96.9.x**, **v96.10.x** и далее (**2026**).

> **Правило курса:** перед релизом игры сверяйте **свой** тег пакета с таблицей ниже и с последними 2–3 релизами на GitHub; строки с пометкой «post-96.0.1» появились **после** записи в GitBook на дату v96.0.1.

---

## Таблица: фичи и изменения API → уроки курса

| Тема Mirror | Урок | Комментарий |
|-------------|------|---------------|
| `NetworkTransformHybrid` (редкий reliable full state + unreliable дельты) | 03 | v96.0.1 |
| `velocity` / `angularVelocity` на приёмнике `NetworkTransform` | 03 | v96.0.1 |
| `NetworkTransform`: `FixedUpdate`, `Physics.SyncTransforms` в `ResetState` / телепорт | 03 | v96.0.1 |
| `NetworkRigidbody*` + **`useFixedUpdate`**: дочерний `FixedUpdate` перекрывает родительский; на **stock** пакете оставляйте **Use Fixed Update** выключенным или ждите фикса upstream | 03 | поведение кода v96.x, зафиксировано в курсе **2026** |
| `SnapShotSettings` вместо старых полей snapshot в `NetworkClient` | 03, 15 | v96.0.1 *change* |
| Hex Spatial Hash Interest Management (2D/3D) | 07 | [док](https://mirror-networking.gitbook.io/docs/manual/interest-management/hex-spatial-hashing.md), v96.0.1 |
| Scene Distance Interest Management | 07 | v96.0.1 |
| Interest Management: все формы в **LateUpdate** | 07 | v96.0.1 *fix* |
| `SetHostVisibility`: terrain tree details, lights, audio, particles | 07, 12 | v96.0.1 |
| `SetHostVisibility`: все **Canvas** renderers | 07, 12 | **v96.10.0** (post-96.0.1) |
| `HashSet` в `SyncVar` / `Command` / `ClientRpc` / `TargetRpc` / `NetworkMessage` | 05, 06, 26 | v96.0.1 |
| `Vector4Long`, half floats для кватернионов; **HalfFloats** в Compression | 13 | v96.0.1 |
| **Vector X Byte** structs (организация типов сжатия) | 13 | **v96.10.0** (post-96.0.1) |
| Отложенное применение spawn payload на клиенте; меньше гонок `netId` | 02, 04 | v96.0.1 *change* |
| `NetworkIdentity.OnDestroy` проверяет `spawned[netId]` перед удалением | 02 | v96.0.1 *fix* |
| `NetworkServer`: `dontListen` → **`listen`** (семантика без двойного отрицания) | 01, 14 | v96.0.1 *change* |
| `connectionId` перенесён на **`NetworkConnectionToClient`** | 01, 02, 06 | v96.0.1 *change* |
| `NetworkManager` / `Transport`: deprecated **`OnApplicationQuit`** → **`OnDestroy`** | 01, 15 | v96.0.1 *change* |
| `NetworkAnimator`: без двойного вызова trigger на host | 21 | v96.0.1 *fix* |
| **Reader / Writer Processor** работает **между сборками** (asmdef) | 05 | v96.0.1 *fix* |
| **Simple Web Transport**: jslib, WebAssembly, меньше аллокаций на сообщение | 01, 18 | v96.0.1 + post-96.0.1 |
| **Encryption Transport**: лог HW acceleration, реализует **Port Transport**; Bouncy Castle в папке транспорта | 01, 29 | v96.0.1 |
| **Network Manager**: кнопка **Clear** у списка spawnable prefabs (редактор) | 02 | v96.0.1 |
| **NetworkServer** проверяет `listen`, отклоняет если не слушает | 01, 14 | v96.0.1 *fix* |
| **NetworkManager.StopClient** больше не зовёт `OnClientDisconnectInternal` (зовёт транспорт) | 15 | v96.0.1 *fix* |
| **NetworkServer** корректно вызывает `NetworkClient.InvokeUnSpawnHandler` из `UnSpawnInternal` | 02 | v96.0.1 *fix* |
| **Netgraph** улучшения | 13 | v96.0.1 |
| **Edgegap Plugin** в примерах (версия обновлялась, напр. 2.3.1) | 14, 25 | v96.0.1 |
| Примеры: **Pooling** в Room / Multiple Additive Scene; портал ждёт `RemovePlayerForConnection` | 27, 08 | v96.0.1 |
| **PredictionUtils**: `freezeRotation` перед constraints | 10 | v96.0.1 *fix* |
| Режим Play в **Prefab Mode** с сетью | — | v96.0.1 *fix*; см. документацию Unity + Mirror |
| **KCP**: `OnClientError` при shutdown без null-крашей | 15 | v96.0.1 *fix* |
| **NetworkIdentity**: проверка **unreliableBaseline** при baseline | 02, 04 | **v96.9.19** |
| **DestroyOwnedObjects** — производительность | 02 | **v96.9.22** |
| **GetStableHashCode16** вместо усечения хеша | 05 | **v96.9.21** |
| WebGL: не заливать ошибками unreliable EntityStateMessages | 18 | **v96.9.23** |

---

## Урок 10 (Lag Compensation)

Модули **`LagCompensation` / история позиций** зависят от ветки и namespace в пакете. Не копируйте код из сторонних блогов: возьмите **пример из репозитория Mirror** на **том же теге**, что в `Packages/manifest.json`.

---

## Как поддерживать этот файл в актуальном виде (2026)

1. Раз в месяц открыть [GitHub Releases](https://github.com/MirrorNetworking/Mirror/releases) и просмотреть **Features / Fixes** для вашей линии `96.x` (в **2026** основной поток правок — там).
2. Новую строку добавлять в таблицу: **тема → урок → краткий комментарий**.
3. Если API переименован — обновить соответствующий `.md` урока, а не только changelog.

# Урок 2: NetworkIdentity, netId и спавн объектов

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · базовый трек · урок 2/15 · Mirror `96.x`

| Ключевые слова | `NetworkIdentity`, `netId`, `assetId`, `sceneId`, `NetworkServer.Spawn` |
|----------------|---------------------------------------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | Dynamic network prefab: projectile, pickup или test cube. |
| Кто владеет state | Server создаёт и уничтожает spawned instance. |
| Как проверить | Host + Client видят один и тот же spawned object с ненулевым `netId`. |
| Артефакт | Prefab в spawnable list и лог `spawn prefab netId`. |

---

## Что должно получиться

Вы умеете:

- отличать prefab asset от spawned instance;
- понимать, почему у prefab `netId == 0`;
- спавнить динамический объект только с сервера;
- уничтожать сетевой объект через Mirror;
- находить типовые ошибки `NetworkIdentity`.

---

## Проблема

Если объект не является сетевым spawned-объектом, Mirror не сможет доставить его SyncVar, Command и Rpc как ожидается. Большая часть ошибок "SyncVar не работает" на самом деле начинается здесь.

---

## Теория коротко

`NetworkIdentity` - паспорт сетевого объекта.

| ID | Где используется |
|----|------------------|
| `assetId` | Связь dynamic prefab с asset в проекте. |
| `sceneId` | Сетевые объекты, заранее лежащие в сцене. |
| `netId` | Runtime ID конкретного spawned instance. |

У prefab asset в Project `netId` обычно 0. Это нормально. У объекта в активном матче после spawn `netId` должен быть ненулевым.

Mirror не поддерживает nested `NetworkIdentity`: не ставьте identity на child objects внутри одного сетевого prefab.

---

## Жизненный цикл

Для сетевой инициализации используйте callbacks `NetworkBehaviour`:

| Callback | Когда применять |
|----------|-----------------|
| `OnStartServer` | Инициализация серверного state. |
| `OnStartClient` | Клиент получил spawned object и начальные данные. |
| `OnStartLocalPlayer` | Настроить ввод, камеру, локальный HUD. |
| `OnStopServer` / `OnStopClient` | Отписки, cleanup, логи. |

`Start()` можно использовать для обычной Unity-инициализации, но не завязывайте на него сетевой порядок.

---

## Практика: серверный спавн

Prefab снаряда:

1. Корень prefab: `NetworkIdentity`.
2. Скрипт снаряда наследуется от `NetworkBehaviour`.
3. Prefab добавлен в `NetworkManager` как spawnable prefab.

Сервер создаёт объект:

```csharp
using Mirror;
using UnityEngine;

public sealed class ProjectileSpawner : NetworkBehaviour
{
    [SerializeField] GameObject projectilePrefab;

    [Server]
    public void SpawnProjectile(Vector3 position, Vector3 direction)
    {
        GameObject instance = Instantiate(
            projectilePrefab,
            position,
            Quaternion.LookRotation(direction));

        NetworkServer.Spawn(instance);
    }
}
```

Уничтожение:

```csharp
[Server]
public void DestroyProjectile(GameObject projectile)
{
    NetworkServer.Destroy(projectile);
}
```

---

## Проверка себя

- Сервер спавнит объект, оба клиента его видят.
- В инспекторе runtime-instance `netId != 0`.
- При `NetworkServer.Destroy` объект исчезает у всех.
- Если убрать prefab из spawnable list, ошибка воспроизводится и понятна.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| Объект виден только серверу | Использован ли `NetworkServer.Spawn(instance)` после `Instantiate`? |
| Ошибка spawn prefab | Prefab зарегистрирован в `NetworkManager` или через `NetworkClient.RegisterPrefab`. |
| `netId == 0` в матче | Вы смотрите prefab asset или объект не spawned. |
| RPC/SyncVar не работает | Скрипт на объекте с `NetworkIdentity` и объект уже spawned. |

---

## Частые ошибки

- `NetworkIdentity` висит на child, а не на root.
- На одном prefab несколько `NetworkIdentity`.
- Клиент вызывает `Instantiate` и ждёт, что объект появится у всех.
- Используется `Destroy(go)` вместо `NetworkServer.Destroy(go)`.
- Ссылки на другие сетевые объекты ожидаются слишком рано при spawn.

---

## Лайфхаки

- Для ссылок между сетевыми объектами безопаснее хранить `uint netId` и резолвить через `NetworkClient.spawned`, если порядок spawn важен.
- Логируйте `netId`, `assetId` и имя prefab при сложных десинхронах.
- Динамические сетевые prefab держите в отдельной папке `NetworkPrefabs`.
- Для часто создаваемых объектов позже изучите pooling, но сначала добейтесь корректного spawn/destroy.

---

## Профессиональный минимум

- Клиент не делает сетевой `Instantiate`.
- Все dynamic network prefabs лежат в понятной папке и зарегистрированы.
- У каждого сетевого prefab один `NetworkIdentity` на root.
- Destroy сетевого объекта идёт через `NetworkServer.Destroy`.

---

## Домашнее задание

Сделайте кнопку "Fire" у локального игрока:

1. Клиент нажимает кнопку.
2. Игрок вызывает `Command`.
3. Сервер проверяет cooldown.
4. Сервер спавнит projectile.
5. Оба клиента видят projectile.

В заметках укажите, где находится `NetworkIdentity` и где объект получает `netId`.

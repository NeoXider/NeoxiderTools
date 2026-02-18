# Despawner

Компонент для «удаления» игрового объекта: возврат в пул (если объект из пула) или `Destroy`. Можно вызывать по выключению, из кода или кнопкой в Inspector.

## Поведение

- **Despawn** — объект возвращается в пул (`PooledObjectInfo.OwnerPool`), если он из пула; иначе вызывается `Destroy(gameObject)`.
- **Despawn On Disable** — при выключении компонента или GameObject вызывается Despawn (опционально).
- **Spawn On Despawn** — перед деспавном можно заспавнить префаб на позиции/ротации этого объекта (или в мире); используется пул через `SpawnFromPool`, если доступен `PoolManager`.
- **On Despawn** — UnityEvent вызывается перед возвратом в пул / уничтожением (подходит для звуков, эффектов, счётчиков).

## Настройка

1. Добавьте **Despawner** на объект.
2. Включите **Despawn On Disable**, если объект должен «удаляться» при выключении (например, при смене сцены или отключении родителя).
3. При необходимости укажите **Spawn Prefab On Despawn** (эффект, дроп и т.п.) и **Spawn Parent** / **Spawn At This Transform**.
4. Подпишите **On Despawn** на нужные реакции (звук, партиклы, логика).
5. Вызов из кода: `GetComponent<Despawner>().Despawn()` или из другого объекта/события.
6. В Inspector доступна кнопка **Despawn** (атрибут `[Button]`) для ручной проверки.

## API

| Метод / свойство | Описание |
|------------------|----------|
| `Despawn()` | Деспавнит этот объект (пул или Destroy). Опционально спавнит префаб, вызывает OnDespawn. |
| `DespawnOther(GameObject target)` | Деспавнит другой объект (удобно из UnityEvent с одним аргументом). |
| `DespawnObject(GameObject target)` | Статический метод: деспавнит любой объект (если есть Despawner — вызывает его Despawn, иначе пул/Destroy). |
| `OnDespawn` | UnityEvent, вызывается перед возвратом в пул или Destroy. |

## Примеры

- Временный объект (снаряд, эффект): включён **Despawn On Disable**, при отключении возврат в пул или уничтожение.
- Дроп при «смерти»: в **Spawn Prefab On Despawn** — префаб дропа/эффекта, **Spawn At This Transform** = true.
- Кнопка «Удалить» в UI: на кнопку вешается UnityEvent → `Despawner.DespawnOther` с целевым объектом (или у целевого объекта вызывается `Despawn()`).
- Из кода по условию: `if (hp <= 0) GetComponent<Despawner>().Despawn();`

## Зависимости

- **PooledObjectInfo** / **PoolManager** — для возврата в пул. Если объект не из пула, выполняется `Destroy`.
- **PoolExtensions.SpawnFromPool** — для опционального спавна префаба при деспавне (если есть PoolManager, иначе `Instantiate`).

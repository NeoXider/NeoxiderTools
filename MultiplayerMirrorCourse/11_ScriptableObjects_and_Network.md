# Урок 11: ScriptableObject, каталоги и ID по сети

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · базовый трек · урок 11/15 · Mirror `96.x`

| Ключевые слова | ScriptableObject, catalog, itemId, version, data contract |
|----------------|-----------------------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | Item/skill/class catalog и stable IDs. |
| Кто владеет state | Server выбирает ID; client локально читает presentation data. |
| Как проверить | Client/server builds резолвят один ID в один и тот же catalog entry. |
| Артефакт | Catalog с ID и проверка отсутствия duplicate/missing ids. |

---

## Что должно получиться

Вы перестаёте отправлять по сети "предмет целиком" и начинаете отправлять стабильные ID. Клиент и сервер читают детали из локального каталога одной версии.

---

## Проблема

ScriptableObject удобен в Unity, но в сети он часто превращается в лишний payload и версионную ловушку. Если у клиента другой каталог, один и тот же ID может означать другой предмет.

---

## Правильная модель

| По сети | Локально |
|---------|----------|
| `itemId` | Название, иконка, описание. |
| `classId` | Stats, prefab, visual setup. |
| `abilityId` | Cooldown, текст, VFX. |
| `catalogVersion` | Проверка совместимости. |

Секретные данные экономики не должны жить только в клиентском ScriptableObject. Клиентский build читается.

---

## Практика

```csharp
using Mirror;

public sealed class PlayerClassState : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnClassChanged))]
    int classId;

    [Server]
    public void ServerSetClass(int newClassId)
    {
        if (!ClassCatalog.Exists(newClassId)) return;
        classId = newClassId;
    }

    void OnClassChanged(int oldValue, int newValue)
    {
        ClassDefinition definition = ClassCatalog.Get(newValue);
        // UI берёт имя/иконку локально по ID.
    }
}
```

---

## Проверка себя

- По сети идёт `int`/`ushort`/`string id`, а не весь SO.
- При перестановке asset в Project ID не меняется.
- Клиент с несовместимым `catalogVersion` получает отказ до матча.
- Сервер проверяет ID по своему каталогу.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| Client показывает другой предмет | Catalog order/ID отличается между builds. |
| Server принимает несуществующий item | Нет validation `Catalog.Exists(id)`. |
| После патча всё сломалось | ID генерировался по index/name без миграции. |
| SO ушёл по сети | Заменить на stable ID и локальный lookup. |

---

## Частые ошибки

- Использовать index массива как ID, а потом менять порядок.
- Хранить цену только на клиенте.
- Отправлять prefab reference вместо ID.
- Не проверять версию каталога при подключении.

---

## Лайфхаки

- Делайте ID явным полем в SO, не зависящим от имени файла.
- Для баланса держите server-side источник правды.
- `catalogVersion` можно включить в auth/handshake.
- Для моддинга заранее решайте, кто имеет право определять каталог.

---

## Профессиональный минимум

- ID стабильны, уникальны и проверяются в editor/build step.
- Balance data, влияющая на честность, доступна server build.
- Client presentation может отличаться, но gameplay math совпадает с server.
- Каталог имеет version/hash для проверки несовместимых клиентов.

---

## Для RPG/Progression

Ability, item, perk и unlock node передаются по сети как stable ID. Клиент может показывать иконку и локализованное имя из своего каталога, но стоимость, доступность, reward и итоговый stat change проверяет server-side catalog. Эта связка используется в [31_RPG_Combat_Server_Authority.md](31_RPG_Combat_Server_Authority.md) и [32_Progression_XP_Rewards.md](32_Progression_XP_Rewards.md).

---

## Домашнее задание

Сделайте каталог классов:

1. У каждого класса есть стабильный `classId`.
2. По сети синхронизируется только `classId`.
3. UI показывает имя и иконку из локального каталога.
4. В заметках описано, как проверяется `catalogVersion`.

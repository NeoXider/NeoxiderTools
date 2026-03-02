# Атрибут SaveField

**Что это:** атрибут для пометки полей, сохраняемых SaveManager. Ключ (key) задаётся в конструкторе; опционально autoSaveOnQuit, autoLoadOnAwake. Пространство имён: `Neo.Save`. Файл: `Scripts/Save/SaveField.cs`.

**Как использовать:** в классе-наследнике SaveableBehaviour пометить поле атрибутом `[SaveField("уникальный_ключ")]`. Ключ должен быть уникален в пределах компонента. Сохранение/загрузка выполняются SaveManager.

---

## Описание атрибута

### SaveField
- **Пространство имен**: `Neo.Save`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Save/SaveField.cs`

**Описание**
Атрибут, используемый для пометки полей, которые должны быть сохранены `SaveManager`.

**Конструктор**
- `public SaveField(string key, bool autoSaveOnQuit = true, bool autoLoadOnAwake = true)`
  - `key`: **Обязательный** параметр. Уникальный строковый ключ, по которому значение поля будет сохранено. Должен быть уникальным в пределах одного компонента.
  - `autoSaveOnQuit`: (По умолчанию `true`) Если `true`, поле будет автоматически сохранено при выходе из приложения.
  - `autoLoadOnAwake`: (По умолчанию `true`) Если `true`, поле будет автоматически загружено при запуске приложения.

**Пример использования**
```csharp
public class PlayerStats : SaveableBehaviour
{
    [SaveField("player_health")]
    private int health = 100;

    [SaveField("player_name")]
    public string playerName = "Hero";
}
```

# GlobalData

**Что это:** сериализуемый класс-контейнер глобальных данных (валюта, уровни, настройки). По умолчанию пустой — поля добавляются вручную. Сохраняется через GlobalSave. Пространство имён: `Neo.Save`. Файл: `Scripts/Save/GlobalSave/GlobalData.cs`.

**Как использовать:** открыть GlobalData.cs и добавить нужные поля (public или [SerializeField]). Доступ из кода: `GlobalSave.data.имяПоля`. Для сериализации поля должны быть сериализуемыми (примитивы, [Serializable] типы).

---

## Описание класса

### GlobalData
- **Пространство имен**: `Neo.Save`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Save/GlobalSave/GlobalData.cs`

**Описание**
Класс, предназначенный для расширения и хранения глобально доступных сохраняемых данных.

**Пример использования**
Разработчик должен открыть файл `GlobalData.cs` и добавить в него необходимые поля:

```csharp
using System;

namespace Neo.Save
{
    [Serializable]
    public class GlobalData
    {
        public int coins = 0;
        public int lastCompletedLevel = -1;
        public bool isMusicEnabled = true;
    }
}
```

После этого доступ к этим полям можно будет получить из любого места в коде через статический класс `GlobalSave.data`.

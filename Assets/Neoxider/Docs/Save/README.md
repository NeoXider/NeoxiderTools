# Модуль Save

**Что это:** модуль сохранения данных в Unity. Три варианта: (1) провайдеры — статический API как PlayerPrefs с переключением бэкенда; (2) компоненты — сохранение полей MonoBehaviour через `SaveableBehaviour` и `[SaveField]`; (3) глобальные данные — `GlobalSave` для валюты и прогресса без привязки к объектам сцены.

**Как с ним работать:**
- Нужен простой ключ–значение: использовать [SaveProvider](./SaveProvider.md) или [SaveProviderSettingsComponent](./SaveProviderSettingsComponent.md) на сцене.
- Нужно сохранять состояние объектов сцены: наследовать компонент от [SaveableBehaviour](./SaveableBehaviour.md), пометить поля [SaveField](./SaveField.md), на сцене добавить [SaveManager](./SaveManager.md).
- Нужно хранить глобальные данные (валюта, флаги): использовать [GlobalSave](./GlobalSave.md) и [GlobalData](./GlobalData.md).

**Навигация:** [← К Docs](../README.md) · оглавление — раздел «Документация по скриптам» ниже

---

## Системы сохранения

1.  **Система провайдеров (SaveProvider)**: Универсальная система сохранения с поддержкой различных провайдеров (PlayerPrefs, файлы). Предоставляет статический API, аналогичный PlayerPrefs, с возможностью переключения между провайдерами.
2.  **Система на основе компонентов (SaveManager)**: Позволяет сохранять состояние отдельных `MonoBehaviour` на сцене. Вы просто наследуете свой компонент от `SaveableBehaviour` и помечаете поля атрибутом `[SaveField]`.
3.  **Глобальное хранилище (GlobalSave)**: Позволяет хранить общие данные (валюта, прогресс и т.д.), не привязанные к объектам на сцене, через статический класс `GlobalSave`.

Все системы интегрированы и используют единую систему провайдеров для гибкости и расширяемости.

## Документация по скриптам

### Система провайдеров (новая)
- [**SaveProvider**](./SaveProvider.md): Статический класс с API как PlayerPrefs для работы с провайдерами сохранения.
- [**ISaveProvider**](./ISaveProvider.md): Интерфейс для всех провайдеров сохранения.
- [**SaveProviderSettings**](./SaveProviderSettings.md): ScriptableObject для настройки провайдеров.
- [**SaveProviderSettingsComponent**](./SaveProviderSettingsComponent.md): MonoBehaviour для инициализации провайдера из настроек в Inspector.

### Система сохранения компонентов
- [**ISaveableComponent**](./ISaveableComponent.md): Интерфейс, который должны реализовывать все сохраняемые компоненты.
- [**SaveableBehaviour**](./SaveableBehaviour.md): Базовый класс для быстрой реализации сохраняемых компонентов.
- [**SaveField (Атрибут)**](./SaveField.md): Атрибут для пометки полей, которые нужно сохранить.
- [**SaveManager**](./SaveManager.md): Ядро системы, управляющее процессом сохранения и загрузки.
- [**PlayerData**](./PlayerData.md): Пример сохраняемого компонента (ISaveableComponent + SaveField).

### Глобальное хранилище
- [**GlobalData**](./GlobalData.md): Класс-контейнер для ваших глобальных данных.
- [**GlobalSave**](./GlobalSave.md): Статический класс для доступа и управления глобальными данными.

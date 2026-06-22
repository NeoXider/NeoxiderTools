# Getting Started — NeoxiderTools

**Назначение:** от установки пакета до первой рабочей сцены за ~5 минут.

**Навигация:** [← К Docs](./README.md) · [English version](../DocsEn/GettingStarted.md)

---

## Требования

| Требование | Версия |
|------------|--------|
| Unity | **2022.1** или новее |
| TextMeshPro | 3.0.6+ (входит в UPM-зависимости) |
| AI Navigation | 1.1.7+ (входит в UPM-зависимости) |
| Input System | 1.14.2+ (входит в UPM-зависимости) |
| UniTask | **обязательно** — установите вручную (см. ниже) |
| DOTween | нужен для модулей `Cards`, `UI`, `Tools/View`, `Tools/Text`; установите при необходимости |

> UniTask и DOTween не входят в `package.json` как UPM-зависимости и устанавливаются в host-проект отдельно.

---

## Установка

### Вариант A — UPM через Git URL (рекомендуется)

1. Откройте **Window → Package Manager**.
2. Нажмите **+** → **Add package from git URL…**
3. Вставьте:
   ```
   https://github.com/NeoXider/NeoxiderTools.git?path=Assets/Neoxider
   ```
4. Нажмите **Add** и дождитесь завершения импорта.

### Вариант B — локальный путь (для разработки)

1. Скопируйте папку `Assets/Neoxider` в проект или укажите её как local package в `manifest.json`:
   ```json
   "com.neoxider.tools": "file:../path/to/Assets/Neoxider"
   ```

### Установка UniTask (обязательно)

Откройте `Packages/manifest.json` и добавьте в `dependencies`:
```json
"com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
```
или скачайте `.unitypackage` с [releases UniTask](https://github.com/Cysharp/UniTask/releases).

---

## Импорт сэмплов

После установки пакета сэмплы не копируются автоматически. Чтобы их импортировать:

1. Откройте **Window → Package Manager**, найдите **NeoxiderTools**.
2. Перейдите на вкладку **Samples**.
3. Нажмите **Import** рядом с нужным сэмплом:

| Сэмпл | Что содержит |
|-------|--------------|
| **Demo Scenes** | Сцены для `AM`, `GridSystem`, `Shop`, `NoCode`, `Condition`, `StateMachine` и других модулей |
| **NeoxiderPages** | Опциональный модуль навигации по UI-страницам (`PM`, `UIPage`, `BtnChangePage`) |

После импорта файлы появятся в `Assets/Samples/NeoxiderTools/<version>/`.

---

## Первая сцена за 5 минут: Audio Manager (AM)

Самый быстрый способ убедиться, что пакет работает — добавить центральный аудио-менеджер **AM** и воспроизвести звук. AM — это синглтон без дополнительных зависимостей.

### Шаг 1 — Создайте GameObject с компонентом AM

В Hierarchy щёлкните правой кнопкой → **Neoxider → Audio → AM** (или через меню **GameObject → Neoxider → Audio → AM**).

Это создаст объект `AM` в сцене с уже подключённым компонентом. AudioSource для эффектов и музыки создаются автоматически.

### Шаг 2 — Добавьте звуковой клип

1. Выберите объект `AM` в Hierarchy.
2. В Inspector найдите массив **Sounds**.
3. Увеличьте размер на 1 (нажмите **+**).
4. Перетащите любой `AudioClip` из ваших ассетов в поле элемента `Element 0 → Clip`.

### Шаг 3 — Воспроизведите звук из кода

Создайте MonoBehaviour и вызовите `AM.I.Play(0)`:

```csharp
using Neo.Audio;
using UnityEngine;

public class SoundTest : MonoBehaviour
{
    private void Start()
    {
        // Воспроизвести звук по индексу 0 из массива Sounds
        AM.I.Play(0);
    }

    private void Update()
    {
        // Воспроизвести звук по нажатию Space
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AM.I.Play(0);
        }
    }
}
```

Добавьте этот скрипт на любой GameObject в сцене.

### Шаг 4 — Воспроизведите звук без кода (No-Code)

Если писать скрипт не нужно, используйте компонент **PlayAudio**:

1. Выберите любой GameObject.
2. **Add Component → Neoxider → Audio → PlayAudio**.
3. Перетащите `AudioClip` в массив **Clips**.
4. Включите галочку **Play On Awake**, чтобы звук играл при старте.

Или привяжите метод `AudioPlay()` компонента `PlayAudio` к любому `UnityEvent` в Inspector — без единой строки кода.

### Шаг 5 — Нажмите Play

Запустите сцену. Звук воспроизведётся автоматически (или при нажатии Space, если использовали `SoundTest`).

> **Совет:** в Inspector компонента AM можно нажать кнопки **Play(int id)** прямо в Edit-режиме — это удобно для проверки клипов без запуска сцены (кнопки появляются благодаря атрибуту `[Button]`).

---

## Дальнейшие шаги

| Что изучить | Документ |
|-------------|----------|
| Полный API аудио-менеджера | [Audio/AM](./Audio/AM.md) |
| Настройки громкости и мьюта | [Audio/AMSettings](./Audio/AMSettings.md) |
| Кнопки с звуком | [Audio/PlayAudioBtn](./Audio/PlayAudioBtn.md) |
| No-code условия и события | [Condition](./Condition/README.md) |
| Сохранение данных | [Save](./Save/README.md) |
| Сеточные игры (Dice, Match3, 2048) | [GridSystem](./GridSystem/README.md) |
| Магазин и валюта | [Shop](./Shop/README.md) |
| Движение и инпут | [Tools](./Tools/README.md) |
| RPG-персонажи и бой | [Rpg](./Rpg/README.md) |
| Готовые сцены с примерами | [Sample-сцены](./Samples.md) |
| Совместимость пакета | [PackageCompatibility](./PackageCompatibility.md) |
| Полный индекс модулей | [README](./README.md) |

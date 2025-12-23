# RandomMusicController

## 1. Введение

`RandomMusicController` — это контроллер для воспроизведения случайной музыки из списка треков без повторов подряд. Он не является `MonoBehaviour` и управляется через `AM`. Это позволяет интегрировать случайную музыку напрямую в аудио-менеджер с взаимным переключением между конкретной и случайной музыкой.

---

## 2. Описание класса

### RandomMusicController
- **Пространство имен**: `Neo.Audio`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Audio/RandomMusicController.cs`

**Описание**
Контроллер для автоматического воспроизведения случайных музыкальных треков из списка с предотвращением повторения одного и того же трека подряд.

**Ключевые особенности**
- **Не MonoBehaviour**: Обычный класс, не требует компонента на GameObject.
- **Автоматическое переключение**: Автоматически переключается на следующий трек после завершения текущего.
- **Без повторов**: Гарантирует, что один и тот же трек не будет проигран дважды подряд (если в списке больше одного трека).
- **События**: Предоставляет события для отслеживания смены треков и остановки.

**Публичные свойства**
- `CurrentTrack` (`AudioClip`): Текущий воспроизводимый трек или `null`.
- `IsPlaying` (`bool`): Возвращает `true`, если музыка воспроизводится.
- `IsPaused` (`bool`): Возвращает `true`, если воспроизведение приостановлено.

**Публичные события**
- `OnTrackChanged` (`Action<AudioClip>`): Вызывается при смене трека.
- `OnStopped` (`Action`): Вызывается при остановке воспроизведения.

**Публичные методы**
- `Initialize(AudioSource audioSource, AudioClip[] tracks)`: Инициализирует контроллер с указанным AudioSource и списком треков. Должен быть вызван перед использованием.
- `Start()`: Начинает воспроизведение случайной музыки из списка треков.
- `Stop()`: Останавливает воспроизведение музыки.
- `Pause()`: Приостанавливает воспроизведение музыки.
- `Resume()`: Возобновляет воспроизведение приостановленной музыки.

---

## 3. Использование

Обычно `RandomMusicController` используется внутри `AM` и не требует прямого создания. Однако если нужно использовать его отдельно:

```csharp
using Neo.Audio;
using UnityEngine;

public class Example : MonoBehaviour
{
    public AudioSource musicSource;
    public AudioClip[] musicTracks;
    
    private RandomMusicController controller;
    
    void Start()
    {
        controller = new RandomMusicController();
        controller.Initialize(musicSource, musicTracks);
        controller.OnTrackChanged += OnTrackChanged;
        controller.Start();
    }
    
    void OnTrackChanged(AudioClip track)
    {
        Debug.Log($"Now playing: {track.name}");
    }
    
    void OnDestroy()
    {
        controller?.Stop();
    }
}
```

---

## 4. Примечания

- Контроллер требует инициализации через `Initialize()` перед использованием.
- Если список треков пуст или равен `null`, воспроизведение не начнется.
- Контроллер использует UniTask для асинхронной работы, поэтому может работать без MonoBehaviour.
- Для управления случайной музыкой рекомендуется использовать методы `AM.EnableRandomMusic()` и `AM.DisableRandomMusic()`.

















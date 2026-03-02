# Модуль Audio

**Что это:** модуль звука: синглтоны AM (воспроизведение эффектов и музыки) и AMSettings (громкость, мьют), компоненты PlayAudio, PlayAudioBtn, AudioControl, RandomMusicController, SettingMixer. Скрипты в `Scripts/Audio/`.

**Навигация:** [← К Docs](../README.md) · оглавление — список ниже

---

## Документация по скриптам

### Корневые скрипты
- [**AMSettings**](./AMSettings.md): Синглтон для управления глобальными настройками звука (громкость, мьют, микшер).
- [**RandomMusicController**](./RandomMusicController.md): Контроллер для случайной музыки (используется внутри AM).
- [**SettingMixer**](./SettingMixer.md): Простая утилита для прямого управления параметром `AudioMixer`.

### Подмодули

- [AudioSimple](#audiosimple)
- [View](#view)

#### AudioSimple
- [**AM (Audio Manager)**](./AM.md): Основной синглтон-плеер для звуковых эффектов и музыки.
- [**PlayAudio**](./PlayAudio.md): Простой компонент-триггер для воспроизведения звука из `AM`.
- [**PlayAudioBtn**](./PlayAudioBtn.md): Компонент для проигрывания звука при нажатии на `UI.Button`.

#### View
- [**AudioControl**](./AudioControl.md): "Умный" UI-компонент для создания ползунков и переключателей для управления звуком.

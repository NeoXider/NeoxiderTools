# Расширения AudioExtensions

## 1. Введение

`AudioExtensions` — это набор методов-расширений для компонента `AudioSource`. Они позволяют легко и плавно управлять громкостью звука, создавая эффекты затухания (fade out) и нарастания (fade in).

---

## 2. Описание методов

### AudioExtensions
- **Пространство имен**: `Neo.Extensions`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Extensions/AudioExtensions.cs`

**Статические методы**
- `FadeTo(this AudioSource source, float targetVolume, float duration)`: Плавно изменяет громкость `AudioSource` до `targetVolume` за указанное время `duration`. Возвращает `CoroutineHandle`, который можно использовать для остановки.
- `FadeOut(this AudioSource source, float duration)`: Упрощенный вызов `FadeTo` для затухания звука до нуля.
- `FadeIn(this AudioSource source, float duration, float targetVolume = 1.0f)`: Упрощенный вызов `FadeTo` для нарастания громкости до указанного значения.

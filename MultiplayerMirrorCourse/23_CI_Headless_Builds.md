# Урок 23: CI и headless-сборки

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · продвинутый трек · урок 8/15 · Mirror `96.x`

| Ключевые слова | CI, Unity batchmode, server build, artifact, secrets |
|----------------|------------------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | CI pipeline, server build method, artifacts. |
| Кто владеет state | CI proves build reproducibility, not gameplay correctness. |
| Как проверить | Clean runner builds server and optionally starts until READY log. |
| Артефакт | `BUILDING.md` plus CI artifact naming rules. |

---

## Что должно получиться

Вы понимаете, как собрать server build в чистой среде и сохранить артефакт без секретов в репозитории.

---

## Проблема

Локальная сборка на ноутбуке не доказывает, что сервер собирается на CI runner, где нет ваших локальных файлов, кэша и случайных настроек.

---

## Минимальный pipeline

```text
checkout
restore/cache Library optional
activate Unity
run tests optional
build Linux server
upload artifact
run smoke start optional
```

CI не доказывает честность матча. Он отвечает на вопрос: "собирается ли проект и стартует ли server build в чистой среде?"

Для Unity Dedicated Server target используйте Build Profiles, script build with `StandaloneBuildSubtarget.Server` или CLI argument `-standaloneBuildSubtarget Server`, в зависимости от версии Unity и выбранного pipeline.

---

## Что документировать

| Поле | Пример |
|------|--------|
| Unity version | Точная версия Editor. |
| Mirror version | `Assets/Mirror/version.txt` или package tag. |
| Build target | Linux Server / Dedicated. |
| Entry method | `CiBuild.BuildServer`. |
| Scenes | Bootstrap + gameplay scenes. |
| Artifact name | `GameServer-linux-x64.zip`. |

---

## Секреты

Не хранить в git:

- Unity license;
- Steam/backend keys;
- Edgegap/cloud tokens;
- SSH private keys;
- production config.

Используйте secrets store CI и ограничение по веткам.

---

## Проверка себя

- CI собирает server build с нуля.
- Server artifact можно скачать.
- Лог сборки содержит Unity/Mirror version.
- Секреты не появляются в yaml и логах.
- `#if UNITY_SERVER` код компилируется.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| CI собирает не тот target | Build profile/subtarget/CLI args. |
| Локально работает, CI нет | Unity version, scenes in build, missing packages, license. |
| Server не стартует после build | Нет bootstrap или READY log; порт/args не переданы. |
| Secret в логах | Masking, env vars, verbose output. |

---

## Частые ошибки

- Разная Unity version локально и в CI.
- Сборка client вместо server.
- Игнор server-only compile errors.
- Секреты в workflow-файле.
- Нет версии артефакта.

---

## Лайфхаки

- Первый pipeline делайте без кэша, чтобы упростить диагностику.
- Кэш включайте после стабильной сборки.
- Добавьте ночной build даже без деплоя.
- Для smoke можно просто запустить server и дождаться `READY`.

---

## Профессиональный минимум

- CI лог содержит Unity version, Mirror version, commit, build target.
- Server artifact versioned and reproducible.
- Server-only code компилируется на каждом relevant PR.
- Secrets передаются через CI store и не печатаются.

---

## Домашнее задание

Создайте `BUILDING.md`:

- точная Unity version;
- команда локальной server build;
- структура CI steps;
- где лежит artifact;
- какие secrets нужны, без значений.

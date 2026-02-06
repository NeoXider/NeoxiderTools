# Git-хуки для NeoxiderTools

## pre-commit: Samples → Samples~

Чтобы в репозитории всегда была папка `Samples~` (для UPM), при коммите нужно переименовывать `Samples` в `Samples~`.

**Установка (один раз после клонирования):**

```bash
# Из корня репозитория
cp scripts/git-hooks/pre-commit-Samples-rename.sh .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit
```

**Windows (Git Bash):** те же команды. В PowerShell можно скопировать файл вручную в `.git\hooks\pre-commit` (без расширения).

**Как это работает:**

- Редактируешь демо/Pages в папке `Samples` (переименовал из `Samples~` вручную).
- При `git commit` хук переименует `Samples` обратно в `Samples~` и добавит изменения в коммит.
- В репозитории всегда хранится `Samples~`; после коммита в рабочей копии снова будет `Samples~`. Чтобы снова редактировать в Unity — переименуй в `Samples`.

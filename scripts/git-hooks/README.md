# Git-хуки для NeoxiderTools

## pre-commit: Samples → Samples~

Сэмплы (Demo, NeoxiderPages) должны лежать в папке `Samples~`: Unity не компилирует её содержимое. Если бы они были в корне пакета или в `Samples`, скрипты компилировались бы в пакете, а при импорте сэмпла копировались в Assets и компилировались снова → ошибки CS0101 (duplicate definition).

**Установка (один раз после клонирования):**

```bash
# Из корня репозитория
cp scripts/git-hooks/pre-commit-Samples-rename.sh .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit
```

**Windows (Git Bash):** те же команды. В PowerShell можно скопировать файл вручную в `.git\hooks\pre-commit` (без расширения).

**Как это работает:**

- Редактируешь сэмплы в папке `Samples` (переименовал из `Samples~` вручную).
- При `git commit` хук переименует `Samples` обратно в `Samples~` и добавит изменения в коммит.

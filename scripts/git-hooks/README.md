# Git-хуки для NeoxiderTools

## pre-commit: только папка Samples (без тильды)

Для совместимости с UPM на Windows в репозитории используется папка `Samples` (без тильды). Папка `Samples~` при клонировании/распаковке может теряться, из-за чего импорт сэмплов в Package Manager выдаёт «path does not exist».

**Установка (один раз после клонирования):**

```bash
# Из корня репозитория
cp scripts/git-hooks/pre-commit-Samples-rename.sh .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit
```

**Windows (Git Bash):** те же команды. В PowerShell можно скопировать файл вручную в `.git\hooks\pre-commit` (без расширения).

**Как это работает:**

- В репозитории всегда должна быть папка `Samples` (с подпапками Demo и NeoxiderPages).
- Если в рабочей копии по ошибке окажется `Samples~`, хук не даст закоммитить и подскажет переименовать в `Samples`.

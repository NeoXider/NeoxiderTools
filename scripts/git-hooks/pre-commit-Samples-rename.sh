#!/bin/sh
# Pre-commit: перед коммитом переименовать Samples -> Samples~,
# чтобы в репозитории всегда хранилась папка Samples~ (для UPM).
ROOT="Assets/Neoxider"
if [ -d "$ROOT/Samples" ] && [ ! -d "$ROOT/Samples~" ]; then
	echo "[pre-commit] Renaming Samples -> Samples~ for UPM..."
	mv "$ROOT/Samples" "$ROOT/Samples~"
	git add -A "$ROOT/"
fi
# Не допускаем коммит с папкой "Samples" (без тильды) — иначе UPM не найдёт Samples~/...
if [ -d "$ROOT/Samples" ]; then
	echo "[pre-commit] ERROR: Folder must be Samples~ for UPM, not Samples. Rename to Samples~ or run hook to fix."
	exit 1
fi
exit 0

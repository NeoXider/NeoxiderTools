#!/bin/sh
# Pre-commit: сэмплы должны быть в Samples~, иначе при импорте дают дубликаты (CS0101).
ROOT="Assets/Neoxider"
if [ -d "$ROOT/Samples" ] && [ -d "$ROOT/Samples~" ]; then
	echo "[pre-commit] ERROR: Удалите одну из папок — в репозитории должна быть только Samples~."
	exit 1
fi
if [ -d "$ROOT/Samples" ] && [ ! -d "$ROOT/Samples~" ]; then
	echo "[pre-commit] Переименование Samples -> Samples~..."
	mv "$ROOT/Samples" "$ROOT/Samples~"
	git add -A "$ROOT/"
fi
exit 0

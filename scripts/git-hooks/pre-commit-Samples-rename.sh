#!/bin/sh
# Pre-commit: в репозитории должна быть папка Samples (без тильды), иначе UPM не находит путь на Windows.
ROOT="Assets/Neoxider"
if [ -d "$ROOT/Samples~" ]; then
	echo "[pre-commit] ERROR: Use folder name Samples (not Samples~) for UPM compatibility. Rename Samples~ to Samples."
	exit 1
fi
exit 0

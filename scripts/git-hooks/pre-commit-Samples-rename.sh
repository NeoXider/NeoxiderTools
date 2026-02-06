#!/bin/sh
# Pre-commit: перед коммитом переименовать Samples -> Samples~,
# чтобы в репозитории всегда хранилась папка Samples~ (для UPM).
ROOT="Assets/Neoxider"
if [ -d "$ROOT/Samples" ] && [ ! -d "$ROOT/Samples~" ]; then
	echo "[pre-commit] Renaming Samples -> Samples~ for UPM..."
	mv "$ROOT/Samples" "$ROOT/Samples~"
	git add -A "$ROOT/"
fi
exit 0

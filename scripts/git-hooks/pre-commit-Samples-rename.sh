#!/bin/sh
# Pre-commit: package samples must live in Samples~. Keeping both Samples and
# Samples~ in the repository can create duplicate scripts after UPM import.
ROOT="Assets/Neoxider"
if [ -d "$ROOT/Samples" ] && [ -d "$ROOT/Samples~" ]; then
	echo "[pre-commit] ERROR: remove one sample folder; repository release layout must keep only Samples~."
	exit 1
fi
if [ -d "$ROOT/Samples" ] && [ ! -d "$ROOT/Samples~" ]; then
	echo "[pre-commit] Renaming Samples -> Samples~..."
	mv "$ROOT/Samples" "$ROOT/Samples~"
	git add -A "$ROOT/"
fi
exit 0

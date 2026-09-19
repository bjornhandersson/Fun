#!/usr/bin/env bash
# One-command start for the fruit fly.
#
#   ./start.sh            build everything, then open the Godot viewer (the gallery of circuits)
#   ./start.sh console    build, then run the headless checks in the terminal (no Godot needed)
#   ./start.sh editor     build, then open the project in the Godot editor
#
# Godot is found from $GODOT, then `godot` on PATH, then the usual macOS app bundles.
# Pass your own binary with:  GODOT=/path/to/godot ./start.sh
set -euo pipefail
cd "$(dirname "$0")"

mode="${1:-viewer}"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "error: 'dotnet' not found. Install the .NET 10 SDK: https://dotnet.microsoft.com/download" >&2
    exit 1
fi

echo "==> Building src/FruitFly.slnx"
dotnet build src/FruitFly.slnx -v quiet -nologo

if [ "$mode" = "console" ]; then
    echo "==> Running headless checks"
    exec dotnet run --project src/FruitFly --no-build
fi

find_godot() {
    if [ -n "${GODOT:-}" ]; then echo "$GODOT"; return; fi
    if command -v godot >/dev/null 2>&1; then command -v godot; return; fi
    for app in /Applications/Godot_mono.app /Applications/Godot.app "$HOME/Applications/Godot_mono.app" "$HOME/Applications/Godot.app"; do
        if [ -x "$app/Contents/MacOS/Godot" ]; then echo "$app/Contents/MacOS/Godot"; return; fi
    done
    for bin in /usr/local/bin/godot-mono /usr/bin/godot-mono; do
        if [ -x "$bin" ]; then echo "$bin"; return; fi
    done
}

godot="$(find_godot || true)"
if [ -z "$godot" ]; then
    cat >&2 <<'MSG'
error: Godot not found.
  Install Godot 4.7 with .NET support ("mono" build) from https://godotengine.org/download
  then either put `godot` on your PATH or run:  GODOT=/path/to/godot ./start.sh
  To skip the viewer entirely:                  ./start.sh console
MSG
    exit 1
fi

case "$mode" in
    viewer) echo "==> Opening viewer with $godot"; exec "$godot" --path src/FruitFly.Godot ;;
    editor) echo "==> Opening editor with $godot"; exec "$godot" --path src/FruitFly.Godot -e ;;
    *)      echo "usage: ./start.sh [viewer|console|editor]" >&2; exit 2 ;;
esac

#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="$ROOT/godot"
LOCAL_GODOT="$ROOT/.tools/godot/Godot_mono.app/Contents/MacOS/Godot"
LOCAL_DOTNET="$ROOT/.tools/dotnet8"
MODE="${1:-run}"

if [[ -n "${GODOT_BIN:-}" ]]; then
  GODOT="$GODOT_BIN"
elif [[ -x "$LOCAL_GODOT" ]]; then
  GODOT="$LOCAL_GODOT"
else
  GODOT="godot-mono"
fi

if [[ -d "$LOCAL_DOTNET" ]]; then
  export DOTNET_ROOT="$LOCAL_DOTNET"
  export PATH="$LOCAL_DOTNET:$PATH"
fi

case "$MODE" in
  check) exec "$GODOT" --headless --build-solutions --path "$PROJECT" --quit-after 3 ;;
  smoke) exec "$GODOT" --headless --path "$PROJECT" -- --smoke-test ;;
  test) exec "$GODOT" --headless --path "$PROJECT" -- --runtime-self-test ;;
  ui-test) exec "$GODOT" --headless --path "$PROJECT" -- --ui-visual-test ;;
  editor) exec "$GODOT" --editor --path "$PROJECT" ;;
  run) exec "$GODOT" --path "$PROJECT" ;;
  *)
    echo "未知 Godot 启动模式：$MODE" >&2
    exit 2
    ;;
esac

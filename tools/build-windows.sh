#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="$ROOT/godot"
OUTPUT_DIR="$ROOT/build/windows"
OUTPUT="$OUTPUT_DIR/ThreeKingdomsSimulator.exe"
LOCAL_GODOT="$ROOT/.tools/godot/Godot_mono.app/Contents/MacOS/Godot"
LOCAL_DOTNET="$ROOT/.tools/dotnet8"

if [[ -n "${GODOT_BIN:-}" ]]; then
  GODOT="$GODOT_BIN"
elif [[ -x "$LOCAL_GODOT" ]]; then
  GODOT="$LOCAL_GODOT"
elif command -v godot-mono >/dev/null 2>&1; then
  GODOT="godot-mono"
elif command -v godot4-mono >/dev/null 2>&1; then
  GODOT="godot4-mono"
elif command -v godot >/dev/null 2>&1; then
  GODOT="godot"
else
  echo "未找到 Godot 4.7 .NET。请安装后设置 GODOT_BIN。" >&2
  exit 1
fi

if [[ -d "$LOCAL_DOTNET" ]]; then
  export DOTNET_ROOT="$LOCAL_DOTNET"
  export PATH="$LOCAL_DOTNET:$PATH"
fi

VERSION="$("$GODOT" --version)"
if [[ "$VERSION" != 4.7.*.mono.* ]]; then
  echo "需要 Godot 4.7 .NET，当前版本：$VERSION" >&2
  exit 1
fi

mkdir -p "$OUTPUT_DIR"

"$GODOT" --headless --path "$PROJECT" --build-solutions --quit-after 3
if ! "$GODOT" --headless --path "$PROJECT" --export-release "Windows Desktop" "$OUTPUT"; then
  echo "Windows 导出失败。请确认已安装 Godot 4.7 Windows x86_64 export template 和 ICU Data。" >&2
  exit 1
fi

if [[ ! -f "$OUTPUT" ]]; then
  echo "Windows 导出失败：未生成 $OUTPUT" >&2
  echo "请确认已为 Godot 4.7 安装 Windows x86_64 export template 和 ICU Data。" >&2
  exit 1
fi

echo "Windows 构建完成：$OUTPUT_DIR"

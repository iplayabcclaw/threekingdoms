#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
STATE="${THREE_KINGDOMS_CLI_STATE:-$ROOT/.playtest/cli-session.json}"
CLI_HOME="$ROOT/.playtest/home"

mkdir -p "$(dirname "$STATE")" "$CLI_HOME"
export HOME="$CLI_HOME"
exec "$ROOT/tools/run-godot.sh" cli --state "$STATE" "$@"

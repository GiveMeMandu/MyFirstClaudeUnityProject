#!/usr/bin/env bash
# unity-cmd.sh — unity-cli v0.3.9 커맨드 라우팅 버그 우회
# Usage: unity-cmd.sh <command> [json-args]
# Examples:
#   unity-cmd.sh console '{"type":"error","lines":20}'
#   unity-cmd.sh exec '{"code":"Time.time"}'
#   unity-cmd.sh menu '{"menu_path":"File/Save Project"}'
#   unity-cmd.sh profiler '{"action":"hierarchy","depth":3}'
#   unity-cmd.sh list

PORT="${UNITY_CLI_PORT:-8090}"
HOST="127.0.0.1"
CMD="${1:?Usage: unity-cmd.sh <command> [json-args]}"
ARGS="${2:-{}}"

if [ "$CMD" = "list" ]; then
  curl -s -X POST "http://$HOST:$PORT/command" \
    -H "Host: $HOST:$PORT" \
    -H "Content-Type: application/json" \
    -d "{\"command\":\"list\"}"
else
  curl -s -X POST "http://$HOST:$PORT/command" \
    -H "Host: $HOST:$PORT" \
    -H "Content-Type: application/json" \
    -d "{\"command\":\"$CMD\",\"args\":$ARGS}"
fi

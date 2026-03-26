#!/usr/bin/env bash
# session-start-tracker.sh — 세션 시작 시 현재 커밋 해시를 기록
# UserPromptSubmit hook으로 등록. 첫 호출에서만 마커 생성.

# stdin에서 session_id 추출
INPUT=$(cat)
SESSION_ID=$(echo "$INPUT" | node -pe "JSON.parse(require('fs').readFileSync('/dev/stdin','utf8')).session_id" 2>/dev/null <<< "$INPUT" || echo "unknown")

if [ "$SESSION_ID" = "unknown" ] || [ -z "$SESSION_ID" ]; then
  exit 0
fi

STATE_DIR="$HOME/.claude/session-state"
mkdir -p "$STATE_DIR"

MARKER="$STATE_DIR/${SESSION_ID}.start"

# 이미 기록된 세션이면 skip
if [ -f "$MARKER" ]; then
  exit 0
fi

# 현재 커밋 해시 기록
git rev-parse HEAD 2>/dev/null > "$MARKER" || echo "none" > "$MARKER"

# 현재 브랜치도 기록
BRANCH=$(git branch --show-current 2>/dev/null || echo "unknown")
echo "$BRANCH" > "$STATE_DIR/${SESSION_ID}.branch"

exit 0

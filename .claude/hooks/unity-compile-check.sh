#!/usr/bin/env bash
# unity-compile-check.sh — Stop 훅
# Claude가 멈출 때 Unity C# 컴파일 에러를 체크한다.
# tsc-check.sh 패턴을 Unity에 맞게 적용.
set -e

UNITY_CLI="/c/Users/wooch/AppData/Local/unity-cli.exe"
SESSION_ID="${session_id:-default}"
CACHE_DIR="$HOME/.claude/unity-cache/$SESSION_ID"

mkdir -p "$CACHE_DIR"

# Unity Editor 연결 확인 (연결 안 되면 조용히 종료)
if ! "$UNITY_CLI" status >/dev/null 2>&1; then
  exit 0
fi

# 컴파일 에러 확인
ERRORS=$("$UNITY_CLI" console --filter error --lines 50 2>/dev/null || echo "")

if [ -z "$ERRORS" ]; then
  # 에러 없음 — 이전 캐시 정리
  rm -f "$CACHE_DIR/last-errors.txt" 2>/dev/null
  exit 0
fi

# 에러 캐시 저장
echo "$ERRORS" > "$CACHE_DIR/last-errors.txt"
echo "unity-cli editor refresh --compile" > "$CACHE_DIR/compile-command.txt"

# stderr로 출력 (사용자에게 보임)
{
  echo ""
  echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
  echo "🚨 Unity C# 컴파일 에러 발견"
  echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
  echo ""
  echo "$ERRORS" | head -15
  ERROR_LINES=$(echo "$ERRORS" | wc -l)
  if [ "$ERROR_LINES" -gt 15 ]; then
    echo "... 외 $((ERROR_LINES - 15))줄"
  fi
  echo ""
  echo "👉 unity-script-fixer 에이전트를 사용하여 에러를 수정하세요"
  echo ""
} >&2

# 캐시 디렉토리 7일 이상 된 것 정리
find "$HOME/.claude/unity-cache" -maxdepth 1 -type d -mtime +7 -exec rm -rf {} \; 2>/dev/null || true

exit 0

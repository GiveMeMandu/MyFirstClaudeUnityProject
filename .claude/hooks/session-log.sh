#!/usr/bin/env bash
# session-log.sh — 세션 종료 시 작업 요약을 Docs/Daily/YYYY-MM-DD.md에 자동 기록
# Stop hook으로 등록

# stdin에서 session_id 추출
INPUT=$(cat)
SESSION_ID=$(echo "$INPUT" | node -pe "JSON.parse(require('fs').readFileSync('/dev/stdin','utf8')).session_id" 2>/dev/null <<< "$INPUT" || echo "unknown")

# ── 기본 정보 수집 ───────────────────────────────────────

DATE=$(date +%Y-%m-%d)
TIME=$(date +%H:%M:%S)
BRANCH=$(git branch --show-current 2>/dev/null || echo "unknown")

# ── 프로젝트 디렉토리 결정 ───────────────────────────────

PROJECT_DIR=$(git rev-parse --show-toplevel 2>/dev/null || echo "")
if [ -z "$PROJECT_DIR" ]; then
  exit 0
fi

LOG_DIR="$PROJECT_DIR/Docs/Daily"
LOG_FILE="$LOG_DIR/${DATE}.md"

mkdir -p "$LOG_DIR"

# ── 세션 중 커밋 추출 ───────────────────────────────────

STATE_DIR="$HOME/.claude/session-state"
START_COMMIT=""
SESSION_BRANCH=""

if [ "$SESSION_ID" != "unknown" ] && [ -f "$STATE_DIR/${SESSION_ID}.start" ]; then
  START_COMMIT=$(cat "$STATE_DIR/${SESSION_ID}.start" 2>/dev/null || echo "")
fi

if [ "$SESSION_ID" != "unknown" ] && [ -f "$STATE_DIR/${SESSION_ID}.branch" ]; then
  SESSION_BRANCH=$(cat "$STATE_DIR/${SESSION_ID}.branch" 2>/dev/null || echo "")
fi

CURRENT_COMMIT=$(git rev-parse HEAD 2>/dev/null || echo "none")

# 세션 중 발생한 커밋 목록
COMMITS=""
DIFF_STAT=""

if [ -n "$START_COMMIT" ] && [ "$START_COMMIT" != "none" ] && [ "$START_COMMIT" != "$CURRENT_COMMIT" ]; then
  COMMITS=$(git log --oneline "${START_COMMIT}..HEAD" 2>/dev/null || echo "")
  DIFF_STAT=$(git diff --stat "${START_COMMIT}..HEAD" 2>/dev/null || echo "")
elif [ "$START_COMMIT" = "$CURRENT_COMMIT" ] || [ "$START_COMMIT" = "none" ]; then
  # 커밋이 없었거나 시작점이 없으면 unstaged 변경사항 확인
  DIFF_STAT=$(git diff --stat 2>/dev/null || echo "")
fi

# ── 파일 생성 (첫 세션) ──────────────────────────────────

if [ ! -f "$LOG_FILE" ]; then
  cat > "$LOG_FILE" <<EOF
---
date: ${DATE}
tags: [daily]
---

# ${DATE} Daily Log

EOF
fi

# ── 엔트리 작성 ──────────────────────────────────────────

{
  echo ""
  echo "---"
  echo ""
  echo "## ${DATE} ${TIME} — [${BRANCH}]"
  echo ""

  if [ -n "$COMMITS" ]; then
    echo "### Commits"
    echo "$COMMITS" | while IFS= read -r line; do
      echo "- ${line}"
    done
    echo ""
  fi

  if [ -n "$DIFF_STAT" ]; then
    echo "### Changed Files"
    echo '```'
    echo "$DIFF_STAT"
    echo '```'
    echo ""
  fi

  if [ -z "$COMMITS" ] && [ -z "$DIFF_STAT" ]; then
    echo "*이 세션에서 코드 변경 없음*"
    echo ""
  fi
} >> "$LOG_FILE"

# ── 세션 로그 자동 커밋 + 푸시 ────────────────────────────

cd "$PROJECT_DIR" 2>/dev/null || true
if [ -f "$LOG_FILE" ]; then
  git add "$LOG_FILE" 2>/dev/null || true
  if ! git diff --cached --quiet 2>/dev/null; then
    git commit -m "docs(daily): 세션 로그 자동 기록 ${DATE}" 2>/dev/null || true
    git push 2>/dev/null || true
  fi
fi

# ── 오래된 세션 상태 정리 (7일+) ─────────────────────────

if [ -d "$STATE_DIR" ]; then
  find "$STATE_DIR" -name "*.start" -mtime +7 -delete 2>/dev/null || true
  find "$STATE_DIR" -name "*.branch" -mtime +7 -delete 2>/dev/null || true
fi

exit 0

#!/usr/bin/env bash
set -euo pipefail

# task-commit.sh — Unity 컴파일 체크 + 커밋 + 푸시
# Usage: task-commit.sh <commit-type> <message...>
# Example: task-commit.sh feat 플레이어 대쉬 기능 추가

UNITY_CLI="/c/Users/wooch/AppData/Local/unity-cli.exe"
VALID_COMMIT_TYPES="feat fix chore docs refactor test"
BRANCH_PATTERN='^(feature|system|hotfix)/'

# ── 인자 검증 ──────────────────────────────────────────────

if [ $# -lt 2 ]; then
  echo "사용법: task-commit.sh <commit-type> <message...>"
  echo "  commit-type: feat | fix | chore | docs | refactor | test"
  echo "  message:     커밋 메시지 (한글 가능)"
  echo ""
  echo "예시: task-commit.sh feat 플레이어 대쉬 기능 추가"
  exit 1
fi

COMMIT_TYPE="$1"
shift
MESSAGE="$*"

# commit-type 검증
if ! echo "$VALID_COMMIT_TYPES" | grep -qw "$COMMIT_TYPE"; then
  echo "오류: 유효하지 않은 커밋 타입 '$COMMIT_TYPE'"
  echo "  허용: $VALID_COMMIT_TYPES"
  exit 1
fi

# ── 브랜치 검증 ────────────────────────────────────────────

BRANCH=$(git branch --show-current)

if ! [[ "$BRANCH" =~ $BRANCH_PATTERN ]]; then
  echo "오류: 태스크 브랜치가 아닙니다."
  echo "  현재 브랜치: $BRANCH"
  echo "  허용 패턴: feature/*, system/*, hotfix/*"
  echo ""
  echo "  /task-start 로 먼저 태스크 브랜치를 생성하세요."
  exit 1
fi

# ── Unity 컴파일 체크 ──────────────────────────────────────

UNITY_CHECK="skip"

if "$UNITY_CLI" status >/dev/null 2>&1; then
  echo "Unity 컴파일 체크 중..."
  "$UNITY_CLI" editor refresh --compile 2>/dev/null || true

  ERRORS=$("$UNITY_CLI" console --filter error --lines 50 2>/dev/null || echo "")

  if [ -n "$ERRORS" ] && [ "$ERRORS" != "[]" ]; then
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "  Unity C# 컴파일 에러 발견 — 커밋 중단"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    echo "$ERRORS" | head -20
    TOTAL=$(echo "$ERRORS" | wc -l)
    if [ "$TOTAL" -gt 20 ]; then
      echo "... 외 $((TOTAL - 20))줄"
    fi
    echo ""
    echo "unity-script-fixer 에이전트를 사용하여 에러를 수정하세요."
    exit 1
  fi
  UNITY_CHECK="pass"
  echo "Unity 컴파일 체크 통과"
else
  echo "경고: Unity Editor가 연결되지 않아 컴파일 체크를 건너뜁니다."
fi

# ── 스테이징 ───────────────────────────────────────────────

git add -A

# staged 변경 확인
if git diff --cached --quiet; then
  echo "커밋할 변경사항이 없습니다."
  exit 0
fi

# ── 커밋 + 푸시 ───────────────────────────────────────────

COMMIT_MSG="${COMMIT_TYPE}: ${MESSAGE}"

git commit -m "$COMMIT_MSG"
git push

COMMIT_HASH=$(git rev-parse --short HEAD)

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  커밋 완료"
echo "  Branch: $BRANCH"
echo "  Commit: $COMMIT_HASH — $COMMIT_MSG"
echo "  Unity:  $UNITY_CHECK"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

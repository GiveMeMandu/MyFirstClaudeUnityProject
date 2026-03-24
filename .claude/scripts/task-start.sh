#!/usr/bin/env bash
set -euo pipefail

# task-start.sh — GitFlow 브랜치 생성
# Usage: task-start.sh <type> <task-id> <slug>
# Example: task-start.sh feature TASK-001 day-night-cycle

VALID_TYPES="feature system hotfix"
TASK_ID_PATTERN='^TASK-[0-9]+$'
SLUG_PATTERN='^[a-z0-9]([a-z0-9-]*[a-z0-9])?$'

# ── 인자 검증 ──────────────────────────────────────────────

if [ $# -lt 3 ]; then
  echo "사용법: task-start.sh <type> <task-id> <slug>"
  echo "  type:    feature | system | hotfix"
  echo "  task-id: TASK-NNN (예: TASK-001)"
  echo "  slug:    영문 kebab-case (예: day-night-cycle)"
  echo ""
  echo "예시: task-start.sh feature TASK-001 day-night-cycle"
  exit 1
fi

TYPE="$1"
TASK_ID=$(echo "$2" | tr '[:lower:]' '[:upper:]')
SLUG="$3"

# type 검증
if ! echo "$VALID_TYPES" | grep -qw "$TYPE"; then
  echo "오류: 유효하지 않은 타입 '$TYPE'"
  echo "  허용: $VALID_TYPES"
  exit 1
fi

# task-id 검증
if ! [[ "$TASK_ID" =~ $TASK_ID_PATTERN ]]; then
  echo "오류: 유효하지 않은 태스크 ID '$TASK_ID'"
  echo "  형식: TASK-NNN (예: TASK-001)"
  exit 1
fi

# slug 검증
if ! [[ "$SLUG" =~ $SLUG_PATTERN ]]; then
  echo "오류: 유효하지 않은 slug '$SLUG'"
  echo "  영문 소문자, 숫자, 하이픈만 허용 (kebab-case)"
  exit 1
fi

# ── 브랜치명 조합 ──────────────────────────────────────────

BRANCH="${TYPE}/${TASK_ID}-${SLUG}"

# ── 이미 존재하는 브랜치 확인 ──────────────────────────────

if git show-ref --verify --quiet "refs/heads/$BRANCH" 2>/dev/null; then
  echo "브랜치 '$BRANCH'가 이미 존재합니다. checkout합니다."
  git checkout "$BRANCH"
  exit 0
fi

if git ls-remote --heads origin "$BRANCH" 2>/dev/null | grep -q "$BRANCH"; then
  echo "원격 브랜치 '$BRANCH'가 존재합니다. checkout합니다."
  git fetch origin "$BRANCH"
  git checkout -b "$BRANCH" "origin/$BRANCH"
  exit 0
fi

# ── base 브랜치 결정 ───────────────────────────────────────

case "$TYPE" in
  feature|system)
    BASE="develop"
    ;;
  hotfix)
    BASE="main"
    ;;
esac

# ── develop 브랜치 자동 생성 (첫 사용 시) ─────────────────

if [ "$BASE" = "develop" ]; then
  if ! git show-ref --verify --quiet refs/heads/develop 2>/dev/null; then
    if git ls-remote --heads origin develop 2>/dev/null | grep -q develop; then
      echo "원격 develop 브랜치를 가져옵니다..."
      git fetch origin develop
      git checkout -b develop origin/develop
    else
      echo "develop 브랜치를 main에서 생성합니다..."
      git checkout main
      git pull origin main
      git checkout -b develop
      git push -u origin develop
      echo "develop 브랜치 생성 + push 완료"
    fi
  fi
fi

# ── base 브랜치 최신화 ─────────────────────────────────────

echo "$BASE 브랜치를 최신화합니다..."
git checkout "$BASE"
git pull origin "$BASE"

# ── 브랜치 생성 ────────────────────────────────────────────

echo "브랜치 '$BRANCH'를 '$BASE'에서 생성합니다..."
git checkout -b "$BRANCH"
git push -u origin "$BRANCH"

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  브랜치 생성 완료"
echo "  Branch: $BRANCH"
echo "  Base:   $BASE"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

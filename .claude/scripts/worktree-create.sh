#!/usr/bin/env bash
set -euo pipefail

# worktree-create.sh — 시스템별 독립 worktree 생성
# Usage: worktree-create.sh <system-slug> [base-branch]
# Example: worktree-create.sh exploration-system develop

SLUG_PATTERN='^[a-z0-9]([a-z0-9-]*[a-z0-9])?$'

if [ $# -lt 1 ]; then
  echo "사용법: worktree-create.sh <system-slug> [base-branch]"
  echo "  system-slug: 영문 kebab-case (예: exploration-system)"
  echo "  base-branch: 분기 기준 (기본: develop)"
  echo ""
  echo "예시: worktree-create.sh exploration-system"
  exit 1
fi

SLUG="$1"
BASE="${2:-develop}"

# slug 검증
if ! [[ "$SLUG" =~ $SLUG_PATTERN ]]; then
  echo "오류: 유효하지 않은 slug '$SLUG'"
  exit 1
fi

REPO_ROOT=$(git rev-parse --show-toplevel)
WT_DIR="$REPO_ROOT/.worktrees"
WT_PATH="$WT_DIR/$SLUG"
BRANCH="feature/$SLUG"

# .worktrees 디렉토리 생성
mkdir -p "$WT_DIR"

# .gitignore에 .worktrees/ 추가 (없으면)
GITIGNORE="$REPO_ROOT/.gitignore"
if [ -f "$GITIGNORE" ]; then
  if ! grep -qF ".worktrees/" "$GITIGNORE" 2>/dev/null; then
    echo "" >> "$GITIGNORE"
    echo "# Git worktrees for parallel development" >> "$GITIGNORE"
    echo ".worktrees/" >> "$GITIGNORE"
  fi
fi

# 이미 존재하는 worktree 확인
if [ -d "$WT_PATH" ]; then
  echo "worktree '$SLUG'가 이미 존재합니다: $WT_PATH"
  echo "  브랜치: $(git -C "$WT_PATH" branch --show-current 2>/dev/null || echo 'unknown')"
  exit 0
fi

# base 브랜치 최신화
echo "$BASE 브랜치 최신화..."
git fetch origin "$BASE" 2>/dev/null || true

# 브랜치가 이미 존재하면 worktree add만 (--checkout)
if git show-ref --verify --quiet "refs/heads/$BRANCH" 2>/dev/null; then
  echo "기존 브랜치 '$BRANCH'로 worktree 생성..."
  git worktree add "$WT_PATH" "$BRANCH"
else
  echo "새 브랜치 '$BRANCH'로 worktree 생성..."
  git worktree add -b "$BRANCH" "$WT_PATH" "origin/$BASE"
fi

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  Worktree 생성 완료"
echo "  Slug:   $SLUG"
echo "  Branch: $BRANCH"
echo "  Path:   $WT_PATH"
echo "  Base:   $BASE"
echo ""
echo "  작업 방법:"
echo "    cd \"$WT_PATH\""
echo "    또는 Claude Code에서: --project \"$WT_PATH\""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

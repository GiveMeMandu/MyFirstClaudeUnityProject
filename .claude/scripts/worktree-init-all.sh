#!/usr/bin/env bash
set -euo pipefail

# worktree-init-all.sh — 여러 시스템의 worktree를 일괄 생성
# Usage: worktree-init-all.sh <slug1> <slug2> ... [--base <branch>]
# Example: worktree-init-all.sh exploration-system encounter-system tech-tree-system

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BASE="develop"
SLUGS=()

# 인자 파싱
while [ $# -gt 0 ]; do
  case "$1" in
    --base)
      BASE="$2"
      shift 2
      ;;
    *)
      SLUGS+=("$1")
      shift
      ;;
  esac
done

if [ ${#SLUGS[@]} -eq 0 ]; then
  echo "사용법: worktree-init-all.sh <slug1> <slug2> ... [--base <branch>]"
  echo ""
  echo "예시:"
  echo "  worktree-init-all.sh exploration-system encounter-system tech-tree-system"
  echo "  worktree-init-all.sh wall-expansion policy-system --base develop"
  exit 1
fi

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  ${#SLUGS[@]}개 Worktree 일괄 생성"
echo "  Base: $BASE"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

SUCCESS=0
FAILED=0

for slug in "${SLUGS[@]}"; do
  echo "── $slug ──────────────────────────────"
  if bash "$SCRIPT_DIR/worktree-create.sh" "$slug" "$BASE"; then
    SUCCESS=$((SUCCESS + 1))
  else
    echo "  실패: $slug"
    FAILED=$((FAILED + 1))
  fi
  echo ""
done

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  완료: ${SUCCESS}개 성공, ${FAILED}개 실패"
echo ""
echo "  Worktree 목록:"
git worktree list
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

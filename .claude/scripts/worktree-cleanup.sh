#!/usr/bin/env bash
set -euo pipefail

# worktree-cleanup.sh — worktree 정리
# Usage: worktree-cleanup.sh <slug>
#        worktree-cleanup.sh --all
#        worktree-cleanup.sh --list

REPO_ROOT=$(git rev-parse --show-toplevel)
WT_DIR="$REPO_ROOT/.worktrees"

if [ $# -lt 1 ]; then
  echo "사용법:"
  echo "  worktree-cleanup.sh <slug>    — 특정 worktree 삭제"
  echo "  worktree-cleanup.sh --all     — 모든 worktree 삭제"
  echo "  worktree-cleanup.sh --list    — worktree 목록 표시"
  exit 1
fi

case "$1" in
  --list)
    echo "현재 Worktree 목록:"
    echo ""
    git worktree list
    echo ""
    if [ -d "$WT_DIR" ]; then
      echo ".worktrees/ 디렉토리:"
      ls -1 "$WT_DIR" 2>/dev/null || echo "  (비어있음)"
    fi
    ;;

  --all)
    echo "모든 worktree를 삭제합니다..."
    echo ""

    if [ ! -d "$WT_DIR" ]; then
      echo "삭제할 worktree가 없습니다."
      exit 0
    fi

    DELETED=0
    for wt_path in "$WT_DIR"/*/; do
      if [ -d "$wt_path" ]; then
        slug=$(basename "$wt_path")
        echo "  삭제: $slug"
        git worktree remove "$wt_path" --force 2>/dev/null || rm -rf "$wt_path"
        DELETED=$((DELETED + 1))
      fi
    done

    git worktree prune 2>/dev/null || true

    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "  ${DELETED}개 worktree 삭제 완료"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    ;;

  *)
    SLUG="$1"
    WT_PATH="$WT_DIR/$SLUG"

    if [ ! -d "$WT_PATH" ]; then
      echo "오류: worktree '$SLUG'가 존재하지 않습니다."
      echo "  경로: $WT_PATH"
      echo ""
      echo "현재 worktree:"
      git worktree list
      exit 1
    fi

    echo "worktree '$SLUG' 삭제 중..."
    git worktree remove "$WT_PATH" --force 2>/dev/null || rm -rf "$WT_PATH"
    git worktree prune 2>/dev/null || true

    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "  Worktree 삭제 완료: $SLUG"
    echo "  브랜치 'feature/$SLUG'는 유지됩니다."
    echo "  브랜치 삭제: git branch -d feature/$SLUG"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    ;;
esac

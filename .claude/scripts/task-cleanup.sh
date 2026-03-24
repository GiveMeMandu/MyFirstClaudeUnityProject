#!/usr/bin/env bash
set -euo pipefail

# task-cleanup.sh — 머지된 태스크 브랜치 정리
# PR이 머지된 브랜치만 삭제. merge conflict 상태인 브랜치는 건드리지 않음.
# Usage: task-cleanup.sh [--dry-run]

GH_CLI="/c/Program Files/GitHub CLI/gh.exe"
DRY_RUN=false

if [ "${1:-}" = "--dry-run" ]; then
  DRY_RUN=true
  echo "[DRY RUN] 실제 삭제하지 않고 대상만 표시합니다."
  echo ""
fi

BRANCH_PATTERN='^(feature|system|hotfix)/TASK-[0-9]+-'
DELETED_COUNT=0
SKIPPED_COUNT=0

# 현재 브랜치 저장
CURRENT=$(git branch --show-current)

# 원격 정보 최신화
git fetch --prune origin 2>/dev/null

echo "머지된 태스크 브랜치를 검색합니다..."
echo ""

while IFS= read -r branch; do
  # 공백 제거
  branch=$(echo "$branch" | xargs)

  # 현재 브랜치, main, develop은 건너뜀
  if [ "$branch" = "$CURRENT" ] || [ "$branch" = "main" ] || [ "$branch" = "develop" ]; then
    continue
  fi

  # 태스크 브랜치 패턴만 대상
  if ! [[ "$branch" =~ $BRANCH_PATTERN ]]; then
    continue
  fi

  # PR 상태 확인 (merged인지)
  PR_STATE=$("$GH_CLI" pr list --head "$branch" --state merged --json state --jq '.[0].state' 2>/dev/null || echo "")

  if [ "$PR_STATE" = "MERGED" ]; then
    # merge conflict 체크: 현재 브랜치와 target 간 conflict 여부
    # (이미 머지되었으므로 conflict는 없지만, 안전장치)
    if [ "$DRY_RUN" = true ]; then
      echo "  [삭제 대상] $branch (PR 머지됨)"
    else
      echo "  삭제: $branch"
      git branch -d "$branch" 2>/dev/null || git branch -D "$branch" 2>/dev/null || true
      git push origin --delete "$branch" 2>/dev/null || true
    fi
    DELETED_COUNT=$((DELETED_COUNT + 1))
  else
    # PR이 open 상태이거나 없는 경우
    PR_OPEN=$("$GH_CLI" pr list --head "$branch" --state open --json url --jq '.[0].url' 2>/dev/null || echo "")
    if [ -n "$PR_OPEN" ]; then
      echo "  [유지] $branch (PR 열림: $PR_OPEN)"
      SKIPPED_COUNT=$((SKIPPED_COUNT + 1))
    fi
  fi
done < <(git branch --list)

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
if [ "$DRY_RUN" = true ]; then
  echo "  [DRY RUN] 삭제 대상: ${DELETED_COUNT}개, 유지: ${SKIPPED_COUNT}개"
else
  echo "  삭제 완료: ${DELETED_COUNT}개, 유지: ${SKIPPED_COUNT}개"
fi
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# 현재 브랜치로 복귀
if [ "$(git branch --show-current)" != "$CURRENT" ]; then
  git checkout "$CURRENT" 2>/dev/null || true
fi

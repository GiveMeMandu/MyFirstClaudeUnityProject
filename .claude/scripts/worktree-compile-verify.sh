#!/usr/bin/env bash
set -euo pipefail

# worktree-compile-verify.sh — worktree 브랜치들을 순차적으로 merge하며 Unity 컴파일 검증
# Usage: worktree-compile-verify.sh <slug1> [slug2...] [--no-cleanup]
# Example: worktree-compile-verify.sh exploration-system encounter-system

UNITY_CLI="/c/Users/wooch/AppData/Local/unity-cli.exe"
NO_CLEANUP=false
SLUGS=()

# 인자 파싱
while [ $# -gt 0 ]; do
  case "$1" in
    --no-cleanup)
      NO_CLEANUP=true
      shift
      ;;
    *)
      SLUGS+=("$1")
      shift
      ;;
  esac
done

if [ ${#SLUGS[@]} -eq 0 ]; then
  echo "사용법: worktree-compile-verify.sh <slug1> [slug2...] [--no-cleanup]"
  echo ""
  echo "예시: worktree-compile-verify.sh exploration-system encounter-system"
  exit 1
fi

# 현재 브랜치 저장
ORIGINAL_BRANCH=$(git branch --show-current)
DATE_TAG=$(date +%Y%m%d-%H%M)
INTEGRATION_BRANCH="integration/verify-${DATE_TAG}"

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  순차 컴파일 검증 시작"
echo "  대상: ${SLUGS[*]}"
echo "  통합 브랜치: $INTEGRATION_BRANCH"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# develop에서 통합 브랜치 생성
git checkout develop
git pull origin develop 2>/dev/null || true
git checkout -b "$INTEGRATION_BRANCH"

PASSED=()
FAILED=()

for slug in "${SLUGS[@]}"; do
  BRANCH="feature/$slug"
  echo "── $slug 머지 시도 ──────────────────"

  # 브랜치 존재 확인
  if ! git show-ref --verify --quiet "refs/heads/$BRANCH" 2>/dev/null; then
    echo "  경고: 브랜치 '$BRANCH'가 존재하지 않습니다. 건너뜁니다."
    FAILED+=("$slug (브랜치 없음)")
    continue
  fi

  # 머지 시도
  if ! git merge --no-ff "$BRANCH" -m "integration: verify $slug"; then
    echo "  실패: 머지 충돌 발생 — $slug"
    git merge --abort
    FAILED+=("$slug (머지 충돌)")
    continue
  fi

  # Unity 컴파일 체크
  if "$UNITY_CLI" status >/dev/null 2>&1; then
    echo "  Unity 컴파일 체크..."
    "$UNITY_CLI" editor refresh --compile 2>/dev/null || true

    ERRORS=$("$UNITY_CLI" console --filter error --lines 50 2>/dev/null || echo "")

    if [ -n "$ERRORS" ] && [ "$ERRORS" != "[]" ]; then
      echo "  실패: 컴파일 에러 — $slug"
      echo "$ERRORS" | head -10
      FAILED+=("$slug (컴파일 에러)")
      # 머지를 되돌리고 다음으로
      git reset --hard HEAD~1
      continue
    fi
    echo "  컴파일 통과"
  else
    echo "  경고: Unity Editor 미연결. 컴파일 체크 건너뜀."
  fi

  PASSED+=("$slug")
  echo "  $slug 검증 완료"
  echo ""
done

# 결과 출력
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  검증 결과"
echo ""
echo "  통과: ${#PASSED[@]}개"
for s in "${PASSED[@]}"; do
  echo "    ✓ $s"
done
echo ""
if [ ${#FAILED[@]} -gt 0 ]; then
  echo "  실패: ${#FAILED[@]}개"
  for s in "${FAILED[@]}"; do
    echo "    ✗ $s"
  done
fi
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# 정리
if [ "$NO_CLEANUP" = false ]; then
  echo ""
  echo "통합 브랜치를 삭제하고 원래 브랜치로 복귀합니다..."
  git checkout "$ORIGINAL_BRANCH"
  git branch -D "$INTEGRATION_BRANCH" 2>/dev/null || true
else
  echo ""
  echo "통합 브랜치 유지: $INTEGRATION_BRANCH"
  echo "  삭제: git branch -D $INTEGRATION_BRANCH"
fi

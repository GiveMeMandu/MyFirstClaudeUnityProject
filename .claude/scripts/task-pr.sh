#!/usr/bin/env bash
set -euo pipefail

# task-pr.sh — 커밋(선택) + 푸시 + PR 생성
# Usage: task-pr.sh [commit-type message...]
# Example: task-pr.sh feat 플레이어 시스템 완성
# Example: task-pr.sh  (인자 없이 — PR만 생성)

UNITY_CLI="/c/Users/wooch/AppData/Local/unity-cli.exe"
GH_CLI="/c/Program Files/GitHub CLI/gh.exe"
VALID_COMMIT_TYPES="feat fix chore docs refactor test"
BRANCH_PATTERN='^(feature|system|hotfix)/TASK-[0-9]+-'

# ── 브랜치 검증 + 파싱 ────────────────────────────────────

BRANCH=$(git branch --show-current)

if ! [[ "$BRANCH" =~ $BRANCH_PATTERN ]]; then
  echo "오류: 태스크 브랜치가 아닙니다."
  echo "  현재 브랜치: $BRANCH"
  echo "  /task-start 로 먼저 태스크 브랜치를 생성하세요."
  exit 1
fi

# 브랜치에서 type, task-id, slug 추출
BRANCH_TYPE=$(echo "$BRANCH" | cut -d'/' -f1)
BRANCH_REST=$(echo "$BRANCH" | cut -d'/' -f2)
TASK_ID=$(echo "$BRANCH_REST" | grep -oE 'TASK-[0-9]+')
SLUG=$(echo "$BRANCH_REST" | sed "s/${TASK_ID}-//")

# ── target 브랜치 결정 ────────────────────────────────────

case "$BRANCH_TYPE" in
  feature|system)
    TARGET="develop"
    ;;
  hotfix)
    TARGET="main"
    ;;
  *)
    echo "오류: 알 수 없는 브랜치 타입 '$BRANCH_TYPE'"
    exit 1
    ;;
esac

# ── 기존 PR 확인 ──────────────────────────────────────────

EXISTING_PR=$("$GH_CLI" pr list --head "$BRANCH" --state open --json url --jq '.[0].url' 2>/dev/null || echo "")

if [ -n "$EXISTING_PR" ]; then
  echo "이미 PR이 존재합니다:"
  echo "  $EXISTING_PR"
  exit 0
fi

# ── 커밋 단계 (인자가 있는 경우) ──────────────────────────

UNITY_CHECK="skip"

if [ $# -ge 2 ]; then
  COMMIT_TYPE="$1"
  shift
  MESSAGE="$*"

  # commit-type 검증
  if ! echo "$VALID_COMMIT_TYPES" | grep -qw "$COMMIT_TYPE"; then
    echo "오류: 유효하지 않은 커밋 타입 '$COMMIT_TYPE'"
    echo "  허용: $VALID_COMMIT_TYPES"
    exit 1
  fi

  # Unity 컴파일 체크
  if "$UNITY_CLI" status >/dev/null 2>&1; then
    echo "Unity 컴파일 체크 중..."
    "$UNITY_CLI" editor refresh --compile 2>/dev/null || true

    ERRORS=$("$UNITY_CLI" console --filter error --lines 50 2>/dev/null || echo "")

    if [ -n "$ERRORS" ] && [ "$ERRORS" != "[]" ]; then
      echo ""
      echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
      echo "  Unity C# 컴파일 에러 발견 — PR 생성 중단"
      echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
      echo ""
      echo "$ERRORS" | head -20
      echo ""
      echo "unity-script-fixer 에이전트를 사용하여 에러를 수정하세요."
      exit 1
    fi
    UNITY_CHECK="pass"
    echo "Unity 컴파일 체크 통과"
  else
    echo "경고: Unity Editor가 연결되지 않아 컴파일 체크를 건너뜁니다."
  fi

  # 스테이징 + 커밋
  git add -A

  if ! git diff --cached --quiet; then
    COMMIT_MSG="${COMMIT_TYPE}: ${MESSAGE}"
    git commit -m "$COMMIT_MSG"
    echo "커밋 완료: $COMMIT_MSG"
  else
    echo "커밋할 변경사항이 없습니다. 기존 커밋으로 PR을 생성합니다."
  fi

  git push
else
  # 인자 없음 — push 상태만 확인
  if ! git rev-parse --abbrev-ref --symbolic-full-name '@{u}' >/dev/null 2>&1; then
    echo "원격 브랜치가 없습니다. push합니다..."
    git push -u origin "$BRANCH"
  else
    # 로컬이 원격보다 앞서있으면 push
    LOCAL=$(git rev-parse HEAD)
    REMOTE=$(git rev-parse '@{u}' 2>/dev/null || echo "none")
    if [ "$LOCAL" != "$REMOTE" ]; then
      git push
    fi
  fi
fi

# ── PR 타이틀 생성 ─────────────────────────────────────────

# slug을 읽기 좋게 변환: kebab-case → Title Case
READABLE_SLUG=$(echo "$SLUG" | tr '-' ' ' | awk '{for(i=1;i<=NF;i++) $i=toupper(substr($i,1,1)) substr($i,2)} 1')
PR_TITLE="[${TASK_ID}] ${READABLE_SLUG}"

# ── PR body 생성 ──────────────────────────────────────────

COMMIT_LIST=$(git log "${TARGET}..HEAD" --oneline 2>/dev/null || echo "(커밋 비교 불가)")

PR_BODY=$(cat <<EOF
## Summary
- **Branch**: \`${BRANCH}\`
- **Type**: ${BRANCH_TYPE}
- **Target**: \`${TARGET}\`

## Changes
\`\`\`
${COMMIT_LIST}
\`\`\`

## Unity Compile
- Status: ${UNITY_CHECK}
EOF
)

# ── PR 생성 ────────────────────────────────────────────────

echo "PR을 생성합니다..."
PR_URL=$("$GH_CLI" pr create \
  --base "$TARGET" \
  --title "$PR_TITLE" \
  --body "$PR_BODY" \
  2>&1)

# ── project-tasks.md 업데이트 ──────────────────────────────

TASKS_FILE="Docs/dev-docs/project-tasks.md"
if [ -f "$TASKS_FILE" ] && grep -qF "$TASK_ID" "$TASKS_FILE" 2>/dev/null; then
  # PR URL 추가
  sed -i "s|\(.*${TASK_ID}.*\)|\1 — PR: ${PR_URL}|" "$TASKS_FILE" 2>/dev/null || true
fi

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  PR 생성 완료"
echo "  Branch: $BRANCH → $TARGET"
echo "  Title:  $PR_TITLE"
echo "  URL:    $PR_URL"
echo ""
echo "  머지 후 브랜치 정리:"
echo "    bash .claude/scripts/task-cleanup.sh"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

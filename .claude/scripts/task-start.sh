#!/usr/bin/env bash
set -euo pipefail

# task-start.sh — GitFlow 브랜치 생성
# Usage: task-start.sh <type> <slug>
# Example: task-start.sh feature resource-system

VALID_TYPES="feature system hotfix"
SLUG_PATTERN='^[a-z0-9]([a-z0-9-]*[a-z0-9])?$'

# ── 인자 검증 ──────────────────────────────────────────────

if [ $# -lt 2 ]; then
  echo "사용법: task-start.sh <type> <slug>"
  echo "  type:  feature | system | hotfix"
  echo "  slug:  영문 kebab-case (예: resource-system)"
  echo ""
  echo "예시:"
  echo "  task-start.sh feature resource-system"
  echo "  task-start.sh hotfix ui-crash"
  exit 1
fi

TYPE="$1"
SLUG="$2"

# type 검증
if ! echo "$VALID_TYPES" | grep -qw "$TYPE"; then
  echo "오류: 유효하지 않은 타입 '$TYPE'"
  echo "  허용: $VALID_TYPES"
  exit 1
fi

# slug 검증
if ! [[ "$SLUG" =~ $SLUG_PATTERN ]]; then
  echo "오류: 유효하지 않은 slug '$SLUG'"
  echo "  영문 소문자, 숫자, 하이픈만 허용 (kebab-case)"
  exit 1
fi

# ── 브랜치명 조합 ──────────────────────────────────────────

BRANCH="${TYPE}/${SLUG}"

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

# ── dev-docs 자동 생성 ────────────────────────────────────

TASK_DIR="Docs/dev-docs/active/${SLUG}"
TODAY=$(date +%Y-%m-%d)

if [ ! -d "$TASK_DIR" ]; then
  mkdir -p "$TASK_DIR"

  cat > "$TASK_DIR/${SLUG}-context.md" <<CTXEOF
# ${SLUG} Context

- **Last Updated**: ${TODAY}
- **Branch**: ${BRANCH}
- **Type**: ${TYPE}
- **Base**: ${BASE}

## 핵심 파일

(구현 시 수정할 파일 목록)

## 결정사항

(아키텍처/설계 결정 기록)

## 의존성

(이 태스크가 의존하는 다른 시스템/파일)
CTXEOF

  cat > "$TASK_DIR/${SLUG}-tasks.md" <<TASKEOF
# ${SLUG} Tasks

- **Last Updated**: ${TODAY}
- **Branch**: ${BRANCH}

## TODO
- [ ] 구현 시작

## In Progress

## Done
TASKEOF

  echo "dev-docs 생성: ${TASK_DIR}/"
fi

# ── project-tasks.md 연동 ─────────────────────────────────

TASKS_FILE="Docs/dev-docs/project-tasks.md"

if [ -f "$TASKS_FILE" ]; then
  TASK_ENTRY="- [ ] ${SLUG} (\`${BRANCH}\`)"

  # 이미 등록되어 있는지 확인
  if ! grep -qF "${SLUG}" "$TASKS_FILE" 2>/dev/null; then
    # "## 진행 중" 섹션 바로 아래에 추가
    if grep -q "## 진행 중" "$TASKS_FILE"; then
      sed -i "/## 진행 중/a ${TASK_ENTRY}" "$TASKS_FILE"
    else
      # 섹션이 없으면 파일 끝에 추가
      echo "" >> "$TASKS_FILE"
      echo "## 진행 중" >> "$TASKS_FILE"
      echo "$TASK_ENTRY" >> "$TASKS_FILE"
    fi
    echo "project-tasks.md에 태스크 등록: ${SLUG}"
  fi
fi

# ── 결과 출력 ─────────────────────────────────────────────

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  브랜치 생성 완료"
echo "  Branch: $BRANCH"
echo "  Base:   $BASE"
echo "  Docs:   $TASK_DIR/"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

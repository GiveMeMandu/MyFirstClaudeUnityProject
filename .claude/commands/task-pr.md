---
description: PR 생성 (선택적 커밋 + 푸시 + PR)
argument-hint: [commit-type message] (예: feat 플레이어 시스템 완성) — PR만 생성하려면 비워두기
---

현재 태스크 브랜치의 PR을 생성합니다. 인자를 제공하면 커밋+푸시 후 PR을 생성합니다.

Arguments: $ARGUMENTS

## Instructions

1. **$ARGUMENTS 파싱** (선택적):
   - 인자가 있으면: 첫 번째 단어 = commit type, 나머지 = message
   - 인자가 없으면: 커밋 없이 기존 커밋으로 PR만 생성

2. **스크립트 실행**:
   ```bash
   bash "$CLAUDE_PROJECT_DIR/.claude/scripts/task-pr.sh" $ARGUMENTS
   ```

3. **결과 보고**:
   - PR URL
   - target 브랜치 (develop 또는 main)
   - 이미 PR이 존재하면 기존 URL 표시
   - Unity 컴파일 에러 시 에러 내용 표시

## PR Target Rules
- `feature/*`, `system/*` → `develop`으로 PR
- `hotfix/*` → `main`으로 PR

## Examples
- `/task-pr feat 플레이어 시스템 완성` — 커밋 + 푸시 + PR
- `/task-pr` — PR만 생성 (이미 커밋된 내용으로)

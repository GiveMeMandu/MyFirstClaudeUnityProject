---
description: 새 태스크 브랜치 생성 (GitFlow)
argument-hint: <type> <task-id> <slug> (예: feature TASK-001 day-night-cycle)
---

새 태스크 작업을 시작합니다.

Arguments: $ARGUMENTS

## Instructions

1. **$ARGUMENTS 파싱**:
   - 첫 번째 단어 = type (`feature`, `system`, `hotfix`)
   - 두 번째 단어 = task-id (`TASK-NNN` 형식)
   - 세 번째 단어 = slug (영문 kebab-case)
   - 인자가 부족하면 사용자에게 물어볼 것

2. **스크립트 실행**:
   ```bash
   bash "$CLAUDE_PROJECT_DIR/.claude/scripts/task-start.sh" <type> <task-id> <slug>
   ```

3. **결과 보고**:
   - 생성된 브랜치명
   - base 브랜치 (develop 또는 main)
   - develop이 새로 생성되었으면 알림

## Branch Types
- `feature` — 게임 기능 (develop에서 분기)
- `system` — 인프라/도구 작업 (develop에서 분기)
- `hotfix` — 긴급 수정 (main에서 분기)

## Examples
- `/task-start feature TASK-001 player-movement`
- `/task-start system TASK-010 build-pipeline`
- `/task-start hotfix TASK-015 crash-on-load`

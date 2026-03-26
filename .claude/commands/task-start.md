---
description: 새 태스크 브랜치 생성 (GitFlow)
argument-hint: <type> <slug> (예: feature resource-system)
---

새 태스크 작업을 시작합니다. 브랜치명은 `<type>/<slug>` 형식입니다.

Arguments: $ARGUMENTS

## Instructions

1. **$ARGUMENTS 파싱**:
   - 첫 번째 단어 = type (`feature`, `system`, `hotfix`)
   - 두 번째 단어 = slug (영문 kebab-case)
   - 인자가 부족하면 사용자에게 물어볼 것

2. **스크립트 실행**:
   ```bash
   bash "$CLAUDE_PROJECT_DIR/.claude/scripts/task-start.sh" <type> <slug>
   ```

3. **결과 보고**:
   - 생성된 브랜치명
   - base 브랜치 (develop 또는 main)

## Branch Types
- `feature` — 게임 기능 (develop에서 분기)
- `system` — 인프라/도구 작업 (develop에서 분기)
- `hotfix` — 긴급 수정 (main에서 분기)

## Examples
- `/task-start feature resource-system`
- `/task-start system build-pipeline`
- `/task-start hotfix ui-crash`

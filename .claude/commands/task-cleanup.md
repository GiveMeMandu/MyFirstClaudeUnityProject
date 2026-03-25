---
description: 머지된 태스크 브랜치 정리 (로컬 + 리모트)
argument-hint: [--dry-run] (미리보기만 하려면 --dry-run)
---

머지된 태스크 브랜치를 정리합니다.

Arguments: $ARGUMENTS

## Instructions

1. **$ARGUMENTS 파싱**:
   - `--dry-run` 옵션이 있으면 실제 삭제 없이 대상만 표시
   - 옵션이 없으면 머지된 브랜치를 로컬/리모트 모두 삭제

2. **스크립트 실행**:
   ```bash
   bash "$CLAUDE_PROJECT_DIR/.claude/scripts/task-cleanup.sh" $ARGUMENTS
   ```

3. **결과 보고**:
   - 삭제된 브랜치 목록
   - 유지된 브랜치 (PR이 열려있는 경우)
   - 총 삭제/유지 개수

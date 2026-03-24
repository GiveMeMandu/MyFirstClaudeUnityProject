---
description: 현재 태스크 브랜치에 커밋 + 푸시 (Unity 컴파일 체크 포함)
argument-hint: <type> <message> (예: feat 플레이어 대쉬 기능 추가)
---

현재 브랜치의 변경사항을 커밋하고 푸시합니다.

Arguments: $ARGUMENTS

## Instructions

1. **$ARGUMENTS 파싱**:
   - 첫 번째 단어 = commit type (`feat`, `fix`, `chore`, `docs`, `refactor`, `test`)
   - 나머지 = 커밋 메시지 (한글 가능)
   - 인자가 부족하면 사용자에게 물어볼 것

2. **스크립트 실행**:
   ```bash
   bash "$CLAUDE_PROJECT_DIR/.claude/scripts/task-commit.sh" <type> <message...>
   ```

3. **결과 보고**:
   - 커밋 해시와 메시지
   - 푸시 상태
   - Unity 컴파일 체크 결과
   - 컴파일 에러 시 unity-script-fixer 에이전트 사용 안내

## Commit Types
- `feat` — 새 기능 추가
- `fix` — 버그 수정
- `chore` — 빌드/설정 변경
- `docs` — 문서 업데이트
- `refactor` — 리팩토링
- `test` — 테스트 관련

## Examples
- `/task-commit feat 플레이어 대쉬 기능 추가`
- `/task-commit fix 낮밤 전환 오류 수정`
- `/task-commit docs GDD 문서 업데이트`

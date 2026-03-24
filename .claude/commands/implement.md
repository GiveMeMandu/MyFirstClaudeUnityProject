---
description: 기획서 기반 완전 자동 구현 (브랜치 → 코드 → 커밋 → PR)
argument-hint: <기획서 경로> (예: Docs/GDD/systems/day-night-system.md)
---

기획서를 읽고 완전 자동으로 구현합니다: 브랜치 생성 → 코드 구현 → Unity 컴파일 체크 → 커밋 → PR

Arguments: $ARGUMENTS

## Instructions

### Phase 0: 기획서 확인

1. `$ARGUMENTS`에서 기획서 경로를 추출
2. 해당 파일을 Read하여 내용을 파악
3. 기획서에서 다음 정보 추출:
   - **시스템 이름** (slug)
   - **필요한 컴포넌트** (스크립트 목록)
   - **핵심 변수** (타입, 기본값)
   - **연동 시스템** (의존성)

기획서가 없거나 경로가 잘못되었으면 사용자에게 알리고 `/시스템기획`을 안내하세요.

### Phase 1: 브랜치 생성

기획서의 slug를 사용하여 task-start 스크립트를 실행합니다.

```bash
bash "$CLAUDE_PROJECT_DIR/.claude/scripts/task-start.sh" feature <slug>
```

- TASK ID는 자동 부여됩니다
- 스크립트 출력에서 부여된 TASK ID와 브랜치명을 기록하세요

### Phase 2: 구현 계획 수립

기획서를 기반으로 구현 계획을 수립합니다:
1. 기획서의 "구현 명세" 섹션을 참조
2. 필요한 C# 스크립트 목록 작성
3. 구현 순서 결정 (의존성 순서)
4. dev-docs의 tasks 파일에 구현 계획 기록:
   - `Docs/dev-docs/active/<slug>/<slug>-tasks.md` 업데이트

### Phase 3: 코드 구현

기획서에 명시된 컴포넌트를 하나씩 구현합니다:
1. `Project_Sun/Assets/Scripts/` 하위에 적절한 폴더 구조 생성
2. C# 스크립트 작성
3. 기획서의 핵심 변수를 `[SerializeField]`로 Inspector 노출
4. 연동 시스템과의 인터페이스 구현
5. ScriptableObject가 필요하면 함께 생성

**중요**: CLAUDE.md의 Unity 개발 가이드라인을 준수하세요.

### Phase 4: Unity 컴파일 체크 + 에러 수정

구현 완료 후 컴파일을 확인합니다:

```bash
/c/Users/wooch/AppData/Local/unity-cli.exe editor refresh --compile
```

에러 확인:
```bash
/c/Users/wooch/AppData/Local/unity-cli.exe console --filter error --lines 50
```

**에러가 있으면**:
1. 에러 내용을 분석
2. 코드를 수정
3. 다시 컴파일 체크
4. 에러가 없어질 때까지 반복
5. 최대 5회 반복 후에도 에러가 남으면 사용자에게 보고

### Phase 5: 커밋

컴파일 통과 후 커밋합니다:

```bash
bash "$CLAUDE_PROJECT_DIR/.claude/scripts/task-commit.sh" feat <기획서의 시스템 이름> 구현
```

### Phase 6: PR 생성

```bash
bash "$CLAUDE_PROJECT_DIR/.claude/scripts/task-pr.sh"
```

- PR은 자동으로 develop 브랜치를 대상으로 생성됩니다
- PR URL을 사용자에게 보고합니다

### Phase 7: 완료 보고

다음 내용을 사용자에게 보고하세요:

1. **기획서**: 사용한 기획서 경로
2. **브랜치**: 생성된 브랜치명
3. **구현 내용**: 생성/수정된 파일 목록
4. **Unity 컴파일**: 통과 여부 (에러 수정 횟수 포함)
5. **커밋**: 커밋 해시와 메시지
6. **PR**: PR URL

dev-docs의 context 파일도 업데이트하세요:
- `Docs/dev-docs/active/<slug>/<slug>-context.md`에 구현 결과 기록

## Error Recovery

- **Unity Editor 미연결**: 경고 출력 후 컴파일 체크 건너뛰고 계속 진행
- **컴파일 에러 반복**: 5회 실패 시 현재까지 작업을 커밋하고 사용자에게 보고
- **브랜치 생성 실패**: 이미 존재하는 브랜치면 checkout하여 계속
- **PR 중복**: 이미 PR이 있으면 URL 표시 후 종료

## Examples
- `/구현 Docs/GDD/systems/day-night-system.md`
- `/구현 Docs/GDD/systems/inventory-system.md`

---
description: 기획서 기반 SF 단위 자동 구현 (브랜치 → SF별 구현/커밋 → PR)
argument-hint: <기획서 경로> (예: Docs/GDD/systems/day-night-system.md)
---

기획서를 읽고 Sub-Feature 단위로 구현합니다: 브랜치 생성 → SF별 (구현 → 컴파일 체크 → 커밋) → 통합 검증 → PR

Arguments: $ARGUMENTS

## Instructions

### Phase 0: 기획서 확인 + SF 목록 추출

1. `$ARGUMENTS`에서 기획서 경로를 추출
2. 해당 파일을 Read하여 내용을 파악
3. 기획서에서 다음 정보 추출:
   - **시스템 이름** (slug)
   - **필요한 컴포넌트** (스크립트 목록)
   - **핵심 변수** (타입, 기본값)
   - **연동 시스템** (의존성)
4. **"구현 단위 (Sub-Features)" 섹션** 확인:
   - **섹션 있음** → SF 테이블 파싱 (ID, 이름, 포함 스크립트, 의존 단위, 크기)
   - **섹션 없음** → "필요한 컴포넌트" 테이블 기반으로 SF 자동 분해:
     - 데이터 모델 (SO, enum, struct) → SF-01~
     - 코어 로직 (상태머신, 매니저) → 이어서
     - 시스템 연동 (인터페이스) → 이어서
     - UI → 이어서
     - VFX/SFX → 이어서
     - 테스트 씬 → 마지막
5. SF 간 의존성으로 **토폴로지 정렬** (실행 순서 결정)

기획서가 없거나 경로가 잘못되었으면 사용자에게 알리고 `/system-design`을 안내하세요.

### Phase 0.5: 재개 감지

이미 진행 중인 작업이 있는지 확인합니다:

1. `git branch --show-current`로 현재 브랜치 확인
2. 현재 브랜치가 해당 시스템의 태스크 브랜치인지 확인
3. `Docs/dev-docs/active/<slug>/<slug>-tasks.md` 존재 여부 확인
4. **이미 진행 중이면**:
   - tasks.md에서 완료된 SF 목록 파악 (커밋 해시가 기록된 SF)
   - `git log --oneline`으로 기존 SF 커밋 확인
   - 완료된 SF를 스킵 목록에 추가
   - "SF-01, SF-02 이미 완료. SF-03부터 재개합니다." 보고
   - Phase 1 건너뛰고 Phase 2로 이동
5. **새로 시작이면**: Phase 1로 진행

### Phase 1: 브랜치 생성

기획서의 slug를 사용하여 task-start 스크립트를 실행합니다.

```bash
bash "$CLAUDE_PROJECT_DIR/.claude/scripts/task-start.sh" feature <slug>
```

- 브랜치명은 `feature/<slug>` 형식으로 생성됩니다
- 스크립트 출력에서 브랜치명을 기록하세요

### Phase 2: SF별 구현 계획 수립

토폴로지 순서로 각 SF의 구현 계획을 수립합니다:

1. 각 SF에 대해:
   - 생성/수정할 파일 목록
   - 의존 SF에서 사용하는 타입/인터페이스 정리
   - 수락 기준 (컴파일 통과, 핵심 기능 동작)
2. `Docs/dev-docs/active/<slug>/<slug>-tasks.md` 에 SF별 체크리스트 기록:

```markdown
## Sub-Features

### SF-01: <이름> [<크기>]
- **상태**: TODO
- **의존**: -
- **파일**: Script1.cs, Script2.cs
- **커밋**: —
- [ ] 구현 항목 1
- [ ] 구현 항목 2
- [ ] Unity 컴파일 통과

### SF-02: <이름> [<크기>]
...

## 완료 이력
| SF | 커밋 해시 | 날짜 |
|---|---|---|
```

### Phase 3: SF 순차 실행 루프

**각 SF에 대해 토폴로지 순서로 반복합니다:**

#### 3a. 코드 구현

- 해당 SF의 스크립트만 구현 (다른 SF의 파일은 건드리지 않음)
- `Project_Sun/Assets/Scripts/` 하위에 적절한 폴더 구조 사용
- 기획서의 핵심 변수를 `[SerializeField]`로 Inspector 노출
- 의존 SF에서 정의된 타입/인터페이스 활용

**중요**: CLAUDE.md의 Unity 개발 가이드라인을 준수하세요.

#### 3b. Unity 컴파일 체크

```bash
/c/Users/wooch/AppData/Local/unity-cli.exe editor refresh --compile
```

에러 확인:
```bash
/c/Users/wooch/AppData/Local/unity-cli.exe console --filter error --lines 50
```

#### 3c. 에러 수정

에러가 있으면:
1. 에러 내용 분석
2. **해당 SF의 파일만** 수정 (다른 SF 파일 수정 금지)
3. 다시 컴파일 체크
4. 최대 5회 반복
5. 5회 후에도 에러가 남으면 현재까지 작업을 커밋하고 사용자에게 보고

#### 3d. SF 커밋

컴파일 통과 후, **해당 SF의 파일만** 선택적으로 커밋합니다:

```bash
bash "$CLAUDE_PROJECT_DIR/.claude/scripts/task-commit-sf.sh" feat <slug> <SF-ID> "<SF 설명>" <file1> [file2...]
```

예시:
```bash
bash "$CLAUDE_PROJECT_DIR/.claude/scripts/task-commit-sf.sh" feat construction-system SF-01 "데이터 모델 및 열거형" Project_Sun/Assets/Scripts/Construction/BuildingEnums.cs Project_Sun/Assets/Scripts/Construction/BuildingData.cs
```

#### 3e. dev-docs 업데이트

- `<slug>-tasks.md`에서 해당 SF 상태를 `DONE`으로 변경
- 커밋 해시를 "완료 이력" 테이블에 기록
- 다음 SF의 상태를 `IN_PROGRESS`로 변경

#### 3f. WBS 업데이트 (선택적)

`Docs/dev-docs/wbs.md`가 존재하면:
- 해당 시스템의 SF 상태를 `완료`로 업데이트

**→ 다음 SF로 반복 (3a부터)**

### Phase 4: 통합 검증

모든 SF 커밋 완료 후:

1. 전체 컴파일 체크
```bash
/c/Users/wooch/AppData/Local/unity-cli.exe editor refresh --compile
```
2. 컴파일 에러 시 수정 후 별도 `fix:` 커밋
3. 시스템 전체가 의도대로 동작하는지 확인

### Phase 5: dev-docs + WBS 최종 갱신

1. `<slug>-context.md` 업데이트:
   - 구현된 파일 목록
   - 아키텍처 결정 사항
   - 알려진 제한/TODO
2. `<slug>-tasks.md` 최종 확인:
   - 모든 SF 체크박스 완료 확인
3. `Docs/dev-docs/wbs.md` (존재 시):
   - 시스템 상태를 `완료` (또는 `진행중`)로 업데이트

### Phase 6: PR 생성

```bash
bash "$CLAUDE_PROJECT_DIR/.claude/scripts/task-pr.sh"
```

- PR은 자동으로 develop 브랜치를 대상으로 생성됩니다
- PR body에 다수 SF 커밋이 표시됩니다

### Phase 7: 완료 보고

다음 내용을 사용자에게 보고하세요:

1. **기획서**: 사용한 기획서 경로
2. **브랜치**: 생성된 브랜치명
3. **SF 실행 결과**:

| SF | 설명 | 커밋 해시 | 파일 수 | 상태 |
|---|---|---|---|---|
| SF-01 | ... | abc1234 | 3 | 완료 |

4. **Unity 컴파일**: 최종 통과 여부
5. **PR**: PR URL

## Error Recovery

- **Unity Editor 미연결**: 경고 출력 후 컴파일 체크 건너뛰고 계속 진행
- **SF 컴파일 에러 반복**: 5회 실패 시 해당 SF까지의 작업을 커밋하고 사용자에게 보고 (후속 SF는 중단)
- **브랜치 생성 실패**: 이미 존재하는 브랜치면 checkout하여 Phase 0.5 재개 감지로 진행
- **PR 중복**: 이미 PR이 있으면 URL 표시 후 종료
- **기획서에 SF 섹션 없음**: 컴포넌트 테이블 기반으로 자동 SF 분해 후 진행

## Examples
- `/implement Docs/GDD/systems/day-night-system.md`
- `/implement Docs/GDD/systems/personnel-system.md`

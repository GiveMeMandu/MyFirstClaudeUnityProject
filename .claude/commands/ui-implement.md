---
description: 학습 계획에 따라 UI 예제를 단계별 구현
argument-hint: <학습 계획 경로> (예: Docs/UI-Study/plans/mvvm-vcontainer.md)
---

학습 계획서에 따라 UI 예제를 Step별로 구현합니다.

Arguments: $ARGUMENTS

## Instructions

### Phase 0: 계획서 확인

1. `$ARGUMENTS`에서 계획서 경로 추출
2. 해당 파일을 Read하여 내용 파악
3. 계획서에서 추출:
   - **학습 주제** (slug)
   - **필요 패키지** 목록
   - **Step 목록** (순서, 파일, 수락 기준)
4. 계획서가 없으면 `/ui-study-plan`을 먼저 실행하라고 안내

### Phase 0.5: 재개 감지

이미 진행 중인 작업이 있는지 확인:

1. `Docs/UI-Study/plans/<slug>.md`에서 체크박스 상태 확인
2. 이미 완료된 Step이 있으면:
   - 완료된 Step 목록 파악
   - 다음 미완료 Step부터 재개
   - "Step 1, 2 완료. Step 3부터 재개합니다." 보고
3. 새로 시작이면 Phase 1로 진행

### Phase 1: 환경 준비

1. **프로젝트 경로 확인**: UI_Study 프로젝트 경로
   - Unity 프로젝트: `UI_Study/`
   - Assets: `UI_Study/Assets/`
2. **폴더 구조 생성**:
   ```
   UI_Study/Assets/_Study/<slug>/
   ├── Scripts/
   ├── UI/
   ├── Scenes/
   └── README.md
   ```
3. **필요 패키지 확인**: 계획서의 패키지가 `UI_Study/Packages/manifest.json`에 있는지 확인
   - 없으면 사용자에게 설치 안내 (unity-cli 또는 수동 설치)

### Phase 2: Step별 구현 루프

**각 Step에 대해 순서대로 반복:**

#### 2a. Step 시작 안내

사용자에게 현재 Step 정보 간략 보고:
- Step 번호와 제목
- 목표
- 생성/수정할 파일 목록

#### 2b. 코드 구현

- 계획서의 파일 목록에 따라 스크립트/UI 파일 생성
- `UI_Study/Assets/_Study/<slug>/` 하위에 작성
- 핵심 개념에 대한 주석은 학습 목적으로 포함 (과하지 않게)
- 리서치 문서의 베스트 프랙티스 준수

**코드 작성 가이드라인:**
- 새 Input System 사용 (UnityEngine.Input 금지)
- SerializeField로 Inspector 노출
- 네이밍: PascalCase (public), _camelCase (private fields)
- nullable 경고 방지: null 체크 또는 null-forgiving operator

#### 2c. Unity 컴파일 체크

```bash
/c/Users/wooch/AppData/Local/unity-cli.exe editor refresh --compile
```

에러 확인:
```bash
/c/Users/wooch/AppData/Local/unity-cli.exe console --filter error --lines 50
```

#### 2d. 에러 수정

에러가 있으면:
1. 에러 내용 분석
2. 해당 Step의 파일만 수정
3. 다시 컴파일 체크
4. 최대 5회 반복
5. 5회 후에도 실패하면 현재까지 작업을 저장하고 사용자에게 보고

#### 2e. Step 완료 기록

1. 계획서(`<slug>.md`)의 해당 Step 체크박스를 완료 처리
2. README.md에 해당 Step의 학습 내용 요약 추가

#### 2f. 커밋

```bash
git add UI_Study/Assets/_Study/<slug>/
git commit -m "$(cat <<'EOF'
feat(ui-study): Step N - <Step 제목>

<간략 설명>

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

**→ 다음 Step으로 반복 (2a부터)**

### Phase 3: 통합 검증

모든 Step 완료 후:

1. 전체 컴파일 체크
2. 계획서의 "검증 체크리스트" 항목 확인
3. 미충족 항목이 있으면 수정

### Phase 4: README 최종화

`UI_Study/Assets/_Study/<slug>/README.md` 최종 작성:

```markdown
# [주제] 학습 예제

## 개요
(무엇을 배웠는지)

## 구조
(폴더/파일 설명)

## 실행 방법
(씬 열기, 플레이 방법)

## 핵심 패턴
(이 예제에서 사용한 주요 패턴)

## 학습 포인트
1. ...
2. ...

## Project_Sun 적용 시 고려사항
(메인 프로젝트에 이 패턴을 적용할 때 주의할 점)
```

### Phase 5: 완료 보고

사용자에게 보고:

| Step | 제목 | 파일 수 | 상태 |
|---|---|---|---|
| 1 | ... | N | 완료 |
| 2 | ... | N | 완료 |

- **컴파일**: 최종 통과 여부
- **다음 단계**: `/ui-review`로 코드 리뷰 권장

## Error Recovery

- **Unity Editor 미연결**: 경고 출력, 컴파일 체크 건너뛰고 진행
- **패키지 미설치**: 사용자에게 설치 안내 후 대기
- **Step 실패**: 해당 Step까지의 작업 커밋 후 사용자에게 보고

## Examples
- `/ui-implement Docs/UI-Study/plans/mvvm-vcontainer.md`
- `/ui-implement Docs/UI-Study/plans/uitoolkit-basics.md`
- `/ui-implement Docs/UI-Study/plans/reactive-binding.md`

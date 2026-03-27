---
description: UI 학습 계획 수립 (리서치 → 예제 구현 순서 결정)
argument-hint: <주제 또는 리서치 문서 경로> (예: "MVVM + VContainer", "Docs/UI-Study/research/uitoolkit-basics.md")
---

UI 학습 계획을 수립하고, 단계별 예제 구현 순서를 결정합니다.

Arguments: $ARGUMENTS

## Instructions

### Phase 1: 입력 분석

1. `$ARGUMENTS` 파싱:
   - **리서치 문서 경로**가 주어졌으면 → 해당 문서 Read
   - **주제 키워드**만 주어졌으면 → `Docs/UI-Study/research/`에서 관련 문서 검색
   - **관련 리서치 없으면** → `/ui-research`를 먼저 실행하라고 안내
2. 기존 학습 계획 확인: `Docs/UI-Study/plans/` 하위

### Phase 2: 사전 지식 확인 (인터뷰)

사용자에게 간단히 확인:
1. 이 주제에 대한 현재 이해 수준 (초급/중급/고급)
2. 특별히 집중하고 싶은 부분이 있는지
3. UI_Study에서 만들고 싶은 최종 결과물 (예: 인벤토리 UI, 설정 화면 등)
4. 사용하려는 프레임워크/라이브러리 조합

### Phase 3: 학습 경로 설계

리서치 결과와 사용자 응답을 기반으로 학습 경로를 설계합니다:

```
Level 1: 기초 (Foundation)
  └─ 핵심 개념 이해 + 최소 예제

Level 2: 패턴 (Patterns)
  └─ 아키텍처 패턴 적용 + 중간 규모 예제

Level 3: 통합 (Integration)
  └─ 여러 라이브러리 통합 + 실전 규모 예제

Level 4: 최적화 (Optimization)
  └─ 성능 최적화 + 프로덕션 패턴
```

### Phase 4: 학습 계획서 작성

**파일 위치**: `Docs/UI-Study/plans/<plan-slug>.md`

```markdown
# [주제] 학습 계획

- **작성일**: YYYY-MM-DD
- **기반 리서치**: [리서치 문서 경로]
- **목표**: [최종 결과물]
- **예상 단계**: N개

---

## 사전 준비

### 필요 패키지
| 패키지 | 버전 | 설치 방법 |
|---|---|---|
| ... | ... | ... |

### 프로젝트 구조
```
UI_Study/Assets/
├── _Study/
│   ├── <plan-slug>/
│   │   ├── Scripts/
│   │   ├── UI/          (UXML/USS 또는 Prefabs)
│   │   ├── Scenes/
│   │   └── README.md
```

---

## 학습 단계

### Step 1: [제목] — Level 1 (Foundation)
- **목표**: [이 단계에서 배우는 것]
- **핵심 개념**: [이해해야 할 개념]
- **예제**: [만들 예제 설명]
- **파일 목록**:
  - `Scripts/Example1.cs` — [역할]
  - `UI/Example1.uxml` — [역할]
- **수락 기준**:
  - [ ] 컴파일 통과
  - [ ] [기능 동작 확인]
- **참고**: [관련 문서/튜토리얼 링크]

### Step 2: [제목] — Level 2 (Patterns)
...

### Step N: [제목] — Level 4 (Optimization)
...

---

## 검증 체크리스트

### 아키텍처
- [ ] View/ViewModel/Model 레이어 분리
- [ ] DI 컨테이너 올바른 사용
- [ ] 구독 해제 누락 없음

### 성능
- [ ] 프레임당 GC Alloc 0
- [ ] Canvas rebuild 최소화
- [ ] 오브젝트 풀링 적용 (동적 리스트)

### 코드 품질
- [ ] 네이밍 컨벤션 준수
- [ ] 매직 넘버 없음
- [ ] 주석 최소 (자기 설명적 코드)

---

## 완료 후 다음 단계
- [ ] `/ui-review`로 코드 리뷰
- [ ] 패턴 문서화 (`Docs/UI-Study/patterns/`)
- [ ] Project_Sun 적용 가능성 평가
```

### Phase 5: 확인 및 안내

1. 계획서를 사용자에게 보여주고 수정 요청 확인
2. 확정되면:
   - `/ui-implement <plan-path>` 로 구현 시작 가능함을 안내
   - 필요한 패키지가 있으면 설치 방법 안내

## Examples
- `/ui-study-plan MVVM + VContainer 기초`
- `/ui-study-plan Docs/UI-Study/research/uitoolkit-basics.md`
- `/ui-study-plan 인벤토리 UI 만들기`
- `/ui-study-plan 리액티브 데이터 바인딩`

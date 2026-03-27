---
description: UI 구현 코드를 베스트 프랙티스 기준으로 리뷰
argument-hint: [경로] (예: UI_Study/Assets/_Study/mvvm-basic, 미지정 시 최근 구현 전체 리뷰)
---

구현된 UI 코드를 베스트 프랙티스 기준으로 리뷰하고 개선점을 제안합니다.

Arguments: $ARGUMENTS

## Instructions

### Phase 1: 리뷰 대상 결정

1. `$ARGUMENTS`가 있으면 해당 경로를 리뷰 대상으로 설정
2. 없으면:
   - `UI_Study/Assets/_Study/` 하위의 모든 학습 예제 폴더 탐색
   - 가장 최근 수정된 폴더를 대상으로 선택
   - 사용자에게 대상 확인

### Phase 2: 코드 수집

1. 대상 경로의 모든 `.cs` 파일 Read
2. `.uxml`, `.uss` 파일이 있으면 함께 Read
3. `.prefab`, `.unity` 씬 파일의 존재 확인
4. 관련 학습 계획서 (`Docs/UI-Study/plans/`) 확인
5. 관련 리서치 문서 (`Docs/UI-Study/research/`) 확인

### Phase 3: 에이전트 리뷰

`ui-architecture-reviewer` 에이전트를 사용하여 심층 리뷰 수행:

리뷰 기준:
1. **레이어 분리**: View/ViewModel/Model 분리 여부
2. **DI 패턴**: 의존성 주입 올바른 사용
3. **리액티브 바인딩**: 구독 관리, 메모리 누수
4. **UI 성능**: GC Alloc, Canvas rebuild, 풀링
5. **코드 품질**: 네이밍, 구조, 가독성
6. **학습 가치**: 예제가 패턴을 명확히 보여주는지

### Phase 4: 리뷰 보고서 작성

**파일 위치**: `Docs/UI-Study/reviews/<slug>-review.md`

```markdown
# [주제] UI 코드 리뷰

- **리뷰일**: YYYY-MM-DD
- **대상**: [경로]
- **등급**: A/B/C/D/F

---

## 요약
(전체 평가 2-3문장)

## Critical Issues (반드시 수정)
### CRIT-01: [제목]
- **파일**: path:line
- **문제**: ...
- **수정안**:
```csharp
// 수정 코드
```

## Warnings (수정 권장)
...

## Suggestions (선택적 개선)
...

## 잘한 점
...

## 학습 포인트
(이 리뷰에서 배울 수 있는 것)
```

### Phase 5: 자동 수정 (선택)

Critical Issues가 있으면 사용자에게 물어봅니다:
- "Critical Issue N건 발견. 자동 수정하시겠습니까?"
- 승인 시 해당 파일 수정 → 컴파일 체크 → 커밋

### Phase 6: 패턴 문서화

리뷰에서 발견된 좋은 패턴이 있으면:
1. `Docs/UI-Study/patterns/` 하위에 패턴 문서 생성/업데이트
2. 재사용 가능한 코드 스니펫 포함

### Phase 7: 완료 보고

사용자에게 보고:
1. **등급**: A~F
2. **Critical**: N건 (자동 수정 여부)
3. **Warning**: N건
4. **다음 단계**: 수정 필요 시 구체적 안내, 아니면 다음 학습 주제 제안

## Examples
- `/ui-review UI_Study/Assets/_Study/mvvm-basic`
- `/ui-review` (최근 구현 자동 감지)
- `/ui-review UI_Study/Assets/_Study/` (전체 리뷰)

---
description: Unity UI 주제/라이브러리/패턴을 심층 리서치
argument-hint: <주제> (예: "UI Toolkit vs UGUI", "VContainer DI 패턴", "MVVM in Unity")
---

Unity UI 관련 주제를 심층 리서치하고 결과를 문서화합니다.

Arguments: $ARGUMENTS

## Instructions

### Phase 1: 주제 분석

1. `$ARGUMENTS`에서 리서치 주제를 파악
2. 주제를 다음 카테고리로 분류:
   - **technology**: 기술 비교 (예: UI Toolkit vs UGUI)
   - **library**: 외부 라이브러리 조사 (예: VContainer, R3, UniTask)
   - **pattern**: 아키텍처 패턴 (예: MVVM, MVP, Flux)
   - **practice**: 베스트 프랙티스 (예: UI 성능 최적화, 접근성)
   - **integration**: 라이브러리 통합 (예: VContainer + UI Toolkit)

### Phase 2: 웹 리서치

Agent tool을 사용하여 `ui-research-specialist` 또는 `web-research-specialist` 에이전트로 심층 조사를 수행합니다:

1. **공식 문서 조사**
   - Unity 공식 문서 (UI Toolkit, UGUI)
   - 라이브러리 공식 GitHub README, Wiki, Samples
   - Unity Blog 포스트

2. **커뮤니티 사례 조사**
   - GitHub에서 해당 주제의 example/sample 프로젝트 검색
   - Unity Forum, Reddit (r/Unity3D, r/gamedev) 스레드
   - Stack Overflow 답변
   - 기술 블로그 포스트

3. **비교 분석** (기술/패턴 비교인 경우)
   - 각 선택지의 장단점
   - 성능 벤치마크 (있는 경우)
   - 실제 프로젝트 적용 사례
   - Unity 버전별 호환성

### Phase 3: 기존 리서치 확인

1. `Docs/UI-Study/research/` 하위에 동일 주제의 기존 문서가 있는지 확인
2. 있으면 업데이트, 없으면 새로 생성

### Phase 4: 리서치 문서 작성

**파일 위치**: `Docs/UI-Study/research/<topic-slug>.md`

```markdown
# [주제] 리서치 리포트

- **작성일**: YYYY-MM-DD
- **카테고리**: technology | library | pattern | practice | integration
- **상태**: 조사완료 | 추가조사필요

---

## 1. 요약
(핵심 발견 사항 3-5문장)

## 2. 상세 분석

### 2.1 [세부 주제 1]
(설명 + 코드 예제)

### 2.2 [세부 주제 2]
...

## 3. 베스트 프랙티스

### DO (권장)
- [ ] (실천 항목)

### DON'T (금지)
- [ ] (회피 항목)

### CONSIDER (상황별)
- [ ] (고려 항목)

## 4. 호환성

| 항목 | 버전 | 비고 |
|---|---|---|
| Unity | 6000.x | ... |
| ... | ... | ... |

## 5. 예제 코드

### 기본 사용법
```csharp
// 예제
```

### 고급 패턴
```csharp
// 예제
```

## 6. UI_Study 적용 계획
(이 리서치를 기반으로 UI_Study에서 어떤 예제를 만들 수 있는지)

## 7. 참고 자료
1. [제목](URL)
2. ...

## 8. 미해결 질문
- [ ] (추가 조사 필요 항목)
```

### Phase 5: 인덱스 업데이트

1. `Docs/UI-Study/research/index.md` 확인 (없으면 생성)
2. 새 리서치 항목 추가

### Phase 6: 완료 보고

사용자에게 보고:
1. **주제**: 조사한 주제
2. **핵심 발견**: 3줄 요약
3. **추천 다음 단계**: `/ui-study-plan`으로 학습 계획 수립 또는 추가 리서치 제안
4. **문서 위치**: 저장된 파일 경로

## Examples
- `/ui-research UI Toolkit vs UGUI 비교`
- `/ui-research VContainer Unity DI`
- `/ui-research MVVM 패턴 Unity 적용`
- `/ui-research UI 성능 최적화 기법`
- `/ui-research R3 리액티브 바인딩`

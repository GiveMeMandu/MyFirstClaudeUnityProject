# UI Toolkit vs UGUI 최종 판정 매트릭스

- **작성일**: 2026-03-29
- **카테고리**: integration
- **상태**: 조사완료
- **기반 문서**: 021~029

---

## 1. 요약

UI Toolkit은 데이터 집약형 화면(목록, 그리드, 설정, HUD)에서 UGUI 대비 성능(드로우콜 9배 감소, CPU 3배 개선)과 개발 편의성(ListView 가상화 내장, USS 선언형 스타일, AI 생성 친화) 모두 우위를 보인다. 반면 월드 공간 UI, CSS 키프레임 애니메이션, 커스텀 셰이더는 2026년 현재 UI Toolkit에서 미지원이므로 이 영역은 UGUI가 유일한 선택지다. 기지 경영 게임의 경우 화면 대부분(HUD, 건설 메뉴, 유닛 목록, 설정, 다이얼로그, 저장/불러오기)을 UI Toolkit 경량 스택(Simple MVP + UniTask + DOTween + C# events)으로 구현하고, 월드 공간 UI(적 HP바, 건물 이름표)만 UGUI로 유지하는 하이브리드 전략을 권고한다. VContainer와 R3는 프로젝트 전체에 도입하지 않고, 복잡도 임계점(화면 15개 이상, 공유 상태 15개 이상, 디바운스/쓰로틀 필요)을 넘는 시점에서만 부분 도입한다.

---

## 2. 12개 영역 마스터 비교표

| # | 영역 | UGUI 스택 | UI Toolkit 스택 | 판정 | 비고 |
|---|------|-----------|----------------|------|------|
| 1 | 아키텍처 | MV(R)P + VContainer | Simple MVP (DI 없이) | ⚖️ 동등 | UI Toolkit은 Bootstrapper 패턴으로 수동 조립. 코드량 약 25% 감소(80줄→60줄), 학습 곡선 대폭 하락. VContainer 없이도 MVP 분리 가능 (doc 026) |
| 2 | DI | VContainer (LifetimeScope 계층) | 불필요 ([SerializeField] + Bootstrapper) | ⚠️ UGUI 우세 | VContainer의 EntryPoint/Scope 자동 관리가 편리하나, UI Toolkit에서는 MonoBehaviour View를 직접 연결하므로 DI 오버헤드 불필요. 화면 15개 초과 시 ScriptableObject 서비스 로케이터 또는 VContainer 부분 도입 (doc 026 섹션 2.5) |
| 3 | 리액티브 | R3 (ReactiveProperty, Subscribe, Throttle/Debounce) | C# event + ObservableValue<T> 래퍼 (R3 부분 도입 가능) | ⚠️ UGUI 우세 | R3의 스트림 연산자(Throttle, Debounce, CombineLatest)는 C# event로 대체 불가. 그러나 소규모 팀(1-2인)에서 대부분의 UI 바인딩은 C# event로 충분. 검색 디바운스/빠른 클릭 방지에만 R3 부분 도입 권장 (doc 026 섹션 2.4, doc 027 섹션 2.4) |
| 4 | 비동기 | UniTask | UniTask (동일) | ⚖️ 동등 | UniTask PR #338 stale로 UI Toolkit 공식 확장 없음. 그러나 UniTaskCompletionSource + clicked 수동 연결로 async 다이얼로그 완전 구현 가능 (doc 027 패턴 A/B/C). destroyCancellationToken 활용 동일 |
| 5 | 네비게이션 | UnityScreenNavigator (Page/Modal/Sheet) | ScreenManager (Dictionary + Push/Pop 스택) | ⚠️ UGUI 우세 | USN은 전환 애니메이션/생명주기/프리팹 풀링 내장. UI Toolkit은 DisplayStyle.None/Flex + DOTween으로 수동 구현. 10개 이하 화면에서는 차이 미미, 30개 이상에서 USN 우위 명확 (doc 026 섹션 2.3) |
| 6 | 애니메이션 | DOTween (Sequence/Ease 완전 지원) + Animator/Timeline | DOTween (getter/setter 람다) + USS Transition | ⚠️ UGUI 우세 | USS transition은 hover/상태 전환에 적합하나 CSS @keyframes 미지원. DOTween은 VisualElement에 DOTween.To() 패턴으로 사용 가능하나 직접 확장 메서드(DOMove 등) 제한적. 복잡 타임라인 시퀀스는 UGUI + Animator가 우위 (doc 027 섹션 2.2, doc 029 섹션 2.4.2) |
| 7 | 스크롤뷰 / 목록 | ScrollRect + FancyScrollView (수동 풀링) | ListView 가상화 (makeItem/bindItem 내장) | ✅ UI Toolkit 우세 | ListView는 10,000개 이상 항목을 버벅임 없이 처리 (UGUI ~500개). 코드량도 풀 관리 ~100줄 → makeItem/bindItem ~20줄로 대폭 감소. userData 캐시 패턴으로 bindItem 최적화 가능 (doc 028 섹션 2.3, doc 029 섹션 2.2) |
| 8 | 로컬라이제이션 | Unity Localization | Unity Localization (동일) | ⚖️ 동등 | 양쪽 모두 Unity Localization 패키지 사용 가능. UI Toolkit은 UXML에서 바인딩 경로 지정 가능 |
| 9 | 에셋 관리 | Addressables + SpriteAtlas V2 | Addressables + Dynamic Atlas | ⚖️ 동등 | UI Toolkit은 PanelSettings에서 동적 아틀라스를 자동 생성하여 드로우콜 최소화. SpriteAtlas V2는 UGUI 전용이나 UI Toolkit에서는 동적 아틀라스가 대체 (doc 029 섹션 2.1) |
| 10 | 입력 | New Input System | New Input System (동일) | ⚖️ 동등 | 양쪽 모두 InputSystemUIInputModule 사용. UI Toolkit은 PointerDownEvent/NavigationMoveEvent로 터치/게임패드 처리. UGUI와 혼용 시 EventSystem 충돌 주의 (doc 029 섹션 2.5) |
| 11 | 테마 / 스타일링 | uPalette (ScriptableObject 팔레트) | USS 변수 (--custom-property) + USS 파일 교체 | ✅ UI Toolkit 우세 | USS는 CSS 변수 시스템으로 테마 전환이 파일 교체 한 줄로 가능. uPalette 외부 패키지 불필요. :hover, :active, :disabled 등 pseudo-class로 상태 스타일을 선언형 처리 (doc 025 USS 선택자, doc 028 전체) |
| 12 | 폰트 | TextMeshPro SDF | UI Toolkit 내장 폰트 시스템 | ⚖️ 동등 | UI Toolkit은 USS -unity-font, -unity-font-style로 폰트 관리. TMP의 고급 기능(글자별 색상, 링크, 인라인 이미지)은 UI Toolkit에서 제한적 |

**판정 범례**: ✅ UI Toolkit 우세 / ⚖️ 동등 / ⚠️ UGUI 우세 / ❌ UI Toolkit 미지원

**종합**: UI Toolkit 우세 2개, 동등 5개, UGUI 우세 4개, 미지원 0개 (단, 월드 공간 UI는 표 외 구조적 한계)

---

## 3. UI 유형별 추천

| UI 유형 | 추천 | 이유 | 근거 문서 |
|---------|------|------|----------|
| 메뉴/설정 화면 | UI Toolkit | TabView 내장, Slider/Toggle/DropdownField 컨트롤 풍부, USS 선언형 스타일링. SetValueWithoutNotify로 초기화/취소 패턴 간결 | doc 028 섹션 2.4 |
| HUD (자원바, 미니맵) | UI Toolkit | 빈번한 텍스트 갱신에서 개별 VisualElement만 dirty (UGUI는 Canvas 전체 rebuild 위험). USS transition으로 값 변경 시 반짝임 효과 코드 없이 구현 | doc 028 섹션 2.1, doc 029 섹션 2.1 |
| 인벤토리/유닛 목록 | UI Toolkit | ListView 가상화로 10,000개 이상 항목 처리. 정렬/필터 후 RefreshItems() 한 줄. 수동 풀링 코드 100줄 절감 | doc 028 섹션 2.3, doc 029 섹션 2.2 |
| 다이얼로그/팝업 | UI Toolkit | pickingMode + position: absolute backdrop으로 모달 차단. display + schedule.Execute + CSS transition 조합으로 fade-in/scale 애니메이션. UniTaskCompletionSource로 async await 가능 | doc 028 섹션 2.5, doc 027 패턴 A |
| 건설 메뉴 (카드 그리드) | UI Toolkit | flex-wrap으로 자동 줄바꿈 그리드. :hover pseudo-class로 코드 없는 호버 효과. EnableInClassList로 잠금/구매가능/비용초과 상태 전환 | doc 028 섹션 2.2 |
| 기술 트리 | UI Toolkit | position: absolute로 노드 자유 배치, generateVisualContent + Painter2D BezierCurveTo로 연결선. ScrollView 내 고정 크기 캔버스 | doc 028 섹션 2.6 |
| 저장/불러오기 | UI Toolkit | ListView + 썸네일/메타데이터 바인딩. Dialog 연동 패턴 자연스러움 | doc 028 섹션 2.7 |
| 월드 공간 UI (HP바, 이름표) | **UGUI** | UI Toolkit 월드 공간 미지원 (2026 기준). RenderTexture 우회는 입력 처리 복잡 + 멀티터치 미지원 + 성능 불확실. Canvas.WorldSpace가 유일한 안정적 선택 | doc 029 섹션 2.4.1 |
| 복잡 애니메이션 UI (파티클 혼합, Timeline 시퀀스) | **UGUI** | CSS @keyframes 미지원, Animator/Timeline 미통합. DOTween getter/setter 패턴은 가능하나 복잡 시퀀스에서 UGUI + Animator가 더 강력 | doc 029 섹션 2.4.2 |
| 커스텀 셰이더 UI (글리치, 디졸브 등) | **UGUI** | UI Toolkit VisualElement에 Material 직접 적용 불가. Graphic.material이 UGUI의 강점 | doc 029 섹션 2.4.5 |
| 드래그 앤 드롭 | **판단 보류** | 양쪽 모두 구현 가능. UGUI는 IBeginDragHandler 트리오 + CanvasGroup.blocksRaycasts 검증됨 (doc 016, 020). UI Toolkit은 PointerManipulator 패턴으로 가능하나 Sortable List 수준의 검증은 미완료 |

---

## 4. 경량 스택 최종 권고

### 기본 스택 (프로젝트 시작 시)

| 역할 | 채택 | 대체 대상 | 비고 |
|------|------|----------|------|
| UI 프레임워크 | **UI Toolkit** (UXML + USS) | UGUI (Canvas + RectTransform) | AI 생성 친화, 선언형 레이아웃 |
| 아키텍처 | **Simple MVP** (View + Presenter + Model) | MV(R)P + VContainer | Bootstrapper 패턴, 수동 조립 |
| 비동기 | **UniTask** | - | 규모 무관 필수. async 다이얼로그 핵심 |
| 애니메이션 | **USS Transition** (단순) + **DOTween** (복잡) | DOTween 단독 | hover/상태→CSS, 시퀀스/스태거→DOTween |
| 상태 관리 | **C# event + ObservableValue<T>** | R3 ReactiveProperty | 10줄 래퍼로 값 변경 알림 |
| 화면 전환 | **ScreenManager** (Push/Pop 스택) | UnityScreenNavigator | 10개 이하 화면에 충분 |
| 월드 UI | **UGUI** (Canvas.WorldSpace) | - | UI Toolkit 미지원 영역 |

### 선택적 확장 (임계점 도달 시)

| 역할 | 도입 시점 | 패키지 |
|------|----------|--------|
| 리액티브 스트림 | 검색 디바운스, 빠른 클릭 쓰로틀 필요 시 | R3 (특정 화면만) |
| DI 컨테이너 | 화면 15개+, 공유 상태 15개+, 팀 3인+ | VContainer |
| 고급 내비게이션 | 화면 30개+, 전환 애니메이션 표준화 필요 시 | UnityScreenNavigator |

### 패키지 의존성 비교

| 구분 | UGUI 풀 스택 | UI Toolkit 경량 스택 |
|------|-------------|---------------------|
| 필수 패키지 | VContainer, R3, UniTask, DOTween, USN, uPalette | UniTask, DOTween |
| 선택 패키지 | FancyScrollView | R3 (부분), VContainer (부분) |
| 학습 비용 | 높음 (DI + Rx + USN 개념) | 낮음 (C# 기초 + USS/UXML) |
| 온보딩 시간 | 2-3주 | 3-5일 |
| AI 생성 정확도 | 낮음 (Inspector 값 생성 불가) | 높음 (UXML/USS = HTML/CSS 유사) |

---

## 5. 스택 도입 임계점

| 조건 | 기본 스택으로 충분 | R3 부분 도입 | VContainer 도입 |
|------|-------------------|-------------|----------------|
| 화면 수 | 1~10개 | 10~30개 (특정 화면만) | 30개+ |
| 공유 상태 소스 | 1~5개 | 5~15개 | 15개+ |
| 스트림 연산 필요 | 없음 | 디바운스/쓰로틀 1-3곳 | 복합 스트림 5곳+ |
| 팀 규모 | 1~2인 | 2~3인 | 3인+ |
| 단위 테스트 필요도 | 낮음 | 중간 | 필수 (인터페이스 주입) |
| 크로스 씬 상태 공유 | ScriptableObject | ScriptableObject + R3 | VContainer Root Scope |
| 디버깅 난이도 | 낮음 (콜스택 명확) | 중간 (Rx 스트림 추적) | 중간 (DI 그래프 추적) |
| 프로젝트 수명 | 프로토타입~소규모 출시 | 중규모 출시 | 대규모 장기 운영 |

**현 프로젝트 판단 (소규모 기지 경영 게임)**:
- 예상 화면 수: 8~15개
- 팀 규모: 1~2인
- 공유 상태: 5개 미만 (자원, 건물, 연구, 전투, 설정)
- **결론**: 기본 스택으로 시작, 검색/빠른 클릭 화면에만 R3 부분 도입

---

## 6. 마이그레이션 전략

### Phase 1: 새 UI 화면만 UI Toolkit으로 (기존 UGUI 유지)

**범위**: 신규 화면(설정, 저장/불러오기, 기술 트리) 개발 시 UI Toolkit 사용 시작

**핵심 작업**:
- PanelSettings 생성 (Scale With Screen Size, 1920x1080 기준)
- Sort Order 레이어 체계 수립 (HUD:0, Screen:10, Popup:20, Toast:30)
- ScreenManager 공통 컴포넌트 구현
- VisualElementTweenExtensions 유틸리티 구축 (doc 027 패턴)
- Bootstrapper + Simple MVP 표준 패턴 확립

**위험도**: **낮음** -- 기존 UGUI에 영향 없음. 독립적 검증 가능

**소요 기간**: 1~2 스프린트

### Phase 2: 데이터 중심 화면 전환 (인벤토리, 유닛 목록)

**범위**: ListView 가상화의 성능 이점이 명확한 화면부터 전환

**핵심 작업**:
- 인벤토리 그리드를 ListView + flex-wrap 그리드로 재구현
- 유닛 목록을 ListView + 정렬/필터로 재구현
- makeItem/bindItem + userData 캐시 패턴 표준화
- UGUI FancyScrollView/수동 풀링 코드 제거

**위험도**: **중간** -- 데이터 바인딩 로직 변경 필요. 기능 회귀 테스트 필수

**소요 기간**: 2~3 스프린트

### Phase 3: HUD 전환

**범위**: 자원 HUD, 미니맵 프레임, 상태 표시줄

**핵심 작업**:
- ResourceHUD를 UI Toolkit Label + USS transition으로 전환
- Canvas 분리 전략(정적/동적) 대신 UIR 증분 업데이트 활용
- 프로파일러로 Canvas rebuild 제거 확인

**위험도**: **중간** -- HUD는 항상 표시되므로 성능 회귀 시 즉시 체감됨. 프로파일러 검증 필수

**소요 기간**: 1~2 스프린트

### Phase 4: 월드 공간 UI는 UGUI 유지

**범위**: 적 HP바, 건물 이름표, 상호작용 프롬프트, 플로팅 데미지 텍스트

**핵심 작업**:
- UGUI Canvas.WorldSpace 유지
- UI Toolkit과 UGUI 혼용 시 EventSystem 단일화 (InputSystemUIInputModule)
- Unity 로드맵에서 월드 공간 UI Toolkit 지원 발표 시 Phase 5로 재평가

**위험도**: **낮음** -- 현상 유지. EventSystem 충돌만 주의

**소요 기간**: 해당 없음 (유지)

### Phase 간 공존 아키텍처

```
Screen Space (UI Toolkit)          World Space (UGUI)
┌────────────────────────┐         ┌─────────────────┐
│ PanelSettings (Toast)  │ SO:30   │                 │
│ PanelSettings (Popup)  │ SO:20   │ Canvas.World    │
│ PanelSettings (Screen) │ SO:10   │  ├─ HP Bars     │
│ PanelSettings (HUD)    │ SO:0    │  ├─ Name Tags   │
│                        │         │  └─ Damage Nums │
└────────────────────────┘         └─────────────────┘
        │                                   │
        └──── InputSystemUIInputModule ─────┘
              (단일 EventSystem)
```

---

## 7. 참고 문서

### UI Toolkit 전수조사 시리즈 (021~029)

| # | 문서 | 핵심 기여 |
|---|------|----------|
| 021 | [VContainer+R3+UniTask UI Toolkit 호환성](vcontainer-r3-unitask-uitoolkit-compatibility.md) | 기존 스택의 UI Toolkit 호환 한계 확인. "전환 시기상조" 근거 수립 |
| 022 | [Thronefall 기술 스택 분석](thronefall-tech-stack-analysis.md) | DI/Reactive 없이 성공한 인디 사례. 경량 스택 정당성 확보 |
| 023 | [AI-Assisted UI 생성 패턴](uitoolkit-ai-generation-patterns.md) | UXML/USS의 AI 생성 우위 입증. LLM 학습 데이터 기반 정확도 분석 |
| 024 | [UI Toolkit Fundamentals](uitoolkit-fundamentals.md) | VisualElement 트리, Yoga/Flexbox, UIDocument/PanelSettings 기초. UGUI 매핑표 |
| 025 | [UXML/USS 심층 분석](uitoolkit-uxml-uss-deep-dive.md) | 선택자/특수성/전환/UQuery/커스텀 컨트롤. CSS 서브셋 한계 정리 |
| 026 | [경량 아키텍처 (DI 없는 Simple MVP)](uitoolkit-lightweight-architecture.md) | 3계층 MVP 완전 예제. ObservableValue<T> 래퍼. DI 도입 임계점 표 |
| 027 | [UniTask + DOTween 실전 패턴](uitoolkit-unitask-dotween-patterns.md) | UniTaskCompletionSource 다이얼로그, DOTween.To() 스타일 트위닝, VisualElementTweenExtensions |
| 028 | [런타임 게임 UI 패턴](uitoolkit-runtime-game-ui-patterns.md) | HUD/건설/유닛/설정/다이얼로그/기술트리/저장 7개 패턴 완전 구현 + UGUI 비교 매트릭스 |
| 029 | [성능 분석 및 한계](uitoolkit-performance-and-limits.md) | 벤치마크(드로우콜 9x/CPU 3x/메모리 2.6x), UsageHints, 4대 한계, 알려진 버그 5종, Unity 로드맵 |

### 기존 UGUI 스택 문서

| # | 문서 | 관련성 |
|---|------|--------|
| 005 | [기술 스택 결정 종합](tech-stack-decisions.md) | UGUI 12개 영역 확정 스택. 본 문서의 비교 기준 |
| 012 | [UGUI 성능 최적화 기법 종합](ugui-performance-optimization.md) | Canvas rebuild 비용 — UI Toolkit 우위 근거 |
| 016 | [UGUI Drag & Drop 구현 패턴](ugui-drag-drop-patterns.md) | 드래그 앤 드롭 "판단 보류" 근거 |
| 020 | [UGUI Sortable List Reorder](ugui-sortable-list-reorder.md) | Placeholder 패턴 검증 완료 — UI Toolkit 동등 검증 미완 |

---

## 8. 미해결 질문

### 구조적 한계 (Unity 로드맵 의존)

1. **월드 공간 UI Toolkit 지원 시기**: Unity 공식 로드맵에 "In Progress"이나 구체적 버전 미발표. 발표 시 Phase 5 재평가 필요
2. **커스텀 셰이더 지원 시기**: "Planned" 상태. 글리치/디졸브 등 특수 UI 효과에 영향
3. **CSS @keyframes 애니메이션**: 로드맵에 명시적 언급 없음. DOTween으로 우회 지속 필요

### 기술적 검증 필요

4. **UI Toolkit + UGUI 혼용 시 EventSystem 안정성**: InputSystemUIInputModule 단일 사용으로 두 시스템 모두 안정적으로 동작하는지 실측 필요
5. **Unity 6.3 런타임 바인딩 성능**: INotifyBindablePropertyChanged 기반 바인딩 vs 직접 label.text 갱신의 실측 CPU 비교
6. **UI Toolkit 드래그 앤 드롭**: PointerManipulator 기반 Sortable List 수준의 검증이 아직 미완. UGUI IBeginDragHandler 트리오와 동등 수준인지 확인 필요
7. **ListView DynamicHeight 실측**: 가변 높이 행 500개에서 FixedHeight 대비 성능 저하 폭 측정 필요

### 프로젝트 운영

8. **마이그레이션 Phase 2-3의 기능 회귀 범위**: 기존 UGUI 화면을 UI Toolkit으로 전환할 때 발생할 수 있는 시각적/기능적 차이 목록 사전 정리 필요
9. **USS 테마 파일 관리 전략**: 다크/라이트 테마 전환 시 USS 변수 파일 구조와 런타임 교체 패턴 정립 필요

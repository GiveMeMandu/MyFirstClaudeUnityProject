# UGUI vs UI Toolkit 비교 회고

- **작성일**: 2026-03-29
- **범위**: UI_Study 01~11 (UGUI 풀 스택) vs 12 (UI Toolkit 경량 스택)
- **상태**: 완료

---

## 1. 요약

UGUI 풀 스택(01~11)과 UI Toolkit 경량 스택(12)을 동일 수준의 게임 UI로 구현한 결과, 경량 스택이 개발 속도, 코드량, AI 활용성에서 분명한 우위를 보였다. 반면 UGUI 풀 스택은 리액티브 스트림 연산, 화면 전환 자동화, 월드 공간 UI에서 대체 불가한 강점이 있었다. 이 문서는 양쪽을 직접 경험한 뒤 얻은 실전 교훈을 정리한다.

---

## 2. 기술 스택 비교

### UGUI 풀 스택 (01~11)

| 역할 | 패키지 |
|------|--------|
| UI 프레임워크 | UGUI (Canvas + RectTransform) |
| 아키텍처 | MV(R)P |
| DI | VContainer (LifetimeScope 계층) |
| 리액티브 | R3 (ReactiveProperty, Subscribe, Throttle/Debounce) |
| 비동기 | UniTask |
| 화면 전환 | UnityScreenNavigator (Page/Modal/Sheet) |
| 애니메이션 | DOTween |
| 테마 | uPalette |
| 스크롤 | ScrollRect + FancyScrollView |

### UI Toolkit 경량 스택 (12)

| 역할 | 패키지 |
|------|--------|
| UI 프레임워크 | UI Toolkit (UXML + USS) |
| 아키텍처 | Simple MVP (Bootstrapper 패턴) |
| DI | 없음 ([SerializeField] 수동 조립) |
| 리액티브 | C# event + ObservableValue<T> 래퍼 (R3 부분 도입: Step 8만) |
| 비동기 | UniTask |
| 화면 전환 | ScreenManager (수동 Push/Pop) |
| 애니메이션 | DOTween + USS Transition |
| 테마 | USS 변수 (--custom-property) |
| 스크롤 | ListView 가상화 (내장) |

---

## 3. 항목별 비교

| 항목 | UGUI 풀 스택 | UI Toolkit 경량 스택 | 판정 |
|------|-------------|---------------------|------|
| **개발 속도** | 보일러플레이트 많음 (LifetimeScope, EntryPoint 등록, Rx 구독 체인) | Bootstrapper + Start()에서 수동 조립. 파일당 코드 약 25% 감소 | UI Toolkit 우세 |
| **코드량** (Presenter 기준) | ~80줄 (DI 등록 + Rx Subscribe + CompositeDisposable) | ~60줄 (C# event += 핸들러 + IDisposable) | UI Toolkit 우세 |
| **학습 곡선** | 높음: DI 개념 + Rx 연산자 + USN 생명주기 + LifetimeScope 계층 (온보딩 2-3주) | 낮음: C# 기초 + USS/UXML 구문 (온보딩 3-5일) | UI Toolkit 우세 |
| **필수 패키지 수** | 6개 (VContainer, R3, UniTask, DOTween, USN, uPalette) | 2개 (UniTask, DOTween) | UI Toolkit 우세 |
| **AI 생성 친화성** | 낮음: Inspector [SerializeField] 바인딩을 AI가 생성 불가. DI 등록 누락 빈발 | 높음: UXML/USS = HTML/CSS 유사. LLM 학습 데이터 풍부. 코드 전체가 텍스트 | UI Toolkit 우세 |
| **렌더링 성능** | Canvas rebuild 비용 (동적 텍스트 갱신 시 전체 Canvas dirty) | UIR 증분 업데이트 (개별 VisualElement만 dirty). 드로우콜 9배 감소 벤치마크 | UI Toolkit 우세 |
| **리스트 성능** | ScrollRect ~500개 한계. FancyScrollView로 풀링 시 ~100줄 추가 코드 | ListView 가상화 내장. 10,000개 이상 처리. makeItem/bindItem ~20줄 | UI Toolkit 우세 |
| **리액티브 스트림 연산** | R3: Throttle, Debounce, CombineLatest, DistinctUntilChanged 완전 지원 | C# event로 대체 불가. Step 8에서 R3 부분 도입으로 해결 | UGUI 우세 |
| **화면 전환 자동화** | USN: 전환 애니메이션/생명주기/프리팹 풀링 내장. 30개 이상 화면에서 강력 | 수동 ScreenManager (DisplayStyle.None/Flex + DOTween). 10개 이하에서 충분 | UGUI 우세 |
| **월드 공간 UI** | Canvas.WorldSpace 완전 지원 (HP바, 이름표, 데미지 넘버) | 미지원 (2026 기준). RenderTexture 우회는 실용성 부족 | UGUI 우세 |
| **복잡 애니메이션** | DOTween + Animator + Timeline 완전 통합 | DOTween.To() getter/setter만 가능. CSS @keyframes 미지원 | UGUI 우세 |
| **커스텀 셰이더** | Graphic.material 직접 적용 (글리치, 디졸브 등) | VisualElement에 Material 적용 불가 | UGUI 우세 |
| **유지보수성** | DI 그래프 추적 필요. Rx 스트림 디버깅 어려움 | 콜스택이 명확. event += handler는 IDE에서 추적 용이 | UI Toolkit 우세 |
| **테마 전환** | uPalette 외부 패키지 필요. ScriptableObject 팔레트 관리 | USS 변수 파일 교체 한 줄. 외부 패키지 불필요 | UI Toolkit 우세 |

---

## 4. 예상 밖이었던 점

### UI Toolkit 쪽

1. **VContainer 없이도 MVP가 깔끔했다**: Bootstrapper에서 `new Presenter(model, view)`로 수동 조립하는 것이 DI 컨테이너 설정보다 오히려 직관적이었다. LifetimeScope 계층 설계에 소비하던 시간이 제로가 되었다.

2. **CSS Transition이 생각보다 강력했다**: hover, active, disabled 상태 전환에 C# 코드가 한 줄도 필요 없었다. USS에서 `transition-duration: 0.2s`만 선언하면 끝. UGUI에서는 DOTween.Sequence를 매번 작성해야 했던 부분이다.

3. **named method + OnDisable 해제 패턴의 위력**: 익명 람다 대신 `clicked += HandleXxx` / `clicked -= HandleXxx`로 통일하니 메모리 누수가 구조적으로 불가능해졌다. R3의 CompositeDisposable보다 추적이 쉬웠다.

4. **ListView 가상화의 압도적 생산성**: UGUI에서 FancyScrollView + 수동 풀링에 100줄 이상 작성하던 것이 makeItem/bindItem 20줄로 대체되었다. userData 캐시 패턴까지 포함해도 40줄 수준.

### UGUI 쪽

1. **R3의 스트림 연산자는 C# event로 절대 대체 불가**: 검색 디바운스를 C# event로 구현하려면 Timer + 상태 플래그 + CancellationToken 조합으로 30줄 이상 필요했다. R3 Debounce는 한 줄. Step 8에서 이 차이를 직접 체험한 것이 가장 큰 교훈이었다.

2. **USN의 화면 전환 생명주기가 아쉬웠다**: 수동 ScreenManager로 10개 이하 화면을 관리하는 것은 문제없었으나, 전환 애니메이션 표준화와 프리팹 풀링이 없어 화면 수가 늘어나면 반복 코드가 급증할 것이 명확했다.

---

## 5. UGUI에서 이어진 패턴

UI Toolkit으로 전환해도 그대로 유효했던 패턴들:

| 패턴 | UGUI 적용 | UI Toolkit 적용 |
|------|----------|----------------|
| MVP 레이어 분리 (View-Presenter-Model) | MV(R)P + VContainer | Simple MVP + Bootstrapper |
| Presenter를 Pure C# + IDisposable로 구현 | 동일 | 동일 |
| Model에 UI 참조 금지 | 동일 | 동일 |
| UniTask async 다이얼로그 (UniTaskCompletionSource) | 동일 | 동일 |
| DOTween Sequence Kill 처리 (OnDestroy) | 동일 | 동일 |
| CancellationToken 생명주기 연동 | destroyCancellationToken | destroyCancellationToken |
| DisplayStyle 토글 (Show/Hide) | gameObject.SetActive | style.display = None/Flex |

---

## 6. UGUI에서 이어지지 않은 패턴

| UGUI 패턴 | UI Toolkit 대체 | 이유 |
|-----------|----------------|------|
| VContainer LifetimeScope 계층 | Bootstrapper 수동 조립 | DI 오버헤드 대비 이점 부족 (소규모) |
| R3 ReactiveProperty 전면 사용 | C# event Action<T> | 단순 값 변경 알림에 Rx 불필요 |
| R3 CompositeDisposable | Presenter.Dispose()에서 -= 해제 | named method라 정확히 해제 가능 |
| UnityScreenNavigator Page/Modal/Sheet | DisplayStyle + DOTween | 10개 이하 화면에서 과잉 |
| uPalette 팔레트 | USS 변수 (--color-primary 등) | USS 변수가 네이티브 테마 시스템 |
| ScrollRect + FancyScrollView 풀링 | ListView 내장 가상화 | 코드량 5배 감소 |
| Canvas 분리 (정적/동적) | UIR 자동 증분 업데이트 | 수동 최적화 불필요 |

---

## 7. 최종 판정: 상황별 선택 기준

| 상황 | 권고 | 이유 |
|------|------|------|
| 신규 프로젝트, 1~2인 팀, 화면 15개 이하 | **UI Toolkit 경량 스택** | 학습 비용 최소, AI 활용 극대화, 패키지 의존성 최소 |
| 대규모 프로젝트, 3인+ 팀, 화면 30개+ | **UGUI 풀 스택** (또는 UI Toolkit + VContainer/R3 부분 도입) | DI/Rx의 구조적 이점이 복잡도 임계점을 넘김 |
| 월드 공간 UI 필수 (HP바, 이름표) | **UGUI** (대체 불가) | UI Toolkit 월드 공간 미지원 |
| 커스텀 셰이더 UI (글리치, 디졸브) | **UGUI** (대체 불가) | VisualElement에 Material 적용 불가 |
| 데이터 중심 UI (리스트, 그리드, 설정) | **UI Toolkit** | ListView 가상화 + USS 선언형 스타일링 |
| 복잡 애니메이션 (Timeline 시퀀스) | **UGUI** | Animator/Timeline 통합, CSS @keyframes 미지원 |
| AI 협업 중심 워크플로 | **UI Toolkit** | UXML/USS 텍스트 기반, LLM 학습 데이터 풍부 |
| 하이브리드 (대부분 스크린 + 일부 월드) | **UI Toolkit + UGUI 공존** | 스크린 UI는 UI Toolkit, 월드는 UGUI 유지 |

---

## 8. 핵심 교훈

1. **"필요한 것만 쓴다"가 가장 강력한 원칙이다.** VContainer와 R3는 훌륭한 도구이지만, 소규모 프로젝트에서는 C# event와 Bootstrapper로 충분했다. 도구를 도입하기 전에 "이것 없이 구현할 수 있는가?"를 먼저 묻는 습관이 중요하다.

2. **AI 생성 친화성은 2026년 시점에서 무시할 수 없는 요소다.** UXML/USS가 HTML/CSS와 유사하여 LLM이 높은 정확도로 생성하는 반면, UGUI의 Inspector 바인딩은 AI가 생성할 수 없다. 이 차이는 개발 속도에 직접 영향을 미쳤다.

3. **R3 부분 도입 전략은 정답이었다.** Step 1-7을 C# event로 구현한 뒤 Step 8에서 Debounce/ThrottleFirst가 필요해지는 시점을 직접 체험함으로써, "전면 도입 vs 부분 도입"의 판단 기준을 체득했다.

4. **두 시스템을 모두 경험해야 올바른 판단이 가능하다.** UGUI 풀 스택만 경험했다면 "VContainer/R3는 필수"라 생각했을 것이고, UI Toolkit만 경험했다면 "DI/Rx는 불필요"라 단정했을 것이다. 양쪽을 모두 구현해본 뒤의 판단은 질적으로 다르다.

---

## 9. 관련 문서

- [UI Toolkit vs UGUI 최종 판정 매트릭스](../research/uitoolkit-vs-ugui-decision-matrix.md)
- [UI Toolkit 경량 스택 코드 리뷰](./12-uitoolkit-lightweight-review.md)
- [기술 스택 결정 종합 (UGUI)](../research/tech-stack-decisions.md)
- [UI Toolkit Simple MVP 패턴](../patterns/uitoolkit-simple-mvp.md)

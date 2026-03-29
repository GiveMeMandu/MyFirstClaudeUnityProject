# Project_Sun UI Toolkit 도입 결정

- **결정일**: 2026-03-29
- **상태**: 권고 (최종 결정 대기)
- **작성 근거**: UI_Study 01~12 학습 결과, uitoolkit-vs-ugui-decision-matrix.md

---

## 1. 현황

Project_Sun은 턴제 기지 경영 게임으로, 현재 UI 시스템은 미구현 상태이다. 기존에 UGUI 풀 스택(MV(R)P + VContainer + R3 + UniTask + DOTween + USN)을 도입 예정이었으나, UI_Study 12번 예제에서 UI Toolkit 경량 스택의 실전 검증을 완료하여 도입 방향을 재검토한다.

### 프로젝트 특성

| 항목 | 값 |
|------|-----|
| 장르 | 턴제 기지 경영 |
| 팀 규모 | 1~2인 |
| 예상 화면 수 | 8~15개 |
| 공유 상태 소스 | 5개 미만 (자원, 건물, 연구, 전투, 설정) |
| 월드 공간 UI | 필요 (건물 이름표, 적 HP바, 플로팅 데미지 텍스트) |
| 데이터 중심 UI | 높음 (건설 메뉴, 유닛 목록, 인벤토리, 기술 트리) |
| 대상 플랫폼 | PC (Steam) |

---

## 2. 권고: 조건부 도입 (하이브리드 전략)

UI Toolkit을 주력 UI 시스템으로 채택하되, 월드 공간 UI와 커스텀 셰이더 UI는 UGUI로 유지하는 하이브리드 전략을 권고한다.

### 권고 근거

1. **성능**: UI Toolkit은 드로우콜 9배 감소, CPU 3배 개선 벤치마크. 건설 메뉴/유닛 목록 등 데이터 집약 화면에서 이점이 크다.
2. **개발 효율**: 필수 패키지 2개(UniTask, DOTween)로 시작 가능. UGUI 풀 스택 대비 온보딩 시간 75% 단축(2-3주 -> 3-5일).
3. **AI 협업**: UXML/USS 텍스트 기반으로 LLM 생성 정확도 높음. Inspector 바인딩 의존 제거.
4. **프로젝트 규모 적합성**: 화면 8~15개, 1~2인 팀은 경량 스택의 최적 범위에 해당.

---

## 3. 도입 범위

| UI 유형 | 권고 | 이유 |
|---------|------|------|
| 메뉴/설정 | UI Toolkit | TabView/Slider/Toggle/DropdownField 내장. USS 선언형 스타일링. AI 생성 용이 |
| HUD (자원바, 미니맵 프레임) | UI Toolkit | 빈번한 텍스트 갱신에서 개별 VisualElement만 dirty. Canvas 전체 rebuild 회피 |
| 인벤토리/유닛 목록 | UI Toolkit | ListView 가상화로 10,000개 이상 항목 처리. 수동 풀링 코드 100줄 절감 |
| 건설 메뉴 (카드 그리드) | UI Toolkit | flex-wrap 자동 줄바꿈. :hover pseudo-class. EnableInClassList 상태 전환 |
| 기술 트리 | UI Toolkit | position: absolute 노드 배치 + Painter2D 연결선 |
| 다이얼로그/팝업 | UI Toolkit | pickingMode backdrop + UniTaskCompletionSource async await |
| 저장/불러오기 | UI Toolkit | ListView + 썸네일 바인딩 |
| 월드 공간 UI (HP바, 이름표) | **UGUI 유지** | UI Toolkit 월드 공간 미지원 (2026 기준). 대안 없음 |
| 플로팅 데미지 텍스트 | **UGUI 유지** | 월드 좌표에서 생성, 오브젝트 풀링 + DOTween 시퀀스 |
| 복잡 애니메이션 UI | **UGUI 유지** | CSS @keyframes 미지원. Animator/Timeline 필요 시 |
| 커스텀 셰이더 UI | **UGUI 유지** | VisualElement에 Material 적용 불가 |

**비율 추정**: UI Toolkit 70~80%, UGUI 20~30%

---

## 4. 기본 기술 스택

| 역할 | 채택 | 비고 |
|------|------|------|
| 스크린 UI | UI Toolkit (UXML + USS) | 주력 |
| 월드 UI | UGUI (Canvas.WorldSpace) | 보조 |
| 아키텍처 | Simple MVP + Bootstrapper | VContainer 미도입 (임계점 도달 전) |
| 비동기 | UniTask | 규모 무관 필수 |
| 애니메이션 | USS Transition + DOTween | 이원 체계 |
| 상태 알림 | C# event + ObservableValue<T> | R3 부분 도입 가능 (임계점 도달 시) |
| 화면 전환 | ScreenManager (수동 Push/Pop) | 15개 이하 화면에 충분 |
| 입력 | New Input System | 이미 설치됨 |

### 선택적 확장 (임계점 도달 시)

| 역할 | 도입 시점 | 패키지 |
|------|----------|--------|
| 리액티브 스트림 | 검색 디바운스, 빠른 클릭 쓰로틀 필요 시 | R3 (특정 화면만) |
| DI 컨테이너 | 화면 15개+, 공유 상태 15개+, 팀 3인+ 중 2개 이상 충족 시 | VContainer |
| 고급 내비게이션 | 화면 30개+, 전환 애니메이션 표준화 필요 시 | UnityScreenNavigator |

---

## 5. 도입 전제 조건

1. **Unity 버전 6.x 이상 사용**: UI Toolkit 런타임 기능이 6.x에서 안정화됨. 5.x LTS에서는 에디터 전용.
2. **PanelSettings 에셋 표준화**: Scale Mode = Scale With Screen Size, Reference Resolution = 1920x1080, Sort Order 레이어 체계 확립.
3. **VisualElementTweenExtensions 유틸리티 구축**: DOTween.To() getter/setter 래퍼 (DOFade, DOTranslateX/Y, DOScale). UI_Study 12번의 구현을 이식.
4. **Bootstrapper + Simple MVP 표준 패턴 확립**: Model(C# event), View(MonoBehaviour + UIDocument), Presenter(Pure C# + IDisposable) 템플릿 정리.
5. **EventSystem 단일화 검증**: UI Toolkit + UGUI 혼용 시 InputSystemUIInputModule 하나로 양쪽 입력이 안정적으로 동작하는지 실측 검증.

---

## 6. 리스크

### 높은 리스크

| # | 리스크 | 영향 | 완화 방안 |
|---|--------|------|----------|
| 1 | UI Toolkit + UGUI 혼용 시 입력 충돌 | 클릭이 양쪽 시스템에 동시 전파되어 의도치 않은 동작 | Phase 1에서 EventSystem 공존 테스트 선행. 포커스 관리 프로토콜 수립 |
| 2 | 월드 공간 UI와 스크린 UI 간 렌더 순서 꼬임 | 월드 HP바가 모달 다이얼로그 위에 표시됨 | Sort Order 레이어 체계 엄격 준수. UGUI Canvas Sort Order < UI Toolkit 최소 Sort Order |

### 중간 리스크

| # | 리스크 | 영향 | 완화 방안 |
|---|--------|------|----------|
| 3 | USS 학습 곡선 (웹 CSS와 차이점) | 지원되지 않는 CSS 속성 사용 시도로 디버깅 시간 낭비 | USS 지원 속성 체크리스트 작성. AI 생성물 검수 프로세스 수립 |
| 4 | DOTween VisualElement 확장의 제한 | DOMove, DOScale 등 직접 확장 메서드 없음. getter/setter 패턴만 가능 | VisualElementTweenExtensions 공통 유틸리티로 래핑 |
| 5 | Unity 버전 업그레이드 시 UI Toolkit API 변경 | 리팩터링 비용 발생 | Unity 6.x LTS 고정. 업그레이드 전 릴리스 노트 확인 |

### 낮은 리스크

| # | 리스크 | 영향 | 완화 방안 |
|---|--------|------|----------|
| 6 | TextMeshPro 고급 기능 부재 (글자별 색상, 인라인 이미지) | 특수 텍스트 효과 불가 | 해당 기능이 필요한 UI만 UGUI로 구현 |
| 7 | 프로젝트 규모 증가 시 DI 필요성 | 수동 조립이 번거로워짐 | 임계점(화면 15개+) 도달 시 VContainer 부분 도입 |

---

## 7. 마이그레이션 로드맵

### Phase 1: 기반 구축 (스프린트 1)

- PanelSettings 에셋 생성 (Scale With Screen Size, 1920x1080)
- Sort Order 레이어 체계 확립 (HUD:0, Screen:10, Popup:20, Toast:30)
- ScreenManager 공통 컴포넌트 이식 (UI_Study 12번 -> Project_Sun)
- VisualElementTweenExtensions 이식
- Bootstrapper + Simple MVP 템플릿 확립
- EventSystem 공존 테스트 (UI Toolkit + UGUI World Space)

### Phase 2: 첫 번째 UI Toolkit 화면 (스프린트 2)

- 설정 화면 (TabView + Slider/Toggle) -- 가장 독립적인 화면으로 시작
- USS 테마 변수 시스템 구축 (--color-primary, --font-size-base 등)
- 저장/불러오기 화면 (ListView + 메타데이터)

### Phase 3: 데이터 중심 화면 (스프린트 3~4)

- 건설 메뉴 (카드 그리드 + flex-wrap + :hover)
- 유닛 목록 (ListView 가상화 + 정렬/필터)
- 인벤토리 (ListView + userData 캐시)
- 기술 트리 (position: absolute + Painter2D 연결선)

### Phase 4: HUD + 다이얼로그 (스프린트 5)

- 자원 HUD (Label + USS transition 값 변경 반짝임)
- 확인/취소 다이얼로그 (UniTaskCompletionSource)
- 로딩 화면 (IProgress<float>)
- 토스트 알림

### Phase 5: 월드 공간 UI 유지 (해당 스프린트)

- 적 HP바 (UGUI Canvas.WorldSpace)
- 건물 이름표 (UGUI Canvas.WorldSpace)
- 플로팅 데미지 텍스트 (UGUI + 오브젝트 풀 + DOTween)
- EventSystem 통합 최종 검증

---

## 8. 의사결정 필요 항목

아래 항목은 프로젝트 담당자가 최종 결정해야 한다.

| # | 항목 | 선택지 | 권고 | 비고 |
|---|------|--------|------|------|
| 1 | 도입 시점 | 즉시 / GDD 확정 후 / 프로토타입 후 | GDD 확정 후 | UI 화면 목록이 확정되어야 Phase 설계 가능 |
| 2 | USS 테마 전략 | 단일 테마 / 다크+라이트 | 단일 테마 | 기지 경영 게임의 톤에 맞는 단일 테마로 시작. 후일 확장 가능 |
| 3 | R3 부분 도입 범위 | 검색 화면만 / 검색+건설 메뉴 / 미도입 | 검색 화면만 | 디바운스가 필요한 최소 범위에서 시작 |
| 4 | 기술 트리 UI 시스템 | UI Toolkit (Painter2D) / UGUI (LineRenderer) | UI Toolkit | Painter2D BezierCurveTo로 곡선 연결 가능. 더 깔끔한 구현 |
| 5 | 월드 UI의 UGUI 아키텍처 | Simple MonoBehaviour / MV(R)P + VContainer | Simple MonoBehaviour | 월드 UI는 단순 표시용. DI/Rx 과잉 |
| 6 | 프로토타입 검증 범위 | 설정 화면 1개 / 설정+HUD / 전체 Phase 1 | Phase 1 전체 | 기반 구축 + EventSystem 공존까지 검증해야 의미 있음 |

---

## 9. 관련 문서

- [UI Toolkit vs UGUI 최종 판정 매트릭스](../research/uitoolkit-vs-ugui-decision-matrix.md)
- [UGUI vs UI Toolkit 비교 회고](./ugui-vs-uitoolkit-retrospective.md)
- [UI Toolkit + UGUI 하이브리드 전략](../patterns/uitoolkit-ugui-hybrid-strategy.md)
- [기술 스택 결정 종합 (UGUI)](../research/tech-stack-decisions.md)
- [UI Toolkit 경량 스택 코드 리뷰](./12-uitoolkit-lightweight-review.md)

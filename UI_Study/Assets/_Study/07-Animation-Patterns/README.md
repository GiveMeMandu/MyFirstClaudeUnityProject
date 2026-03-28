# 07 - Animation Patterns

DOTween 기반 UI 애니메이션 패턴 학습 모듈.

## Tech Stack
- MV(R)P + VContainer + R3 + DOTween

## Scenes

### 01-ButtonEffects
4가지 버튼 마이크로-인터랙션 데모:
- **Scale Punch** - `DOPunchScale`으로 "톡" 치는 스케일 효과
- **Color Flash** - `DOColor`로 색상 플래시 (Yoyo 루프)
- **Shake** - `DOShakeAnchorPos`로 위치 흔들기
- **Bounce** - `DOScale` 시퀀스 (축소 -> 오버슈트 바운스 복귀)

각 버튼 아래에 클릭 카운터 텍스트 표시.

### 02-StaggerList
8개 리스트 아이템의 시차(stagger) 애니메이션:
- **Show**: `DOAnchorPosX` 슬라이드 인 + `DOFade`, 아이템당 0.08초 딜레이
- **Hide**: 역순 애니메이션 (슬라이드 아웃 + 페이드)
- Speed 슬라이더로 애니메이션 속도 조절 (0.5x ~ 2.0x)

### 03-PanelTransition
두 패널 사이의 4가지 전환 트랜지션:
- **Fade** - 크로스 페이드
- **Slide Left** - 왼쪽으로 슬라이드 아웃, 오른쪽에서 슬라이드 인
- **Scale Pop In** - 축소 사라짐 + 팝업 등장 (OutBack 이징)
- **Flip Y** - Y축 기준 카드 뒤집기 효과

## Scene Setup Guide

### 01-ButtonEffects
1. Canvas (Screen Space - Overlay) 생성
2. `ButtonEffectsLifetimeScope` 추가
3. 4개 Button + 4개 TextMeshProUGUI(카운터) 배치
4. `ButtonEffectsView` 컴포넌트에 버튼과 텍스트 연결

### 02-StaggerList
1. Canvas 생성
2. `StaggerListLifetimeScope` 추가
3. 8개 Panel (CanvasGroup 포함) + Toggle Button + Speed Slider(0.5~2.0) 배치
4. `StaggerListView` 컴포넌트에 아이템 리스트, 버튼, 슬라이더 연결
5. Slider: Min=0.5, Max=2.0, Value=1.0

### 03-PanelTransition
1. Canvas 생성
2. `PanelTransitionLifetimeScope` 추가
3. Panel A, Panel B (서로 다른 색상/내용) + 4개 전환 버튼 배치
4. `PanelTransitionView` 컴포넌트에 패널과 버튼 연결

## Architecture
```
View      — SerializeField + DOTween 애니메이션 메서드 (OnDestroy에서 DOKill)
Presenter — R3로 이벤트 바인딩, View 메서드 호출 (IInitializable, IDisposable)
Model     — ReactiveProperty 상태 관리 (Scene 2만 해당)
```

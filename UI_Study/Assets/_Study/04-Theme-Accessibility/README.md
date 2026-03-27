# uPalette 테마 + 접근성 학습 예제

## 개요

테마 관리(라이트/다크/색약)와 접근성(폰트 스케일링, 색약 모드)을 MV(R)P 패턴으로 구현.

## 구조

```
Scripts/
├── Services/
│   ├── ThemeService.cs          — 테마 전환 + 색상 팔레트
│   └── AccessibilityService.cs  — 폰트 스케일 + 색약 모드
├── Views/
│   └── ThemeDemoView.cs         — 테마/접근성 설정 UI
├── Presenters/
│   └── ThemeAccessibilityPresenter.cs — 서비스 ↔ 뷰 바인딩
└── LifetimeScopes/
    └── ThemeDemoLifetimeScope.cs
```

## 핵심 패턴

### ThemeService — ReactiveProperty로 테마 상태 관리
```csharp
// 테마 변경 시 모든 구독자에게 자동 전파
themeService.CurrentTheme.Subscribe(theme => ApplyColors(theme));
themeService.CycleTheme(); // Light → Dark → ColorBlind
```

### 폰트 스케일링 — transform.localScale 방식
```csharp
// TMP의 fontSize를 직접 변경하면 레이아웃이 깨질 수 있음
// localScale로 스케일하면 레이아웃 유지
text.transform.localScale = Vector3.one * fontScale;
```

### 색약 모드 — 대체 팔레트
- uPalette 사용 시: 색약용 팔레트 에셋을 별도로 만들고 전환
- 현재 데모: ThemeService 내부에서 색상값 분기

## 학습 포인트

1. 색상을 하드코딩하지 않고 서비스를 통해 중앙 관리
2. ReactiveProperty로 테마 변화가 모든 UI에 자동 반영
3. uPalette 도입 시 ThemeService의 색상 로직을 팔레트 에셋으로 교체
4. 접근성은 최소 폰트 스케일링 + 색약 모드로 시작

## Project_Sun 적용 시 고려사항

- ThemeService를 Root LifetimeScope에 Singleton으로 등록
- uPalette의 Palette 에셋으로 색상 관리를 교체
- PlayerPrefs로 사용자 설정 영속화
- 시스템 접근성 설정 감지: AccessibilitySettings.fontScaleChanged

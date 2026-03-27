# uPalette 테마 + 접근성 학습 계획

- **작성일**: 2026-03-28
- **전제**: 01~03 완료
- **목표**: uPalette로 테마 관리(색상 중앙화, 런타임 전환)와 기본 접근성(폰트 스케일링, 색약 모드) 구현
- **예상 단계**: 2개

---

## 학습 단계

### Step 1: uPalette 테마 관리

- **목표**: uPalette로 색상 팔레트를 중앙 관리하고 런타임 테마 전환
- **핵심 개념**: Palette 에셋, Entry 키 참조, 런타임 Apply
- **파일 목록**:
  - `Scripts/Services/ThemeService.cs` — 테마 전환 서비스
  - `Scripts/Views/ThemeDemoView.cs` — 테마 적용 데모 UI
  - `Scripts/Presenters/ThemeDemoPresenter.cs`
  - `Scripts/LifetimeScopes/ThemeDemoLifetimeScope.cs`
- **수락 기준**:
  - [ ] 컴파일 통과
  - [ ] 버튼 클릭으로 라이트/다크 테마 전환
  - [ ] 모든 UI 요소가 팔레트 키로 색상 참조

### Step 2: 접근성 — 폰트 스케일링 + 색약 모드

- **목표**: 폰트 크기 조절과 색약 대체 팔레트 적용
- **핵심 개념**: AccessibilitySettings, 폰트 스케일 팩터, 색약 팔레트
- **파일 목록**:
  - `Scripts/Services/AccessibilityService.cs` — 폰트 스케일 + 색약 모드
  - `Scripts/Views/AccessibilitySettingsView.cs` — 설정 UI
  - `Scripts/Presenters/AccessibilityPresenter.cs`
- **수락 기준**:
  - [ ] 컴파일 통과
  - [ ] 슬라이더로 폰트 크기 조절
  - [ ] 색약 모드 토글 시 팔레트 전환

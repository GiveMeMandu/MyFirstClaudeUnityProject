# Addressables + SpriteAtlas 에셋 관리 학습 계획

- **작성일**: 2026-03-28
- **전제**: 01-MVRP-Foundation, 02-Screen-Navigation 완료
- **목표**: UI 에셋을 Addressables로 로드/해제하고, SpriteAtlas V2로 드로우콜을 최적화한다
- **예상 단계**: 3개

---

## 학습 단계

### Step 1: Addressables 기초 — 비동기 에셋 로드/해제

- **목표**: Addressables로 스프라이트와 프리팹을 비동기 로드하고, 수명 관리
- **핵심 개념**: AssetReference, LoadAssetAsync, Release, AsyncOperationHandle
- **파일 목록**:
  - `Scripts/Services/AddressableAssetService.cs` — 에셋 로드/해제 래핑
  - `Scripts/Views/AssetDemoView.cs` — 로드된 스프라이트 표시
  - `Scripts/Presenters/AssetDemoPresenter.cs` — 로드 요청/해제 관리
  - `Scripts/LifetimeScopes/AssetDemoLifetimeScope.cs`
- **수락 기준**:
  - [ ] 컴파일 통과
  - [ ] 버튼 클릭 → Addressables로 스프라이트 로드 → Image에 표시
  - [ ] 해제 버튼 → Release 호출 → 메모리 반환

### Step 2: SpriteAtlas V2 — 아틀라스 그루핑

- **목표**: SpriteAtlas로 UI 스프라이트를 그룹화하고 Addressables와 연동
- **핵심 개념**: SpriteAtlas 생성, Addressable 마킹, 아틀라스 단위 로드
- **파일 목록**:
  - `Scripts/Views/AtlasGalleryView.cs` — 아틀라스 내 스프라이트 갤러리
  - `Scripts/Presenters/AtlasGalleryPresenter.cs` — 아틀라스 로드/스프라이트 전환
- **수락 기준**:
  - [ ] 컴파일 통과
  - [ ] SpriteAtlas를 Addressable로 로드하여 개별 스프라이트 접근
  - [ ] 아틀라스 해제 시 모든 스프라이트 메모리 반환

### Step 3: 화면 전환과 에셋 수명 연동

- **목표**: 화면 진입 시 에셋 로드, 이탈 시 해제하는 패턴
- **핵심 개념**: IDisposable에서 Release, LifetimeScope와 에셋 수명 동기화
- **파일 목록**:
  - `Scripts/Services/ScopedAssetLoader.cs` — Dispose 시 자동 Release
  - `Scripts/Presenters/ScopedAssetPresenter.cs` — 화면 수명과 에셋 연동
- **수락 기준**:
  - [ ] 컴파일 통과
  - [ ] 화면 Push → 에셋 로드, Pop → 자동 Release
  - [ ] Dispose에서 모든 AsyncOperationHandle 해제 확인

---

## 완료 후 다음 단계
- [ ] 4단계 학습 계획: uPalette 테마 + 접근성

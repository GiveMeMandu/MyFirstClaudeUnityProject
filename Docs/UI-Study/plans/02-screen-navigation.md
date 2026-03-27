# UnityScreenNavigator 화면 전환 학습 계획

- **작성일**: 2026-03-28
- **기반 리서치**: [tech-stack-decisions.md](../research/tech-stack-decisions.md)
- **전제**: 01-MVRP-Foundation 완료
- **목표**: UnityScreenNavigator로 Page/Modal/Sheet 화면 전환을 구현하고, VContainer LifetimeScope 계층과 통합한다
- **예상 단계**: 4개

---

## 사전 준비

### 필요 패키지 (이미 설치됨)

| 패키지 | 상태 |
|---|---|
| UnityScreenNavigator | manifest.json 추가 완료 |
| VContainer | 설치됨 |
| R3 | 설치됨 |
| UniTask | 설치됨 |
| DOTween | 설치됨 |

### 프로젝트 구조

```
UI_Study/Assets/
├── _Study/
│   ├── 02-Screen-Navigation/
│   │   ├── Scripts/
│   │   │   ├── Pages/
│   │   │   ├── Modals/
│   │   │   ├── Sheets/
│   │   │   ├── Services/
│   │   │   └── LifetimeScopes/
│   │   ├── Prefabs/
│   │   │   ├── Pages/
│   │   │   ├── Modals/
│   │   │   └── Sheets/
│   │   ├── Scenes/
│   │   └── README.md
```

---

## 학습 단계

### Step 1: Page 기본 — Push/Pop 네비게이션

- **목표**: PageContainer를 사용한 기본 화면 스택 관리
- **핵심 개념**: PageContainer, Push, Pop, 뒤로 가기, 전환 애니메이션
- **예제**: 메인 화면 → 상세 화면 → 설정 화면 (3단계 스택)
- **파일 목록**:
  - `Scripts/Pages/MainPageView.cs` — 메인 화면 (상세로 이동 버튼)
  - `Scripts/Pages/DetailPageView.cs` — 상세 화면 (설정으로 이동 + 뒤로 가기)
  - `Scripts/Pages/SettingsPageView.cs` — 설정 화면 (뒤로 가기)
  - `Scripts/Services/NavigationService.cs` — PageContainer 래핑
  - `Scripts/LifetimeScopes/NavigationLifetimeScope.cs`
  - `Prefabs/Pages/MainPage.prefab`
  - `Prefabs/Pages/DetailPage.prefab`
  - `Prefabs/Pages/SettingsPage.prefab`
  - `Scenes/01-Page-Navigation.unity`
- **수락 기준**:
  - [ ] 컴파일 통과
  - [ ] Main → Detail → Settings Push 동작
  - [ ] 뒤로 가기 (Pop) 동작
  - [ ] 전환 애니메이션 동작

### Step 2: Modal 다이얼로그 — 팝업 스택

- **목표**: ModalContainer로 모달 다이얼로그 관리
- **핵심 개념**: ModalContainer, 배경 차단, 모달 스택
- **예제**: Step 1의 상세 화면에서 확인 모달 띄우기
- **파일 목록**:
  - `Scripts/Modals/ConfirmModalView.cs` — 확인/취소 모달
  - `Scripts/Modals/InfoModalView.cs` — 정보 표시 모달
  - `Scripts/Services/ModalService.cs` — ModalContainer 래핑 + await 패턴
  - `Prefabs/Modals/ConfirmModal.prefab`
  - `Prefabs/Modals/InfoModal.prefab`
  - `Scenes/02-Modal-Dialog.unity`
- **수락 기준**:
  - [ ] 컴파일 통과
  - [ ] 모달이 뒤 화면 입력 차단
  - [ ] `bool result = await modalService.ShowConfirmAsync()` 동작
  - [ ] 모달 위에 모달 스택 가능

### Step 3: Sheet 탭 — 카테고리 전환

- **목표**: SheetContainer로 탭 기반 콘텐츠 전환
- **핵심 개념**: SheetContainer, Show, 탭 동기화
- **예제**: 인벤토리 화면의 무기/방어구/소비 탭
- **파일 목록**:
  - `Scripts/Sheets/WeaponSheetView.cs`
  - `Scripts/Sheets/ArmorSheetView.cs`
  - `Scripts/Sheets/ConsumableSheetView.cs`
  - `Scripts/Services/TabService.cs` — SheetContainer + 탭 바 동기화
  - `Prefabs/Sheets/WeaponSheet.prefab`
  - `Prefabs/Sheets/ArmorSheet.prefab`
  - `Prefabs/Sheets/ConsumableSheet.prefab`
  - `Scenes/03-Sheet-Tabs.unity`
- **수락 기준**:
  - [ ] 컴파일 통과
  - [ ] 탭 클릭 → 시트 전환 동작
  - [ ] 히스토리 없음 (뒤로 가기 불필요)

### Step 4: VContainer LifetimeScope 통합 — 동적 스코프

- **목표**: 화면 전환 시 LifetimeScope 자식 스코프 생성/파괴
- **핵심 개념**: CreateChildFromPrefab, EnqueueParent, IAssetLoader 커스텀
- **예제**: Page 전환 시 해당 화면의 Presenter가 자식 스코프에서 생성되고, Pop 시 파괴
- **파일 목록**:
  - `Scripts/Services/VContainerPageLoader.cs` — IAssetLoader + VContainer 통합
  - `Scripts/LifetimeScopes/DetailPageLifetimeScope.cs` — 상세 화면 전용 스코프
  - `Scripts/Presenters/DetailPagePresenter.cs` — 자식 스코프에서 주입
  - `Scenes/04-Scoped-Navigation.unity`
- **수락 기준**:
  - [ ] 컴파일 통과
  - [ ] Page Push 시 자식 LifetimeScope 생성
  - [ ] Page Pop 시 자식 LifetimeScope Dispose
  - [ ] Presenter의 IDisposable.Dispose() 호출 확인 (로그)

---

## 검증 체크리스트

### 아키텍처
- [ ] 모든 화면이 Page/Modal/Sheet 분류에 따라 올바른 Container 사용
- [ ] VContainer 자식 스코프가 화면 수명과 동기화
- [ ] NavigationService/ModalService가 VContainer로 주입

### Canvas 레이어
- [ ] Pages: Sort Order 20
- [ ] Modals: Sort Order 30
- [ ] Overlay: Sort Order 40

---

## 완료 후 다음 단계

- [ ] `/ui-review 02-Screen-Navigation`으로 코드 리뷰
- [ ] 3단계 학습 계획: Addressables + SpriteAtlas 에셋 관리
- [ ] 4단계 학습 계획: uPalette 테마 + 접근성

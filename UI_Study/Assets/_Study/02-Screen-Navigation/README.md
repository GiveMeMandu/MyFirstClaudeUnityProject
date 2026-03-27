# Screen Navigation 학습 예제

## 개요

UnityScreenNavigator를 사용한 Page/Modal/Sheet 화면 전환 패턴과 VContainer LifetimeScope 동적 스코프 통합.

## 구조

```
Scripts/
├── Pages/
│   ├── MainPageView.cs         — Step 1: 메인 화면 (USN Page 상속)
│   ├── DetailPageView.cs       — Step 1: 상세 화면
│   └── SettingsPageView.cs     — Step 1: 설정 화면
├── Modals/
│   ├── ConfirmModalView.cs     — Step 2: 확인/취소 모달 (USN Modal 상속)
│   └── InfoModalView.cs        — Step 2: 정보 모달
├── Sheets/
│   ├── WeaponSheetView.cs      — Step 3: 무기 탭 (USN Sheet 상속)
│   ├── ArmorSheetView.cs       — Step 3: 방어구 탭
│   └── ConsumableSheetView.cs  — Step 3: 소비 탭
├── Presenters/
│   └── DetailPagePresenter.cs  — Step 4: 자식 스코프 Presenter
├── Services/
│   ├── NavigationService.cs       — PageContainer 래핑
│   ├── NavigationBootstrapper.cs  — 초기 Page Push + 이벤트 연결
│   ├── ModalService.cs            — ModalContainer + UniTask await
│   ├── TabService.cs              — SheetContainer + 탭 동기화
│   └── VContainerPageLoader.cs    — 동적 자식 LifetimeScope 생성
└── LifetimeScopes/
    ├── NavigationLifetimeScope.cs      — 기본 씬 스코프
    └── DetailPageLifetimeScope.cs      — 상세 페이지 전용 자식 스코프
```

## 핵심 패턴

### UnityScreenNavigator 화면 분류
| 타입 | Container | 용도 | 스택 |
|---|---|---|---|
| Page | PageContainer | 주요 화면 전환 | Push/Pop 히스토리 |
| Modal | ModalContainer | 팝업/다이얼로그 | Push/Pop, 배경 차단 |
| Sheet | SheetContainer | 탭 전환 | 히스토리 없음 |

### VContainer 동적 스코프
```csharp
// Page Push 시 자식 스코프 생성
_childScope = _parentScope.CreateChild(builder =>
{
    builder.RegisterComponent(detailPage);
    builder.RegisterEntryPoint<DetailPagePresenter>();
});

// Pop 시 Dispose → Presenter.Dispose() 자동 호출
_childScope?.Dispose();
```

### ModalService await 패턴
```csharp
bool confirmed = await _modalService.ShowConfirmAsync("정말 삭제?");
```

## 학습 포인트

1. USN의 Page/Modal/Sheet는 MonoBehaviour를 상속하는 특수 클래스
2. resourceKey는 Resources 폴더 내 프리팹 이름과 매칭
3. ModalContainer는 자동으로 배경 차단 처리
4. VContainer의 CreateChild로 화면 수명에 맞는 DI 스코프 구현
5. 자식 스코프 Dispose 시 등록된 IDisposable 모두 정리됨

## Project_Sun 적용 시 고려사항

- Resources 대신 Addressables로 프리팹 로딩 시 커스텀 IAssetLoader 구현 필요
- Canvas Sort Order: Pages(20), Modals(30), Toast(35), Overlay(40)
- 화면별 LifetimeScope 프리팹을 만들고 CreateChildFromPrefab 사용
- UnityScreenNavigator의 전환 애니메이션은 ScriptableObject로 디자이너 커스터마이즈 가능

# Addressables + SpriteAtlas 학습 예제

## 개요

Addressables로 UI 에셋을 비동기 로드/해제하고, SpriteAtlas V2로 드로우콜을 최적화하는 패턴.

## 구조

```
Scripts/
├── Services/
│   ├── AddressableAssetService.cs  — 범용 에셋 로드/해제 서비스
│   └── ScopedAssetLoader.cs        — Dispose 시 자동 Release (스코프 연동)
├── Views/
│   ├── AssetDemoView.cs            — 스프라이트 표시 + 로드/해제 버튼
│   └── AtlasGalleryView.cs         — 아틀라스 스프라이트 갤러리
├── Presenters/
│   ├── AssetDemoPresenter.cs       — 수동 로드/해제 데모
│   ├── AtlasGalleryPresenter.cs    — SpriteAtlas 로드 + 순회
│   └── ScopedAssetPresenter.cs     — 화면 수명 연동 자동 해제
└── LifetimeScopes/
    └── AssetDemoLifetimeScope.cs
```

## 핵심 패턴

### ScopedAssetLoader — 화면 수명과 에셋 수명 동기화
```csharp
// VContainer에 Scoped로 등록
builder.Register<ScopedAssetLoader>(Lifetime.Scoped);

// Presenter에서 사용
var sprite = await _assetLoader.LoadAsync<Sprite>("key", ct);

// LifetimeScope Dispose 시 자동으로 모든 핸들 Release
```

### SpriteAtlas Addressable 로드
```csharp
var handle = Addressables.LoadAssetAsync<SpriteAtlas>("AtlasKey");
var atlas = await handle.Task;
var sprites = new Sprite[atlas.spriteCount];
atlas.GetSprites(sprites);
// 해제 시 Addressables.Release(handle) → 모든 스프라이트 메모리 반환
```

## 학습 포인트

1. Addressables는 에셋을 주소(key)로 비동기 로드 — Resources.Load 대체
2. AsyncOperationHandle을 반드시 Release해야 메모리 누수 방지
3. SpriteAtlas를 Addressable로 마킹하면 아틀라스 단위로 로드/해제
4. ScopedAssetLoader 패턴으로 화면 수명과 에셋 수명을 자동 동기화
5. 개별 스프라이트가 아닌 SpriteAtlas를 Addressable로 마킹해야 함

## Project_Sun 적용 시 고려사항

- Atlas 그루핑: Atlas_Common(항상), Atlas_HUD, Atlas_BuildPanel 등 화면별
- UnityScreenNavigator의 IAssetLoader를 Addressables로 교체
- 대량 아이템 아이콘은 카테고리별 SpriteAtlas 분리

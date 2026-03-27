using System;
using R3;
using UIStudy.Assets.Services;
using UIStudy.Assets.Views;
using UnityEngine;
using VContainer.Unity;

namespace UIStudy.Assets.Presenters
{
    /// <summary>
    /// 스코프 연동 에셋 Presenter.
    /// ScopedAssetLoader를 사용하여 화면 수명과 에셋 수명을 동기화.
    /// Dispose 시 로드된 모든 에셋이 자동 해제됨.
    /// </summary>
    public class ScopedAssetPresenter : IInitializable, IDisposable
    {
        private readonly AssetDemoView _view;
        private readonly ScopedAssetLoader _assetLoader;
        private readonly CompositeDisposable _disposables = new();

        private const string SpriteAddress = "DemoSprite";

        public ScopedAssetPresenter(AssetDemoView view, ScopedAssetLoader assetLoader)
        {
            _view = view;
            _assetLoader = assetLoader;
        }

        public void Initialize()
        {
            Debug.Log("[ScopedAssetPresenter] Initialize — 화면 진입, 에셋 로드 대기");
            _view.SetStatus("Scoped Mode — Load 클릭 시 로드, 화면 이탈 시 자동 해제");
            _view.SetReleaseInteractable(false);

            _view.OnLoadClick
                .SubscribeAwait(async (_, ct) =>
                {
                    _view.SetStatus("Loading (scoped)...");
                    var sprite = await _assetLoader.LoadAsync<Sprite>(SpriteAddress, ct);
                    if (sprite != null)
                    {
                        _view.SetSprite(sprite);
                        _view.SetStatus($"Loaded: {sprite.name} (자동 해제 대기)");
                    }
                    else
                    {
                        _view.SetStatus("Load failed — Addressable에 DemoSprite를 등록하세요");
                    }
                }, AwaitOperation.Drop, configureAwait: false)
                .AddTo(_disposables);
        }

        public void Dispose()
        {
            Debug.Log("[ScopedAssetPresenter] Dispose — 화면 이탈, 에셋 자동 해제");
            _disposables.Dispose();
            // ScopedAssetLoader.Dispose()는 VContainer가 별도로 호출
        }
    }
}

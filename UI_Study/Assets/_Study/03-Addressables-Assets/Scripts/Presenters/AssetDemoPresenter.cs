using System;
using R3;
using UIStudy.Assets.Services;
using UIStudy.Assets.Views;
using UnityEngine;
using VContainer.Unity;

namespace UIStudy.Assets.Presenters
{
    /// <summary>
    /// 에셋 로드 데모 Presenter.
    /// 버튼 클릭으로 Addressables 에셋을 로드/해제.
    /// </summary>
    public class AssetDemoPresenter : IInitializable, IDisposable
    {
        private readonly AssetDemoView _view;
        private readonly AddressableAssetService _assetService;
        private readonly CompositeDisposable _disposables = new();
        private Sprite _loadedSprite;

        // Addressables에 등록된 에셋 주소 (사용자가 실제 에셋 등록 후 변경 필요)
        private const string SpriteAddress = "DemoSprite";

        public AssetDemoPresenter(AssetDemoView view, AddressableAssetService assetService)
        {
            _view = view;
            _assetService = assetService;
        }

        public void Initialize()
        {
            _view.SetStatus("Ready — Load 버튼을 클릭하세요");
            _view.SetReleaseInteractable(false);
            _view.SetSprite(null);

            _view.OnLoadClick
                .SubscribeAwait(async (_, ct) =>
                {
                    _view.SetStatus("Loading...");
                    try
                    {
                        _loadedSprite = await _assetService.LoadAsync<Sprite>(SpriteAddress, ct);
                        if (_loadedSprite != null)
                        {
                            _view.SetSprite(_loadedSprite);
                            _view.SetStatus($"Loaded: {_loadedSprite.name}");
                            _view.SetReleaseInteractable(true);
                        }
                        else
                        {
                            _view.SetStatus("Load failed — Addressable에 DemoSprite를 등록하세요");
                        }
                    }
                    catch (Exception ex)
                    {
                        _view.SetStatus($"Error: {ex.Message}");
                    }
                }, AwaitOperation.Drop, configureAwait: false)
                .AddTo(_disposables);

            _view.OnReleaseClick
                .Subscribe(_ =>
                {
                    _assetService.Dispose();
                    _loadedSprite = null;
                    _view.SetSprite(null);
                    _view.SetStatus("Released — 메모리 반환 완료");
                    _view.SetReleaseInteractable(false);
                })
                .AddTo(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }
}

using System;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;
using UIStudy.Assets.Views;
using VContainer.Unity;

namespace UIStudy.Assets.Presenters
{
    /// <summary>
    /// 아틀라스 갤러리 Presenter.
    /// SpriteAtlas를 Addressables로 로드하고, 내부 스프라이트를 순회.
    /// </summary>
    public class AtlasGalleryPresenter : IInitializable, IDisposable
    {
        private readonly AtlasGalleryView _view;
        private readonly CompositeDisposable _disposables = new();

        private AsyncOperationHandle<SpriteAtlas> _atlasHandle;
        private Sprite[] _sprites = Array.Empty<Sprite>();
        private int _currentIndex;

        // Addressables에 등록된 SpriteAtlas 주소
        private const string AtlasAddress = "DemoAtlas";

        public AtlasGalleryPresenter(AtlasGalleryView view)
        {
            _view = view;
        }

        public void Initialize()
        {
            _view.SetName("Loading atlas...");

            _view.OnNextClick
                .Subscribe(_ => Navigate(1))
                .AddTo(_disposables);

            _view.OnPrevClick
                .Subscribe(_ => Navigate(-1))
                .AddTo(_disposables);

            LoadAtlasAsync().Forget();
        }

        private async UniTaskVoid LoadAtlasAsync()
        {
            try
            {
                _atlasHandle = Addressables.LoadAssetAsync<SpriteAtlas>(AtlasAddress);
                var atlas = await _atlasHandle.Task;

                if (atlas == null)
                {
                    _view.SetName("Atlas not found — Addressable에 DemoAtlas를 등록하세요");
                    return;
                }

                // 아틀라스에서 모든 스프라이트 추출
                _sprites = new Sprite[atlas.spriteCount];
                atlas.GetSprites(_sprites);

                if (_sprites.Length > 0)
                {
                    _currentIndex = 0;
                    ShowCurrent();
                }
                else
                {
                    _view.SetName("Atlas is empty");
                }
            }
            catch (Exception ex)
            {
                _view.SetName($"Error: {ex.Message}");
            }
        }

        private void Navigate(int direction)
        {
            if (_sprites.Length == 0) return;
            _currentIndex = (_currentIndex + direction + _sprites.Length) % _sprites.Length;
            ShowCurrent();
        }

        private void ShowCurrent()
        {
            var sprite = _sprites[_currentIndex];
            _view.SetSprite(sprite);
            _view.SetName(sprite.name);
            _view.SetIndex($"{_currentIndex + 1} / {_sprites.Length}");
        }

        public void Dispose()
        {
            // SpriteAtlas 핸들 해제 — 모든 스프라이트 메모리 반환
            if (_atlasHandle.IsValid())
            {
                Addressables.Release(_atlasHandle);
                Debug.Log("[AtlasGallery] SpriteAtlas released");
            }
            _disposables.Dispose();
        }
    }
}

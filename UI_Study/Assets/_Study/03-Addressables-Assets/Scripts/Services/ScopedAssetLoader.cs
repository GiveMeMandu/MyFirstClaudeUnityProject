using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UIStudy.Assets.Services
{
    /// <summary>
    /// 스코프 기반 에셋 로더 — Dispose 시 로드된 모든 에셋 자동 해제.
    /// VContainer의 Lifetime.Scoped로 등록하면 LifetimeScope 파괴 시 자동 정리.
    ///
    /// 학습 포인트:
    /// - 화면 Push → ScopedAssetLoader 생성 → 에셋 로드
    /// - 화면 Pop → LifetimeScope Dispose → ScopedAssetLoader.Dispose() → 에셋 자동 Release
    /// </summary>
    public class ScopedAssetLoader : IDisposable
    {
        private readonly List<AsyncOperationHandle> _handles = new();
        private bool _disposed;

        public async UniTask<T> LoadAsync<T>(string key, CancellationToken ct = default) where T : class
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ScopedAssetLoader));

            var handle = Addressables.LoadAssetAsync<T>(key);
            _handles.Add(handle);

            var result = await handle.Task.AsUniTask().AttachExternalCancellation(ct);
            return handle.Status == AsyncOperationStatus.Succeeded ? result : null;
        }

        public async UniTask<GameObject> InstantiateAsync(string key,
            Transform parent = null, CancellationToken ct = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ScopedAssetLoader));

            var handle = Addressables.InstantiateAsync(key, parent);
            _handles.Add(handle);

            return await handle.Task.AsUniTask().AttachExternalCancellation(ct);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var handle in _handles)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }

            Debug.Log($"[ScopedAssetLoader] Disposed — {_handles.Count} handles released");
            _handles.Clear();
        }
    }
}

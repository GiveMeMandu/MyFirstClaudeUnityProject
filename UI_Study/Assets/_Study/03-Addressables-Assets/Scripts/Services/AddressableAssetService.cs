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
    /// Addressables 에셋 로드/해제를 관리하는 서비스.
    /// 로드된 핸들을 추적하여 Dispose 시 일괄 해제.
    /// </summary>
    public class AddressableAssetService : IDisposable
    {
        private readonly List<AsyncOperationHandle> _handles = new();

        /// <summary>
        /// 주소(key)로 에셋을 비동기 로드.
        /// </summary>
        public async UniTask<T> LoadAsync<T>(string key, CancellationToken ct = default) where T : class
        {
            var handle = Addressables.LoadAssetAsync<T>(key);
            _handles.Add(handle);

            var result = await handle.Task.AsUniTask().AttachExternalCancellation(ct);
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[AddressableAssetService] Failed to load: {key}");
                return null;
            }

            Debug.Log($"[AddressableAssetService] Loaded: {key}");
            return result;
        }

        /// <summary>
        /// AssetReference로 에셋을 비동기 로드.
        /// </summary>
        public async UniTask<T> LoadAsync<T>(AssetReference reference, CancellationToken ct = default) where T : class
        {
            var handle = reference.LoadAssetAsync<T>();
            _handles.Add(handle);

            var result = await handle.Task.AsUniTask().AttachExternalCancellation(ct);
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[AddressableAssetService] Failed to load AssetReference");
                return null;
            }

            return result;
        }

        /// <summary>
        /// 특정 핸들 해제.
        /// </summary>
        public void Release(AsyncOperationHandle handle)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
                _handles.Remove(handle);
                Debug.Log("[AddressableAssetService] Released handle");
            }
        }

        /// <summary>
        /// 모든 로드된 핸들 일괄 해제.
        /// </summary>
        public void Dispose()
        {
            foreach (var handle in _handles)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }
            _handles.Clear();
            Debug.Log($"[AddressableAssetService] Disposed — all handles released");
        }
    }
}

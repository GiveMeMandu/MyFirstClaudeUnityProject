using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UIStudy.Advanced.Models;
using UIStudy.Advanced.Views;
using UnityEngine;

namespace UIStudy.Advanced.Services
{
    /// <summary>
    /// 토스트 알림 큐 서비스 — 순차적으로 하나씩 표시.
    /// 큐 최대 5개 제한, 초과 시 가장 오래된 것 폐기.
    /// </summary>
    public class ToastService : IDisposable
    {
        private readonly ToastView _toastView;
        private readonly Queue<ToastData> _queue = new();
        private readonly CancellationTokenSource _cts = new();
        private bool _isShowing;

        private const int MaxQueueSize = 5;

        public ToastService(ToastView toastView)
        {
            _toastView = toastView;
            ProcessQueueAsync(_cts.Token).Forget();
        }

        /// <summary>
        /// 토스트 큐에 추가.
        /// </summary>
        public void Enqueue(string message, ToastType type = ToastType.Info, float duration = 2.5f)
        {
            if (_queue.Count >= MaxQueueSize)
                _queue.Dequeue(); // 가장 오래된 것 폐기

            _queue.Enqueue(new ToastData(message, type, duration));
        }

        private async UniTaskVoid ProcessQueueAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (_queue.Count > 0 && !_isShowing)
                {
                    _isShowing = true;
                    var data = _queue.Dequeue();

                    try
                    {
                        await _toastView.ShowAsync(data, ct);
                    }
                    catch (OperationCanceledException) { break; }

                    _isShowing = false;
                }

                try { await UniTask.NextFrame(ct); }
                catch (OperationCanceledException) { break; }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}

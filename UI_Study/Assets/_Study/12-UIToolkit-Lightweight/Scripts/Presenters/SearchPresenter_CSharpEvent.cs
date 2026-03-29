using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 8: C# Event + UniTask.Delay 수동 디바운스.
    ///
    /// 한계 시연:
    /// - 빠른 타이핑 시 여러 비동기 검색이 동시 실행됨
    /// - 이전 검색 결과가 최신 결과를 덮어쓸 수 있는 레이스 컨디션
    /// - CancellationTokenSource 수동 관리 필요
    /// - 스로틀도 별도 구현 필요
    /// </summary>
    public class SearchPresenter_CSharpEvent : IDisposable
    {
        private readonly SearchPanelView _view;
        private readonly List<string> _allItems;

        private CancellationTokenSource _debounceCts;
        private int _searchCount;
        private int _buildCount;
        private DateTime _lastBuildTime = DateTime.MinValue;
        private static readonly TimeSpan ThrottleInterval = TimeSpan.FromMilliseconds(1000);

        public SearchPresenter_CSharpEvent(SearchPanelView view, List<string> items)
        {
            _view = view;
            _allItems = items;

            _view.OnSearchTextChanged += HandleSearchChanged;
            _view.OnBuildClicked      += HandleBuildClicked;
        }

        public void Initialize()
        {
            _view.SetResults(_allItems.Take(50).ToList());
            _view.SetSearchStatus("");
            _view.SetBuildCount(0);
            _view.AppendLog("[C#] Presenter initialized — manual debounce mode");
        }

        /// <summary>
        /// 수동 디바운스: CancellationTokenSource를 매번 재생성하여 이전 검색 취소.
        ///
        /// 문제점:
        /// 1) CTS 수동 Cancel/Dispose 필요 (누수 가능)
        /// 2) 취소 타이밍에 따라 결과 덮어쓰기 가능 (레이스 컨디션)
        /// 3) 코드가 장황하고 에러 prone
        /// </summary>
        private void HandleSearchChanged(string text)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = new CancellationTokenSource();
            _view.SetSearchStatus("Typing...");
            DebounceSearchAsync(text, _debounceCts.Token).Forget();
        }

        private async UniTaskVoid DebounceSearchAsync(string text, CancellationToken ct)
        {
            try
            {
                // 300ms 대기 — 수동 디바운스
                await UniTask.Delay(300, cancellationToken: ct);

                _searchCount++;
                _view.SetSearchStatus($"Searching... (#{_searchCount})");
                _view.AppendLog($"[C#] Search #{_searchCount}: \"{text}\"");

                // 시뮬레이트 비동기 검색 (실제로는 필터링)
                await UniTask.Delay(50, cancellationToken: ct);

                var results = PerformSearch(text);
                _view.SetResults(results);
                _view.SetSearchStatus($"Done (#{_searchCount})");

                Debug.Log($"[CSharpEvent] Search #{_searchCount} complete: \"{text}\" → {results.Count} results");
            }
            catch (OperationCanceledException)
            {
                // 디바운스에 의해 취소됨 — 정상 동작
            }
        }

        /// <summary>
        /// 수동 스로틀: DateTime 비교로 빌드 빈도 제한.
        /// R3 ThrottleFirst에 비해 장황하고 시간 정확도 낮음.
        /// </summary>
        private void HandleBuildClicked()
        {
            var now = DateTime.UtcNow;
            if (now - _lastBuildTime < ThrottleInterval)
            {
                _view.SetBuildStatus("Throttled! Wait...");
                _view.AppendLog("[C#] Build throttled (manual DateTime check)");
                return;
            }

            _lastBuildTime = now;
            _buildCount++;
            _view.SetBuildCount(_buildCount);
            _view.SetBuildStatus($"Build #{_buildCount} executed!");
            _view.AppendLog($"[C#] Build #{_buildCount} OK");
            Debug.Log($"[CSharpEvent] Build #{_buildCount}");
        }

        private List<string> PerformSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return _allItems.Take(50).ToList();

            string lower = query.ToLowerInvariant();
            return _allItems
                .Where(item => item.ToLowerInvariant().Contains(lower))
                .Take(50)
                .ToList();
        }

        public void Dispose()
        {
            _view.OnSearchTextChanged -= HandleSearchChanged;
            _view.OnBuildClicked      -= HandleBuildClicked;

            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = null;
        }
    }
}

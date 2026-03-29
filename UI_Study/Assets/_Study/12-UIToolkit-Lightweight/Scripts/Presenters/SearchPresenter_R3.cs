using System;
using System.Collections.Generic;
using System.Linq;
using R3;
using UnityEngine;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 8: R3 Observable 버전 — Debounce + ThrottleFirst.
    ///
    /// C# Event를 Observable.FromEvent로 변환 후 연산자 체이닝.
    /// 수동 CTS/DateTime 관리 불필요, 선언적이고 레이스 컨디션 없음.
    ///
    /// 비교:
    /// - C# Event: ~30줄 수동 디바운스 + CTS 관리 + 레이스 컨디션 위험
    /// - R3: 3줄 체이닝 (FromEvent → Debounce → Subscribe)
    /// </summary>
    public class SearchPresenter_R3 : IDisposable
    {
        private readonly SearchPanelView _view;
        private readonly List<string> _allItems;
        private readonly CompositeDisposable _disposables = new();

        private int _searchCount;
        private int _buildCount;

        public SearchPresenter_R3(SearchPanelView view, List<string> items)
        {
            _view = view;
            _allItems = items;
        }

        public void Initialize()
        {
            _view.SetResults(_allItems.Take(50).ToList());
            _view.SetSearchStatus("");
            _view.SetBuildCount(0);
            _view.AppendLog("[R3] Presenter initialized — Observable Debounce mode");

            // C# event → R3 Observable: 검색 디바운스
            // FromEvent로 변환 후 Debounce 적용 — 300ms 내 마지막 값만 처리
            Observable.FromEvent<string>(
                    h => _view.OnSearchTextChanged += h,
                    h => _view.OnSearchTextChanged -= h)
                .Do(_ => _view.SetSearchStatus("Typing..."))
                .Debounce(TimeSpan.FromMilliseconds(300))
                .Subscribe(HandleDebouncedSearch)
                .AddTo(_disposables);

            // C# event → R3 Observable: 빌드 스로틀
            // ThrottleFirst로 1초 내 첫 클릭만 처리 — 중복 클릭 자동 무시
            Observable.FromEvent(
                    h => _view.OnBuildClicked += h,
                    h => _view.OnBuildClicked -= h)
                .ThrottleFirst(TimeSpan.FromMilliseconds(1000))
                .Subscribe(HandleThrottledBuild)
                .AddTo(_disposables);
        }

        private void HandleDebouncedSearch(string text)
        {
            _searchCount++;
            _view.AppendLog($"[R3] Search #{_searchCount}: \"{text}\"");

            var results = PerformSearch(text);
            _view.SetResults(results);
            _view.SetSearchStatus($"Done (#{_searchCount})");

            Debug.Log($"[R3] Search #{_searchCount} complete: \"{text}\" → {results.Count} results");
        }

        private void HandleThrottledBuild(Unit _)
        {
            _buildCount++;
            _view.SetBuildCount(_buildCount);
            _view.SetBuildStatus($"Build #{_buildCount} executed!");
            _view.AppendLog($"[R3] Build #{_buildCount} OK (ThrottleFirst)");
            Debug.Log($"[R3] Build #{_buildCount}");
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
            _disposables.Dispose();
        }
    }
}

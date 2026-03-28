using System;
using System.Collections.Generic;
using R3;
using UIStudy.R3Advanced.Models;
using UIStudy.R3Advanced.Views;
using UnityEngine;
using VContainer.Unity;

namespace UIStudy.R3Advanced.Presenters
{
    /// <summary>
    /// 검색 Presenter — Debounce 패턴 데모.
    /// InputField 변경을 300ms 디바운싱하여 검색을 실행한다.
    /// </summary>
    public class SearchPresenter : IInitializable, IDisposable
    {
        private readonly SearchModel _model;
        private readonly SearchView _view;
        private readonly CompositeDisposable _disposables = new();

        public SearchPresenter(SearchModel model, SearchView view)
        {
            _model = model;
            _view = view;
        }

        public void Initialize()
        {
            // 초기 상태: 전체 목록 표시
            DisplayFilteredResults(string.Empty);

            // InputField 변경 → SearchQuery에 즉시 반영
            _view.OnSearchValueChanged
                .Subscribe(query =>
                {
                    _model.SearchQuery.Value = query;

                    // 입력 중이면 "Searching..." 표시
                    if (!string.IsNullOrEmpty(query))
                        _view.SetStatusText("Searching...");
                })
                .AddTo(_disposables);

            // SearchQuery를 300ms 디바운싱 후 필터 실행
            _model.SearchQuery
                .Debounce(TimeSpan.FromMilliseconds(300))
                .Subscribe(query =>
                {
                    DisplayFilteredResults(query);
                    _view.SetStatusText(string.Empty);
                    Debug.Log($"[SearchPresenter] Search executed: \"{query}\"");
                })
                .AddTo(_disposables);
        }

        private void DisplayFilteredResults(string query)
        {
            List<string> results = _model.FilterItems(query);
            _view.SetResultCount($"Results: {results.Count}");
            _view.DisplayResults(results);
        }

        public void Dispose() => _disposables.Dispose();
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 7: 인벤토리 Presenter — Pure C# + IDisposable.
    /// 검색 → 필터 → RefreshItems(), 정렬 → 재정렬 → RefreshItems(),
    /// 선택 → 상세 패널 업데이트.
    /// </summary>
    public class InventoryPresenter : IDisposable
    {
        private readonly InventoryModel _model;
        private readonly InventoryView _view;

        private string _currentSearch = "";
        private SortMode _currentSort = SortMode.Name;
        private List<ItemData> _currentItems = new();

        public InventoryPresenter(InventoryModel model, InventoryView view)
        {
            _model = model;
            _view = view;

            _view.OnSearchChanged  += HandleSearchChanged;
            _view.OnSortChanged    += HandleSortChanged;
            _view.OnItemSelected   += HandleItemSelected;
        }

        public void Initialize()
        {
            RefreshItems();
            _view.ClearDetail();
        }

        private void HandleSearchChanged(string search)
        {
            _currentSearch = search;
            RefreshItems();
        }

        private void HandleSortChanged(int sortIndex)
        {
            _currentSort = (SortMode)sortIndex;
            RefreshItems();
        }

        private void HandleItemSelected(int index)
        {
            if (index >= 0 && index < _currentItems.Count)
            {
                _view.SetDetail(_currentItems[index]);
            }
        }

        private void RefreshItems()
        {
            var filtered = _model.GetFiltered(_currentSearch);
            _currentItems = _model.GetSorted(filtered, _currentSort);
            _view.SetItems(_currentItems);

            Debug.Log($"[InventoryPresenter] Showing {_currentItems.Count} items " +
                      $"(search=\"{_currentSearch}\", sort={_currentSort})");
        }

        public void Dispose()
        {
            _view.OnSearchChanged  -= HandleSearchChanged;
            _view.OnSortChanged    -= HandleSortChanged;
            _view.OnItemSelected   -= HandleItemSelected;
        }
    }
}

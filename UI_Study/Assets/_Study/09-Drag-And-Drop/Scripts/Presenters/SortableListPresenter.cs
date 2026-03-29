using System;
using R3;
using UIStudy.DragDrop.Models;
using UIStudy.DragDrop.Views;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer.Unity;

namespace UIStudy.DragDrop.Presenters
{
    /// <summary>
    /// 정렬 리스트 Presenter — 드래그 리오더 로직 + 모델/뷰 바인딩.
    /// </summary>
    public class SortableListPresenter : IInitializable, IDisposable
    {
        private readonly SortableListModel _model;
        private readonly SortableListView _listView;
        private readonly CompositeDisposable _disposables = new();

        private int _dragFromIndex = -1;
        private int _currentInsertIndex = -1;
        private Transform _listContainer;

        public SortableListPresenter(SortableListModel model, SortableListView listView)
        {
            _model = model;
            _listView = listView;
        }

        public void Initialize()
        {
            _listContainer = _listView.ListContainer;

            // 모델 -> 뷰 바인딩
            _model.Items
                .Subscribe(items => _listView.BindAll(items))
                .AddTo(_disposables);

            // 각 아이템의 드래그 이벤트 바인딩
            foreach (var item in _listView.Items)
            {
                item.OnBeginDragEvent
                    .Subscribe(index =>
                    {
                        _dragFromIndex = index;
                        _currentInsertIndex = index;
                        _listView.SetStatus($"Dragging: {_model.Items.Value[index]}");
                    })
                    .AddTo(_disposables);

                item.OnDragEvent
                    .Subscribe(HandleDrag)
                    .AddTo(_disposables);

                item.OnEndDragEvent
                    .Subscribe(_ => HandleEndDrag(item))
                    .AddTo(_disposables);
            }

            _listView.SetStatus("Drag items to reorder.");
        }

        private void HandleDrag(PointerEventData eventData)
        {
            if (_dragFromIndex < 0) return;

            var insertIndex = _listView.CalculateInsertIndex(
                eventData.position, eventData.pressEventCamera);

            if (insertIndex != _currentInsertIndex)
            {
                _currentInsertIndex = insertIndex;
                _listView.ShowInsertionIndicator(insertIndex);
            }
        }

        private void HandleEndDrag(SortableItemView item)
        {
            _listView.HideInsertionIndicator();

            if (_dragFromIndex >= 0 && _currentInsertIndex >= 0 &&
                _dragFromIndex != _currentInsertIndex)
            {
                // 모델 업데이트 (뷰는 Subscribe에서 자동 갱신)
                _model.MoveItem(_dragFromIndex, _currentInsertIndex);

                // 아이템을 리스트 컨테이너로 복귀
                item.ReturnToParent(_listContainer, _currentInsertIndex);

                _listView.SetStatus(
                    $"Moved from position {_dragFromIndex + 1} to {_currentInsertIndex + 1}");
            }
            else
            {
                // 스냅백
                item.SnapBack();
                _listView.SetStatus("Drag items to reorder.");
            }

            _dragFromIndex = -1;
            _currentInsertIndex = -1;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}

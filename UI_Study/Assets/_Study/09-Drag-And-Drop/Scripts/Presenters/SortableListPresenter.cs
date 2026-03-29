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
    /// 정렬 리스트 Presenter — Placeholder 방식 라이브 리오더.
    /// 드래그 중 Placeholder SiblingIndex를 실시간 변경하여
    /// VerticalLayoutGroup이 다른 항목을 자동으로 밀어주는 패턴.
    /// </summary>
    public class SortableListPresenter : IInitializable, IDisposable
    {
        private readonly SortableListModel _model;
        private readonly SortableListView _listView;
        private readonly CompositeDisposable _disposables = new();

        private SortableItemView _draggedItem;

        public SortableListPresenter(SortableListModel model, SortableListView listView)
        {
            _model = model;
            _listView = listView;
        }

        public void Initialize()
        {
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
                        _draggedItem = item;
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
            if (_draggedItem == null) return;

            // Placeholder SiblingIndex를 실시간 갱신
            // → VLG가 다른 항목을 자동으로 밀어줌 (라이브 리오더)
            _listView.UpdatePlaceholderIndex(_draggedItem, eventData);
        }

        private void HandleEndDrag(SortableItemView item)
        {
            _listView.HideInsertionIndicator();

            if (item.Placeholder != null)
            {
                int fromIndex = item.ItemIndex;
                int toIndex = item.Placeholder.transform.GetSiblingIndex();

                // Placeholder 위치로 복귀
                item.ReturnToPlaceholder();

                // 모델 업데이트
                if (fromIndex != toIndex)
                {
                    _model.MoveItem(fromIndex, toIndex);
                    _listView.SetStatus(
                        $"Moved from position {fromIndex + 1} to {toIndex + 1}");
                }
                else
                {
                    _listView.SetStatus("Drag items to reorder.");
                }
            }
            else
            {
                item.SnapBack();
                _listView.SetStatus("Drag items to reorder.");
            }

            _draggedItem = null;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}

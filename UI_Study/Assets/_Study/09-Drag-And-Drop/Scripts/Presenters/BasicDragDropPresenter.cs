using System;
using R3;
using UIStudy.DragDrop.Views;
using VContainer.Unity;

namespace UIStudy.DragDrop.Presenters
{
    /// <summary>
    /// 기본 드래그앤드롭 Presenter — 드롭 이벤트 처리 + 상태 텍스트 갱신.
    /// </summary>
    public class BasicDragDropPresenter : IInitializable, IDisposable
    {
        private readonly BasicDragDropDemoView _demoView;
        private readonly CompositeDisposable _disposables = new();

        public BasicDragDropPresenter(BasicDragDropDemoView demoView)
        {
            _demoView = demoView;
        }

        public void Initialize()
        {
            _demoView.SetStatus("Drag an item to a zone!");

            // Equip 존 드롭 이벤트 바인딩
            _demoView.EquipZone.OnItemDropped
                .Subscribe(item =>
                {
                    _demoView.SetStatus($"Equipped: {item.ItemLabel}");
                    _demoView.DisableItem(item);
                })
                .AddTo(_disposables);

            // Discard 존 드롭 이벤트 바인딩
            _demoView.DiscardZone.OnItemDropped
                .Subscribe(item =>
                {
                    _demoView.SetStatus($"Discarded: {item.ItemLabel}");
                    _demoView.DisableItem(item);
                })
                .AddTo(_disposables);

            // 각 드래그 아이템의 EndDrag 이벤트 — 유효하지 않은 드롭 시 스냅백
            foreach (var item in _demoView.DraggableItems)
            {
                item.OnEndDragEvent
                    .Subscribe(draggedItem =>
                    {
                        // 아이템이 아직 활성 상태이면 드롭되지 않은 것 → 스냅백
                        if (draggedItem.gameObject.activeSelf)
                        {
                            draggedItem.SnapBack();
                        }
                    })
                    .AddTo(_disposables);
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}

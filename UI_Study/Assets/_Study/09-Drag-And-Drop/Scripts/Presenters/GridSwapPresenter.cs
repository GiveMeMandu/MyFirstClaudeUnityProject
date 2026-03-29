using System;
using R3;
using UIStudy.DragDrop.Models;
using UIStudy.DragDrop.Views;
using VContainer.Unity;

namespace UIStudy.DragDrop.Presenters
{
    /// <summary>
    /// 그리드 스왑 Presenter — 슬롯 간 드래그앤드롭 스왑 로직.
    /// </summary>
    public class GridSwapPresenter : IInitializable, IDisposable
    {
        private readonly GridSwapModel _model;
        private readonly GridSwapView _gridView;
        private readonly CompositeDisposable _disposables = new();

        public GridSwapPresenter(GridSwapModel model, GridSwapView gridView)
        {
            _model = model;
            _gridView = gridView;
        }

        public void Initialize()
        {
            // 모델 -> 뷰 바인딩
            _model.SlotContents
                .Subscribe(contents => _gridView.BindAll(contents))
                .AddTo(_disposables);

            // 각 슬롯의 드롭 이벤트 바인딩
            foreach (var slot in _gridView.Slots)
            {
                var targetIndex = slot.SlotIndex;

                slot.OnDropReceived
                    .Subscribe(sourceIndex =>
                    {
                        var contents = _model.SlotContents.Value;
                        var sourceName = contents[sourceIndex] ?? "empty";
                        var targetName = contents[slot.SlotIndex] ?? "empty";

                        _model.SwapSlots(sourceIndex, slot.SlotIndex);

                        if (string.IsNullOrEmpty(targetName) || targetName == "empty")
                        {
                            _gridView.SetStatus(
                                $"Moved [{sourceName}] to slot {slot.SlotIndex + 1}");
                        }
                        else
                        {
                            _gridView.SetStatus(
                                $"Swapped [{sourceName}] <-> [{targetName}]");
                        }
                    })
                    .AddTo(_disposables);
            }

            _gridView.SetStatus("Drag items between slots to swap.");
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}

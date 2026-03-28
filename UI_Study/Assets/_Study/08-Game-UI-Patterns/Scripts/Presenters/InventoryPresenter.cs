using System;
using R3;
using UIStudy.GameUI.Models;
using UIStudy.GameUI.Views;
using VContainer.Unity;

namespace UIStudy.GameUI.Presenters
{
    /// <summary>
    /// 인벤토리 Presenter — 모델 바인딩 + 슬롯 클릭 -> 디테일 패널.
    /// </summary>
    public class InventoryPresenter : IInitializable, IDisposable
    {
        private readonly InventoryModel _model;
        private readonly InventoryGridView _gridView;
        private readonly CompositeDisposable _disposables = new();
        private readonly ReactiveProperty<int> _selectedIndex = new(-1);

        public InventoryPresenter(InventoryModel model, InventoryGridView gridView)
        {
            _model = model;
            _gridView = gridView;
        }

        public void Initialize()
        {
            // 아이템 배열 변경 시 그리드 갱신
            _model.Items
                .Subscribe(items =>
                {
                    _gridView.BindAll(items);
                    // 선택 상태도 다시 적용
                    UpdateDetailPanel();
                })
                .AddTo(_disposables);

            // 선택 인덱스 변경 시 하이라이트 + 디테일 갱신
            _selectedIndex
                .Subscribe(index =>
                {
                    _gridView.SetSelectedSlot(index);
                    UpdateDetailPanel();
                })
                .AddTo(_disposables);

            // 각 슬롯 버튼 클릭 바인딩
            var slots = _gridView.Slots;
            for (int i = 0; i < slots.Length; i++)
            {
                var slotIndex = i; // 캡처용 로컬 변수
                slots[i].Button.OnClickAsObservable()
                    .Subscribe(_ =>
                    {
                        // 같은 슬롯 재클릭 시 선택 해제
                        if (_selectedIndex.Value == slotIndex)
                            _selectedIndex.Value = -1;
                        else
                            _selectedIndex.Value = slotIndex;
                    })
                    .AddTo(_disposables);
            }

            // 초기 데이터 로드
            LoadSampleData();
            _gridView.SetGold(1250);
        }

        private void UpdateDetailPanel()
        {
            var index = _selectedIndex.Value;
            if (index < 0 || index >= InventoryModel.SlotCount)
            {
                _gridView.HideDetail();
                return;
            }

            var item = _model.Items.Value[index];
            _gridView.ShowDetail(item); // null이면 패널 숨김
        }

        private void LoadSampleData()
        {
            var items = new InventoryItem[InventoryModel.SlotCount];

            items[0] = new InventoryItem("Iron Sword", "[S]", 1, ItemRarity.Common,
                "A sturdy iron sword. Reliable in battle.");
            items[1] = new InventoryItem("Health Potion", "[P]", 5, ItemRarity.Common,
                "Restores 50 HP when consumed.");
            items[2] = new InventoryItem("Mana Crystal", "[M]", 3, ItemRarity.Rare,
                "A rare crystal pulsing with arcane energy.");
            items[4] = new InventoryItem("Dragon Scale", "[D]", 1, ItemRarity.Epic,
                "A shimmering scale from an ancient dragon.");
            items[5] = new InventoryItem("Arrows", "[A]", 20, ItemRarity.Common,
                "Standard wooden arrows.");
            items[7] = new InventoryItem("Enchanted Ring", "[R]", 1, ItemRarity.Rare,
                "Grants minor resistance to frost.");
            items[10] = new InventoryItem("Phoenix Feather", "[F]", 1, ItemRarity.Epic,
                "Legendary item. Revives the holder once.");
            items[11] = new InventoryItem("Bread", "[B]", 10, ItemRarity.Common,
                "Simple bread. Satisfies hunger.");

            _model.SetAll(items);
        }

        public void Dispose()
        {
            _selectedIndex.Dispose();
            _disposables.Dispose();
        }
    }
}

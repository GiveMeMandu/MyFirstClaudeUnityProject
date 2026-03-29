using R3;

namespace UIStudy.AdvancedHUD.Models
{
    /// <summary>
    /// Radial Menu 모델 — 8개 메뉴 항목 + 현재 선택 인덱스.
    /// </summary>
    public class RadialMenuModel
    {
        public static readonly string[] MenuItems =
        {
            "Attack", "Defend", "Item", "Magic",
            "Run", "Status", "Equip", "Save"
        };

        /// <summary>
        /// 현재 선택된 메뉴 인덱스. -1 = 선택 없음.
        /// </summary>
        public ReactiveProperty<int> SelectedIndex { get; } = new(-1);
    }
}

using R3;

namespace UIStudy.DragDrop.Models
{
    /// <summary>
    /// 그리드 슬롯 스왑 모델 — 3x3 그리드의 아이템 배치를 관리.
    /// null 또는 빈 문자열 = 빈 슬롯.
    /// </summary>
    public class GridSwapModel
    {
        public const int SlotCount = 9;

        /// <summary>
        /// 슬롯 내용 배열 (9개). null = 빈 슬롯.
        /// </summary>
        public ReactiveProperty<string[]> SlotContents { get; }

        public GridSwapModel()
        {
            SlotContents = new ReactiveProperty<string[]>(new string[]
            {
                "A", "B", "C",
                "D", null, "E",
                null, null, null
            });
        }

        /// <summary>
        /// 두 슬롯의 내용을 교환.
        /// </summary>
        public void SwapSlots(int indexA, int indexB)
        {
            if (indexA < 0 || indexA >= SlotCount) return;
            if (indexB < 0 || indexB >= SlotCount) return;
            if (indexA == indexB) return;

            var contents = SlotContents.Value;
            (contents[indexA], contents[indexB]) = (contents[indexB], contents[indexA]);
            SlotContents.ForceNotify();
        }
    }
}

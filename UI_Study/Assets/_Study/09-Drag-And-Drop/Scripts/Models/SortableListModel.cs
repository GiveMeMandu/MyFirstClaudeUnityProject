using R3;

namespace UIStudy.DragDrop.Models
{
    /// <summary>
    /// 정렬 가능 리스트 모델 — 문자열 배열의 순서를 관리.
    /// </summary>
    public class SortableListModel
    {
        /// <summary>
        /// 리스트 아이템 배열. 순서 변경 시 ForceNotify로 알림.
        /// </summary>
        public ReactiveProperty<string[]> Items { get; }

        public SortableListModel()
        {
            Items = new ReactiveProperty<string[]>(new string[]
            {
                "Task 1", "Task 2", "Task 3",
                "Task 4", "Task 5", "Task 6"
            });
        }

        /// <summary>
        /// fromIndex 아이템을 toIndex 위치로 이동. 사이 아이템들은 밀림.
        /// </summary>
        public void MoveItem(int fromIndex, int toIndex)
        {
            var items = Items.Value;
            if (fromIndex < 0 || fromIndex >= items.Length) return;
            if (toIndex < 0 || toIndex >= items.Length) return;
            if (fromIndex == toIndex) return;

            var moving = items[fromIndex];

            // 배열에서 제거 후 삽입
            if (fromIndex < toIndex)
            {
                for (int i = fromIndex; i < toIndex; i++)
                    items[i] = items[i + 1];
            }
            else
            {
                for (int i = fromIndex; i > toIndex; i--)
                    items[i] = items[i - 1];
            }

            items[toIndex] = moving;
            Items.ForceNotify();
        }
    }
}

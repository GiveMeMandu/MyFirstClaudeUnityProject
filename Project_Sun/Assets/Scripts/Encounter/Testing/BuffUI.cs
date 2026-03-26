using UnityEngine;

namespace ProjectSun.Encounter.Testing
{
    /// <summary>
    /// IMGUI 기반 활성 버프 표시 UI.
    /// 상단 바 우측에 아이콘/텍스트, 호버 시 설명.
    /// </summary>
    public class BuffUI : MonoBehaviour
    {
        [SerializeField] private BuffManager buffManager;

        private int hoveredBuffIndex = -1;

        private void OnGUI()
        {
            if (buffManager == null) return;
            var buffs = buffManager.ActiveBuffs;
            if (buffs.Count == 0) return;

            float startX = Screen.width - 10;
            float y = 40;
            float btnWidth = 100;
            float btnHeight = 22;

            // 우측 상단에 버프 배지 나열
            for (int i = 0; i < buffs.Count; i++)
            {
                var buff = buffs[i];
                startX -= (btnWidth + 5);

                string label = buff.Type switch
                {
                    BuffType.ProductionBonus => $"생산+{buff.Value * 100:F0}%",
                    BuffType.AttackBonus => $"공격+{buff.Value * 100:F0}%",
                    BuffType.DefenseResourceHalf => "방어비용↓",
                    _ => buff.Type.ToString()
                };
                label += $" ({buff.RemainingTurns}t)";

                var rect = new Rect(startX, y, btnWidth, btnHeight);

                GUI.backgroundColor = buff.Type switch
                {
                    BuffType.ProductionBonus => new Color(0.3f, 0.8f, 0.3f),
                    BuffType.AttackBonus => new Color(0.9f, 0.3f, 0.3f),
                    BuffType.DefenseResourceHalf => new Color(0.3f, 0.6f, 0.9f),
                    _ => Color.gray
                };

                GUI.Box(rect, label, new GUIStyle(GUI.skin.box) { fontSize = 11 });
                GUI.backgroundColor = Color.white;

                // 호버 감지
                if (rect.Contains(Event.current.mousePosition))
                {
                    hoveredBuffIndex = i;
                }
            }

            // 호버 설명
            if (hoveredBuffIndex >= 0 && hoveredBuffIndex < buffs.Count)
            {
                var buff = buffs[hoveredBuffIndex];
                string tooltip = $"{buff.Type}\n효과: {buff.Value * 100:F0}%\n남은 턴: {buff.RemainingTurns}\n출처: {buff.SourceName}";

                var mousePos = Event.current.mousePosition;
                float tooltipW = 200;
                float tooltipH = 70;
                GUI.Box(new Rect(mousePos.x - tooltipW, mousePos.y + 20, tooltipW, tooltipH), tooltip,
                    new GUIStyle(GUI.skin.box) { fontSize = 12, alignment = TextAnchor.UpperLeft, wordWrap = true });

                hoveredBuffIndex = -1;
            }
        }
    }
}

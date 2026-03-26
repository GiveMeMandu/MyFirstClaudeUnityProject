using UnityEngine;

namespace ProjectSun.Resource.Testing
{
    /// <summary>
    /// IMGUI 기반 자원 표시 UI.
    /// 화면 상단 중앙에 기초/고급/방어 자원 표시.
    /// </summary>
    public class ResourceUI : MonoBehaviour
    {
        [SerializeField] private ResourceManager resourceManager;

        private void OnGUI()
        {
            if (resourceManager == null) return;

            float barWidth = 400;
            float barHeight = 30;
            float x = (Screen.width - barWidth) / 2;

            GUILayout.BeginArea(new Rect(x, 5, barWidth, barHeight));
            GUILayout.BeginHorizontal("box");

            var style = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter };

            GUILayout.Label($"Basic: {resourceManager.BasicResource}", style, GUILayout.Width(120));
            GUILayout.Label($"Advanced: {resourceManager.AdvancedResource}", style, GUILayout.Width(130));
            GUILayout.Label($"Defense: {resourceManager.DefenseResource}", style, GUILayout.Width(120));

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}

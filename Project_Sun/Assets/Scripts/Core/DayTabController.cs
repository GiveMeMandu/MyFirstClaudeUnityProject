using UnityEngine;

namespace ProjectSun.V2.Core
{
    /// <summary>
    /// 낮 페이즈 탭 전환 + 밤 시작 IMGUI 버튼.
    /// GameDirector와 연결하여 탭 전환/밤 전환을 제어.
    /// 추후 UI Toolkit 탭 바로 교체 예정.
    /// </summary>
    public class DayTabController : MonoBehaviour
    {
        [SerializeField] GameDirector director;

        string[] _tabNames = { "Construction", "Workforce", "Exploration" };
        int _selectedTab;

        void OnGUI()
        {
            if (director == null) return;

            // 상단 탭 바
            GUILayout.BeginArea(new Rect(10, 10, 500, 40));
            GUILayout.BeginHorizontal();

            for (int i = 0; i < _tabNames.Length; i++)
            {
                var style = i == _selectedTab ? GUI.skin.button : GUI.skin.box;
                if (GUILayout.Button(_tabNames[i], style, GUILayout.Width(120), GUILayout.Height(30)))
                {
                    _selectedTab = i;
                    director.ShowDayTab(i);
                }
            }

            GUILayout.FlexibleSpace();

            // 밤 시작 버튼
            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("START NIGHT", GUILayout.Width(120), GUILayout.Height(30)))
            {
                director.StartNight();
            }
            GUI.backgroundColor = Color.white;

            // 밤 확인 버튼 (미리보기 후)
            GUI.backgroundColor = new Color(1f, 0.5f, 0.2f);
            if (GUILayout.Button("CONFIRM", GUILayout.Width(80), GUILayout.Height(30)))
            {
                director.ConfirmNight();
            }
            GUI.backgroundColor = Color.white;

            // 밤 종료 버튼
            GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
            if (GUILayout.Button("END NIGHT", GUILayout.Width(100), GUILayout.Height(30)))
            {
                director.EndNight();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}

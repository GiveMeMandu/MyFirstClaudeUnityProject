using UnityEngine;

namespace ProjectSun.Encounter.Testing
{
    /// <summary>
    /// IMGUI 기반 인카운터 팝업 UI.
    /// 선택지 버튼 표시, 비용/조건 미충족 시 비활성화.
    /// </summary>
    public class EncounterUI : MonoBehaviour
    {
        [SerializeField] private EncounterManager encounterManager;

        private void OnGUI()
        {
            if (encounterManager == null || !encounterManager.IsWaitingForChoice) return;

            var encounter = encounterManager.CurrentEncounter;
            if (encounter == null) return;

            float panelWidth = 420;
            float panelHeight = 60 + encounter.choices.Count * 45 + 40;
            float x = (Screen.width - panelWidth) / 2;
            float y = (Screen.height - panelHeight) / 2;

            // 반투명 배경
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(x, y, panelWidth, panelHeight));
            GUILayout.BeginVertical("box");

            // 제목
            string categoryLabel = encounter.category == EncounterCategory.Daily ? "[일상]" : "[중요]";
            GUILayout.Label($"<b>{categoryLabel} {encounter.encounterName}</b>",
                new GUIStyle(GUI.skin.label) { richText = true, fontSize = 16, alignment = TextAnchor.MiddleCenter });
            GUILayout.Space(5);

            // 설명
            GUILayout.Label(encounter.description,
                new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true, alignment = TextAnchor.MiddleCenter });
            GUILayout.Space(10);

            // 선택지 버튼
            for (int i = 0; i < encounter.choices.Count; i++)
            {
                var choice = encounter.choices[i];
                bool available = encounterManager.IsChoiceAvailable(choice);

                string label = choice.choiceText;

                // 비용 표시
                if (!string.IsNullOrEmpty(choice.costResourceId) && choice.costAmount > 0)
                {
                    label += $" (비용: {choice.costResourceId} {choice.costAmount})";
                }

                // 조건 표시
                if (!string.IsNullOrEmpty(choice.requiredBuildingName))
                {
                    label += available
                        ? $" [조건 충족: {choice.requiredBuildingName}]"
                        : $" [필요: {choice.requiredBuildingName}]";
                }

                GUI.enabled = available;
                GUI.backgroundColor = available ? Color.white : new Color(0.5f, 0.5f, 0.5f);

                if (GUILayout.Button(label, GUILayout.Height(35)))
                {
                    encounterManager.SelectChoice(i);
                }
            }

            GUI.enabled = true;
            GUI.backgroundColor = Color.white;

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}

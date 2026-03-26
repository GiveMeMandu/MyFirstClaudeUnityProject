using UnityEngine;

namespace ProjectSun.Policy.Testing
{
    /// <summary>
    /// 정책 시스템 테스트 컨트롤러.
    /// 턴 시뮬레이션으로 정책 해금/제정 테스트.
    /// </summary>
    public class PolicyTestController : MonoBehaviour
    {
        [SerializeField] private PolicyManager policyManager;
        [SerializeField] private PolicyEffectResolver effectResolver;
        [SerializeField] private PolicyUI policyUI;

        private int simulatedTurn = 1;

        private void OnGUI()
        {
            if (policyManager == null) return;

            float y = 10;
            float x = 10;
            float w = 250;

            GUILayout.BeginArea(new Rect(x, y, w, 300));
            GUILayout.BeginVertical("box");

            GUILayout.Label($"<b>정책 테스트 컨트롤</b>",
                new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 });
            GUILayout.Space(5);

            GUILayout.Label($"현재 턴: {simulatedTurn}");
            GUILayout.Label($"제정된 정책: {policyManager.EnactedNodes.Count}");
            GUILayout.Label($"선택 가능: {(policyManager.HasUnlockedNodes() ? "있음" : "없음")}");
            GUILayout.Label($"전체 완료: {(policyManager.AreAllPoliciesResolved() ? "예" : "아니오")}");

            GUILayout.Space(10);

            if (GUILayout.Button("다음 턴 →", GUILayout.Height(30)))
            {
                simulatedTurn++;
                policyManager.OnNewTurn(simulatedTurn);
            }

            if (GUILayout.Button("턴 초기화", GUILayout.Height(25)))
            {
                simulatedTurn = 1;
                policyManager.SetPolicyTree(policyManager.PolicyTree);
                policyManager.OnNewTurn(simulatedTurn);
            }

            GUILayout.Space(5);
            if (policyUI != null && GUILayout.Button("정책 패널 토글", GUILayout.Height(25)))
            {
                policyUI.TogglePanel();
            }

            // 효과 요약
            if (effectResolver != null)
            {
                GUILayout.Space(10);
                GUILayout.Label("<b>주요 수정자</b>",
                    new GUIStyle(GUI.skin.label) { richText = true });

                DrawModifierLine("기초생산", effectResolver.GetResourceProductionModifier(Resource.ResourceType.Basic));
                DrawModifierLine("고급생산", effectResolver.GetResourceProductionModifier(Resource.ResourceType.Advanced));
                DrawModifierLine("방어생산", effectResolver.GetResourceProductionModifier(Resource.ResourceType.Defense));
                DrawModifierLine("건설비용", effectResolver.GetBuildCostModifier());
                DrawModifierLine("인력효율", effectResolver.GetWorkerEfficiencyModifier());
                DrawModifierLine("타워공격", effectResolver.GetTowerDamageModifier());
                DrawModifierLine("타워사거리", effectResolver.GetTowerRangeModifier());
                DrawModifierLine("방벽HP", effectResolver.GetWallHPModifier());
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawModifierLine(string label, float value)
        {
            if (Mathf.Approximately(value, 0f)) return;
            string sign = value >= 0 ? "+" : "";
            GUILayout.Label($"  {label}: {sign}{value * 100f:F0}%");
        }
    }
}

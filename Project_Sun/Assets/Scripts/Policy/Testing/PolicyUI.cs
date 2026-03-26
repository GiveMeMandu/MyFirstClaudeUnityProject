using UnityEngine;

namespace ProjectSun.Policy.Testing
{
    /// <summary>
    /// IMGUI 기반 정책 트리 UI.
    /// 카테고리별 노드 목록 + 상태 표시 + 제정 버튼.
    /// </summary>
    public class PolicyUI : MonoBehaviour
    {
        [SerializeField] private PolicyManager policyManager;
        [SerializeField] private PolicyEffectResolver effectResolver;

        private bool showPolicyPanel = true;
        private bool showEffectsPanel;
        private Vector2 scrollPos;
        private PolicyNodeSO confirmNode;

        private readonly string[] categoryNames = { "내정", "탐사", "방어" };
        private readonly Color[] categoryColors =
        {
            new(0.2f, 0.6f, 0.3f),
            new(0.3f, 0.4f, 0.7f),
            new(0.7f, 0.3f, 0.2f)
        };

        private void OnGUI()
        {
            if (policyManager == null || policyManager.PolicyTree == null) return;

            // 확인 팝업
            if (confirmNode != null)
            {
                DrawConfirmPopup();
                return;
            }

            if (!showPolicyPanel) return;

            float panelWidth = 500;
            float panelHeight = 550;
            float x = Screen.width - panelWidth - 10;
            float y = 10;

            GUILayout.BeginArea(new Rect(x, y, panelWidth, panelHeight));
            GUILayout.BeginVertical("box");

            // 헤더
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b>정책 시스템</b>",
                new GUIStyle(GUI.skin.label) { richText = true, fontSize = 16 });
            GUILayout.FlexibleSpace();
            showEffectsPanel = GUILayout.Toggle(showEffectsPanel, "효과 보기");
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            scrollPos = GUILayout.BeginScrollView(scrollPos);

            // 카테고리별 노드
            for (int c = 0; c < 3; c++)
            {
                var category = (PolicyCategory)c;
                DrawCategorySection(category, categoryNames[c], categoryColors[c]);
            }

            // 활성 효과 패널
            if (showEffectsPanel && effectResolver != null)
            {
                DrawActiveEffects();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawCategorySection(PolicyCategory category, string label, Color color)
        {
            GUI.backgroundColor = color;
            GUILayout.BeginVertical("box");
            GUI.backgroundColor = Color.white;

            GUILayout.Label($"<b>── {label} ──</b>",
                new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14, alignment = TextAnchor.MiddleCenter });

            var nodes = policyManager.PolicyTree.GetNodesForCategory(category);
            foreach (var node in nodes)
            {
                if (node == null) continue;
                DrawNodeRow(node);
            }

            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private void DrawNodeRow(PolicyNodeSO node)
        {
            var state = policyManager.GetNodeState(node);

            GUILayout.BeginHorizontal("box");

            // 상태 아이콘
            string stateIcon = state switch
            {
                PolicyNodeState.Locked => "🔒",
                PolicyNodeState.Unlocked => "🔓",
                PolicyNodeState.Enacted => "✅",
                PolicyNodeState.BranchLocked => "❌",
                _ => "?"
            };

            string branchLabel = node.IsBranchNode ? " [분기]" : "";

            GUILayout.Label($"{stateIcon} {node.NodeName}{branchLabel}",
                new GUIStyle(GUI.skin.label) { richText = true, fontSize = 12 },
                GUILayout.Width(200));

            // 효과 요약
            string effectSummary = GetEffectSummary(node);
            GUILayout.Label(effectSummary,
                new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true },
                GUILayout.Width(180));

            // 제정 버튼
            GUI.enabled = state == PolicyNodeState.Unlocked;
            if (GUILayout.Button("제정", GUILayout.Width(50), GUILayout.Height(25)))
            {
                confirmNode = node;
            }
            GUI.enabled = true;

            GUILayout.EndHorizontal();
        }

        private string GetEffectSummary(PolicyNodeSO node)
        {
            if (node.Effects.Count == 0) return "(효과 없음)";

            var sb = new System.Text.StringBuilder();
            foreach (var effect in node.Effects)
            {
                if (sb.Length > 0) sb.Append(", ");
                string sign = effect.value >= 0 ? "+" : "";
                string suffix = effect.isPercentage ? "%" : "";
                float displayVal = effect.isPercentage ? effect.value * 100f : effect.value;
                sb.Append($"{GetEffectName(effect.effectType)} {sign}{displayVal:F0}{suffix}");
            }
            return sb.ToString();
        }

        private string GetEffectName(PolicyEffectType type)
        {
            return type switch
            {
                PolicyEffectType.BasicProductionMod => "기초생산",
                PolicyEffectType.AdvancedProductionMod => "고급생산",
                PolicyEffectType.DefenseProductionMod => "방어생산",
                PolicyEffectType.AllProductionMod => "전체생산",
                PolicyEffectType.BuildCostMod => "건설비용",
                PolicyEffectType.WorkerEfficiencyMod => "인력효율",
                PolicyEffectType.HealingSpeedMod => "회복속도",
                PolicyEffectType.TowerDamageMod => "타워공격",
                PolicyEffectType.TowerRangeMod => "타워사거리",
                PolicyEffectType.WallHPMod => "방벽HP",
                PolicyEffectType.WallRepairCostMod => "수리비용",
                PolicyEffectType.DefenseResourceCostMod => "방어자원비",
                PolicyEffectType.ExplorationSpeedMod => "탐사속도",
                PolicyEffectType.ExplorationRewardMod => "탐사보상",
                PolicyEffectType.ExplorationDamageMod => "탐사피해",
                PolicyEffectType.EncounterChanceMod => "인카운터확률",
                PolicyEffectType.HopeInstant => "희망(즉발)",
                PolicyEffectType.DiscontentInstant => "불만(즉발)",
                PolicyEffectType.HopePerTurn => "희망/턴",
                PolicyEffectType.DiscontentPerTurn => "불만/턴",
                _ => type.ToString()
            };
        }

        private void DrawActiveEffects()
        {
            GUILayout.Space(5);
            GUILayout.Label("<b>활성 정책 효과</b>",
                new GUIStyle(GUI.skin.label) { richText = true, fontSize = 13 });

            var effects = effectResolver.GetAllActiveEffects();
            if (effects.Count == 0)
            {
                GUILayout.Label("(제정된 정책이 없습니다)");
                return;
            }

            foreach (var effect in effects)
            {
                string sign = effect.value >= 0 ? "+" : "";
                string suffix = effect.isPercentage ? "%" : "";
                float displayVal = effect.isPercentage ? effect.value * 100f : effect.value;
                GUILayout.Label($"  • {GetEffectName(effect.effectType)}: {sign}{displayVal:F0}{suffix}");
            }
        }

        private void DrawConfirmPopup()
        {
            float w = 350;
            float h = 200;
            float x = (Screen.width - w) / 2;
            float y = (Screen.height - h) / 2;

            // 반투명 배경
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(x, y, w, h));
            GUILayout.BeginVertical("box");

            GUILayout.Label("<b>정책 제정 확인</b>",
                new GUIStyle(GUI.skin.label) { richText = true, fontSize = 15, alignment = TextAnchor.MiddleCenter });
            GUILayout.Space(10);

            GUILayout.Label($"'{confirmNode.NodeName}'을(를) 제정하시겠습니까?",
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true });

            if (confirmNode.IsBranchNode && confirmNode.BranchPairNode != null)
            {
                GUI.color = new Color(1f, 0.8f, 0.8f);
                GUILayout.Label($"⚠ '{confirmNode.BranchPairNode.NodeName}'이(가) 영구 잠깁니다!",
                    new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
                GUI.color = Color.white;
            }

            GUILayout.Space(5);
            GUILayout.Label(GetEffectSummary(confirmNode),
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 12 });

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("제정", GUILayout.Height(30)))
            {
                policyManager.EnactNode(confirmNode);
                confirmNode = null;
            }
            if (GUILayout.Button("취소", GUILayout.Height(30)))
            {
                confirmNode = null;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        public void TogglePanel()
        {
            showPolicyPanel = !showPolicyPanel;
        }
    }
}

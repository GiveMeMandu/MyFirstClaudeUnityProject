using ProjectSun.Resource;
using ProjectSun.Turn;
using UnityEngine;

namespace ProjectSun.WallExpansion.Testing
{
    /// <summary>
    /// IMGUI 기반 방벽 확장 시스템 테스트 UI.
    /// 확장 버튼, 비용 표시, 조건 툴팁, 해금 상태 모니터링.
    /// </summary>
    public class WallExpansionTestUI : MonoBehaviour
    {
        [Header("연동")]
        [SerializeField] private WallExpansionManager wallExpansionManager;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private FeatureUnlockManager featureUnlockManager;
        [SerializeField] private DefenseRangeController defenseRangeController;

        [Header("UI 설정")]
        [SerializeField] private bool showUI = true;

        private string logMessage = "";
        private float logTimer;
        private Vector2 scrollPosition;

        private void Start()
        {
            if (wallExpansionManager != null)
            {
                wallExpansionManager.OnExpansionCompleted += level =>
                    Log($"방벽 레벨 {level} 확장 완료!");

                wallExpansionManager.OnExpansionFailed += reason =>
                    Log($"확장 실패: {reason}");

                wallExpansionManager.OnFeaturesUnlocked += features =>
                {
                    foreach (var f in features)
                        Log($"시스템 해금: {f}");
                };
            }
        }

        private void Update()
        {
            if (logTimer > 0f)
                logTimer -= Time.deltaTime;
        }

        private void Log(string msg)
        {
            logMessage = msg;
            logTimer = 4f;
            Debug.Log($"[WallExpansionTestUI] {msg}");
        }

        private void OnGUI()
        {
            if (!showUI) return;

            GUI.skin.label.fontSize = 14;
            GUI.skin.button.fontSize = 13;
            GUI.skin.box.fontSize = 13;

            // Right panel to avoid overlap with existing construction test on left
            float panelWidth = 300f;
            float panelX = Screen.width - panelWidth - 10f;

            GUILayout.BeginArea(new Rect(panelX, 10, panelWidth, Screen.height - 20));
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            DrawStatusPanel();
            GUILayout.Space(10);
            DrawExpansionPanel();
            GUILayout.Space(10);
            DrawResourcePanel();
            GUILayout.Space(10);
            DrawFeatureUnlockPanel();
            GUILayout.Space(10);
            DrawDefensePanel();
            GUILayout.Space(10);
            DrawDebugPanel();
            GUILayout.Space(10);
            DrawLogPanel();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawStatusPanel()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Wall Expansion System</b>",
                new GUIStyle(GUI.skin.label) { richText = true, fontSize = 16 });
            GUILayout.Space(3);

            if (wallExpansionManager != null)
            {
                GUILayout.Label($"Wall Level: {wallExpansionManager.CurrentWallLevel} / {wallExpansionManager.MaxWallLevel}");
                GUILayout.Label($"Expanding: {wallExpansionManager.IsExpanding}");
            }

            if (turnManager != null)
            {
                GUILayout.Label($"Turn: {turnManager.CurrentTurn}  Phase: {turnManager.CurrentPhase}");
            }

            GUILayout.EndVertical();
        }

        private void DrawExpansionPanel()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Expansion</b>",
                new GUIStyle(GUI.skin.label) { richText = true });

            if (wallExpansionManager == null)
            {
                GUILayout.Label("WallExpansionManager not assigned.");
                GUILayout.EndVertical();
                return;
            }

            if (wallExpansionManager.IsMaxLevel)
            {
                GUILayout.Label("MAX LEVEL REACHED");
                GUILayout.EndVertical();
                return;
            }

            var nextData = wallExpansionManager.GetNextLevelData();
            if (nextData != null)
            {
                GUILayout.Label($"Next: Lv.{nextData.level}");
                GUILayout.Label($"Cost: Basic {nextData.basicCost} / Advanced {nextData.advancedCost}");
                GUILayout.Label($"Unlock Slots: {nextData.slotIds.Count}");
                GUILayout.Label($"Spawn Points: +{nextData.additionalSpawnPoints}");

                if (nextData.minTurn > 0)
                    GUILayout.Label($"Min Turn: {nextData.minTurn}");
                if (nextData.requiresResearch)
                    GUILayout.Label("Requires Research");

                if (nextData.unlockedFeatures != null && nextData.unlockedFeatures.Count > 0)
                {
                    string features = string.Join(", ", nextData.unlockedFeatures);
                    GUILayout.Label($"Unlocks: {features}");
                }
            }

            GUILayout.Space(5);

            bool canExpand = wallExpansionManager.CanExpand(out string reason);
            GUI.enabled = canExpand;

            if (GUILayout.Button("EXPAND WALL", GUILayout.Height(40)))
            {
                wallExpansionManager.TryExpand();
            }

            GUI.enabled = true;

            if (!canExpand && !string.IsNullOrEmpty(reason))
            {
                GUILayout.Label($"<color=yellow>{reason}</color>",
                    new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true });
            }

            GUILayout.EndVertical();
        }

        private void DrawResourcePanel()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Resources</b>",
                new GUIStyle(GUI.skin.label) { richText = true });

            if (resourceManager != null)
            {
                GUILayout.Label($"Basic: {resourceManager.BasicResource}");
                GUILayout.Label($"Advanced: {resourceManager.AdvancedResource}");
                GUILayout.Label($"Defense: {resourceManager.DefenseResource}");
            }

            GUILayout.EndVertical();
        }

        private void DrawFeatureUnlockPanel()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Unlocked Features</b>",
                new GUIStyle(GUI.skin.label) { richText = true });

            if (featureUnlockManager != null)
            {
                var unlocked = featureUnlockManager.GetUnlockedFeatures();
                if (unlocked.Count == 0)
                {
                    GUILayout.Label("(none)");
                }
                else
                {
                    foreach (var feature in unlocked)
                    {
                        GUILayout.Label($"  - {feature}");
                    }
                }
            }
            else
            {
                GUILayout.Label("FeatureUnlockManager not assigned.");
            }

            GUILayout.EndVertical();
        }

        private void DrawDefensePanel()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Defense Range</b>",
                new GUIStyle(GUI.skin.label) { richText = true });

            if (defenseRangeController != null)
            {
                GUILayout.Label($"Active Spawn Points: {defenseRangeController.ActiveSpawnPointCount} / {defenseRangeController.AllSpawnPoints.Count}");
            }
            else if (wallExpansionManager != null)
            {
                GUILayout.Label($"Active Spawn Points: {wallExpansionManager.GetActiveSpawnPointCount()}");
            }

            GUILayout.EndVertical();
        }

        private void DrawDebugPanel()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Debug</b>",
                new GUIStyle(GUI.skin.label) { richText = true });

            if (resourceManager != null)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("+50 Basic"))
                    resourceManager.AddResource(ResourceType.Basic, 50);
                if (GUILayout.Button("+20 Adv"))
                    resourceManager.AddResource(ResourceType.Advanced, 20);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("+100 Basic"))
                    resourceManager.AddResource(ResourceType.Basic, 100);
                if (GUILayout.Button("+50 Adv"))
                    resourceManager.AddResource(ResourceType.Advanced, 50);
                GUILayout.EndHorizontal();
            }

            if (featureUnlockManager != null)
            {
                if (GUILayout.Button("Reset All Unlocks"))
                {
                    featureUnlockManager.ResetAll();
                    Log("All feature unlocks reset.");
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawLogPanel()
        {
            if (logTimer > 0f && !string.IsNullOrEmpty(logMessage))
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label(logMessage, new GUIStyle(GUI.skin.label) { wordWrap = true });
                GUILayout.EndVertical();
            }
        }
    }
}

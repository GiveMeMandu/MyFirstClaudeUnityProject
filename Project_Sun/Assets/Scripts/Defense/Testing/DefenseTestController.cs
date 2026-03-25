using Unity.Collections;
using Unity.Entities;
using ProjectSun.Defense.ECS;
using UnityEngine;

namespace ProjectSun.Defense.Testing
{
    /// <summary>
    /// IMGUI 기반 방어 시스템 테스트 컨트롤러.
    /// 전투 시작/중지, 배속, 카메라 전환, 통계 표시.
    /// </summary>
    public class DefenseTestController : MonoBehaviour
    {
        [Header("연동")]
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private BattleCameraController cameraController;

        [Header("디버그")]
        [SerializeField] private bool showHealthBars = true;

        private string logMessage = "";
        private float logTimer;
        private bool isDoubleSpeed;
        private BattleStatisticsData lastStats;
        private bool showStatsPanel;

        public bool ShowHealthBars => showHealthBars;

        private void Start()
        {
            if (battleManager != null)
            {
                battleManager.OnBattleStateChanged += OnBattleStateChanged;
                battleManager.OnBattleEnded += OnBattleEnded;
            }
        }

        private void Update()
        {
            if (logTimer > 0f)
                logTimer -= Time.unscaledDeltaTime;
        }

        private void OnBattleStateChanged(BattleState state)
        {
            Log($"Battle State: {state}");
        }

        private void OnBattleEnded(BattleStatisticsData stats)
        {
            lastStats = stats;
            showStatsPanel = true;
        }

        private void Log(string msg)
        {
            logMessage = msg;
            logTimer = 3f;
            Debug.Log($"[DefenseTest] {msg}");
        }

        private void OnGUI()
        {
            GUI.skin.label.fontSize = 14;
            GUI.skin.button.fontSize = 13;
            GUI.skin.box.fontSize = 13;

            GUILayout.BeginArea(new Rect(10, 10, 280, Screen.height - 20));

            DrawControlPanel();
            GUILayout.Space(10);
            DrawStatusPanel();
            GUILayout.Space(10);
            DrawLogPanel();

            GUILayout.EndArea();

            if (showStatsPanel && lastStats != null)
            {
                DrawStatsPanel();
            }
        }

        private void DrawControlPanel()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Defense System PoC</b>",
                new GUIStyle(GUI.skin.label) { richText = true, fontSize = 16 });
            GUILayout.Space(5);

            var state = battleManager != null ? battleManager.State : BattleState.Idle;

            if (state == BattleState.Idle)
            {
                GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
                if (GUILayout.Button("Start Night Battle", GUILayout.Height(40)))
                {
                    battleManager?.StartBattle();
                }
                GUI.backgroundColor = Color.white;
            }
            else if (state == BattleState.InProgress)
            {
                GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
                if (GUILayout.Button("Stop Battle", GUILayout.Height(30)))
                {
                    battleManager?.StopBattle();
                    isDoubleSpeed = false;
                }
                GUI.backgroundColor = Color.white;

                GUILayout.Space(5);

                // 배속
                string speedLabel = isDoubleSpeed ? "Speed: 2x (click: 1x)" : "Speed: 1x (click: 2x)";
                if (GUILayout.Button(speedLabel, GUILayout.Height(25)))
                {
                    isDoubleSpeed = !isDoubleSpeed;
                    battleManager?.SetTimeScale(isDoubleSpeed ? 2f : 1f);
                }
            }
            else if (state == BattleState.Victory || state == BattleState.Defeat)
            {
                string resultText = state == BattleState.Victory ?
                    "<color=green><b>VICTORY!</b></color>" :
                    "<color=red><b>DEFEAT...</b></color>";
                GUILayout.Label(resultText, new GUIStyle(GUI.skin.label) { richText = true, fontSize = 18, alignment = TextAnchor.MiddleCenter });

                if (GUILayout.Button("Reset", GUILayout.Height(30)))
                {
                    battleManager?.StopBattle();
                    showStatsPanel = false;
                    isDoubleSpeed = false;
                }
            }

            GUILayout.Space(5);

            // 카메라 전환
            string camMode = cameraController != null && cameraController.IsFreeCamMode ? "Free Cam" : "Top-Down";
            if (GUILayout.Button($"Camera: {camMode}", GUILayout.Height(25)))
            {
                cameraController?.ToggleCameraMode();
            }

            // 체력바 토글
            string hpLabel = showHealthBars ? "Health Bars: ON" : "Health Bars: OFF";
            if (GUILayout.Button(hpLabel, GUILayout.Height(25)))
            {
                showHealthBars = !showHealthBars;
            }

            GUILayout.EndVertical();
        }

        private void DrawStatusPanel()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Status</b>", new GUIStyle(GUI.skin.label) { richText = true });

            if (battleManager != null)
            {
                int remaining = battleManager.GetRemainingEnemyCount();
                var stats = battleManager.Statistics;
                int killed = stats != null ? stats.TotalKilled : 0;
                int total = stats != null ? stats.TotalSpawned : 0;

                GUILayout.Label($"Enemies: {remaining} alive");
                GUILayout.Label($"Killed: {killed} / {total}");
                GUILayout.Label($"State: {battleManager.State}");
                GUILayout.Label($"Speed: {battleManager.TimeScale:F1}x");

                if (stats != null)
                {
                    GUILayout.Label($"Building Damage: {stats.TotalDamageToBuildings:F0}");
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawLogPanel()
        {
            if (logTimer > 0f && !string.IsNullOrEmpty(logMessage))
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label(logMessage);
                GUILayout.EndVertical();
            }
        }

        private void DrawStatsPanel()
        {
            float panelWidth = 300;
            float panelHeight = 300;
            float x = (Screen.width - panelWidth) / 2;
            float y = (Screen.height - panelHeight) / 2;

            GUILayout.BeginArea(new Rect(x, y, panelWidth, panelHeight));
            GUILayout.BeginVertical("box");

            GUILayout.Label("<b>Battle Results</b>",
                new GUIStyle(GUI.skin.label) { richText = true, fontSize = 16, alignment = TextAnchor.MiddleCenter });
            GUILayout.Space(10);

            GUILayout.Label($"Killed: {lastStats.TotalKilled} / {lastStats.TotalSpawned}");
            GUILayout.Label($"Total Building Damage: {lastStats.TotalDamageToBuildings:F0}");
            GUILayout.Space(5);

            if (lastStats.BuildingDamageMap.Count > 0)
            {
                GUILayout.Label("<b>Building Damage:</b>",
                    new GUIStyle(GUI.skin.label) { richText = true });
                foreach (var kvp in lastStats.BuildingDamageMap)
                {
                    GUILayout.Label($"  {kvp.Key}: {kvp.Value:F0}");
                }
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Close", GUILayout.Height(30)))
            {
                showStatsPanel = false;
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            if (battleManager != null)
            {
                battleManager.OnBattleStateChanged -= OnBattleStateChanged;
                battleManager.OnBattleEnded -= OnBattleEnded;
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectSun.Defense
{
    /// <summary>
    /// 전투 UI 관리.
    /// 밤 시작 버튼, 배속 조절, 전투 통계 표시.
    /// </summary>
    public class BattleUIManager : MonoBehaviour
    {
        [Header("전투 컨트롤")]
        [SerializeField] private Button startBattleButton;
        [SerializeField] private Button stopBattleButton;
        [SerializeField] private Button speedToggleButton;
        [SerializeField] private Button cameraModeButton;

        [Header("상태 표시")]
        [SerializeField] private TextMeshProUGUI battleStateText;
        [SerializeField] private TextMeshProUGUI speedText;
        [SerializeField] private TextMeshProUGUI enemyCountText;
        [SerializeField] private TextMeshProUGUI cameraModeText;

        [Header("통계 패널")]
        [SerializeField] private GameObject statisticsPanel;
        [SerializeField] private TextMeshProUGUI statisticsText;
        [SerializeField] private Button closeStatsButton;

        [Header("체력바 토글")]
        [SerializeField] private Button healthBarToggleButton;
        [SerializeField] private TextMeshProUGUI healthBarToggleText;

        [Header("연동")]
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private BattleCameraController cameraController;

        private bool isDoubleSpeed;
        private bool healthBarsEnabled = true;

        public bool HealthBarsEnabled => healthBarsEnabled;

        private void Start()
        {
            if (startBattleButton != null)
                startBattleButton.onClick.AddListener(OnStartBattle);

            if (stopBattleButton != null)
                stopBattleButton.onClick.AddListener(OnStopBattle);

            if (speedToggleButton != null)
                speedToggleButton.onClick.AddListener(OnToggleSpeed);

            if (cameraModeButton != null)
                cameraModeButton.onClick.AddListener(OnToggleCameraMode);

            if (closeStatsButton != null)
                closeStatsButton.onClick.AddListener(OnCloseStats);

            if (healthBarToggleButton != null)
                healthBarToggleButton.onClick.AddListener(OnToggleHealthBars);

            if (battleManager != null)
            {
                battleManager.OnBattleStateChanged += OnBattleStateChanged;
                battleManager.OnBattleEnded += OnBattleEnded;
            }

            if (statisticsPanel != null)
                statisticsPanel.SetActive(false);

            UpdateUI();
        }

        private void Update()
        {
            if (battleManager == null) return;

            if (battleManager.State == BattleState.InProgress)
            {
                UpdateEnemyCount();
            }
        }

        private void OnStartBattle()
        {
            battleManager?.StartBattle();
        }

        private void OnStopBattle()
        {
            battleManager?.StopBattle();
        }

        private void OnToggleSpeed()
        {
            isDoubleSpeed = !isDoubleSpeed;
            battleManager?.SetTimeScale(isDoubleSpeed ? 2f : 1f);
            UpdateUI();
        }

        private void OnToggleCameraMode()
        {
            cameraController?.ToggleCameraMode();
            UpdateUI();
        }

        private void OnToggleHealthBars()
        {
            healthBarsEnabled = !healthBarsEnabled;
            UpdateUI();
        }

        private void OnCloseStats()
        {
            if (statisticsPanel != null)
                statisticsPanel.SetActive(false);
        }

        private void OnBattleStateChanged(BattleState state)
        {
            UpdateUI();
        }

        private void OnBattleEnded(BattleStatisticsData stats)
        {
            ShowStatistics(stats);
        }

        private void UpdateUI()
        {
            var state = battleManager != null ? battleManager.State : BattleState.Idle;

            if (battleStateText != null)
                battleStateText.text = state switch
                {
                    BattleState.Idle => "대기",
                    BattleState.Preparing => "준비",
                    BattleState.InProgress => "전투 중",
                    BattleState.Victory => "승리!",
                    BattleState.Defeat => "패배...",
                    _ => ""
                };

            if (speedText != null)
                speedText.text = isDoubleSpeed ? "2x" : "1x";

            if (startBattleButton != null)
                startBattleButton.gameObject.SetActive(state == BattleState.Idle);

            if (stopBattleButton != null)
                stopBattleButton.gameObject.SetActive(state == BattleState.InProgress);

            if (speedToggleButton != null)
                speedToggleButton.gameObject.SetActive(state == BattleState.InProgress);

            if (cameraModeText != null && cameraController != null)
                cameraModeText.text = cameraController.IsFreeCamMode ? "Free" : "Top-Down";

            if (healthBarToggleText != null)
                healthBarToggleText.text = healthBarsEnabled ? "HP Bar: ON" : "HP Bar: OFF";
        }

        private void UpdateEnemyCount()
        {
            if (enemyCountText == null || battleManager == null) return;

            var stats = battleManager.Statistics;
            if (stats != null)
            {
                int remaining = stats.TotalSpawned - stats.TotalKilled;
                enemyCountText.text = $"Enemies: {remaining}";
            }
        }

        private void ShowStatistics(BattleStatisticsData stats)
        {
            if (statisticsPanel == null || statisticsText == null) return;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"처치: {stats.TotalKilled} / {stats.TotalSpawned}");
            sb.AppendLine($"건물 총 피해: {stats.TotalDamageToBuildings:F0}");
            sb.AppendLine();

            if (stats.BuildingDamageMap.Count > 0)
            {
                sb.AppendLine("== 건물별 피해 ==");
                foreach (var kvp in stats.BuildingDamageMap)
                {
                    sb.AppendLine($"  {kvp.Key}: {kvp.Value:F0}");
                }
            }

            statisticsText.text = sb.ToString();
            statisticsPanel.SetActive(true);
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

using System.Text;
using UnityEditor;
using UnityEngine;

namespace ProjectSun.V2.Editor
{
    /// <summary>
    /// Balance Simulator EditorWindow.
    /// Simulates N turns of resource income, wave scaling, and damage estimation
    /// without entering Play mode. Accessible via Window > Project Sun > Balance Simulator.
    /// </summary>
    public class BalanceSimulatorWindow : EditorWindow
    {
        [MenuItem("Window/Project Sun/Balance Simulator")]
        static void Open()
        {
            var window = GetWindow<BalanceSimulatorWindow>("Balance Simulator");
            window.minSize = new Vector2(520, 400);
        }

        int _turnCount = 25;
        Vector2 _scrollPos;
        string _resultText = "";
        bool _hasResult;

        void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Balance Simulator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Simulates N turns of resource income, wave scaling, and damage estimation. " +
                "No Play mode required.",
                MessageType.Info);

            EditorGUILayout.Space(4);
            _turnCount = EditorGUILayout.IntSlider("Turns to Simulate", _turnCount, 5, 50);

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Simulate", GUILayout.Height(32)))
            {
                RunSimulation();
            }

            EditorGUILayout.Space(8);

            if (_hasResult)
            {
                EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));
                EditorGUILayout.TextArea(_resultText, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndScrollView();
            }
        }

        void RunSimulation()
        {
            var sb = new StringBuilder();

            // ── Initial state (mirrors GameDirector.CreateDefaultGameState) ──
            int basicAmount = 60;
            int advancedAmount = 20;
            int relicAmount = 0;
            int basicCap = 100;
            int advancedCap = 40;

            // Buildings: HQ + 2 towers + 1 production
            int towerCount = 2;
            int productionCount = 1;
            const int towerDamage = 15;

            // Tracking
            int totalBasicEarned = 0;
            int totalAdvancedEarned = 0;
            int peakEnemyCount = 0;
            int projectedGameOverTurn = -1;

            sb.AppendLine("=== BALANCE SIMULATION ===");
            sb.AppendLine($"Turns: {_turnCount}");
            sb.AppendLine($"Initial: B={basicAmount}/{basicCap}, A={advancedAmount}/{advancedCap}");
            sb.AppendLine($"Towers: {towerCount}, Production: {productionCount}");
            sb.AppendLine("─────────────────────────────────────────────────");
            sb.AppendLine();

            for (int turn = 1; turn <= _turnCount; turn++)
            {
                // ── Resource Income ──
                int basicIncome = 5 + (productionCount * 3); // base + production buildings
                int advancedIncome = 1;

                basicAmount = Mathf.Min(basicAmount + basicIncome, basicCap);
                advancedAmount = Mathf.Min(advancedAmount + advancedIncome, advancedCap);

                totalBasicEarned += basicIncome;
                totalAdvancedEarned += advancedIncome;

                // ── Wave Scaling ──
                int enemyCount = Mathf.RoundToInt(10f * Mathf.Pow(1.2f, turn - 1));
                float enemyHP = 30f * Mathf.Pow(1.1f, turn - 1);
                const int enemyDamage = 8;

                if (enemyCount > peakEnemyCount)
                    peakEnemyCount = enemyCount;

                // ── Damage Estimation ──
                // Incoming damage: enemies * damage * 5 attacks each
                float incomingDamage = enemyCount * enemyDamage * 5f;
                // Defense output: towers * tower_damage * turn_factor * 0.5 efficiency
                float defenseOutput = towerCount * towerDamage * turn * 0.5f;
                float damageRatio = defenseOutput > 0f ? incomingDamage / defenseOutput : float.MaxValue;

                // ── Defense Grade ──
                string grade;
                if (damageRatio <= 0.5f)
                    grade = "S (Perfect)";
                else if (damageRatio <= 1.0f)
                    grade = "A (Strong)";
                else if (damageRatio <= 1.5f)
                    grade = "B (Adequate)";
                else if (damageRatio <= 2.5f)
                    grade = "C (Struggling)";
                else
                {
                    grade = "D (Critical)";
                    if (projectedGameOverTurn < 0)
                        projectedGameOverTurn = turn;
                }

                sb.AppendLine($"Turn {turn,3}: " +
                    $"B={basicAmount,4} A={advancedAmount,3} R={relicAmount,2} | " +
                    $"Enemies={enemyCount,4} HP={enemyHP,7:F1} | " +
                    $"DmgRatio={damageRatio,5:F2} | Grade={grade}");
            }

            // ── Summary ──
            sb.AppendLine();
            sb.AppendLine("─────────────────────────────────────────────────");
            sb.AppendLine("=== SUMMARY ===");
            sb.AppendLine($"Total Basic Earned:   {totalBasicEarned}");
            sb.AppendLine($"Total Advanced Earned: {totalAdvancedEarned}");
            sb.AppendLine($"Peak Enemy Count:      {peakEnemyCount}");
            sb.AppendLine($"Projected Game-Over:   {(projectedGameOverTurn > 0 ? $"Turn {projectedGameOverTurn}" : "None (survived all turns)")}");
            sb.AppendLine();

            if (projectedGameOverTurn > 0 && projectedGameOverTurn <= 10)
                sb.AppendLine("[WARNING] Early game-over predicted. Consider buffing towers or nerfing wave scaling.");
            else if (projectedGameOverTurn < 0)
                sb.AppendLine("[INFO] Player survives all simulated turns. Consider increasing difficulty.");

            _resultText = sb.ToString();
            _hasResult = true;

            Debug.Log($"[BalanceSimulator] Simulation complete — {_turnCount} turns, " +
                $"peak enemies: {peakEnemyCount}, " +
                $"game-over: {(projectedGameOverTurn > 0 ? $"Turn {projectedGameOverTurn}" : "None")}");
        }
    }
}

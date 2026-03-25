using UnityEngine;

namespace ProjectSun.Turn.Testing
{
    /// <summary>
    /// IMGUI 기반 턴 시스템 테스트 컨트롤러.
    /// 턴 종료 버튼, 현재 턴/페이즈 표시, 게임오버 처리.
    /// </summary>
    public class TurnTestController : MonoBehaviour
    {
        [Header("연동")]
        [SerializeField] private TurnManager turnManager;

        private string logMessage = "";
        private float logTimer;
        private bool gameEnded;
        private string gameEndMessage;

        private void Start()
        {
            if (turnManager != null)
            {
                turnManager.OnPhaseChanged += OnPhaseChanged;
                turnManager.OnTurnChanged += OnTurnChanged;
                turnManager.OnGameOver += OnGameOver;
            }
        }

        private void Update()
        {
            if (logTimer > 0f)
                logTimer -= Time.unscaledDeltaTime;
        }

        private void OnPhaseChanged(TurnPhase phase)
        {
            Log($"Phase: {phase}");
        }

        private void OnTurnChanged(int turn)
        {
            Log($"Turn {turn} started");
        }

        private void OnGameOver(GameOverReason reason)
        {
            gameEnded = true;
            gameEndMessage = reason == GameOverReason.Victory
                ? "<color=green><b>VICTORY! Scenario Complete!</b></color>"
                : "<color=red><b>DEFEAT... Base Destroyed.</b></color>";
        }

        private void Log(string msg)
        {
            logMessage = msg;
            logTimer = 3f;
        }

        private void OnGUI()
        {
            GUI.skin.label.fontSize = 14;
            GUI.skin.button.fontSize = 13;
            GUI.skin.box.fontSize = 13;

            GUILayout.BeginArea(new Rect(10, 10, 260, Screen.height - 20));

            DrawTurnPanel();
            GUILayout.Space(10);
            DrawLogPanel();

            GUILayout.EndArea();

            if (gameEnded)
            {
                DrawGameOverPanel();
            }
        }

        private void DrawTurnPanel()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Turn System</b>",
                new GUIStyle(GUI.skin.label) { richText = true, fontSize = 16 });
            GUILayout.Space(5);

            if (turnManager != null)
            {
                GUILayout.Label($"Turn: {turnManager.CurrentTurn} / {turnManager.TotalTurns}");
                GUILayout.Label($"Phase: {turnManager.CurrentPhase}");
            }

            GUILayout.Space(5);

            bool canEndTurn = turnManager != null &&
                              turnManager.CurrentPhase == TurnPhase.DayPhase &&
                              !gameEnded;

            GUI.enabled = canEndTurn;
            GUI.backgroundColor = canEndTurn ? new Color(0.3f, 0.7f, 1f) : Color.gray;

            if (GUILayout.Button("End Turn (Night)", GUILayout.Height(40)))
            {
                turnManager?.EndTurn();
            }

            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

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

        private void DrawGameOverPanel()
        {
            float w = 350, h = 150;
            float x = (Screen.width - w) / 2;
            float y = (Screen.height - h) / 2 - 100;

            GUILayout.BeginArea(new Rect(x, y, w, h));
            GUILayout.BeginVertical("box");

            GUILayout.Label(gameEndMessage,
                new GUIStyle(GUI.skin.label) { richText = true, fontSize = 20, alignment = TextAnchor.MiddleCenter });

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            if (turnManager != null)
            {
                turnManager.OnPhaseChanged -= OnPhaseChanged;
                turnManager.OnTurnChanged -= OnTurnChanged;
                turnManager.OnGameOver -= OnGameOver;
            }
        }
    }
}

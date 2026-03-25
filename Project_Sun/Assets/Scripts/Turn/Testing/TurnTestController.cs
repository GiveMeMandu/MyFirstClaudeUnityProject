using ProjectSun.Construction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectSun.Turn.Testing
{
    /// <summary>
    /// IMGUI 기반 턴 시스템 테스트 컨트롤러.
    /// 턴 종료 버튼, 현재 턴/페이즈 표시, 건물 클릭 선택, 게임오버 처리.
    /// </summary>
    public class TurnTestController : MonoBehaviour
    {
        [Header("연동")]
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private BuildingManager buildingManager;
        [SerializeField] private Camera mainCamera;

        private string logMessage = "";
        private float logTimer;
        private bool gameEnded;
        private string gameEndMessage;

        // 건물 선택
        private BuildingSlot selectedSlot;

        private void Start()
        {
            if (turnManager != null)
            {
                turnManager.OnPhaseChanged += OnPhaseChanged;
                turnManager.OnTurnChanged += OnTurnChanged;
                turnManager.OnGameOver += OnGameOver;
            }

            if (mainCamera == null)
                mainCamera = Camera.main;
        }

        private void Update()
        {
            if (logTimer > 0f)
                logTimer -= Time.unscaledDeltaTime;

            HandleSlotSelection();
        }

        private void HandleSlotSelection()
        {
            var mouse = Mouse.current;
            if (mouse == null || mainCamera == null) return;
            if (!mouse.leftButton.wasPressedThisFrame) return;

            Vector2 mousePos = mouse.position.ReadValue();

            // GUI 영역 위 클릭 무시 (좌측 패널)
            if (mousePos.x < 280f) return;

            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hit, 200f))
            {
                var slot = hit.collider.GetComponentInParent<BuildingSlot>();
                if (slot != null)
                {
                    selectedSlot = slot;
                    Log($"Selected: {(slot.CurrentBuildingData != null ? slot.CurrentBuildingData.buildingName : "Unknown")}");
                }
            }
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
            DrawSelectedSlotPanel();
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

        private void DrawSelectedSlotPanel()
        {
            GUILayout.BeginVertical("box");

            if (selectedSlot == null)
            {
                GUILayout.Label("Click a building to inspect");
                GUILayout.EndVertical();
                return;
            }

            var data = selectedSlot.CurrentBuildingData;
            string name = data != null ? data.buildingName : "(no data)";

            GUILayout.Label($"<b>{name}</b>",
                new GUIStyle(GUI.skin.label) { richText = true, fontSize = 15 });
            GUILayout.Label($"State: {selectedSlot.State}");

            if (data != null)
                GUILayout.Label($"Category: {data.category}");

            if (selectedSlot.Health != null)
            {
                var h = selectedSlot.Health;
                float ratio = h.HPRatio * 100f;
                string hpColor = ratio > 60 ? "green" : ratio > 30 ? "yellow" : "red";
                GUILayout.Label($"HP: <color={hpColor}>{h.CurrentHP:F0} / {h.MaxHP:F0} ({ratio:F0}%)</color>",
                    new GUIStyle(GUI.skin.label) { richText = true });
            }

            if (data != null && data.category == BuildingCategory.Defense)
            {
                GUILayout.Label($"Tower Range: {data.towerRange:F0}");
                GUILayout.Label($"Tower Dmg: {data.towerDamage:F0}");
                GUILayout.Label($"Attack Speed: {data.towerAttackSpeed:F1}/s");
                GUILayout.Label($"Anti-Air: {(data.towerCanTargetAir ? "Yes" : "No")}");
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

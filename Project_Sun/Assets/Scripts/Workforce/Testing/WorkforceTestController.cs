using ProjectSun.Construction;
using ProjectSun.Turn;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectSun.Workforce.Testing
{
    /// <summary>
    /// IMGUI 기반 인력 배치 테스트 UI.
    /// 건물 선택 → 슬롯별 +/- 버튼으로 인력 배치.
    /// 낮 페이즈에서만 배치 변경 가능.
    /// </summary>
    public class WorkforceTestController : MonoBehaviour
    {
        [Header("연동")]
        [SerializeField] private WorkforceManager workforceManager;
        [SerializeField] private BuildingManager buildingManager;
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private Camera mainCamera;

        private BuildingSlot selectedSlot;
        private Vector2 scrollPos;

        private bool IsDayPhase => turnManager == null || turnManager.CurrentPhase == TurnPhase.DayPhase;

        private void Update()
        {
            HandleSlotSelection();
        }

        private void HandleSlotSelection()
        {
            var mouse = Mouse.current;
            if (mouse == null || mainCamera == null) return;
            if (!mouse.leftButton.wasPressedThisFrame) return;

            Vector2 mousePos = mouse.position.ReadValue();
            if (mousePos.x < 280f) return; // GUI 영역

            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hit, 200f))
            {
                var slot = hit.collider.GetComponentInParent<BuildingSlot>();
                if (slot != null)
                    selectedSlot = slot;
            }
        }

        private void OnGUI()
        {
            if (workforceManager == null) return;

            GUI.skin.label.fontSize = 13;
            GUI.skin.button.fontSize = 12;
            GUI.skin.box.fontSize = 12;

            // 우측 패널 (인력 배치)
            float panelWidth = 260;
            GUILayout.BeginArea(new Rect(Screen.width - panelWidth - 10, 10, panelWidth, Screen.height - 20));

            DrawWorkforcePanel();
            GUILayout.Space(10);
            DrawSlotPanel();
            GUILayout.Space(10);
            DrawHealingPanel();

            GUILayout.EndArea();
        }

        private void DrawWorkforcePanel()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Workforce</b>",
                new GUIStyle(GUI.skin.label) { richText = true, fontSize = 15 });
            GUILayout.Space(3);

            GUILayout.Label($"Total: {workforceManager.TotalWorkers}");
            GUILayout.Label($"<color=green>Healthy: {workforceManager.HealthyCount}</color>",
                new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label($"<color=red>Injured: {workforceManager.InjuredCount}</color>",
                new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label($"Assigned: {workforceManager.AssignedCount}");
            GUILayout.Label($"<color=yellow>Idle: {workforceManager.IdleCount}</color>",
                new GUIStyle(GUI.skin.label) { richText = true });

            GUILayout.Space(3);
            if (!IsDayPhase)
            {
                GUILayout.Label("<color=orange>Night — assignment locked</color>",
                    new GUIStyle(GUI.skin.label) { richText = true });
            }
            GUI.enabled = IsDayPhase;
            if (GUILayout.Button("Unassign All"))
            {
                workforceManager.UnassignAll();
            }
            GUI.enabled = true;

            GUILayout.EndVertical();
        }

        private void DrawSlotPanel()
        {
            GUILayout.BeginVertical("box");

            if (selectedSlot == null)
            {
                GUILayout.Label("Click a building to assign workers");
                GUILayout.EndVertical();
                return;
            }

            var data = selectedSlot.CurrentBuildingData;
            string name = data != null ? data.buildingName : "Unknown";

            GUILayout.Label($"<b>{name}</b>",
                new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 });
            GUILayout.Label($"State: {selectedSlot.State}");

            if (selectedSlot.Health != null)
            {
                float ratio = selectedSlot.Health.HPRatio * 100f;
                GUILayout.Label($"HP: {selectedSlot.Health.CurrentHP:F0}/{selectedSlot.Health.MaxHP:F0} ({ratio:F0}%)");
            }

            GUILayout.Space(5);

            var slots = workforceManager.GetBuildingSlots(selectedSlot);
            if (slots == null || slots.Count == 0)
            {
                GUILayout.Label("No worker slots");
                GUILayout.EndVertical();
                return;
            }

            GUILayout.Label("<b>Worker Slots:</b>",
                new GUIStyle(GUI.skin.label) { richText = true });

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                GUILayout.BeginHorizontal();

                GUILayout.Label($"{slot.SlotName}", GUILayout.Width(90));
                GUILayout.Label($"{slot.AssignedWorkers}/{slot.MaxWorkers}", GUILayout.Width(35));

                GUI.enabled = IsDayPhase && slot.AssignedWorkers > 0;
                if (GUILayout.Button("-", GUILayout.Width(25)))
                {
                    workforceManager.UnassignWorker(selectedSlot, i);
                }
                GUI.enabled = IsDayPhase && slot.CanAddWorker && workforceManager.IdleCount > 0;
                if (GUILayout.Button("+", GUILayout.Width(25)))
                {
                    workforceManager.AssignWorker(selectedSlot, i);
                }
                GUI.enabled = true;

                GUILayout.Label($"({slot.SlotType})", GUILayout.Width(80));

                GUILayout.EndHorizontal();
            }

            int totalWorkers = workforceManager.GetBuildingTotalWorkers(selectedSlot);
            GUILayout.Label($"Total in building: {totalWorkers}");

            if (data != null && data.category == BuildingCategory.Defense)
            {
                bool active = workforceManager.IsTowerActive(selectedSlot);
                string status = active
                    ? "<color=green>Tower: ACTIVE</color>"
                    : "<color=red>Tower: INACTIVE (need 1+ worker)</color>";
                GUILayout.Label(status, new GUIStyle(GUI.skin.label) { richText = true });
            }

            GUILayout.EndVertical();
        }

        private void DrawHealingPanel()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>HQ Healing</b>",
                new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 });

            var healSlot = workforceManager.HealingSlot;
            if (healSlot == null)
            {
                GUILayout.Label("No healing slot");
                GUILayout.EndVertical();
                return;
            }

            GUILayout.Label($"Injured workers: {workforceManager.InjuredCount}");

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Healers: {healSlot.AssignedWorkers}/{healSlot.MaxWorkers}", GUILayout.Width(120));

            GUI.enabled = IsDayPhase && healSlot.AssignedWorkers > 0;
            if (GUILayout.Button("-", GUILayout.Width(25)))
            {
                workforceManager.UnassignHealer();
            }
            GUI.enabled = IsDayPhase && healSlot.CanAddWorker && workforceManager.IdleCount > 0;
            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                workforceManager.AssignHealer();
            }
            GUI.enabled = true;

            GUILayout.EndHorizontal();

            string healInfo = healSlot.AssignedWorkers > 0
                ? "Healing BOOSTED (faster recovery)"
                : "Natural recovery only";
            GUILayout.Label(healInfo);

            GUILayout.EndVertical();
        }
    }
}

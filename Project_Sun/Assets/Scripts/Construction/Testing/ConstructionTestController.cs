using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectSun.Construction.Testing
{
    /// <summary>
    /// IMGUI-based test controller for the construction system.
    /// Provides slot selection, construction commands, turn advancement, and damage testing.
    /// </summary>
    public class ConstructionTestController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BuildingManager buildingManager;
        [SerializeField] private Camera mainCamera;

        [Header("Test Settings")]
        [SerializeField] private float testDamageAmount = 40f;

        private BuildingSlot selectedSlot;
        private BuildingSlotVisual selectedVisual;
        private int turnCount;
        private string logMessage = "";
        private float logTimer;

        private void Update()
        {
            HandleSlotSelection();

            if (logTimer > 0f)
                logTimer -= Time.deltaTime;
        }

        private void HandleSlotSelection()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            if (!mouse.leftButton.wasPressedThisFrame) return;
            if (mainCamera == null) return;

            Vector2 mousePos = mouse.position.ReadValue();

            // Don't raycast if mouse is over GUI area (left panel)
            if (mousePos.x < 280f) return;

            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                var slot = hit.collider.GetComponentInParent<BuildingSlot>();
                if (slot != null)
                {
                    SelectSlot(slot);
                }
            }
        }

        private void SelectSlot(BuildingSlot slot)
        {
            // Deselect previous
            if (selectedVisual != null)
                selectedVisual.SetSelected(false);

            selectedSlot = slot;
            selectedVisual = slot.GetComponent<BuildingSlotVisual>();

            if (selectedVisual != null)
                selectedVisual.SetSelected(true);
        }

        private void Log(string msg)
        {
            logMessage = msg;
            logTimer = 3f;
            Debug.Log($"[ConstructionTest] {msg}");
        }

        private void OnGUI()
        {
            GUI.skin.label.fontSize = 14;
            GUI.skin.button.fontSize = 13;
            GUI.skin.box.fontSize = 13;

            // Left panel
            GUILayout.BeginArea(new Rect(10, 10, 260, Screen.height - 20));

            DrawGlobalPanel();
            GUILayout.Space(10);
            DrawSelectedSlotPanel();
            GUILayout.Space(10);
            DrawLogPanel();

            GUILayout.EndArea();
        }

        private void DrawGlobalPanel()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label($"<b>Construction Test</b>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 16 });
            GUILayout.Space(5);

            GUILayout.Label($"Turn: {turnCount}");

            if (buildingManager != null)
            {
                int activeCount = buildingManager.GetSlotsByState(BuildingSlotState.Active).Count;
                int constructingCount = buildingManager.GetSlotsByState(BuildingSlotState.Constructing).Count;
                float defenseCost = buildingManager.GetTotalDefenseResourceCost();

                GUILayout.Label($"Active: {activeCount}  Building: {constructingCount}");
                GUILayout.Label($"Defense Resource: {buildingManager.CurrentDefenseResource:F0} (Used: {defenseCost:F0})");
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Next Turn", GUILayout.Height(35)))
            {
                if (buildingManager != null)
                {
                    buildingManager.ProcessTurn();
                    turnCount++;
                    Log($"Turn {turnCount} processed.");
                }
            }

            if (GUILayout.Button("Reveal All Hidden Slots"))
            {
                if (buildingManager != null)
                {
                    foreach (var s in buildingManager.AllSlots)
                    {
                        if (s.State == BuildingSlotState.Hidden)
                            s.Reveal();
                    }
                    Log("All hidden slots revealed.");
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawSelectedSlotPanel()
        {
            GUILayout.BeginVertical("box");

            if (selectedSlot == null)
            {
                GUILayout.Label("Click a slot to select");
                GUILayout.EndVertical();
                return;
            }

            var data = selectedSlot.CurrentBuildingData;
            string name = data != null ? data.buildingName : "(no data)";

            GUILayout.Label($"<b>{name}</b>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 15 });
            GUILayout.Label($"State: {selectedSlot.State}");
            GUILayout.Label($"Tier: {selectedSlot.CurrentTier}  Workers: {selectedSlot.AssignedWorkers}");

            if (selectedSlot.Health != null)
            {
                var h = selectedSlot.Health;
                GUILayout.Label($"HP: {h.CurrentHP:F0} / {h.MaxHP:F0} ({h.HPRatio * 100:F0}%)");
            }

            if (data != null)
            {
                GUILayout.Label($"Category: {data.category}");
                GUILayout.Label($"Build Turns: {data.constructionTurns}");
            }

            GUILayout.Space(5);

            // State-specific actions
            switch (selectedSlot.State)
            {
                case BuildingSlotState.Empty:
                    DrawEmptyActions();
                    break;
                case BuildingSlotState.Constructing:
                case BuildingSlotState.Upgrading:
                    DrawConstructingActions();
                    break;
                case BuildingSlotState.Active:
                    DrawActiveActions();
                    break;
                case BuildingSlotState.Destroyed:
                    DrawDestroyedActions();
                    break;
                case BuildingSlotState.Repairing:
                    DrawRepairingActions();
                    break;
            }

            // Worker assignment (available during construction/upgrade/repair/active)
            if (selectedSlot.State == BuildingSlotState.Constructing ||
                selectedSlot.State == BuildingSlotState.Upgrading ||
                selectedSlot.State == BuildingSlotState.Repairing ||
                selectedSlot.State == BuildingSlotState.Active)
            {
                GUILayout.Space(5);
                DrawWorkerControls();
            }

            // Damage test (available when targetable)
            if (selectedSlot.IsTargetable)
            {
                GUILayout.Space(5);
                DrawDamageControls();
            }

            GUILayout.EndVertical();
        }

        private void DrawEmptyActions()
        {
            if (selectedSlot.AssignedBuilding == null)
            {
                GUILayout.Label("No building assigned to this slot.");
                return;
            }

            if (GUILayout.Button($"Build: {selectedSlot.AssignedBuilding.buildingName}"))
            {
                if (buildingManager != null && buildingManager.RequestConstruction(selectedSlot))
                {
                    Log($"Construction started: {selectedSlot.AssignedBuilding.buildingName}");
                }
                else
                {
                    Log("Cannot start construction.");
                }
            }
        }

        private void DrawConstructingActions()
        {
            if (GUILayout.Button("Cancel"))
            {
                if (buildingManager != null)
                {
                    buildingManager.CancelConstruction(selectedSlot);
                    Log("Construction cancelled.");
                }
            }
        }

        private void DrawActiveActions()
        {
            var data = selectedSlot.CurrentBuildingData;
            if (data == null) return;

            if (data.upgradeBranches != null && data.upgradeBranches.Count > 0)
            {
                GUILayout.Label("Upgrade Branches:");
                foreach (var branch in data.upgradeBranches)
                {
                    if (branch == null) continue;
                    string label = branch.requiresResearch ? $"{branch.branchName} (Locked)" : branch.branchName;
                    GUI.enabled = !branch.requiresResearch;
                    if (GUILayout.Button($"Upgrade: {label}"))
                    {
                        if (buildingManager != null && buildingManager.RequestUpgrade(selectedSlot, branch))
                        {
                            Log($"Upgrade started: {branch.branchName}");
                        }
                    }
                    GUI.enabled = true;
                }
            }
        }

        private void DrawDestroyedActions()
        {
            if (GUILayout.Button("Start Repair"))
            {
                if (buildingManager != null && buildingManager.RequestRepair(selectedSlot))
                {
                    Log("Repair started.");
                }
            }
        }

        private void DrawRepairingActions()
        {
            GUILayout.Label("Repairing in progress...");
        }

        private void DrawWorkerControls()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Workers: {selectedSlot.AssignedWorkers}", GUILayout.Width(100));

            if (GUILayout.Button("-", GUILayout.Width(30)))
            {
                selectedSlot.SetWorkers(selectedSlot.AssignedWorkers - 1);
            }
            if (GUILayout.Button("+", GUILayout.Width(30)))
            {
                selectedSlot.SetWorkers(selectedSlot.AssignedWorkers + 1);
            }

            GUILayout.EndHorizontal();
        }

        private void DrawDamageControls()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Damage:", GUILayout.Width(60));
            string dmgStr = GUILayout.TextField(testDamageAmount.ToString("F0"), GUILayout.Width(50));
            if (float.TryParse(dmgStr, out float val))
                testDamageAmount = val;

            if (GUILayout.Button("Hit!"))
            {
                if (selectedSlot.Health != null)
                {
                    selectedSlot.Health.TakeDamage(testDamageAmount);
                    Log($"Dealt {testDamageAmount} damage. HP: {selectedSlot.Health.CurrentHP:F0}/{selectedSlot.Health.MaxHP:F0}");
                }
            }
            GUILayout.EndHorizontal();
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
    }
}

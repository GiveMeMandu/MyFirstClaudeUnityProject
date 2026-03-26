using UnityEngine;

namespace ProjectSun.Construction.Testing
{
    /// <summary>
    /// Visualizes BuildingSlot state using primitive meshes and colors.
    /// Attach to the same GameObject as BuildingSlot.
    /// </summary>
    public class BuildingSlotVisual : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MeshRenderer slotIndicator;
        [SerializeField] private MeshRenderer buildingModel;
        [SerializeField] private TextMesh stateLabel;

        [Header("Settings")]
        [SerializeField] private bool selected;

        private BuildingSlot slot;
        private Material slotMaterial;
        private Material buildingMaterial;
        private float pulseTimer;

        private static readonly Color ColorHidden = new(0.3f, 0.3f, 0.3f, 0f);
        private static readonly Color ColorEmpty = new(1f, 1f, 1f, 0.3f);
        private static readonly Color ColorConstructing = new(1f, 0.9f, 0.2f, 0.6f);
        private static readonly Color ColorUpgrading = new(1f, 0.8f, 0f, 0.8f);
        private static readonly Color ColorDamaged = new(0.8f, 0.2f, 0.1f, 0.5f);
        private static readonly Color ColorDestroyed = new(0.2f, 0.15f, 0.1f, 0.8f);
        private static readonly Color ColorRepairing = new(0.3f, 0.6f, 1f, 0.7f);
        private static readonly Color ColorSelected = new(1f, 1f, 0f, 0.4f);

        private void Awake()
        {
            slot = GetComponent<BuildingSlot>();

            if (slotIndicator != null)
            {
                slotMaterial = new Material(slotIndicator.sharedMaterial);
                slotIndicator.material = slotMaterial;
            }

            if (buildingModel != null)
            {
                buildingMaterial = new Material(buildingModel.sharedMaterial);
                buildingModel.material = buildingMaterial;
            }
        }

        private void OnEnable()
        {
            if (slot != null)
                slot.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            if (slot != null)
                slot.OnStateChanged -= HandleStateChanged;
        }

        private void Start()
        {
            UpdateVisual(slot != null ? slot.State : BuildingSlotState.Hidden);
        }

        private void Update()
        {
            if (slot == null) return;

            var state = slot.State;
            if (state == BuildingSlotState.Upgrading || state == BuildingSlotState.Repairing)
            {
                pulseTimer += Time.deltaTime * 3f;
                float pulse = (Mathf.Sin(pulseTimer) + 1f) * 0.5f;

                if (buildingMaterial != null)
                {
                    Color baseColor = state == BuildingSlotState.Upgrading ? ColorUpgrading : ColorRepairing;
                    Color target = GetCategoryColor();
                    buildingMaterial.color = Color.Lerp(target, baseColor, pulse);
                }
            }
        }

        public void SetSelected(bool isSelected)
        {
            selected = isSelected;
            UpdateSlotIndicatorColor();
        }

        private void HandleStateChanged(BuildingSlot changedSlot, BuildingSlotState newState)
        {
            UpdateVisual(newState);
        }

        private void UpdateVisual(BuildingSlotState state)
        {
            pulseTimer = 0f;

            switch (state)
            {
                case BuildingSlotState.Hidden:
                    SetVisibility(false, false);
                    UpdateLabel("Hidden");
                    break;

                case BuildingSlotState.Empty:
                    SetVisibility(true, false);
                    UpdateSlotIndicatorColor();
                    UpdateLabel("Empty");
                    break;

                case BuildingSlotState.Constructing:
                    SetVisibility(true, true);
                    SetBuildingColor(ColorConstructing);
                    UpdateSlotIndicatorColor();
                    UpdateLabel("Building...");
                    break;

                case BuildingSlotState.Active:
                    SetVisibility(true, true);
                    SetBuildingColor(GetCategoryColor());
                    UpdateSlotIndicatorColor();
                    UpdateLabel(GetBuildingName());
                    break;

                case BuildingSlotState.Upgrading:
                    SetVisibility(true, true);
                    UpdateSlotIndicatorColor();
                    UpdateLabel("Upgrading...");
                    break;

                case BuildingSlotState.Damaged:
                    SetVisibility(true, true);
                    Color dmgColor = Color.Lerp(GetCategoryColor(), ColorDamaged, 0.5f);
                    SetBuildingColor(dmgColor);
                    UpdateSlotIndicatorColor();
                    UpdateLabel($"{GetBuildingName()} (DMG)");
                    break;

                case BuildingSlotState.Destroyed:
                    SetVisibility(true, true);
                    SetBuildingColor(ColorDestroyed);
                    if (buildingModel != null)
                        buildingModel.transform.localScale = new Vector3(1.2f, 0.3f, 1.2f);
                    UpdateSlotIndicatorColor();
                    UpdateLabel("Destroyed");
                    break;

                case BuildingSlotState.Repairing:
                    SetVisibility(true, true);
                    if (buildingModel != null)
                        buildingModel.transform.localScale = Vector3.one;
                    UpdateSlotIndicatorColor();
                    UpdateLabel("Repairing...");
                    break;
            }
        }

        private void SetVisibility(bool showIndicator, bool showBuilding)
        {
            if (slotIndicator != null)
                slotIndicator.gameObject.SetActive(showIndicator);
            if (buildingModel != null)
            {
                buildingModel.gameObject.SetActive(showBuilding);
                if (showBuilding)
                    buildingModel.transform.localScale = Vector3.one;
            }
            if (stateLabel != null)
                stateLabel.gameObject.SetActive(showIndicator);
        }

        private void SetBuildingColor(Color color)
        {
            if (buildingMaterial != null)
                buildingMaterial.color = color;
        }

        private void UpdateSlotIndicatorColor()
        {
            if (slotMaterial == null) return;

            if (selected)
                slotMaterial.color = ColorSelected;
            else if (slot != null && slot.State == BuildingSlotState.Empty)
                slotMaterial.color = ColorEmpty;
            else
                slotMaterial.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);
        }

        private Color GetCategoryColor()
        {
            if (slot == null || slot.CurrentBuildingData == null)
                return Color.white;

            return slot.CurrentBuildingData.category switch
            {
                BuildingCategory.Resource => new Color(0.2f, 0.8f, 0.3f),
                BuildingCategory.Defense => new Color(0.9f, 0.2f, 0.2f),
                BuildingCategory.Research => new Color(0.3f, 0.5f, 1f),
                BuildingCategory.Administration => new Color(0.7f, 0.3f, 0.9f),
                BuildingCategory.Exploration => new Color(1f, 0.6f, 0.1f),
                BuildingCategory.Wall => new Color(0.6f, 0.6f, 0.6f),
                _ => Color.white
            };
        }

        private string GetBuildingName()
        {
            if (slot == null || slot.CurrentBuildingData == null) return "???";
            return slot.CurrentBuildingData.buildingName;
        }

        private void UpdateLabel(string text)
        {
            if (stateLabel != null)
                stateLabel.text = text;
        }

        private void OnDestroy()
        {
            if (slotMaterial != null) Destroy(slotMaterial);
            if (buildingMaterial != null) Destroy(buildingMaterial);
        }
    }
}

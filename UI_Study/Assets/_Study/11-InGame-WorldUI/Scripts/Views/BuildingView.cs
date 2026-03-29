using UIStudy.InGameUI.Models;
using UnityEngine;

namespace UIStudy.InGameUI.Views
{
    /// <summary>
    /// MonoBehaviour attached to each 3D cube building.
    /// Holds BuildingData reference and applies the color to MeshRenderer.
    /// No click handling — that is the Presenter's responsibility.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(MeshRenderer))]
    public class BuildingView : MonoBehaviour
    {
        private BuildingData _data;
        private MeshRenderer _meshRenderer;

        public BuildingData Data => _data;

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        /// <summary>
        /// Assign building data and apply the visual color.
        /// Called by Presenter or LifetimeScope during setup.
        /// </summary>
        public void SetData(BuildingData data)
        {
            _data = data;

            if (_meshRenderer == null)
                _meshRenderer = GetComponent<MeshRenderer>();

            // Create a material instance to avoid shared-material side effects
            _meshRenderer.material.color = data.BuildingColor;
        }
    }
}

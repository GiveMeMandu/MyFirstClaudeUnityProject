using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ProjectSun.V2.Defense.ECS
{
    /// <summary>
    /// 플로우 필드 초기 설정.
    /// 씬에 배치하면 Start()에서 FlowFieldConfig 싱글턴 엔티티를 생성.
    ///
    /// 그리드 커버리지: WorldOrigin + (Width * CellSize, Height * CellSize)
    /// 기본값: 100x100 x 2m = 200m x 200m, 원점(-100, 0, -100)
    /// → 월드 (-100,-100) ~ (100, 100) 커버
    /// </summary>
    public class FlowFieldSetup : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] int gridWidth = 100;
        [SerializeField] int gridHeight = 100;
        [SerializeField] float cellSize = 2f;
        [SerializeField] Vector3 worldOrigin = new Vector3(-100, 0, -100);

        void Start()
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            var entity = em.CreateEntity();
            em.AddComponentData(entity, new FlowFieldConfig
            {
                GridWidth = gridWidth,
                GridHeight = gridHeight,
                CellSize = cellSize,
                WorldOrigin = new float3(worldOrigin.x, worldOrigin.y, worldOrigin.z),
                LastComputedFrame = -1,
                NeedsRecompute = true
            });

            Debug.Log($"[FlowField] Grid: {gridWidth}x{gridHeight}, cell={cellSize}m, " +
                      $"world=({worldOrigin.x},{worldOrigin.z}) ~ " +
                      $"({worldOrigin.x + gridWidth * cellSize},{worldOrigin.z + gridHeight * cellSize})");
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            float w = gridWidth * cellSize;
            float h = gridHeight * cellSize;
            var center = worldOrigin + new Vector3(w * 0.5f, 0, h * 0.5f);
            Gizmos.DrawWireCube(center, new Vector3(w, 0.1f, h));
        }
    }
}

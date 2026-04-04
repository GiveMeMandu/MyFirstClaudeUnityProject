using Unity.Entities;
using Unity.Mathematics;

namespace ProjectSun.V2.Defense.ECS
{
    /// <summary>
    /// 플로우 필드 그리드 설정 싱글턴.
    /// FlowFieldSetup(MonoBehaviour)가 밤 전투 시작 시 생성.
    /// </summary>
    public struct FlowFieldConfig : IComponentData
    {
        public int GridWidth;
        public int GridHeight;
        public float CellSize;
        public float3 WorldOrigin;
        /// <summary>마지막 재계산 시 프레임. -1이면 미계산.</summary>
        public int LastComputedFrame;
        /// <summary>true면 다음 프레임에 재계산. 건물 파괴/건설 시 set.</summary>
        public bool NeedsRecompute;
    }

    /// <summary>
    /// 플로우 필드를 따라 이동하는 엔티티에 부착.
    /// EnemyTag + FlowFieldAgent = 플로우 필드 기반 이동.
    /// FlowFieldAgent가 없는 적은 기존 EnemyMovementSystem으로 폴백.
    /// </summary>
    public struct FlowFieldAgent : IComponentData
    {
        /// <summary>
        /// true: 공중 유닛 전용 필드 사용 (방벽 통과).
        /// false: 지상 필드 사용 (방벽 = 장애물).
        /// </summary>
        public bool UseAirField;
    }
}

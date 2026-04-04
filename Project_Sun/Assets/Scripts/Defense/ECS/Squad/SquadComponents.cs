using Unity.Entities;
using Unity.Mathematics;

namespace ProjectSun.V2.Defense.ECS
{
    /// <summary>분대 엔티티 태그.</summary>
    public struct SquadTag : IComponentData { }

    /// <summary>분대 식별자. 0-based 인덱스.</summary>
    public struct SquadId : IComponentData
    {
        public int Value;
    }

    /// <summary>분대 스탯.</summary>
    public struct SquadStats : IComponentData
    {
        public float CombatPower;
        public float AttackRange;
        public float AttackSpeed;   // attacks per second
        public float MoveSpeed;
        public float MaxHP;
        public float CurrentHP;
        public int MemberCount;     // NPC 분대원 수 (5~10)
    }

    /// <summary>
    /// 분대 현재 명령.
    /// SquadCommandSystem이 큐에서 꺼내 설정.
    /// SquadMovementSystem이 읽어 이동/전투 처리.
    /// </summary>
    public struct SquadCommand : IComponentData
    {
        /// <summary>0=Idle, 1=Move, 2=AttackMove, 3=HoldPosition</summary>
        public SquadCommandType Type;
        /// <summary>이동/공격 목표 위치.</summary>
        public float3 TargetPosition;
        /// <summary>공격 대상 엔티티 (AttackMove에서 특정 대상 지정 시).</summary>
        public Entity TargetEntity;
        /// <summary>명령이 설정된 시간 (반응 지연 측정용).</summary>
        public double IssuedTime;
    }

    public enum SquadCommandType : byte
    {
        Idle = 0,
        Move = 1,           // 지정 위치로 이동. 이동 중 교전하지 않음
        AttackMove = 2,     // 지정 위치로 이동하며 경로상 적을 공격
        HoldPosition = 3    // 현재 위치에서 정지. 사거리 내 적만 공격
    }

    /// <summary>분대 공격 타이머.</summary>
    public struct SquadAttackTimer : IComponentData
    {
        public float TimeSinceLastAttack;
    }

    /// <summary>
    /// 분대 선택 상태 (UI 피드백용).
    /// MonoBehaviour(SquadInputController)가 읽는다.
    /// </summary>
    public struct SquadSelected : IComponentData
    {
        public bool Value;
    }
}

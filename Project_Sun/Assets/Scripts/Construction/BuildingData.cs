using System.Collections.Generic;
using ProjectSun.Workforce;
using UnityEngine;

namespace ProjectSun.Construction
{
    [CreateAssetMenu(fileName = "NewBuilding", menuName = "ProjectSun/Construction/Building Data")]
    public class BuildingData : ScriptableObject
    {
        [Header("기본 정보")]
        public string buildingName;

        [TextArea(2, 4)]
        public string description;

        public BuildingCategory category;

        [Tooltip("본부 건물 여부 (파괴 시 게임오버)")]
        public bool isHeadquarters;

        [Header("건설")]
        [Tooltip("건설 자원 비용")]
        public List<ResourceCost> constructionCost = new();

        [Tooltip("건설 소요 턴 (인력 배치 턴 기준)")]
        [Min(1)]
        public int constructionTurns = 1;

        [Header("인력")]
        [Tooltip("기본 인력 슬롯 수")]
        [Min(1)]
        public int baseWorkerSlots = 1;

        [Tooltip("건설 시 동시 투입 가능 인력 수 (기본값, 연구로 확장)")]
        [Min(1)]
        public int maxConstructionWorkers = 1;

        [Header("체력")]
        [Tooltip("건물 최대 HP")]
        [Min(1f)]
        public float maxHP = 100f;

        [Tooltip("턴당 자동 회복량 (손상 상태에서만 적용)")]
        [Min(0f)]
        public float autoRepairRate = 25f;

        [Tooltip("파괴 후 수리 소요 턴")]
        [Min(1)]
        public int repairTurns = 2;

        [Header("방어 운영 (Defense 카테고리 전용)")]
        [Tooltip("인력 슬롯당 운영 자원 소모량")]
        public float defenseResourceCostPerSlot;

        [Header("타워 스탯 (Defense 카테고리 전용)")]
        [Tooltip("타워 사거리")]
        [Min(0f)]
        public float towerRange = 8f;

        [Tooltip("타워 데미지")]
        [Min(0f)]
        public float towerDamage = 10f;

        [Tooltip("타워 공격 속도 (초당 공격 횟수)")]
        [Min(0.1f)]
        public float towerAttackSpeed = 1f;

        [Tooltip("공중 유닛 공격 가능 여부")]
        public bool towerCanTargetAir;

        [Header("인력 슬롯 구성")]
        [Tooltip("건물의 인력 슬롯 구성 (null이면 카테고리 기반 기본 슬롯 1개 생성)")]
        public WorkerSlotConfig workerSlotConfig;

        [Header("업그레이드 분기")]
        [Tooltip("현재 티어")]
        [Min(1)]
        public int tier = 1;

        [Tooltip("사용 가능한 업그레이드 분기 목록")]
        public List<UpgradeBranchData> upgradeBranches = new();

        [Header("비주얼")]
        [Tooltip("건설 완료 시 사용할 프리팹")]
        public GameObject completedPrefab;

        [Tooltip("건설 중 사용할 프리팹 (비계 등)")]
        public GameObject constructionPrefab;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSun.WallExpansion
{
    /// <summary>
    /// 방벽 확장 레벨 1개의 정적 데이터.
    /// WallExpansionDataSO.levels 리스트의 원소로 사용.
    /// </summary>
    [Serializable]
    public class WallExpansionLevelData
    {
        [Header("레벨 정보")]
        [Tooltip("이 데이터가 나타내는 확장 목표 레벨 (Lv.0→Lv.1이면 1)")]
        [Min(1)]
        public int level = 1;

        [Header("비용")]
        [Tooltip("기초 자원 비용")]
        [Min(0)]
        public int basicCost;

        [Tooltip("고급 자원 비용")]
        [Min(0)]
        public int advancedCost;

        [Header("슬롯 해금")]
        [Tooltip("이 레벨에서 해금되는 BuildingSlot ID(이름) 목록")]
        public List<string> slotIds = new();

        [Header("시스템 해금")]
        [Tooltip("이 레벨에서 해금되는 게임 시스템 목록 (시나리오별 선택적)")]
        public List<FeatureUnlockType> unlockedFeatures = new();

        [Header("방어 범위")]
        [Tooltip("이 레벨에서 추가되는 적 진입 경로(스폰 포인트) 수")]
        [Min(0)]
        public int additionalSpawnPoints = 1;

        [Header("선행 조건 (선택적)")]
        [Tooltip("기술 연구 완료가 필요한지 여부")]
        public bool requiresResearch;

        [Tooltip("최소 턴 조건 (0 = 제한 없음)")]
        [Min(0)]
        public int minTurn;
    }
}

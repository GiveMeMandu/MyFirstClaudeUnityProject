using System;
using System.Collections.Generic;
using ProjectSun.Construction;
using ProjectSun.Resource;
using ProjectSun.Turn;
using UnityEngine;

namespace ProjectSun.WallExpansion
{
    /// <summary>
    /// 방벽 레벨 관리, 확장 명령 처리, 슬롯 해금 조율.
    /// 시나리오 시작 시 WallExpansionDataSO를 할당하여 사용.
    /// </summary>
    public class WallExpansionManager : MonoBehaviour
    {
        [Header("시나리오 데이터")]
        [Tooltip("현재 시나리오의 방벽 확장 데이터 (null이면 기본값 생성)")]
        [SerializeField] private WallExpansionDataSO expansionData;

        [Header("연동")]
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private BuildingManager buildingManager;
        [SerializeField] private TurnManager turnManager;

        [Header("씬 기반 슬롯 그룹 (SO의 slotIds와 병행 사용 가능)")]
        [Tooltip("씬에 배치된 WallExpansionSlotGroup 목록. 레벨 매칭으로 해금.")]
        [SerializeField] private List<WallExpansionSlotGroup> slotGroups = new();

        [Header("런타임 상태")]
        [SerializeField] private int currentWallLevel;

        private bool isExpanding;

        // ── Public Properties ──

        public int CurrentWallLevel => currentWallLevel;
        public int MaxWallLevel => expansionData != null ? expansionData.MaxLevel : 4;
        public bool IsMaxLevel => currentWallLevel >= MaxWallLevel;
        public bool IsExpanding => isExpanding;
        public WallExpansionDataSO ExpansionData => expansionData;

        // ── Events ──

        /// <summary>확장 시작 직전 (연출 시작). int = 새 레벨.</summary>
        public event Action<int> OnExpansionStarted;

        /// <summary>확장 완료 (슬롯 해금 + 스폰 포인트 갱신 완료). int = 새 레벨.</summary>
        public event Action<int> OnExpansionCompleted;

        /// <summary>시스템 해금 이벤트. 해금된 FeatureUnlockType 리스트.</summary>
        public event Action<List<FeatureUnlockType>> OnFeaturesUnlocked;

        /// <summary>확장 실패 (사유 메시지).</summary>
        public event Action<string> OnExpansionFailed;

        // ── Lifecycle ──

        private void Awake()
        {
            if (expansionData == null)
            {
                Debug.LogWarning("[WallExpansionManager] ExpansionData가 없습니다. 기본값을 사용합니다.");
            }
        }

        // ── Public API ──

        /// <summary>
        /// 시나리오 데이터를 런타임에 할당 (시나리오 시스템 연동용).
        /// </summary>
        public void SetExpansionData(WallExpansionDataSO data)
        {
            expansionData = data;
        }

        /// <summary>
        /// 다음 레벨 확장 가능 여부를 검증하고, 불가 사유를 out으로 반환.
        /// </summary>
        public bool CanExpand(out string reason)
        {
            reason = string.Empty;

            if (isExpanding)
            {
                reason = "확장 진행 중입니다.";
                return false;
            }

            if (IsMaxLevel)
            {
                reason = "최대 확장 완료";
                return false;
            }

            // 낮 페이즈에서만 확장 허용
            if (turnManager != null && turnManager.CurrentPhase != TurnPhase.DayPhase)
            {
                reason = "낮 페이즈에서만 확장 가능합니다.";
                return false;
            }

            int targetLevel = currentWallLevel + 1;
            var levelData = GetNextLevelData();

            if (levelData == null)
            {
                reason = "확장 데이터를 찾을 수 없습니다.";
                return false;
            }

            // 최소 턴 조건
            if (levelData.minTurn > 0 && turnManager != null && turnManager.CurrentTurn < levelData.minTurn)
            {
                reason = $"턴 {levelData.minTurn} 이후에 확장 가능합니다. (현재: 턴 {turnManager.CurrentTurn})";
                return false;
            }

            // 연구 선행 조건
            if (levelData.requiresResearch)
            {
                // 추후 기술 트리 시스템 연동 시 검증
                reason = "선행 연구가 필요합니다.";
                return false;
            }

            // 자원 검증
            if (resourceManager != null)
            {
                if (!HasEnoughResources(levelData))
                {
                    int currentBasic = resourceManager.GetResource(ResourceType.Basic);
                    int currentAdvanced = resourceManager.GetResource(ResourceType.Advanced);
                    reason = $"자원 부족 — 기초: {currentBasic}/{levelData.basicCost}, 고급: {currentAdvanced}/{levelData.advancedCost}";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 확장 명령 실행. 자원 즉시 차감 → 확장 처리.
        /// 반환값: 성공 여부.
        /// </summary>
        public bool TryExpand()
        {
            if (!CanExpand(out string reason))
            {
                OnExpansionFailed?.Invoke(reason);
                Debug.Log($"[WallExpansionManager] 확장 실패: {reason}");
                return false;
            }

            var levelData = GetNextLevelData();
            if (levelData == null) return false;

            // 자원 선차감
            if (resourceManager != null)
            {
                SpendExpansionCost(levelData);
            }

            // 확장 시작
            isExpanding = true;
            currentWallLevel++;

            OnExpansionStarted?.Invoke(currentWallLevel);

            // 슬롯 해금 (SO 기반 slotIds)
            RevealSlots(levelData);

            // 슬롯 해금 (씬 기반 SlotGroup)
            RevealSlotGroups(currentWallLevel);

            // 시스템 해금
            if (levelData.unlockedFeatures != null && levelData.unlockedFeatures.Count > 0)
            {
                OnFeaturesUnlocked?.Invoke(levelData.unlockedFeatures);
            }

            // 확장 완료
            isExpanding = false;
            OnExpansionCompleted?.Invoke(currentWallLevel);

            Debug.Log($"[WallExpansionManager] 방벽 레벨 {currentWallLevel} 확장 완료. " +
                      $"슬롯 {levelData.slotIds.Count}개 해금, 스폰 포인트 +{levelData.additionalSpawnPoints}");

            return true;
        }

        /// <summary>
        /// 다음 확장 레벨의 비용/조건 정보를 반환. UI 표시용.
        /// </summary>
        public WallExpansionLevelData GetNextLevelData()
        {
            if (IsMaxLevel) return null;

            int targetLevel = currentWallLevel + 1;

            if (expansionData != null)
            {
                return expansionData.GetLevelData(targetLevel);
            }

            // 폴백: 기본 비용
            return new WallExpansionLevelData
            {
                level = targetLevel,
                basicCost = 50 + targetLevel * 30,
                advancedCost = Mathf.Max(0, (targetLevel - 1) * 20),
                additionalSpawnPoints = 1
            };
        }

        /// <summary>
        /// 현재 레벨에서 총 활성 스폰 포인트 수를 반환.
        /// BattleManager가 스폰 시 참조.
        /// </summary>
        public int GetActiveSpawnPointCount()
        {
            int count = 1; // Lv.0 기본 1개

            if (expansionData == null) return count + currentWallLevel;

            for (int lv = 1; lv <= currentWallLevel; lv++)
            {
                var data = expansionData.GetLevelData(lv);
                if (data != null)
                {
                    count += data.additionalSpawnPoints;
                }
                else
                {
                    count += 1; // 폴백
                }
            }

            return count;
        }

        // ── Private Helpers ──

        private bool HasEnoughResources(WallExpansionLevelData levelData)
        {
            if (resourceManager == null) return true;

            int currentBasic = resourceManager.GetResource(ResourceType.Basic);
            int currentAdvanced = resourceManager.GetResource(ResourceType.Advanced);

            return currentBasic >= levelData.basicCost && currentAdvanced >= levelData.advancedCost;
        }

        private void SpendExpansionCost(WallExpansionLevelData levelData)
        {
            if (levelData.basicCost > 0)
            {
                resourceManager.SpendResource(ResourceType.Basic, levelData.basicCost);
            }
            if (levelData.advancedCost > 0)
            {
                resourceManager.SpendResource(ResourceType.Advanced, levelData.advancedCost);
            }
        }

        private void RevealSlots(WallExpansionLevelData levelData)
        {
            if (buildingManager == null || levelData.slotIds == null) return;

            foreach (var slotId in levelData.slotIds)
            {
                var slot = FindSlotById(slotId);
                if (slot != null)
                {
                    slot.Reveal();
                }
                else
                {
                    Debug.LogWarning($"[WallExpansionManager] 슬롯 '{slotId}'을(를) 찾을 수 없습니다.");
                }
            }
        }

        private void RevealSlotGroups(int level)
        {
            foreach (var group in slotGroups)
            {
                if (group != null && group.UnlockLevel == level)
                {
                    group.RevealAll();
                }
            }
        }

        private BuildingSlot FindSlotById(string slotId)
        {
            if (buildingManager == null) return null;

            foreach (var slot in buildingManager.AllSlots)
            {
                if (slot != null && slot.gameObject.name == slotId)
                {
                    return slot;
                }
            }

            return null;
        }
    }
}

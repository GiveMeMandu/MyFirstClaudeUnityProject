using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSun.WallExpansion
{
    /// <summary>
    /// 방벽 확장에 따른 방어 범위(적 스폰 포인트) 관리.
    /// WallExpansionManager.OnExpansionCompleted에 구독하여 스폰 포인트를 활성화.
    /// BattleManager의 spawnPoints 리스트와 동기화.
    /// </summary>
    public class DefenseRangeController : MonoBehaviour
    {
        [Header("스폰 포인트 (레벨 순서대로 배치)")]
        [Tooltip("모든 스폰 포인트. 인덱스 순서대로 활성화됨.\n" +
                 "인덱스 0은 항상 활성 (Lv.0 기본). 나머지는 확장에 따라 활성화.")]
        [SerializeField] private List<Transform> allSpawnPoints = new();

        [Header("연동")]
        [SerializeField] private WallExpansionManager wallExpansionManager;

        [Header("런타임 상태")]
        [SerializeField] private int activeSpawnPointCount = 1;

        public int ActiveSpawnPointCount => activeSpawnPointCount;
        public IReadOnlyList<Transform> AllSpawnPoints => allSpawnPoints;

        /// <summary>활성 스폰 포인트 수가 변경됨. int = 새 활성 수.</summary>
        public event Action<int> OnActiveSpawnPointsChanged;

        private void Start()
        {
            if (wallExpansionManager != null)
            {
                wallExpansionManager.OnExpansionCompleted += HandleExpansionCompleted;

                // 초기 상태 동기화
                SyncActiveCount();
            }

            ApplySpawnPointVisibility();
        }

        private void OnDestroy()
        {
            if (wallExpansionManager != null)
            {
                wallExpansionManager.OnExpansionCompleted -= HandleExpansionCompleted;
            }
        }

        /// <summary>
        /// 현재 활성 스폰 포인트 목록을 반환.
        /// BattleManager가 전투 시작 시 참조.
        /// </summary>
        public List<Transform> GetActiveSpawnPoints()
        {
            var active = new List<Transform>();
            for (int i = 0; i < activeSpawnPointCount && i < allSpawnPoints.Count; i++)
            {
                if (allSpawnPoints[i] != null)
                {
                    active.Add(allSpawnPoints[i]);
                }
            }
            return active;
        }

        /// <summary>
        /// 직접 활성 수를 설정 (테스트/디버그용).
        /// </summary>
        public void SetActiveSpawnPointCount(int count)
        {
            activeSpawnPointCount = Mathf.Clamp(count, 1, allSpawnPoints.Count);
            ApplySpawnPointVisibility();
            OnActiveSpawnPointsChanged?.Invoke(activeSpawnPointCount);
        }

        private void HandleExpansionCompleted(int newLevel)
        {
            SyncActiveCount();
            ApplySpawnPointVisibility();
            OnActiveSpawnPointsChanged?.Invoke(activeSpawnPointCount);

            Debug.Log($"[DefenseRangeController] 방벽 레벨 {newLevel} — 활성 스폰 포인트: {activeSpawnPointCount}/{allSpawnPoints.Count}");
        }

        private void SyncActiveCount()
        {
            if (wallExpansionManager != null)
            {
                activeSpawnPointCount = Mathf.Min(
                    wallExpansionManager.GetActiveSpawnPointCount(),
                    allSpawnPoints.Count);
            }
        }

        /// <summary>
        /// 비활성 스폰 포인트를 시각적으로 숨기고 활성만 표시.
        /// </summary>
        private void ApplySpawnPointVisibility()
        {
            for (int i = 0; i < allSpawnPoints.Count; i++)
            {
                if (allSpawnPoints[i] != null)
                {
                    allSpawnPoints[i].gameObject.SetActive(i < activeSpawnPointCount);
                }
            }
        }
    }
}

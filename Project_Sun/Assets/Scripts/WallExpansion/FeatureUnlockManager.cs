using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSun.WallExpansion
{
    /// <summary>
    /// 시스템 해금 프레임워크.
    /// WallExpansionManager.OnFeaturesUnlocked에 구독하여 해금 상태를 관리하고,
    /// 개별 시스템이 구독할 수 있는 이벤트를 발행.
    /// </summary>
    public class FeatureUnlockManager : MonoBehaviour
    {
        [Header("연동")]
        [SerializeField] private WallExpansionManager wallExpansionManager;

        [Header("런타임 상태 (읽기 전용)")]
        [SerializeField] private List<FeatureUnlockType> unlockedFeatures = new();

        /// <summary>개별 기능이 해금될 때 발행. FeatureUnlockType = 방금 해금된 기능.</summary>
        public event Action<FeatureUnlockType> OnFeatureUnlocked;

        /// <summary>해금 상태 전체가 변경될 때 발행 (배치 해금 시 한 번).</summary>
        public event Action OnUnlockStateChanged;

        private void Start()
        {
            if (wallExpansionManager != null)
            {
                wallExpansionManager.OnFeaturesUnlocked += HandleFeaturesUnlocked;
            }
        }

        private void OnDestroy()
        {
            if (wallExpansionManager != null)
            {
                wallExpansionManager.OnFeaturesUnlocked -= HandleFeaturesUnlocked;
            }
        }

        /// <summary>
        /// 특정 기능이 해금되었는지 확인.
        /// UI, 시스템 접근 제어 등에서 사용.
        /// </summary>
        public bool IsUnlocked(FeatureUnlockType feature)
        {
            return unlockedFeatures.Contains(feature);
        }

        /// <summary>
        /// 현재 해금된 모든 기능 목록 반환.
        /// </summary>
        public IReadOnlyList<FeatureUnlockType> GetUnlockedFeatures()
        {
            return unlockedFeatures;
        }

        /// <summary>
        /// 기능을 수동으로 해금 (치트/디버그/이벤트 보상 등).
        /// </summary>
        public void UnlockFeature(FeatureUnlockType feature)
        {
            if (feature == FeatureUnlockType.None) return;
            if (unlockedFeatures.Contains(feature)) return;

            unlockedFeatures.Add(feature);
            OnFeatureUnlocked?.Invoke(feature);
            OnUnlockStateChanged?.Invoke();

            Debug.Log($"[FeatureUnlockManager] 기능 해금: {feature}");
        }

        /// <summary>
        /// 모든 해금 상태 초기화 (시나리오 재시작 시).
        /// </summary>
        public void ResetAll()
        {
            unlockedFeatures.Clear();
            OnUnlockStateChanged?.Invoke();
        }

        private void HandleFeaturesUnlocked(List<FeatureUnlockType> features)
        {
            bool anyNew = false;

            foreach (var feature in features)
            {
                if (feature == FeatureUnlockType.None) continue;
                if (unlockedFeatures.Contains(feature)) continue;

                unlockedFeatures.Add(feature);
                OnFeatureUnlocked?.Invoke(feature);
                anyNew = true;

                Debug.Log($"[FeatureUnlockManager] 방벽 확장으로 기능 해금: {feature}");
            }

            if (anyNew)
            {
                OnUnlockStateChanged?.Invoke();
            }
        }
    }
}

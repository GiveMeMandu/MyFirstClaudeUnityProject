using System;
using System.Collections;
using UnityEngine;

namespace ProjectSun.WallExpansion
{
    /// <summary>
    /// 방벽 확장 시 시각 연출 관리.
    /// ① 방벽 팽창 애니메이션 (스케일 펀치)
    /// ② 새 영역 밝아지는 효과 (머티리얼 알파/색상 전환)
    /// WallExpansionManager.OnExpansionStarted에 구독하여 연출 시작.
    /// </summary>
    public class WallExpansionVFX : MonoBehaviour
    {
        [Header("연동")]
        [SerializeField] private WallExpansionManager wallExpansionManager;

        [Header("방벽 팽창 연출")]
        [Tooltip("팽창 스케일 증가량 (1.0 + 이 값까지 커졌다 돌아옴)")]
        [SerializeField] private float expandScaleAmount = 0.2f;

        [Tooltip("팽창 대상 Transform (null이면 자신)")]
        [SerializeField] private Transform expandTarget;

        [Header("영역 밝아짐 연출")]
        [Tooltip("확장 시 밝아질 오브젝트들 (레벨 순서대로, 리스트의 리스트 대용)")]
        [SerializeField] private GameObject[] areaRevealObjects;

        [Header("파티클")]
        [SerializeField] private ParticleSystem expansionParticle;

        [Header("사운드")]
        [SerializeField] private AudioClip expansionSound;

        private AudioSource audioSource;
        private float animDuration = 1.5f;
        private bool isPlaying;

        /// <summary>연출 완료 시 발행.</summary>
        public event Action OnVFXCompleted;

        public bool IsPlaying => isPlaying;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
            }

            if (expandTarget == null)
            {
                expandTarget = transform;
            }
        }

        private void Start()
        {
            if (wallExpansionManager != null)
            {
                wallExpansionManager.OnExpansionStarted += HandleExpansionStarted;

                // SO에서 연출 시간 가져오기
                if (wallExpansionManager.ExpansionData != null)
                {
                    animDuration = wallExpansionManager.ExpansionData.ExpansionAnimDuration;
                }
            }

            // 초기: 미해금 영역 숨기기
            HideAllAreaObjects();
        }

        private void OnDestroy()
        {
            if (wallExpansionManager != null)
            {
                wallExpansionManager.OnExpansionStarted -= HandleExpansionStarted;
            }
        }

        /// <summary>
        /// 수동으로 연출 재생 (테스트/디버그용).
        /// </summary>
        public void PlayExpansionVFX(int newLevel)
        {
            if (isPlaying) return;
            StartCoroutine(ExpansionSequence(newLevel));
        }

        private void HandleExpansionStarted(int newLevel)
        {
            PlayExpansionVFX(newLevel);
        }

        private IEnumerator ExpansionSequence(int newLevel)
        {
            isPlaying = true;

            // 사운드
            if (expansionSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(expansionSound);
            }

            // 파티클
            if (expansionParticle != null)
            {
                expansionParticle.Play();
            }

            // 방벽 팽창 스케일 애니메이션
            yield return StartCoroutine(ScaleExpandCoroutine());

            // 영역 밝아짐 (레벨에 해당하는 오브젝트 활성화)
            RevealAreaForLevel(newLevel);

            isPlaying = false;
            OnVFXCompleted?.Invoke();
        }

        private IEnumerator ScaleExpandCoroutine()
        {
            Vector3 originalScale = expandTarget.localScale;
            Vector3 expandedScale = originalScale * (1f + expandScaleAmount);
            float halfDuration = animDuration * 0.5f;
            float elapsed = 0f;

            // Expand
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / halfDuration);
                expandTarget.localScale = Vector3.Lerp(originalScale, expandedScale, t);
                yield return null;
            }

            // Contract back
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / halfDuration);
                expandTarget.localScale = Vector3.Lerp(expandedScale, originalScale, t);
                yield return null;
            }

            expandTarget.localScale = originalScale;
        }

        private void RevealAreaForLevel(int level)
        {
            // areaRevealObjects는 인덱스 0 = Lv.1, 인덱스 1 = Lv.2, ...
            int index = level - 1;
            if (index >= 0 && index < areaRevealObjects.Length && areaRevealObjects[index] != null)
            {
                areaRevealObjects[index].SetActive(true);
            }
        }

        private void HideAllAreaObjects()
        {
            if (areaRevealObjects == null) return;

            int currentLevel = wallExpansionManager != null ? wallExpansionManager.CurrentWallLevel : 0;

            for (int i = 0; i < areaRevealObjects.Length; i++)
            {
                if (areaRevealObjects[i] != null)
                {
                    // 이미 해금된 레벨의 오브젝트는 보이게 유지
                    areaRevealObjects[i].SetActive(i < currentLevel);
                }
            }
        }
    }
}

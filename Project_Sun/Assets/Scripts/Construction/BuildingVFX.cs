using System.Collections;
using UnityEngine;

namespace ProjectSun.Construction
{
    public class BuildingVFX : MonoBehaviour
    {
        [Header("완성 연출")]
        [SerializeField] private float completionScalePunchAmount = 0.3f;
        [SerializeField] private float completionScaleDuration = 0.4f;
        [SerializeField] private ParticleSystem completionParticle;
        [SerializeField] private AudioClip completionSound;

        [Header("건설중 연출")]
        [SerializeField] private GameObject scaffoldingPrefab;

        [Header("파괴 연출")]
        [SerializeField] private ParticleSystem destructionParticle;
        [SerializeField] private AudioClip destructionSound;

        private BuildingSlot slot;
        private AudioSource audioSource;
        private GameObject activeScaffolding;

        private void Awake()
        {
            slot = GetComponent<BuildingSlot>();
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
            }

            if (slot != null)
            {
                slot.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (slot != null)
            {
                slot.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(BuildingSlot changedSlot, BuildingSlotState newState)
        {
            switch (newState)
            {
                case BuildingSlotState.Constructing:
                case BuildingSlotState.Upgrading:
                    ShowScaffolding();
                    break;
                case BuildingSlotState.Active:
                    HideScaffolding();
                    PlayCompletionEffect();
                    break;
                case BuildingSlotState.Destroyed:
                    HideScaffolding();
                    PlayDestructionEffect();
                    break;
                case BuildingSlotState.Repairing:
                    ShowScaffolding();
                    break;
            }
        }

        private void PlayCompletionEffect()
        {
            // Scale punch: 스케일을 키웠다 원래 크기로
            StartCoroutine(ScalePunchCoroutine());

            if (completionParticle != null)
            {
                completionParticle.Play();
            }

            if (completionSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(completionSound);
            }
        }

        private void PlayDestructionEffect()
        {
            if (destructionParticle != null)
            {
                destructionParticle.Play();
            }

            if (destructionSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(destructionSound);
            }
        }

        private void ShowScaffolding()
        {
            if (scaffoldingPrefab != null && activeScaffolding == null)
            {
                activeScaffolding = Instantiate(scaffoldingPrefab, transform.position, transform.rotation, transform);
            }
        }

        private void HideScaffolding()
        {
            if (activeScaffolding != null)
            {
                Destroy(activeScaffolding);
                activeScaffolding = null;
            }
        }

        private IEnumerator ScalePunchCoroutine()
        {
            Vector3 originalScale = transform.localScale;
            Vector3 punchScale = originalScale * (1f + completionScalePunchAmount);
            float elapsed = 0f;
            float halfDuration = completionScaleDuration * 0.5f;

            // Scale up
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                transform.localScale = Vector3.Lerp(originalScale, punchScale, t);
                yield return null;
            }

            // Scale back
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                transform.localScale = Vector3.Lerp(punchScale, originalScale, t);
                yield return null;
            }

            transform.localScale = originalScale;
        }
    }
}

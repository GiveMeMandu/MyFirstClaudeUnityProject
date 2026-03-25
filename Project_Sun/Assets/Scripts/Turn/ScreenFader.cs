using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectSun.Turn
{
    /// <summary>
    /// 화면 페이드 연출.
    /// 낮→밤: 어두워짐 (nightAmbientIntensity까지).
    /// 밤→낮: 밝아짐 (완전 투명).
    /// Canvas 위에 전체 화면 Image로 구현.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] private Image fadeImage;

        private Canvas canvas;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            canvas.sortingOrder = 999;

            if (fadeImage == null)
            {
                var go = new GameObject("FadeImage");
                go.transform.SetParent(transform, false);
                fadeImage = go.AddComponent<Image>();
                fadeImage.color = new Color(0, 0, 0, 0);
                fadeImage.raycastTarget = false;

                var rect = fadeImage.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            SetAlpha(0f);
        }

        /// <summary>
        /// 낮→밤 페이드 (어두워짐). nightIntensity=0이면 완전 암전, 0.3이면 약간 보임.
        /// </summary>
        public Coroutine FadeToNight(float duration, float nightIntensity)
        {
            float targetAlpha = 1f - nightIntensity;
            return StartCoroutine(FadeCoroutine(0f, targetAlpha, duration));
        }

        /// <summary>
        /// 밤→낮 페이드 (밝아짐)
        /// </summary>
        public Coroutine FadeToDay(float duration)
        {
            return StartCoroutine(FadeCoroutine(fadeImage.color.a, 0f, duration));
        }

        /// <summary>
        /// 즉시 투명으로 리셋
        /// </summary>
        public void Reset()
        {
            SetAlpha(0f);
        }

        private IEnumerator FadeCoroutine(float from, float to, float duration)
        {
            float elapsed = 0f;
            SetAlpha(from);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // 부드러운 전환을 위해 SmoothStep 사용
                t = t * t * (3f - 2f * t);
                SetAlpha(Mathf.Lerp(from, to, t));
                yield return null;
            }

            SetAlpha(to);
        }

        private void SetAlpha(float alpha)
        {
            if (fadeImage != null)
            {
                fadeImage.color = new Color(0, 0, 0, alpha);
            }
        }
    }
}

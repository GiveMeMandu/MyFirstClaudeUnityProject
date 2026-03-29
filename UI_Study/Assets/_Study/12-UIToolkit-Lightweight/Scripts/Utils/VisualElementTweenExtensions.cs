using DG.Tweening;
using UnityEngine.UIElements;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 4: DOTween.To() getter/setter 래퍼로 VisualElement 속성 트위닝.
    /// UI Toolkit에는 DOTween 직접 확장이 없으므로 수동 구현.
    /// </summary>
    public static class VisualElementTweenExtensions
    {
        /// <summary>opacity 0~1 트위닝</summary>
        public static Tween DOFade(this VisualElement el, float endValue, float duration)
        {
            return DOTween.To(
                () => el.resolvedStyle.opacity,
                x => el.style.opacity = x,
                endValue, duration);
        }

        /// <summary>translate X 트위닝 (px)</summary>
        public static Tween DOTranslateX(this VisualElement el, float endValue, float duration)
        {
            return DOTween.To(
                () => el.resolvedStyle.translate.x,
                x =>
                {
                    var cur = el.resolvedStyle.translate;
                    el.style.translate = new Translate(x, cur.y);
                },
                endValue, duration);
        }

        /// <summary>translate Y 트위닝 (px)</summary>
        public static Tween DOTranslateY(this VisualElement el, float endValue, float duration)
        {
            return DOTween.To(
                () => el.resolvedStyle.translate.y,
                y =>
                {
                    var cur = el.resolvedStyle.translate;
                    el.style.translate = new Translate(cur.x, y);
                },
                endValue, duration);
        }

        /// <summary>균등 스케일 트위닝</summary>
        public static Tween DOScale(this VisualElement el, float endValue, float duration)
        {
            float startVal = el.resolvedStyle.scale.value.x;
            return DOTween.To(
                () => startVal,
                s =>
                {
                    startVal = s;
                    el.style.scale = new Scale(new UnityEngine.Vector3(s, s, 1f));
                },
                endValue, duration);
        }
    }
}

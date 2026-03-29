using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 4: DOTween 스태거 애니메이션 — 리스트 아이템이 0.05초 간격으로 순차 슬라이드 인.
    /// </summary>
    public class StaggerListView : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;
        [SerializeField] private float _staggerDelay = 0.05f;
        [SerializeField] private float _slideDuration = 0.3f;

        private VisualElement _listContainer;
        private Sequence _staggerSequence;

        private void OnEnable()
        {
            var root = _document.rootVisualElement;
            _listContainer = root.Q<VisualElement>("stagger-list");
        }

        private void OnDestroy()
        {
            _staggerSequence?.Kill();
        }

        public void PlayStagger()
        {
            _staggerSequence?.Kill();
            _staggerSequence = DOTween.Sequence();

            var items = _listContainer.Children();
            int index = 0;

            foreach (var item in items)
            {
                // 초기 상태: 왼쪽으로 밀림 + 투명
                item.style.opacity = 0f;
                item.style.translate = new Translate(-30f, 0f);

                float delay = index * _staggerDelay;

                // 각 아이템에 개별 딜레이 적용
                _staggerSequence
                    .Insert(delay, item.DOFade(1f, _slideDuration))
                    .Insert(delay, item.DOTranslateX(0f, _slideDuration).SetEase(Ease.OutCubic));

                index++;
            }
        }
    }
}

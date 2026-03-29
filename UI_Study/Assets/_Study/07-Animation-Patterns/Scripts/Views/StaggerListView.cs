using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.Animation.Views
{
    /// <summary>
    /// 8개 리스트 아이템의 시차(stagger) 애니메이션을 시연하는 View.
    /// Show 시 각 아이템이 슬라이드 인 + 페이드 인, Hide 시 역순 애니메이션.
    ///
    /// LayoutGroup 충돌 방지:
    /// Start()에서 VLG로 위치 계산 → 캐싱 → VLG 비활성화 → DOTween 자유 애니메이션
    /// </summary>
    public class StaggerListView : MonoBehaviour
    {
        [Header("List Items (8 panels)")]
        [SerializeField] private List<RectTransform> _items = new();

        [Header("Controls")]
        [SerializeField] private Button _toggleButton;
        [SerializeField] private Slider _speedSlider;

        [Header("Animation Settings")]
        [SerializeField] private float _baseDuration = 0.3f;
        [SerializeField] private float _staggerDelay = 0.08f;
        [SerializeField] private float _slideDistance = 300f;

        private readonly List<CanvasGroup> _itemCanvasGroups = new();
        private readonly List<Vector2> _itemOriginalPositions = new();
        private Sequence _currentSequence;
        private bool _initialized;

        // Read-only accessors for Presenter
        public Button ToggleButton => _toggleButton;
        public Slider SpeedSlider => _speedSlider;

        private void Awake()
        {
            foreach (var item in _items)
            {
                var cg = item.GetComponent<CanvasGroup>();
                if (cg == null) cg = item.gameObject.AddComponent<CanvasGroup>();
                _itemCanvasGroups.Add(cg);
            }
        }

        private void Start()
        {
            // VLG가 레이아웃을 계산한 후 위치 캐싱
            var parentVLG = _items[0].parent.GetComponent<VerticalLayoutGroup>();

            // 레이아웃 강제 재계산 (Start 시점에 VLG 위치 확정)
            if (parentVLG != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentVLG.GetComponent<RectTransform>());

            // 원래 위치 캐싱
            foreach (var item in _items)
                _itemOriginalPositions.Add(item.anchoredPosition);

            // VLG 비활성화 — 이후 DOTween이 anchoredPosition을 자유롭게 제어
            if (parentVLG != null)
                parentVLG.enabled = false;

            // 초기 상태: 숨김 (왼쪽으로 밀려남 + 투명)
            for (int i = 0; i < _items.Count; i++)
            {
                _itemCanvasGroups[i].alpha = 0f;
                _items[i].anchoredPosition = new Vector2(
                    _itemOriginalPositions[i].x - _slideDistance,
                    _itemOriginalPositions[i].y);
            }

            _initialized = true;
        }

        /// <summary>
        /// 모든 아이템을 시차 애니메이션으로 표시한다.
        /// </summary>
        /// <param name="speedMultiplier">애니메이션 속도 배율 (1 = 기본).</param>
        public void ShowItems(float speedMultiplier)
        {
            if (!_initialized) return;
            KillCurrentSequence();

            var duration = _baseDuration / Mathf.Max(speedMultiplier, 0.1f);
            var delay = _staggerDelay / Mathf.Max(speedMultiplier, 0.1f);

            _currentSequence = DOTween.Sequence();

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                var cg = _itemCanvasGroups[i];
                var targetPos = _itemOriginalPositions[i];
                var startPos = new Vector2(targetPos.x - _slideDistance, targetPos.y);

                // 시작 상태 설정
                item.anchoredPosition = startPos;
                cg.alpha = 0f;

                float insertTime = i * delay;
                _currentSequence.Insert(insertTime,
                    item.DOAnchorPos(targetPos, duration).SetEase(Ease.OutCubic));
                _currentSequence.Insert(insertTime,
                    cg.DOFade(1f, duration * 0.7f));
            }

            _currentSequence.SetAutoKill(true);
        }

        /// <summary>
        /// 모든 아이템을 시차 애니메이션으로 숨긴다.
        /// </summary>
        /// <param name="speedMultiplier">애니메이션 속도 배율 (1 = 기본).</param>
        public void HideItems(float speedMultiplier)
        {
            if (!_initialized) return;
            KillCurrentSequence();

            var duration = _baseDuration / Mathf.Max(speedMultiplier, 0.1f);
            var delay = _staggerDelay / Mathf.Max(speedMultiplier, 0.1f);

            _currentSequence = DOTween.Sequence();

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                var cg = _itemCanvasGroups[i];
                var originalPos = _itemOriginalPositions[i];
                var hiddenPos = new Vector2(originalPos.x - _slideDistance, originalPos.y);

                float insertTime = i * delay;
                _currentSequence.Insert(insertTime,
                    item.DOAnchorPos(hiddenPos, duration).SetEase(Ease.InCubic));
                _currentSequence.Insert(insertTime,
                    cg.DOFade(0f, duration * 0.7f));
            }

            _currentSequence.SetAutoKill(true);
        }

        private void KillCurrentSequence()
        {
            if (_currentSequence != null && _currentSequence.IsActive())
            {
                _currentSequence.Kill();
            }
            _currentSequence = null;
        }

        private void OnDestroy()
        {
            KillCurrentSequence();
            foreach (var item in _items)
            {
                if (item != null) item.DOKill();
            }
            foreach (var cg in _itemCanvasGroups)
            {
                if (cg != null) cg.DOKill();
            }
        }
    }
}

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.GameUI.Views
{
    /// <summary>
    /// 대화 View — 화자 이름, 본문 (타이프라이터), 계속 화살표, 스킵 버튼.
    /// </summary>
    public class DialogView : MonoBehaviour
    {
        [Header("Dialog UI")]
        [SerializeField] private TextMeshProUGUI _speakerText;
        [SerializeField] private TextMeshProUGUI _bodyText;
        [SerializeField] private CanvasGroup _continueArrow;
        [SerializeField] private Button _skipButton;
        [SerializeField] private CanvasGroup _dialogPanel;

        private Tween _arrowBlink;

        public Button SkipButton => _skipButton;

        /// <summary>
        /// 대화창 전체 표시/숨김.
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_dialogPanel != null)
            {
                _dialogPanel.alpha = visible ? 1f : 0f;
                _dialogPanel.blocksRaycasts = visible;
                _dialogPanel.interactable = visible;
            }
            gameObject.SetActive(visible);
        }

        /// <summary>
        /// 화자 이름 설정.
        /// </summary>
        public void SetSpeaker(string speaker)
        {
            _speakerText.text = speaker;
        }

        /// <summary>
        /// 타이프라이터 효과 — maxVisibleCharacters 패턴.
        /// ct가 취소되면 전체 텍스트를 즉시 표시.
        /// </summary>
        public async UniTask TypeText(string text, float charDelay, CancellationToken ct)
        {
            HideContinueArrow();
            _bodyText.text = text;
            _bodyText.maxVisibleCharacters = 0;

            for (int i = 0; i <= text.Length; i++)
            {
                if (ct.IsCancellationRequested)
                {
                    // 스킵: 전체 텍스트 즉시 표시
                    _bodyText.maxVisibleCharacters = text.Length;
                    return;
                }

                _bodyText.maxVisibleCharacters = i;

                try
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(charDelay),
                        delayType: DelayType.UnscaledDeltaTime,
                        cancellationToken: ct);
                }
                catch (OperationCanceledException)
                {
                    // 스킵 발생 — 전체 텍스트 표시
                    _bodyText.maxVisibleCharacters = text.Length;
                    return;
                }
            }
        }

        /// <summary>
        /// 전체 텍스트 즉시 표시 (스킵용).
        /// </summary>
        public void ShowFullText()
        {
            _bodyText.maxVisibleCharacters = _bodyText.text.Length;
        }

        /// <summary>
        /// 계속 화살표 깜빡임 시작.
        /// </summary>
        public void ShowContinueArrow()
        {
            if (_continueArrow == null) return;
            _continueArrow.alpha = 1f;
            _arrowBlink?.Kill();
            _arrowBlink = _continueArrow
                .DOFade(0f, 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        /// <summary>
        /// 계속 화살표 숨김.
        /// </summary>
        public void HideContinueArrow()
        {
            _arrowBlink?.Kill();
            if (_continueArrow != null)
                _continueArrow.alpha = 0f;
        }

        private void OnDestroy()
        {
            _arrowBlink?.Kill();
        }
    }
}

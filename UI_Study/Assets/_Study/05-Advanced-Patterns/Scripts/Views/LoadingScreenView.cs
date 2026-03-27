using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.Advanced.Views
{
    /// <summary>
    /// 로딩 화면 View — 프로그레스 바 + 상태 텍스트.
    /// IProgress&lt;float&gt;과 연동하여 실시간 업데이트.
    /// </summary>
    public class LoadingScreenView : MonoBehaviour
    {
        [SerializeField] private Slider _progressBar;
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private TextMeshProUGUI _statusText;

        public void SetProgress(float progress)
        {
            _progressBar.value = progress;
            _progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }

        public void SetStatus(string status) => _statusText.text = status;

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}

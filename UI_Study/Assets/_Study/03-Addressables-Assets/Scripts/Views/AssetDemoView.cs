using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.Assets.Views
{
    /// <summary>
    /// 에셋 로드 데모 View — 로드된 스프라이트를 Image에 표시.
    /// </summary>
    public class AssetDemoView : MonoBehaviour
    {
        [SerializeField] private Image _displayImage;
        [SerializeField] private Button _loadButton;
        [SerializeField] private Button _releaseButton;
        [SerializeField] private TextMeshProUGUI _statusText;

        public Observable<Unit> OnLoadClick => _loadButton.OnClickAsObservable();
        public Observable<Unit> OnReleaseClick => _releaseButton.OnClickAsObservable();

        public void SetSprite(Sprite sprite)
        {
            _displayImage.sprite = sprite;
            _displayImage.color = sprite != null ? Color.white : Color.clear;
        }

        public void SetStatus(string status) => _statusText.text = status;
        public void SetReleaseInteractable(bool interactable) => _releaseButton.interactable = interactable;
    }
}

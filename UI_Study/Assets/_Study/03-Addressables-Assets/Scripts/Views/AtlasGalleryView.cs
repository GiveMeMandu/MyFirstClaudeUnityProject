using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIStudy.Assets.Views
{
    /// <summary>
    /// 아틀라스 갤러리 View — 아틀라스 내 스프라이트를 순회 표시.
    /// </summary>
    public class AtlasGalleryView : MonoBehaviour
    {
        [SerializeField] private Image _displayImage;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _prevButton;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _indexText;

        public Observable<Unit> OnNextClick => _nextButton.OnClickAsObservable();
        public Observable<Unit> OnPrevClick => _prevButton.OnClickAsObservable();

        public void SetSprite(Sprite sprite)
        {
            _displayImage.sprite = sprite;
            _displayImage.color = sprite != null ? Color.white : Color.clear;
        }

        public void SetName(string name) => _nameText.text = name;
        public void SetIndex(string index) => _indexText.text = index;
    }
}

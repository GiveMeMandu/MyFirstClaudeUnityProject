using TMPro;
using UnityEngine;
using UnityScreenNavigator.Runtime.Core.Sheet;

namespace UIStudy.Navigation.Sheets
{
    public class WeaponSheetView : Sheet
    {
        [SerializeField] private TextMeshProUGUI _contentText;

        public void SetContent(string content) => _contentText.text = content;
    }
}

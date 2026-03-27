using R3;
using UnityEngine;

namespace UIStudy.Advanced.Models
{
    /// <summary>
    /// SerializableReactiveProperty — Inspector에서 값을 실시간 편집 가능.
    /// Inspector에서 값을 바꾸면 ForceNotify()가 호출되어 구독자에게 전파.
    /// 일반 ReactiveProperty는 Inspector에 노출되지 않음.
    /// </summary>
    public class InspectorModel : MonoBehaviour
    {
        [Header("Inspector에서 실시간 편집 가능")]
        public SerializableReactiveProperty<int> Health = new(100);
        public SerializableReactiveProperty<string> PlayerName = new("Player");
        public SerializableReactiveProperty<float> Speed = new(5.0f);
        public SerializableReactiveProperty<bool> IsInvincible = new(false);
        public SerializableReactiveProperty<Color> TintColor = new(Color.white);
    }
}

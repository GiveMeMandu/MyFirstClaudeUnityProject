using System;
using UnityEngine;

namespace ProjectSun.Turn
{
    /// <summary>
    /// 토스트 메시지 UI.
    /// 제목과 내용을 표시하고, 확인 버튼 클릭 시 닫힘.
    /// PoC에서는 IMGUI로 구현.
    /// </summary>
    public class ToastMessage : MonoBehaviour
    {
        private string title;
        private string message;
        private bool visible;

        public bool IsVisible => visible;

        public event Action OnDismissed;

        /// <summary>
        /// 토스트 메시지 표시
        /// </summary>
        public void Show(string title, string message)
        {
            this.title = title;
            this.message = message;
            visible = true;
        }

        /// <summary>
        /// 토스트 메시지 닫기
        /// </summary>
        public void Dismiss()
        {
            visible = false;
            OnDismissed?.Invoke();
        }

        private void OnGUI()
        {
            if (!visible) return;

            float panelWidth = 350;
            float panelHeight = 180;
            float x = (Screen.width - panelWidth) / 2;
            float y = (Screen.height - panelHeight) / 2;

            // 반투명 배경
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(x, y, panelWidth, panelHeight));
            GUILayout.BeginVertical("box");

            // 제목
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            GUILayout.Label(title, titleStyle);
            GUILayout.Space(10);

            // 내용
            var messageStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            GUILayout.Label(message, messageStyle);
            GUILayout.Space(15);

            // 확인 버튼
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("확인", GUILayout.Height(35)))
            {
                Dismiss();
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}

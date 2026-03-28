namespace UIStudy.GameUI.Models
{
    /// <summary>
    /// 대화 한 줄 — 화자, 본문, 글자 표시 간격.
    /// </summary>
    public readonly struct DialogLine
    {
        public readonly string Speaker;
        public readonly string Text;
        public readonly float CharDelay;

        public DialogLine(string speaker, string text, float charDelay = 0.03f)
        {
            Speaker = speaker;
            Text = text;
            CharDelay = charDelay;
        }
    }
}

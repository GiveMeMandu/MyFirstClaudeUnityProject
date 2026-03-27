namespace UIStudy.Advanced.Models
{
    public enum ToastType { Success, Warning, Error, Info }

    public struct ToastData
    {
        public string Message;
        public ToastType Type;
        public float Duration;

        public ToastData(string message, ToastType type = ToastType.Info, float duration = 2.5f)
        {
            Message = message;
            Type = type;
            Duration = duration;
        }
    }
}

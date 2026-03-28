using System;
using R3;

namespace UIStudy.Animation.Models
{
    /// <summary>
    /// Stagger 리스트의 표시/숨김 상태를 관리하는 모델.
    /// </summary>
    public class StaggerModel : IDisposable
    {
        public ReactiveProperty<bool> IsVisible { get; } = new(false);

        public void Toggle() => IsVisible.Value = !IsVisible.Value;

        public void Dispose() => IsVisible.Dispose();
    }
}

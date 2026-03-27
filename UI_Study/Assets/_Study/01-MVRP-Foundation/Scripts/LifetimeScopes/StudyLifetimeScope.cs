using UIStudy.MVRP.Models;
using VContainer;
using VContainer.Unity;

namespace UIStudy.MVRP.LifetimeScopes
{
    /// <summary>
    /// 루트 LifetimeScope — VContainer의 DI 컨테이너 설정.
    /// 씬에 빈 GameObject를 만들고 이 컴포넌트를 붙인다.
    /// </summary>
    public class StudyLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // Plain C# 클래스 등록 — Singleton으로 하나만 생성
            builder.Register<GameConfig>(Lifetime.Singleton);

            // IStartable 엔트리포인트 등록 — Start()가 자동 호출됨
            builder.RegisterEntryPoint<SimpleService>();
        }
    }
}

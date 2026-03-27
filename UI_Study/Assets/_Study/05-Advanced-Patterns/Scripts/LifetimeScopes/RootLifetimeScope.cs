using UIStudy.Advanced.Services;
using VContainer;
using VContainer.Unity;

namespace UIStudy.Advanced.LifetimeScopes
{
    /// <summary>
    /// Project Root LifetimeScope — VContainerSettings에 등록하여 앱 시작 시 자동 생성.
    /// DontDestroyOnLoad로 씬 전환에도 유지.
    /// </summary>
    public class RootLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<UIFactory>(Lifetime.Singleton);
            builder.RegisterEntryPoint<GameLoopService>();
            builder.RegisterEntryPoint<AsyncBootstrapper>();
        }
    }
}

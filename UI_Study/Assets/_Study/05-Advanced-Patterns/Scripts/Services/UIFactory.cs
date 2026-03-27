using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIStudy.Advanced.Services
{
    /// <summary>
    /// RegisterFactory 패턴 — 런타임 파라미터와 DI 의존성을 결합하여 오브젝트 생성.
    ///
    /// 등록 방법:
    /// builder.RegisterFactory&lt;string, UIPanel&gt;(container =>
    /// {
    ///     var resolver = container.Resolve&lt;IObjectResolver&gt;();
    ///     return panelName => {
    ///         var prefab = Resources.Load&lt;UIPanel&gt;(panelName);
    ///         return resolver.Instantiate(prefab);
    ///     };
    /// }, Lifetime.Scoped);
    /// </summary>
    public class UIFactory
    {
        private readonly IObjectResolver _resolver;

        public UIFactory(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        /// <summary>
        /// IObjectResolver.Instantiate — 프리팹 생성 + 모든 MonoBehaviour에 DI 주입.
        /// UnityEngine.Object.Instantiate 대신 반드시 이것을 사용.
        /// </summary>
        public T Instantiate<T>(T prefab, Transform parent = null) where T : Component
        {
            return _resolver.Instantiate(prefab, parent);
        }

        /// <summary>
        /// InjectGameObject — 이미 존재하는 GameObject에 DI 주입.
        /// Addressables로 로드한 프리팹에 사용.
        /// </summary>
        public void InjectInto(GameObject gameObject)
        {
            _resolver.InjectGameObject(gameObject);
        }
    }
}

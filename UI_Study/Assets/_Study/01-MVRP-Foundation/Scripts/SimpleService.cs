using UnityEngine;
using VContainer.Unity;

namespace UIStudy.MVRP
{
    /// <summary>
    /// VContainer의 IStartable을 구현하는 Pure C# 서비스.
    /// 생성자 주입으로 GameConfig를 받아 사용한다.
    /// IStartable.Start()는 VContainer가 자동 호출한다.
    /// </summary>
    public class SimpleService : IStartable
    {
        private readonly Models.GameConfig _config;

        // 생성자 주입 — VContainer가 GameConfig 인스턴스를 자동으로 제공
        public SimpleService(Models.GameConfig config)
        {
            _config = config;
        }

        // VContainer의 IStartable — MonoBehaviour.Start()와 유사하지만 Pure C#
        public void Start()
        {
            Debug.Log($"[SimpleService] DI 성공! GameTitle={_config.GameTitle}, StartingGold={_config.StartingGold}, MaxPopulation={_config.MaxPopulation}");
        }
    }
}

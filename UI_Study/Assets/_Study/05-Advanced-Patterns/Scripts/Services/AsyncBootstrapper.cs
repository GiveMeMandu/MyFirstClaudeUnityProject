using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace UIStudy.Advanced.Services
{
    /// <summary>
    /// IAsyncStartable — VContainer의 비동기 초기화 엔트리포인트.
    /// UniTask가 설치되면 자동 활성화 (VCONTAINER_UNITASK_INTEGRATION).
    /// StartAsync는 컨테이너 빌드 후 Start() 타이밍에 호출.
    /// CancellationToken은 LifetimeScope 파괴 시 취소됨.
    /// </summary>
    public class AsyncBootstrapper : IAsyncStartable
    {
        public async UniTask StartAsync(CancellationToken cancellation)
        {
            Debug.Log("[AsyncBootstrapper] 비동기 초기화 시작...");

            // 예: 설정 파일 로드, 원격 데이터 패치 등
            await UniTask.Delay(500, cancellationToken: cancellation);
            Debug.Log("[AsyncBootstrapper] 설정 로드 완료 (시뮬레이션)");

            await UniTask.Delay(300, cancellationToken: cancellation);
            Debug.Log("[AsyncBootstrapper] 초기 데이터 준비 완료");

            Debug.Log("[AsyncBootstrapper] 비동기 초기화 완료 — 게임 시작 가능");
        }
    }
}

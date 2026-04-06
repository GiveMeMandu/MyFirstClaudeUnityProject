using System;
using UnityEngine;
using ProjectSun.V2.Data;
using ProjectSun.V2.Defense.Bridge;

namespace ProjectSun.V2.Core
{
    /// <summary>
    /// 게임오버 관리. 본부 HP 0 시 패배 트리거.
    /// BattleUIBridge 폴링 결과 또는 BattleResultCollector 결과로 게임오버 판정.
    /// SF-WD-017.
    /// </summary>
    public class GameOverManager : MonoBehaviour
    {
        public enum GameOverReason
        {
            HeadquartersDestroyed,
            Victory // M2에서 구현 (25턴 생존)
        }

        [Header("References")]
        [SerializeField] BattleUIBridge battleUIBridge;

        /// <summary>게임오버 발생 시 발행. UI가 구독하여 게임오버 화면 표시.</summary>
        public event Action<GameOverReason> OnGameOver;

        /// <summary>게임오버 상태인지 여부.</summary>
        public bool IsGameOver { get; private set; }

        /// <summary>게임오버 사유.</summary>
        public GameOverReason? Reason { get; private set; }

        void Update()
        {
            if (IsGameOver) return;
            if (battleUIBridge == null) return;

            // 밤 전투 중 본부 HP 실시간 감시
            if (battleUIBridge.HeadquartersMaxHP > 0f && battleUIBridge.HeadquartersHP <= 0f)
            {
                TriggerGameOver(GameOverReason.HeadquartersDestroyed);
            }
        }

        /// <summary>
        /// BattleResultCollector 결과로 게임오버 판정.
        /// 밤 종료 후 결과 수집에서 본부 파괴가 확인된 경우 호출.
        /// </summary>
        public void CheckResult(WaveResult result)
        {
            if (IsGameOver) return;
            if (result == null) return;

            if (result.headquartersDestroyed)
            {
                TriggerGameOver(GameOverReason.HeadquartersDestroyed);
            }
        }

        /// <summary>게임오버 상태 초기화 (새 게임 시작 시).</summary>
        public void Reset()
        {
            IsGameOver = false;
            Reason = null;
        }

        void TriggerGameOver(GameOverReason reason)
        {
            if (IsGameOver) return;

            IsGameOver = true;
            Reason = reason;
            Time.timeScale = 0f;

            Debug.LogWarning($"[GameOverManager] GAME OVER — {reason}");
            OnGameOver?.Invoke(reason);
        }
    }
}

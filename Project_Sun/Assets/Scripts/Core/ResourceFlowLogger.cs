using UnityEngine;
using ProjectSun.V2.Data;

namespace ProjectSun.V2.Core
{
    /// <summary>
    /// 턴별 자원 흐름 로그. Debug.Log 기반 밸런싱 이터레이션 도구.
    /// 턴 시작/종료 시 자원 스냅샷을 기록하여 수입/지출을 추적한다.
    /// SF-ECO-011.
    /// </summary>
    public class ResourceFlowLogger : MonoBehaviour
    {
        // 턴 시작 시점의 자원 스냅샷
        int _snapshotBasic;
        int _snapshotAdvanced;
        int _snapshotRelic;
        int _snapshotTurn;

        /// <summary>
        /// 턴(낮 페이즈) 시작 시 호출. 자원 잔고 스냅샷 저장.
        /// </summary>
        public void OnTurnStart(GameState state)
        {
            _snapshotTurn = state.currentTurn;
            _snapshotBasic = state.resources.basicAmount;
            _snapshotAdvanced = state.resources.advancedAmount;
            _snapshotRelic = state.resources.relicAmount;

            Debug.Log($"[ResourceFlow] ═══ Turn {_snapshotTurn} START ═══ " +
                      $"Basic: {_snapshotBasic}/{state.resources.basicCap} | " +
                      $"Advanced: {_snapshotAdvanced}/{state.resources.advancedCap} | " +
                      $"Relic: {_snapshotRelic}");
        }

        /// <summary>
        /// 낮 페이즈 종료(밤 진입 직전) 시 호출. 낮 동안 건설/배치로 소모한 자원 기록.
        /// </summary>
        public void OnDayEnd(GameState state)
        {
            int basicDelta = state.resources.basicAmount - _snapshotBasic;
            int advancedDelta = state.resources.advancedAmount - _snapshotAdvanced;
            int relicDelta = state.resources.relicAmount - _snapshotRelic;

            Debug.Log($"[ResourceFlow] ─── Turn {_snapshotTurn} DAY SPENT ─── " +
                      $"Basic: {FormatDelta(basicDelta)} | " +
                      $"Advanced: {FormatDelta(advancedDelta)} | " +
                      $"Relic: {FormatDelta(relicDelta)} | " +
                      $"Remaining: B{state.resources.basicAmount} A{state.resources.advancedAmount} R{state.resources.relicAmount}");

            // 밤 진입 전 스냅샷 갱신 (밤 보상 계산의 기준점)
            _snapshotBasic = state.resources.basicAmount;
            _snapshotAdvanced = state.resources.advancedAmount;
            _snapshotRelic = state.resources.relicAmount;
        }

        /// <summary>
        /// 밤 종료 후 보상 적용 직후 호출. 방어 보상 자원 기록.
        /// </summary>
        public void OnNightReward(GameState state, WaveResult result)
        {
            Debug.Log($"[ResourceFlow] ─── Turn {result.turnNumber} NIGHT REWARD ─── " +
                      $"Grade: {result.grade} (dmg: {result.damageRatio:P0}) | " +
                      $"Earned: B+{result.basicReward} A+{result.advancedReward} R+{result.relicReward} | " +
                      $"Balance: B{state.resources.basicAmount} A{state.resources.advancedAmount} R{state.resources.relicAmount}");
        }

        /// <summary>
        /// 턴 종료 시 호출. 턴 전체 수지 요약 출력.
        /// </summary>
        public void OnTurnEnd(GameState state)
        {
            int totalBasicDelta = state.resources.basicAmount - _snapshotBasic;
            int totalAdvancedDelta = state.resources.advancedAmount - _snapshotAdvanced;
            int totalRelicDelta = state.resources.relicAmount - _snapshotRelic;

            // 턴 시작 대비 전체 변동 (스냅샷은 OnDayEnd에서 갱신되었으므로 밤 보상분만 표시됨)
            // 전체 턴 요약은 원래 스냅샷 기준으로 재계산
            Debug.Log($"[ResourceFlow] ═══ Turn {_snapshotTurn} END ═══ " +
                      $"Night Net: B{FormatDelta(totalBasicDelta)} A{FormatDelta(totalAdvancedDelta)} R{FormatDelta(totalRelicDelta)} | " +
                      $"Final: B{state.resources.basicAmount}/{state.resources.basicCap} " +
                      $"A{state.resources.advancedAmount}/{state.resources.advancedCap} " +
                      $"R{state.resources.relicAmount}");
        }

        static string FormatDelta(int delta)
        {
            return delta >= 0 ? $"+{delta}" : delta.ToString();
        }
    }
}

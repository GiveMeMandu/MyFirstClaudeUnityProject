using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSun.Exploration
{
    /// <summary>
    /// 원정대 런타임 데이터.
    /// 인원, 위치, 상태, 이동 경로를 관리.
    /// </summary>
    [Serializable]
    public class ExpeditionTeam
    {
        [SerializeField] private int teamId;
        [SerializeField] private int memberCount;
        [SerializeField] private ExpeditionState state = ExpeditionState.Idle;
        [SerializeField] private int currentNodeIndex = -1;
        [SerializeField] private int targetNodeIndex = -1;
        [SerializeField] private int turnsRemaining;

        // 이동 경로 (노드 인덱스 리스트)
        private List<int> movePath = new();
        private int movePathIndex;

        public int TeamId => teamId;
        public int MemberCount => memberCount;
        public ExpeditionState State => state;
        public int CurrentNodeIndex => currentNodeIndex;
        public int TargetNodeIndex => targetNodeIndex;
        public int TurnsRemaining => turnsRemaining;

        public ExpeditionTeam(int id)
        {
            teamId = id;
            state = ExpeditionState.Idle;
            currentNodeIndex = -1;
        }

        /// <summary>
        /// 원정대 인원 설정
        /// </summary>
        public void SetMembers(int count)
        {
            memberCount = Mathf.Max(0, count);
        }

        /// <summary>
        /// 기지 노드에 배치 (초기화)
        /// </summary>
        public void PlaceAtBase(int baseNodeIndex)
        {
            currentNodeIndex = baseNodeIndex;
            targetNodeIndex = -1;
            state = ExpeditionState.Idle;
            movePath.Clear();
            movePathIndex = 0;
            turnsRemaining = 0;
        }

        /// <summary>
        /// 목적지 설정 및 이동 시작
        /// </summary>
        public void SetDestination(int destNodeIndex, int travelTurns)
        {
            targetNodeIndex = destNodeIndex;
            turnsRemaining = travelTurns;
            state = ExpeditionState.Moving;
        }

        /// <summary>
        /// 귀환 경로 설정
        /// </summary>
        public void SetReturnPath(List<int> path, List<int> turnsList)
        {
            if (path == null || path.Count == 0) return;

            movePath = new List<int>(path);
            movePathIndex = 0;
            targetNodeIndex = path[path.Count - 1]; // 기지 노드
            state = ExpeditionState.Returning;

            // 첫 번째 구간의 소요 턴수
            turnsRemaining = turnsList.Count > 0 ? turnsList[0] : 1;
        }

        /// <summary>
        /// 턴 종료 시 이동 처리. 도착 시 true 반환.
        /// </summary>
        public bool ProcessMoveTurn()
        {
            if (state != ExpeditionState.Moving && state != ExpeditionState.Returning)
                return false;

            turnsRemaining--;

            if (turnsRemaining <= 0)
            {
                if (state == ExpeditionState.Moving)
                {
                    // 목적지 도착
                    currentNodeIndex = targetNodeIndex;
                    targetNodeIndex = -1;
                    state = ExpeditionState.Arrived;
                    return true;
                }
                else if (state == ExpeditionState.Returning)
                {
                    // 귀환 경로의 다음 노드 도착
                    movePathIndex++;
                    if (movePathIndex < movePath.Count)
                    {
                        currentNodeIndex = movePath[movePathIndex - 1];
                    }

                    if (movePathIndex >= movePath.Count)
                    {
                        // 기지 도착
                        currentNodeIndex = targetNodeIndex;
                        targetNodeIndex = -1;
                        state = ExpeditionState.Idle;
                        movePath.Clear();
                        movePathIndex = 0;
                        return true;
                    }
                    else
                    {
                        // 다음 구간 이동 계속 (기본 1턴)
                        turnsRemaining = 1;
                        return false;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 기지에 도착했는지 (귀환 완료)
        /// </summary>
        public bool IsAtBase(int baseNodeIndex)
        {
            return state == ExpeditionState.Idle && currentNodeIndex == baseNodeIndex;
        }

        /// <summary>
        /// 해산 (인력 복귀)
        /// </summary>
        public void Disband()
        {
            memberCount = 0;
            state = ExpeditionState.Idle;
            currentNodeIndex = -1;
            targetNodeIndex = -1;
            movePath.Clear();
            turnsRemaining = 0;
        }
    }
}

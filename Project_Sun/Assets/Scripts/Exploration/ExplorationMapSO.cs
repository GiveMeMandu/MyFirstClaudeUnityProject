using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSun.Exploration
{
    /// <summary>
    /// 탐사 맵 전체를 정의하는 SO.
    /// 노드 목록, 간선(연결/소요 턴수), 시작 노드를 포함.
    /// 시나리오(기지)마다 1개씩 고정 배치.
    /// </summary>
    [CreateAssetMenu(fileName = "NewExplorationMap", menuName = "ProjectSun/Exploration/Map")]
    public class ExplorationMapSO : ScriptableObject
    {
        [Header("맵 정보")]
        public string mapName;

        [TextArea(2, 4)]
        public string mapDescription;

        [Header("노드")]
        [Tooltip("맵에 포함된 모든 노드")]
        public List<MapNodeEntry> nodes = new();

        [Header("간선")]
        [Tooltip("노드 간 연결 (양방향)")]
        public List<MapEdge> edges = new();

        [Header("시작점")]
        [Tooltip("기지 위치 (시작 노드 인덱스)")]
        public int baseNodeIndex;

        /// <summary>
        /// 특정 노드의 인접 노드 인덱스 + 소요 턴수 반환
        /// </summary>
        public List<(int neighborIndex, int travelTurns)> GetNeighbors(int nodeIndex)
        {
            var result = new List<(int, int)>();
            foreach (var edge in edges)
            {
                if (edge.nodeIndexA == nodeIndex)
                    result.Add((edge.nodeIndexB, edge.travelTurns));
                else if (edge.nodeIndexB == nodeIndex)
                    result.Add((edge.nodeIndexA, edge.travelTurns));
            }
            return result;
        }

        /// <summary>
        /// 노드 인덱스가 유효한지 확인
        /// </summary>
        public bool IsValidIndex(int index)
        {
            return index >= 0 && index < nodes.Count;
        }
    }

    /// <summary>
    /// 맵 내 노드 엔트리. 노드 SO 참조 + 위치 정보.
    /// </summary>
    [Serializable]
    public struct MapNodeEntry
    {
        [Tooltip("노드 SO 참조")]
        public ExplorationNodeSO nodeData;

        [Tooltip("맵 상 표시 위치 (UI 렌더링용, 0~1 정규화)")]
        public Vector2 mapPosition;
    }

    /// <summary>
    /// 노드 간 간선. 양방향 연결 + 이동 소요 턴수.
    /// </summary>
    [Serializable]
    public struct MapEdge
    {
        [Tooltip("연결 노드 A 인덱스")]
        public int nodeIndexA;

        [Tooltip("연결 노드 B 인덱스")]
        public int nodeIndexB;

        [Tooltip("이동 소요 턴수")]
        [Min(1)]
        public int travelTurns;
    }
}

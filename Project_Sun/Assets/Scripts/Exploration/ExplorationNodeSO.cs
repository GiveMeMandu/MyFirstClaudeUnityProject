using System;
using System.Collections.Generic;
using ProjectSun.Encounter;
using UnityEngine;

namespace ProjectSun.Exploration
{
    /// <summary>
    /// 개별 탐사 노드 정의.
    /// 노드 유형, 보상, 힌트 아이콘, 재방문 가능 여부를 설정.
    /// </summary>
    [CreateAssetMenu(fileName = "NewExplorationNode", menuName = "ProjectSun/Exploration/Node")]
    public class ExplorationNodeSO : ScriptableObject
    {
        [Header("기본 정보")]
        public string nodeName;

        [TextArea(2, 4)]
        public string description;

        public ExplorationNodeType nodeType;

        [Tooltip("재방문 가능 여부 (추후 상점 등 확장용)")]
        public bool isRevisitable;

        [Header("힌트")]
        [Tooltip("안개 상태에서 보이는 힌트 텍스트")]
        public string hintText;

        [Header("보상 — 자원 노드")]
        [Tooltip("자원 보상 목록 (Resource 유형 전용)")]
        public List<NodeResourceReward> resourceRewards = new();

        [Header("보상 — 정찰 노드")]
        [Tooltip("몇 턴 후 웨이브 정보를 공개할지 (Recon 유형 전용)")]
        [Min(1)]
        public int reconTurnsAhead = 1;

        [Header("보상 — 인카운터 노드")]
        [Tooltip("이 노드에서 발생할 인카운터 (Encounter 유형 전용)")]
        public EncounterDefinitionSO encounterDefinition;

        [Header("보상 — 기술 노드")]
        [Tooltip("해금되는 기술 ID (Tech 유형 전용, 추후 연동)")]
        public string techUnlockId;
    }

    [Serializable]
    public struct NodeResourceReward
    {
        [Tooltip("자원 종류 ID (basic, advanced, defense)")]
        public string resourceId;

        [Tooltip("획득량")]
        public int amount;
    }
}

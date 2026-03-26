using System.Collections.Generic;
using ProjectSun.Construction;
using UnityEngine;

namespace ProjectSun.TechTree
{
    [CreateAssetMenu(fileName = "NewTechNode", menuName = "ProjectSun/TechTree/Tech Node")]
    public class TechNodeSO : ScriptableObject
    {
        [Header("기본 정보")]
        public string nodeName;

        [TextArea(2, 4)]
        public string description;

        public TechCategory category;

        [Header("연구 비용")]
        [Tooltip("연구 착수 시 소모되는 자원 (최초 1회)")]
        public List<ResourceCost> researchCost = new();

        [Header("연구 진행")]
        [Tooltip("완료에 필요한 총 연구 포인트")]
        [Min(1)]
        public int requiredResearchPoints = 2;

        [Header("선행 연구")]
        [Tooltip("이 노드를 연구하기 전에 완료해야 하는 노드 목록")]
        public List<TechNodeSO> prerequisites = new();

        [Header("완료 효과")]
        public List<TechNodeEffect> effects = new();

        [Header("UI 배치")]
        [Tooltip("트리 UI 내 노드 위치 (카테고리 내 상대 좌표)")]
        public Vector2 nodePosition;
    }
}

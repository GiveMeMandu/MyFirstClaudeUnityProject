using System.Collections.Generic;
using UnityEngine;

namespace ProjectSun.TechTree
{
    [CreateAssetMenu(fileName = "NewTechCategory", menuName = "ProjectSun/TechTree/Tech Category")]
    public class TechTreeCategorySO : ScriptableObject
    {
        [Header("카테고리 정보")]
        public string categoryName;
        public TechCategory category;

        [Header("노드 목록")]
        [Tooltip("이 카테고리에 속하는 연구 노드들")]
        public List<TechNodeSO> nodes = new();
    }
}

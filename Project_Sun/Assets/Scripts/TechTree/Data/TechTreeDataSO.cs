using System.Collections.Generic;
using UnityEngine;

namespace ProjectSun.TechTree
{
    [CreateAssetMenu(fileName = "NewTechTree", menuName = "ProjectSun/TechTree/Tech Tree Data")]
    public class TechTreeDataSO : ScriptableObject
    {
        [Header("트리 정보")]
        public string treeName;

        [TextArea(2, 4)]
        public string description;

        [Header("카테고리 목록")]
        public List<TechTreeCategorySO> categories = new();

        [Header("밸런스")]
        [Tooltip("인력 1명당 턴 종료 시 생산하는 연구 포인트")]
        [Min(0.1f)]
        public float researchPointsPerWorker = 1f;
    }
}

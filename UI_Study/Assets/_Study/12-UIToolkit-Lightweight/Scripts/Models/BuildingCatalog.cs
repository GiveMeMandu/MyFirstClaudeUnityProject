using System.Collections.Generic;
using UnityEngine;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 6: 건물 카탈로그 — BuildingData 목록을 담은 ScriptableObject.
    /// CreateAssetMenu로 에셋 생성 가능.
    /// </summary>
    [CreateAssetMenu(
        fileName = "BuildingCatalog",
        menuName = "UI Study/Building Catalog",
        order = 0)]
    public class BuildingCatalog : ScriptableObject
    {
        [SerializeField] private List<BuildingData> _buildings = new();

        public IReadOnlyList<BuildingData> Buildings => _buildings;

        /// <summary>
        /// 런타임 기본 카탈로그 생성 (에셋 없을 때 데모용).
        /// </summary>
        public static BuildingCatalog CreateDefault()
        {
            var catalog = CreateInstance<BuildingCatalog>();
            catalog._buildings = new List<BuildingData>
            {
                new("Barracks", "Trains infantry units. Higher levels unlock elite soldiers.", 120, 80),
                new("Farm", "Produces food for your population. Upgrade for higher yield.", 60, 40),
                new("Lumber Mill", "Processes wood more efficiently. Reduces build costs.", 80, 120),
                new("Gold Mine", "Extracts gold from the earth. Steady income source.", 150, 60),
                new("Watchtower", "Provides vision and defense. Warns of incoming attacks.", 100, 100),
                new("Market", "Enables resource trading. Better rates at higher levels.", 200, 150),
            };
            return catalog;
        }
    }
}

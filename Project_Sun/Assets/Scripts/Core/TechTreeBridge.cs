using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectSun.V2.Data;

namespace ProjectSun.V2.Core
{
    /// <summary>
    /// V2 기술 트리 브릿지 (S3-3).
    /// 20개 노드 (15 공통 + 5 기지별) 연구 관리.
    /// GameState 기반 자원 소모/효과 적용.
    /// </summary>
    public class TechTreeBridge : MonoBehaviour
    {
        GameState _gameState;
        readonly HashSet<string> _completedIds = new();
        string _currentResearchId;
        int _progress; // 0 ~ researchTurns

        /// <summary>연구 시작 시 발행.</summary>
        public event Action<TechNode> OnResearchStarted;

        /// <summary>연구 완료 시 발행.</summary>
        public event Action<TechNode> OnResearchCompleted;

        // ── Tech Node Data ──

        static readonly TechNode[] AllNodes =
        {
            // 15 Common
            new() { Id = "improved_farming", Name = "Improved Farming",
                Description = "Enhanced agricultural techniques increase basic resource output.",
                Category = TechCategory.Economy, CostBasic = 5, CostAdvanced = 0,
                ResearchTurns = 2, Prerequisites = Array.Empty<string>() },

            new() { Id = "advanced_materials", Name = "Advanced Materials",
                Description = "Refined processing yields more advanced resources per turn.",
                Category = TechCategory.Economy, CostBasic = 8, CostAdvanced = 3,
                ResearchTurns = 3, Prerequisites = new[] { "improved_farming" } },

            new() { Id = "fortified_walls", Name = "Fortified Walls",
                Description = "Reinforced structures withstand more punishment.",
                Category = TechCategory.Defense, CostBasic = 10, CostAdvanced = 0,
                ResearchTurns = 2, Prerequisites = Array.Empty<string>() },

            new() { Id = "watchtower_upgrade", Name = "Watchtower Upgrade",
                Description = "Extended range allows towers to engage threats earlier.",
                Category = TechCategory.Defense, CostBasic = 5, CostAdvanced = 5,
                ResearchTurns = 3, Prerequisites = new[] { "fortified_walls" } },

            new() { Id = "efficient_construction", Name = "Efficient Construction",
                Description = "Streamlined building processes reduce material waste.",
                Category = TechCategory.Utility, CostBasic = 6, CostAdvanced = 0,
                ResearchTurns = 2, Prerequisites = Array.Empty<string>() },

            new() { Id = "medical_training", Name = "Medical Training",
                Description = "Better medical knowledge speeds up injury recovery.",
                Category = TechCategory.Utility, CostBasic = 4, CostAdvanced = 2,
                ResearchTurns = 2, Prerequisites = Array.Empty<string>() },

            new() { Id = "scout_training", Name = "Scout Training",
                Description = "Experienced scouts travel faster on expeditions.",
                Category = TechCategory.Utility, CostBasic = 5, CostAdvanced = 0,
                ResearchTurns = 1, Prerequisites = Array.Empty<string>() },

            new() { Id = "resource_storage", Name = "Resource Storage",
                Description = "Expanded warehouses hold more supplies.",
                Category = TechCategory.Economy, CostBasic = 8, CostAdvanced = 0,
                ResearchTurns = 2, Prerequisites = Array.Empty<string>() },

            new() { Id = "weapon_smithing", Name = "Weapon Smithing",
                Description = "Forged weapons deal significantly more damage.",
                Category = TechCategory.Defense, CostBasic = 6, CostAdvanced = 4,
                ResearchTurns = 3, Prerequisites = new[] { "watchtower_upgrade" } },

            new() { Id = "deep_mining", Name = "Deep Mining",
                Description = "Underground excavation uncovers richer veins.",
                Category = TechCategory.Economy, CostBasic = 10, CostAdvanced = 5,
                ResearchTurns = 3, Prerequisites = new[] { "advanced_materials" } },

            new() { Id = "rapid_deployment", Name = "Rapid Deployment",
                Description = "Better training gives squads more staying power.",
                Category = TechCategory.Defense, CostBasic = 7, CostAdvanced = 3,
                ResearchTurns = 2, Prerequisites = Array.Empty<string>() },

            new() { Id = "siege_engineering", Name = "Siege Engineering",
                Description = "Mechanical improvements increase tower fire rate.",
                Category = TechCategory.Defense, CostBasic = 8, CostAdvanced = 6,
                ResearchTurns = 3, Prerequisites = new[] { "weapon_smithing" } },

            new() { Id = "advanced_medicine", Name = "Advanced Medicine",
                Description = "Breakthrough treatments heal injuries instantly.",
                Category = TechCategory.Utility, CostBasic = 6, CostAdvanced = 4,
                ResearchTurns = 2, Prerequisites = new[] { "medical_training" } },

            new() { Id = "logistics_network", Name = "Logistics Network",
                Description = "Coordinated supply chains boost all production.",
                Category = TechCategory.Utility, CostBasic = 10, CostAdvanced = 5,
                ResearchTurns = 3, Prerequisites = new[] { "efficient_construction" } },

            new() { Id = "emergency_protocols", Name = "Emergency Protocols",
                Description = "Automated alerts slow time when buildings take damage.",
                Category = TechCategory.Defense, CostBasic = 5, CostAdvanced = 3,
                ResearchTurns = 2, Prerequisites = Array.Empty<string>() },

            // 5 Base-specific (Outpost Alpha)
            new() { Id = "desert_adaptation", Name = "Desert Adaptation",
                Description = "Inhabitants acclimate to the harsh desert environment.",
                Category = TechCategory.Utility, CostBasic = 4, CostAdvanced = 0,
                ResearchTurns = 1, Prerequisites = Array.Empty<string>() },

            new() { Id = "sand_barrier", Name = "Sand Barrier",
                Description = "Walls reinforced with compacted sand offer superior protection.",
                Category = TechCategory.Defense, CostBasic = 6, CostAdvanced = 2,
                ResearchTurns = 2, Prerequisites = new[] { "desert_adaptation" } },

            new() { Id = "oasis_discovery", Name = "Oasis Discovery",
                Description = "A hidden oasis provides additional resource storage.",
                Category = TechCategory.Economy, CostBasic = 8, CostAdvanced = 0,
                ResearchTurns = 2, Prerequisites = new[] { "desert_adaptation" } },

            new() { Id = "nomad_alliance", Name = "Nomad Alliance",
                Description = "Befriending local nomads brings a new citizen upon completion.",
                Category = TechCategory.Utility, CostBasic = 5, CostAdvanced = 5,
                ResearchTurns = 3, Prerequisites = new[] { "oasis_discovery" } },

            new() { Id = "sandstorm_tech", Name = "Sandstorm Tech",
                Description = "Harnessing sandstorms slows approaching enemies.",
                Category = TechCategory.Defense, CostBasic = 10, CostAdvanced = 8,
                ResearchTurns = 3, Prerequisites = new[] { "sand_barrier" } },
        };

        // ── Public API ──

        public string CurrentResearchId => _currentResearchId;
        public int Progress => _progress;
        public IReadOnlyCollection<string> CompletedIds => _completedIds;

        public void Initialize(GameState state)
        {
            _gameState = state;
        }

        /// <summary>사전 조건 충족 + 미완료 노드 반환.</summary>
        public List<TechNode> GetAvailableResearch()
        {
            var available = new List<TechNode>();
            foreach (var node in AllNodes)
            {
                if (_completedIds.Contains(node.Id)) continue;
                if (node.Id == _currentResearchId) continue;
                if (!PrerequisitesMet(node)) continue;
                available.Add(node);
            }
            return available;
        }

        /// <summary>모든 노드 반환 (UI 전체 목록 표시용).</summary>
        public TechNode[] GetAllNodes() => AllNodes;

        /// <summary>특정 노드 조회.</summary>
        public TechNode GetNode(string nodeId)
        {
            return AllNodes.FirstOrDefault(n => n.Id == nodeId);
        }

        /// <summary>연구 시작. 자원 소모.</summary>
        public bool StartResearch(string nodeId)
        {
            if (_gameState == null) return false;
            if (!string.IsNullOrEmpty(_currentResearchId)) return false;

            var node = GetNode(nodeId);
            if (node == null) return false;
            if (_completedIds.Contains(nodeId)) return false;
            if (!PrerequisitesMet(node)) return false;

            if (!_gameState.resources.CanAfford(node.CostBasic, node.CostAdvanced))
            {
                Debug.LogWarning($"[TechTreeBridge] Cannot afford {node.Name}");
                return false;
            }

            _gameState.resources.Spend(node.CostBasic, node.CostAdvanced);
            _currentResearchId = nodeId;
            _progress = 0;

            OnResearchStarted?.Invoke(node);
            Debug.Log($"[TechTreeBridge] Research started: {node.Name} ({node.ResearchTurns} turns)");
            return true;
        }

        /// <summary>턴 진행. 연구 진행도 +1, 완료 시 효과 적용.</summary>
        public void ProcessTurn()
        {
            if (string.IsNullOrEmpty(_currentResearchId)) return;

            var node = GetNode(_currentResearchId);
            if (node == null) return;

            _progress++;
            Debug.Log($"[TechTreeBridge] Research progress: {node.Name} {_progress}/{node.ResearchTurns}");

            if (_progress >= node.ResearchTurns)
            {
                _completedIds.Add(_currentResearchId);
                ApplyEffect(_currentResearchId);

                Debug.Log($"[TechTreeBridge] Research completed: {node.Name}");
                OnResearchCompleted?.Invoke(node);

                _currentResearchId = null;
                _progress = 0;
            }
        }

        /// <summary>특정 기술이 연구 완료되었는지 확인.</summary>
        public bool IsResearched(string nodeId)
        {
            return _completedIds.Contains(nodeId);
        }

        // ── Internal ──

        bool PrerequisitesMet(TechNode node)
        {
            if (node.Prerequisites == null || node.Prerequisites.Length == 0) return true;
            foreach (var prereq in node.Prerequisites)
            {
                if (!_completedIds.Contains(prereq)) return false;
            }
            return true;
        }

        void ApplyEffect(string nodeId)
        {
            if (_gameState == null) return;

            switch (nodeId)
            {
                case "improved_farming":
                    // basicPerTurn +2 (flavor — actual production is building-based)
                    _gameState.resources.Add(2);
                    Debug.Log("[TechTreeBridge] Effect: basic production +2 applied as immediate bonus");
                    break;

                case "advanced_materials":
                    // advancedPerTurn +1
                    _gameState.resources.Add(0, 1);
                    Debug.Log("[TechTreeBridge] Effect: advanced production +1 applied");
                    break;

                case "fortified_walls":
                    // building HP +20%
                    foreach (var b in _gameState.buildings)
                    {
                        if (b.state == BuildingSlotStateV2.Active || b.state == BuildingSlotStateV2.Damaged)
                        {
                            int bonus = Mathf.RoundToInt(b.maxHP * 0.2f);
                            b.maxHP += bonus;
                            b.currentHP += bonus;
                        }
                    }
                    Debug.Log("[TechTreeBridge] Effect: all building HP +20%");
                    break;

                case "watchtower_upgrade":
                    // tower range +3 (flavor — logged)
                    Debug.Log("[TechTreeBridge] Effect: tower range +3 (applied in combat)");
                    break;

                case "efficient_construction":
                    // build cost -10% (flavor — logged)
                    Debug.Log("[TechTreeBridge] Effect: build cost -10% (applied on build)");
                    break;

                case "medical_training":
                    // injured recovery faster (flavor — logged)
                    Debug.Log("[TechTreeBridge] Effect: injured recovery speed improved");
                    break;

                case "scout_training":
                    // exploration travel -1 turn (flavor — logged)
                    Debug.Log("[TechTreeBridge] Effect: exploration travel -1 turn");
                    break;

                case "resource_storage":
                    // basic cap +20, advanced cap +10
                    _gameState.resources.basicCap += 20;
                    _gameState.resources.advancedCap += 10;
                    Debug.Log("[TechTreeBridge] Effect: basic cap +20, advanced cap +10");
                    break;

                case "weapon_smithing":
                    // tower damage +20% (flavor — logged)
                    Debug.Log("[TechTreeBridge] Effect: tower damage +20% (applied in combat)");
                    break;

                case "deep_mining":
                    // advanced production +2
                    _gameState.resources.Add(0, 2);
                    Debug.Log("[TechTreeBridge] Effect: advanced production +2 applied");
                    break;

                case "rapid_deployment":
                    // squad HP +20 (flavor — logged)
                    Debug.Log("[TechTreeBridge] Effect: squad HP +20 (applied in combat)");
                    break;

                case "siege_engineering":
                    // tower attack speed +30% (flavor — logged)
                    Debug.Log("[TechTreeBridge] Effect: tower attack speed +30% (applied in combat)");
                    break;

                case "advanced_medicine":
                    // injured instant recovery
                    foreach (var c in _gameState.citizens)
                    {
                        if (c.state == CitizenState.Injured || c.state == CitizenState.Recovering)
                            c.state = CitizenState.Idle;
                    }
                    Debug.Log("[TechTreeBridge] Effect: all injured citizens recovered");
                    break;

                case "logistics_network":
                    // all production +1
                    _gameState.resources.Add(1, 1);
                    Debug.Log("[TechTreeBridge] Effect: all production +1 applied");
                    break;

                case "emergency_protocols":
                    // auto-slow on building damage (flavor — logged)
                    Debug.Log("[TechTreeBridge] Effect: emergency protocols active");
                    break;

                case "desert_adaptation":
                    // heat resistance (flavor)
                    Debug.Log("[TechTreeBridge] Effect: desert adaptation (heat resistance)");
                    break;

                case "sand_barrier":
                    // wall HP +30
                    foreach (var b in _gameState.buildings)
                    {
                        if (b.state == BuildingSlotStateV2.Active || b.state == BuildingSlotStateV2.Damaged)
                        {
                            b.maxHP += 30;
                            b.currentHP += 30;
                        }
                    }
                    Debug.Log("[TechTreeBridge] Effect: all building HP +30");
                    break;

                case "oasis_discovery":
                    // basic cap +30
                    _gameState.resources.basicCap += 30;
                    Debug.Log("[TechTreeBridge] Effect: basic cap +30");
                    break;

                case "nomad_alliance":
                    // +1 citizen
                    string id = $"citizen_{_gameState.citizens.Count}";
                    _gameState.citizens.Add(new CitizenRuntimeState
                    {
                        citizenId = id,
                        displayName = $"Nomad {_gameState.citizens.Count}",
                        aptitude = (CitizenAptitude)UnityEngine.Random.Range(1, 5),
                        proficiencyLevel = 1,
                        state = CitizenState.Idle
                    });
                    Debug.Log("[TechTreeBridge] Effect: +1 citizen (Nomad)");
                    break;

                case "sandstorm_tech":
                    // enemies slowed 20% (flavor — logged)
                    Debug.Log("[TechTreeBridge] Effect: enemies slowed 20% (applied in combat)");
                    break;
            }
        }
    }

    // ── Data Types ──

    public enum TechCategory { Economy, Defense, Utility }

    [Serializable]
    public class TechNode
    {
        public string Id;
        public string Name;
        public string Description;
        public TechCategory Category;
        public int CostBasic;
        public int CostAdvanced;
        public int ResearchTurns;
        public string[] Prerequisites;
    }
}

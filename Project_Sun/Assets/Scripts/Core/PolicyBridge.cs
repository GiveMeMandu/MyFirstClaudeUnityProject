using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSun.V2.Data;

namespace ProjectSun.V2.Core
{
    /// <summary>
    /// V2 정책 브릿지 (S3-4).
    /// 이진 선택 정책 시스템 — A 또는 B 선택, 되돌릴 수 없음.
    /// 특정 턴에 해금되며 즉시 효과 적용.
    /// </summary>
    public class PolicyBridge : MonoBehaviour
    {
        GameState _gameState;

        /// <summary>선택된 정책. key=policyId, value=true면 A, false면 B.</summary>
        readonly Dictionary<string, bool> _chosenPolicies = new();

        /// <summary>정책 해금 시 발행.</summary>
        public event Action<PolicyData> OnPolicyAvailable;

        /// <summary>정책 선택 시 발행.</summary>
        public event Action<PolicyData, bool> OnPolicyChosen;

        // ── Policy Data ──

        static readonly PolicyData[] AllPolicies =
        {
            new()
            {
                Id = "labor_law", Title = "Labor Law",
                Description = "How should work shifts be organized?",
                UnlockTurn = 3,
                OptionAName = "Mandatory Overtime",
                OptionADescription = "Workers push harder for increased output.",
                OptionAEffects = "Production +2 per turn, morale impact",
                OptionBName = "Fair Shifts",
                OptionBDescription = "Balanced schedules improve citizen wellbeing.",
                OptionBEffects = "Injured recovery 1 turn faster",
            },
            new()
            {
                Id = "defense_doctrine", Title = "Defense Doctrine",
                Description = "What defensive strategy should we adopt?",
                UnlockTurn = 5,
                OptionAName = "Fortification Focus",
                OptionADescription = "Invest in stronger walls and structures.",
                OptionAEffects = "Building HP +15%",
                OptionBName = "Strike Force",
                OptionBDescription = "Prioritize offensive firepower.",
                OptionBEffects = "Tower damage +25%",
            },
            new()
            {
                Id = "resource_policy", Title = "Resource Policy",
                Description = "How should we manage our resources?",
                UnlockTurn = 8,
                OptionAName = "Stockpile",
                OptionADescription = "Build reserves for emergencies.",
                OptionAEffects = "All resource caps +30",
                OptionBName = "Distribution",
                OptionBDescription = "Spread resources for immediate use.",
                OptionBEffects = "Production +3 per turn",
            },
            new()
            {
                Id = "exploration_charter", Title = "Exploration Charter",
                Description = "What exploration doctrine should we follow?",
                UnlockTurn = 12,
                OptionAName = "Aggressive",
                OptionADescription = "Fast but risky expeditions.",
                OptionAEffects = "Travel -1 turn, injury risk increased",
                OptionBName = "Cautious",
                OptionBDescription = "Slower but safer with better findings.",
                OptionBEffects = "Travel +1 turn, better rewards",
            },
            new()
            {
                Id = "final_stand", Title = "Final Stand",
                Description = "How do we prepare for the endgame?",
                UnlockTurn = 15,
                OptionAName = "Bunker Down",
                OptionADescription = "Fortify the headquarters at all costs.",
                OptionAEffects = "HQ HP +100",
                OptionBName = "All-Out Attack",
                OptionBDescription = "Maximize offensive capability.",
                OptionBEffects = "All tower damage x1.5",
            },
        };

        // ── Public API ──

        public void Initialize(GameState state)
        {
            _gameState = state;
        }

        /// <summary>현재 턴에 대기 중인 정책 반환. 없으면 null.</summary>
        public PolicyData GetPendingPolicy()
        {
            if (_gameState == null) return null;

            foreach (var policy in AllPolicies)
            {
                if (_gameState.currentTurn >= policy.UnlockTurn && !_chosenPolicies.ContainsKey(policy.Id))
                    return policy;
            }
            return null;
        }

        /// <summary>정책 선택. isOptionA=true면 A, false면 B.</summary>
        public bool ChooseOption(string policyId, bool isOptionA)
        {
            if (_gameState == null) return false;
            if (_chosenPolicies.ContainsKey(policyId)) return false;

            PolicyData policy = null;
            foreach (var p in AllPolicies)
            {
                if (p.Id == policyId) { policy = p; break; }
            }
            if (policy == null) return false;

            _chosenPolicies[policyId] = isOptionA;
            ApplyEffect(policyId, isOptionA);

            OnPolicyChosen?.Invoke(policy, isOptionA);
            string chosen = isOptionA ? policy.OptionAName : policy.OptionBName;
            Debug.Log($"[PolicyBridge] Policy chosen: {policy.Title} → {chosen}");
            return true;
        }

        /// <summary>선택된 정책 목록 반환.</summary>
        public IReadOnlyDictionary<string, bool> GetChosenPolicies() => _chosenPolicies;

        /// <summary>모든 정책 데이터 반환.</summary>
        public PolicyData[] GetAllPolicies() => AllPolicies;

        /// <summary>정책 해금 체크 호출 (낮 시작 시).</summary>
        public void CheckPendingPolicy()
        {
            var pending = GetPendingPolicy();
            if (pending != null)
            {
                OnPolicyAvailable?.Invoke(pending);
                Debug.Log($"[PolicyBridge] Policy available: {pending.Title} (Turn {pending.UnlockTurn})");
            }
        }

        // ── Internal ──

        void ApplyEffect(string policyId, bool isOptionA)
        {
            if (_gameState == null) return;

            switch (policyId)
            {
                case "labor_law":
                    if (isOptionA)
                    {
                        // Production +2
                        _gameState.resources.Add(2);
                        Debug.Log("[PolicyBridge] Effect: production +2 (Mandatory Overtime)");
                    }
                    else
                    {
                        // Faster recovery (flavor — recover injured citizens now)
                        foreach (var c in _gameState.citizens)
                        {
                            if (c.state == CitizenState.Recovering)
                                c.state = CitizenState.Idle;
                        }
                        Debug.Log("[PolicyBridge] Effect: recovery improved (Fair Shifts)");
                    }
                    break;

                case "defense_doctrine":
                    if (isOptionA)
                    {
                        // Building HP +15%
                        foreach (var b in _gameState.buildings)
                        {
                            if (b.state == BuildingSlotStateV2.Active || b.state == BuildingSlotStateV2.Damaged)
                            {
                                int bonus = Mathf.RoundToInt(b.maxHP * 0.15f);
                                b.maxHP += bonus;
                                b.currentHP += bonus;
                            }
                        }
                        Debug.Log("[PolicyBridge] Effect: building HP +15% (Fortification Focus)");
                    }
                    else
                    {
                        // Tower damage +25% (flavor — logged)
                        Debug.Log("[PolicyBridge] Effect: tower damage +25% (Strike Force, applied in combat)");
                    }
                    break;

                case "resource_policy":
                    if (isOptionA)
                    {
                        // All caps +30
                        _gameState.resources.basicCap += 30;
                        _gameState.resources.advancedCap += 30;
                        Debug.Log("[PolicyBridge] Effect: all caps +30 (Stockpile)");
                    }
                    else
                    {
                        // Production +3
                        _gameState.resources.Add(3, 3);
                        Debug.Log("[PolicyBridge] Effect: production +3 (Distribution)");
                    }
                    break;

                case "exploration_charter":
                    if (isOptionA)
                    {
                        // Travel -1 turn (flavor — logged)
                        Debug.Log("[PolicyBridge] Effect: travel -1 turn (Aggressive, applied in exploration)");
                    }
                    else
                    {
                        // Better rewards (flavor — logged)
                        Debug.Log("[PolicyBridge] Effect: better rewards (Cautious, applied in exploration)");
                    }
                    break;

                case "final_stand":
                    if (isOptionA)
                    {
                        // HQ HP +100
                        var hq = _gameState.buildings.Find(b => b.buildingId == "headquarters");
                        if (hq != null)
                        {
                            hq.maxHP += 100;
                            hq.currentHP += 100;
                        }
                        Debug.Log("[PolicyBridge] Effect: HQ HP +100 (Bunker Down)");
                    }
                    else
                    {
                        // All tower damage x1.5 (flavor — logged)
                        Debug.Log("[PolicyBridge] Effect: all tower damage x1.5 (All-Out Attack, applied in combat)");
                    }
                    break;
            }
        }
    }

    // ── Data Type ──

    [Serializable]
    public class PolicyData
    {
        public string Id;
        public string Title;
        public string Description;
        public int UnlockTurn;
        public string OptionAName;
        public string OptionADescription;
        public string OptionAEffects;
        public string OptionBName;
        public string OptionBDescription;
        public string OptionBEffects;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSun.V2.Data;

namespace ProjectSun.V2.Core
{
    /// <summary>
    /// V2 인카운터 브릿지. V1 EncounterManager 대체.
    /// GameState 기반 인카운터 발생/선택/효과 적용.
    /// SF-ENC-001/002/003.
    /// </summary>
    public class EncounterBridge : MonoBehaviour
    {
        [Header("Pity Settings")]
        [SerializeField] float baseChance = 0.4f;
        [SerializeField] float pityIncrement = 0.15f;

        GameState _gameState;
        int _consecutiveMisses;

        // SF-ENC-004/005: 인카운터 콘텐츠 (M3에서 SO로 분리 예정)
        static readonly EncounterData[] DailyEncounters =
        {
            new() { Id = "daily_trade", Title = "Wandering Merchant",
                Description = "A trader offers supplies in exchange for advanced materials.",
                Choices = new[] {
                    new ChoiceData { Text = "Trade (5 Advanced → 15 Basic)", CostAdvanced = 5, RewardBasic = 15 },
                    new ChoiceData { Text = "Decline" }
                }},
            new() { Id = "daily_scout", Title = "Scout Report",
                Description = "Your scouts spotted a supply cache nearby.",
                Choices = new[] {
                    new ChoiceData { Text = "Retrieve (+8 Basic)", RewardBasic = 8 },
                    new ChoiceData { Text = "Ignore" }
                }},
            new() { Id = "daily_storm", Title = "Dust Storm",
                Description = "A storm approaches. Reinforce or shelter?",
                Choices = new[] {
                    new ChoiceData { Text = "Reinforce (-5 Basic)", CostBasic = 5 },
                    new ChoiceData { Text = "Shelter (building -20 HP)", BuildingDamage = 20 }
                }},
            new() { Id = "daily_harvest", Title = "Wild Growth",
                Description = "Unusual plants sprouted near the base overnight.",
                Choices = new[] {
                    new ChoiceData { Text = "Harvest (+6 Basic)", RewardBasic = 6 },
                    new ChoiceData { Text = "Study (+2 Advanced)", RewardAdvanced = 2 }
                }},
            new() { Id = "daily_refugee", Title = "Lone Wanderer",
                Description = "A lone survivor approaches, wounded but willing to work.",
                Choices = new[] {
                    new ChoiceData { Text = "Take in (+1 citizen, injured)", RewardCitizens = 1, InjuryCitizen = true },
                    new ChoiceData { Text = "Give supplies (-3 Basic)", CostBasic = 3 }
                }},
            new() { Id = "daily_cache", Title = "Hidden Cache",
                Description = "Workers found a buried container during construction.",
                Choices = new[] {
                    new ChoiceData { Text = "Open it (+4 Advanced)", RewardAdvanced = 4 },
                    new ChoiceData { Text = "Too risky (building -10 HP)", BuildingDamage = 10 }
                }},
            new() { Id = "daily_fog", Title = "Strange Fog",
                Description = "A thick fog rolled in. Visibility is near zero.",
                Choices = new[] {
                    new ChoiceData { Text = "Stay alert (-2 Basic for torches)", CostBasic = 2 },
                    new ChoiceData { Text = "Wait it out" }
                }},
            new() { Id = "daily_repair", Title = "Makeshift Repairs",
                Description = "Citizens found salvageable parts from wreckage.",
                Choices = new[] {
                    new ChoiceData { Text = "Use for repairs (+10 Basic)", RewardBasic = 10 },
                    new ChoiceData { Text = "Craft tools (+1 Advanced)", RewardAdvanced = 1 }
                }},
            new() { Id = "daily_morale", Title = "Campfire Stories",
                Description = "Citizens gathered to share stories of the old world.",
                Choices = new[] {
                    new ChoiceData { Text = "Join them (community)" },
                    new ChoiceData { Text = "Keep working (+3 Basic)", RewardBasic = 3 }
                }},
            new() { Id = "daily_earthquake", Title = "Minor Tremor",
                Description = "The ground shook briefly. Some structures may be affected.",
                Choices = new[] {
                    new ChoiceData { Text = "Inspect all (-3 Basic)", CostBasic = 3 },
                    new ChoiceData { Text = "Ignore (building -15 HP)", BuildingDamage = 15 }
                }},
        };

        static readonly EncounterData[] MajorEncounters =
        {
            new() { Id = "major_survivors", Title = "Survivor Group",
                Description = "A group of survivors requests entry. They bring supplies but also mouths to feed.",
                Choices = new[] {
                    new ChoiceData { Text = "Welcome all (+2 citizens, -10 Basic)", RewardCitizens = 2, CostBasic = 10 },
                    new ChoiceData { Text = "Select skilled only (+1 citizen)", RewardCitizens = 1 },
                    new ChoiceData { Text = "Turn away (+5 Basic)", RewardBasic = 5 }
                }},
            new() { Id = "major_ruin", Title = "Ancient Ruin",
                Description = "Explorers found a ruin with strange technology.",
                Choices = new[] {
                    new ChoiceData { Text = "Excavate carefully (+1 Relic)", RewardRelic = 1 },
                    new ChoiceData { Text = "Rush extraction (+3 Advanced, risk injury)", RewardAdvanced = 3, InjuryCitizen = true },
                    new ChoiceData { Text = "Seal it (no risk)" }
                }},
            new() { Id = "major_caravan", Title = "Trade Caravan",
                Description = "A well-armed caravan offers a major trade deal.",
                Choices = new[] {
                    new ChoiceData { Text = "Buy supplies (-8 Advanced → +25 Basic)", CostAdvanced = 8, RewardBasic = 25 },
                    new ChoiceData { Text = "Sell excess (-15 Basic → +6 Advanced)", CostBasic = 15, RewardAdvanced = 6 },
                    new ChoiceData { Text = "No deal" }
                }},
            new() { Id = "major_raiders", Title = "Raider Threat",
                Description = "Raiders spotted nearby. They demand tribute or fight.",
                Choices = new[] {
                    new ChoiceData { Text = "Pay tribute (-10 Basic, -3 Advanced)", CostBasic = 10, CostAdvanced = 3 },
                    new ChoiceData { Text = "Fight (building -30 HP, +1 citizen)", BuildingDamage = 30, RewardCitizens = 1 },
                    new ChoiceData { Text = "Negotiate (-5 Basic)", CostBasic = 5 }
                }},
            new() { Id = "major_artifact", Title = "Mysterious Artifact",
                Description = "An expedition returned with a glowing artifact of unknown origin.",
                Choices = new[] {
                    new ChoiceData { Text = "Research (+2 Relic)", RewardRelic = 2 },
                    new ChoiceData { Text = "Trade for supplies (+20 Basic)", RewardBasic = 20 },
                    new ChoiceData { Text = "Destroy it (citizen injured)", InjuryCitizen = true }
                }},
        };

        /// <summary>인카운터 시작 시 발행. UI 팝업 트리거.</summary>
        public event Action<EncounterData> OnEncounterStarted;

        /// <summary>인카운터 종료 (선택 완료) 시 발행.</summary>
        public event Action OnEncounterEnded;

        /// <summary>현재 대기 중인 인카운터.</summary>
        public EncounterData CurrentEncounter { get; private set; }

        public bool IsWaiting { get; private set; }

        public void Initialize(GameState state)
        {
            _gameState = state;
        }

        /// <summary>일상 인카운터 시도 (낮 시작 시). SF-ENC-001.</summary>
        public bool TryDailyEncounter()
        {
            float chance = baseChance + _consecutiveMisses * pityIncrement;
            if (UnityEngine.Random.value >= chance)
            {
                _consecutiveMisses++;
                return false;
            }

            _consecutiveMisses = 0;
            var enc = DailyEncounters[UnityEngine.Random.Range(0, DailyEncounters.Length)];
            ShowEncounter(enc);
            return true;
        }

        /// <summary>중요 인카운터 강제 발생. SF-ENC-003.</summary>
        public void TriggerMajorEncounter()
        {
            var enc = MajorEncounters[UnityEngine.Random.Range(0, MajorEncounters.Length)];
            ShowEncounter(enc);
        }

        void ShowEncounter(EncounterData enc)
        {
            CurrentEncounter = enc;
            IsWaiting = true;
            OnEncounterStarted?.Invoke(enc);
            Debug.Log($"[EncounterBridge] {enc.Title} — {enc.Choices.Length} choices");
        }

        /// <summary>선택지 적용. UI에서 호출. SF-ENC-002.</summary>
        public void ApplyChoice(int choiceIndex)
        {
            if (!IsWaiting || CurrentEncounter == null) return;
            if (choiceIndex < 0 || choiceIndex >= CurrentEncounter.Choices.Length) return;

            var choice = CurrentEncounter.Choices[choiceIndex];

            // 비용
            if (choice.CostBasic > 0) _gameState.resources.Spend(choice.CostBasic);
            if (choice.CostAdvanced > 0) _gameState.resources.Spend(0, choice.CostAdvanced);

            // 보상
            _gameState.resources.Add(choice.RewardBasic, choice.RewardAdvanced, choice.RewardRelic);

            // 시민 합류
            for (int i = 0; i < choice.RewardCitizens; i++)
            {
                string id = $"citizen_{_gameState.citizens.Count}";
                _gameState.citizens.Add(new CitizenRuntimeState
                {
                    citizenId = id,
                    displayName = $"Newcomer {_gameState.citizens.Count}",
                    aptitude = (CitizenAptitude)UnityEngine.Random.Range(1, 5),
                    proficiencyLevel = 0,
                    state = CitizenState.Idle
                });
            }

            // 건물 데미지
            if (choice.BuildingDamage > 0 && _gameState.buildings.Count > 0)
            {
                int idx = UnityEngine.Random.Range(0, _gameState.buildings.Count);
                var b = _gameState.buildings[idx];
                b.currentHP = Mathf.Max(0, b.currentHP - choice.BuildingDamage);
                if (b.currentHP == 0 && b.state == BuildingSlotStateV2.Active)
                    b.state = BuildingSlotStateV2.Damaged;
            }

            // 시민 부상
            if (choice.InjuryCitizen)
            {
                var idle = _gameState.citizens.FindAll(c => c.state == CitizenState.Idle);
                if (idle.Count > 0)
                    idle[UnityEngine.Random.Range(0, idle.Count)].state = CitizenState.Injured;
            }

            Debug.Log($"[EncounterBridge] Choice: {choice.Text}");

            IsWaiting = false;
            CurrentEncounter = null;
            OnEncounterEnded?.Invoke();
        }
    }

    [Serializable]
    public class EncounterData
    {
        public string Id;
        public string Title;
        public string Description;
        public ChoiceData[] Choices;
    }

    [Serializable]
    public class ChoiceData
    {
        public string Text;
        public int CostBasic;
        public int CostAdvanced;
        public int RewardBasic;
        public int RewardAdvanced;
        public int RewardRelic;
        public int RewardCitizens;
        public int BuildingDamage;
        public bool InjuryCitizen;
    }
}

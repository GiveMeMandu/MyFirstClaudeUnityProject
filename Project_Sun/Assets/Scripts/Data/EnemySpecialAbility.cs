using System;
using UnityEngine;

namespace ProjectSun.V2.Data
{
    [Serializable]
    public class EnemySpecialAbility
    {
        public string abilityId;
        public string displayName;
        [TextArea(1, 3)]
        public string description;

        [Header("Wall Bypass")]
        [Tooltip("Ignores walls and surfaces underground (Burrower)")]
        public bool bypassWalls;

        [Header("Wall Bonus Damage")]
        [Tooltip("Damage multiplier against walls (Charger: 2.0)")]
        public float wallDamageMultiplier = 1f;

        [Header("Death Explosion")]
        [Tooltip("Deals AoE damage on death (Bloater)")]
        public bool explodesOnDeath;
        public float deathExplosionRadius;
        public float deathExplosionDamage;

        [Header("Ranged Damage Reduction")]
        [Tooltip("Ranged damage reduction percentage 0~1 (Armored: 0.4)")]
        [Range(0f, 1f)]
        public float rangedDamageReduction;

        [Header("Stealth")]
        [Tooltip("Invisible until within watchtower vision range (Stalker)")]
        public bool hasStealth;

        [Header("Front Shield")]
        [Tooltip("Absorbs ranged damage from the front (Shield)")]
        public bool hasFrontShield;
        public float shieldHP;

        [Header("Buff Aura")]
        [Tooltip("Buffs nearby allies (Matriarch)")]
        public bool hasBuffAura;
        public float auraRadius;
        public float auraSpeedBonus;
        public float auraAttackBonus;

        [Header("Enrage")]
        [Tooltip("Gains bonus attack below HP threshold (Devastator)")]
        public bool hasEnrage;
        [Range(0f, 1f)]
        public float enrageHPThreshold;
        public float enrageAttackMultiplier;

        [Header("Building Bonus Damage")]
        [Tooltip("Damage multiplier against buildings (Bombardier: 1.5)")]
        public float buildingDamageMultiplier = 1f;

        [Header("Wall Bypass Attempt")]
        [Tooltip("Tries to find alternate route around walls (Sprinter)")]
        public bool attemptsWallBypass;
    }
}

using UnityEngine;

namespace ProjectSun.Defense
{
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "ProjectSun/Defense/Enemy Data")]
    public class EnemyDataSO : ScriptableObject
    {
        [Header("기본 정보")]
        public string enemyName;
        public EnemyType enemyType;

        [Header("스탯")]
        [Min(1f)]
        public float hp = 30f;

        [Min(0.1f)]
        public float speed = 3f;

        [Min(0f)]
        public float damage = 5f;

        [Min(0.1f)]
        public float attackRange = 2f;

        [Min(0.1f)]
        public float attackInterval = 1f;

        [Header("비주얼")]
        [Min(0.1f)]
        public float scale = 1f;
        public Color tintColor = Color.white;
        public GameObject prefab;
    }
}

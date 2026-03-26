namespace ProjectSun.TechTree
{
    public enum TechNodeState
    {
        Locked,         // 선행 연구 미완료 — 잠금 아이콘
        Available,      // 연구 가능 — 밝게 표시
        InProgress,     // 연구 진행 중 — 진행 바
        Paused,         // 전환으로 일시 중지 — 진행도 보존
        Completed       // 연구 완료 — 체크 표시
    }

    public enum TechEffectType
    {
        BuildingUpgrade,    // 건물 업그레이드 분기 해금
        SlotReveal,         // 숨겨진 건물 슬롯 공개
        StatBonus,          // 능력치 보너스 (글로벌/건물별)
        BuildingSlotAdd,    // 건물 내 인력 슬롯 추가
        FeatureUnlock       // 새로운 기능 해금
    }

    public enum TechCategory
    {
        Economy,    // 자원/생산
        Defense,    // 방어/전투
        Utility     // 건설/탐사/행정
    }
}

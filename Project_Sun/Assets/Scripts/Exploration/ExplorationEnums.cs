namespace ProjectSun.Exploration
{
    public enum ExplorationNodeType
    {
        Resource,       // 자원 획득
        Recon,          // 적 웨이브 정보 공개
        Encounter,      // 중요 인카운터 발생
        Tech            // 기술 해금 조건 충족
    }

    public enum FogState
    {
        Hidden,         // 존재 자체를 모름
        Hinted,         // 유형 아이콘만 보임 (연결 경로 표시)
        Revealed        // 완전 공개 (방문 완료)
    }

    public enum ExpeditionState
    {
        Idle,           // 기지 대기
        Moving,         // 목적지로 이동 중
        Arrived,        // 노드 도착, 대기 중
        Returning       // 기지로 귀환 중
    }
}

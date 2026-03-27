namespace UIStudy.MVRP.Models
{
    /// <summary>
    /// 설정 데이터 — Plain C# class, MonoBehaviour 아님.
    /// VContainer에서 RegisterInstance로 등록하여 다른 클래스에 주입한다.
    /// 파라미터 없는 기본 생성자를 사용해야 VContainer가 해결 가능.
    /// </summary>
    public class GameConfig
    {
        public string GameTitle { get; set; } = "UI Study";
        public int StartingGold { get; set; } = 100;
        public int MaxPopulation { get; set; } = 50;
    }
}

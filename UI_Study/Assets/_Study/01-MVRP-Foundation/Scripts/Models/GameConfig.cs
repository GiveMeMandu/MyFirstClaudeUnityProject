namespace UIStudy.MVRP.Models
{
    /// <summary>
    /// 설정 데이터 — Plain C# class, MonoBehaviour 아님.
    /// VContainer에서 Register로 등록하여 다른 클래스에 주입한다.
    /// </summary>
    public class GameConfig
    {
        public string GameTitle { get; }
        public int StartingGold { get; }
        public int MaxPopulation { get; }

        public GameConfig(string gameTitle = "UI Study", int startingGold = 100, int maxPopulation = 50)
        {
            GameTitle = gameTitle;
            StartingGold = startingGold;
            MaxPopulation = maxPopulation;
        }
    }
}

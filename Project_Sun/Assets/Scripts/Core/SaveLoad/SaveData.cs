using System;
using ProjectSun.V2.Data;

namespace ProjectSun.V2.Core
{
    [Serializable]
    public class SaveData
    {
        public int saveVersion = 1;
        public string timestamp;
        public GameState gameState;

        public static SaveData Create(GameState state)
        {
            return new SaveData
            {
                saveVersion = 1,
                timestamp = DateTime.UtcNow.ToString("o"),
                gameState = state
            };
        }
    }
}

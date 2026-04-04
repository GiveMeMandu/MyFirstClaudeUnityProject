using System;
using System.Collections.Generic;

namespace ProjectSun.V2.Data
{
    [Serializable]
    public class GameState
    {
        public int currentTurn = 1;
        public PhaseType currentPhase = PhaseType.Day;

        public ResourceState resources = new();

        public List<BuildingRuntimeState> buildings = new();
        public List<CitizenRuntimeState> citizens = new();
        public List<ExplorationNodeRuntimeState> explorationNodes = new();
        public List<WaveResult> waveHistory = new();
    }
}

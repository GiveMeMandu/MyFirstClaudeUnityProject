using System;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 6: 4종 자원 모델 — Gold, Wood, Food, Pop.
    /// 각 자원별 C# event로 변경 알림. UI 참조 없는 순수 C# 클래스.
    /// </summary>
    public class GameResourceModel
    {
        private int _gold;
        private int _wood;
        private int _food;
        private int _pop;

        public event Action<int> GoldChanged;
        public event Action<int> WoodChanged;
        public event Action<int> FoodChanged;
        public event Action<int> PopChanged;

        public int Gold
        {
            get => _gold;
            private set { _gold = value; GoldChanged?.Invoke(value); }
        }

        public int Wood
        {
            get => _wood;
            private set { _wood = value; WoodChanged?.Invoke(value); }
        }

        public int Food
        {
            get => _food;
            private set { _food = value; FoodChanged?.Invoke(value); }
        }

        public int Pop
        {
            get => _pop;
            private set { _pop = value; PopChanged?.Invoke(value); }
        }

        public GameResourceModel(int gold = 500, int wood = 300, int food = 200, int pop = 10)
        {
            _gold = gold;
            _wood = wood;
            _food = food;
            _pop = pop;
        }

        public void GainResource(ResourceType type, int amount)
        {
            switch (type)
            {
                case ResourceType.Gold: Gold += amount; break;
                case ResourceType.Wood: Wood += amount; break;
                case ResourceType.Food: Food += amount; break;
                case ResourceType.Pop:  Pop  += amount; break;
            }
        }

        public bool SpendResource(ResourceType type, int amount)
        {
            switch (type)
            {
                case ResourceType.Gold:
                    if (_gold < amount) return false;
                    Gold -= amount;
                    return true;
                case ResourceType.Wood:
                    if (_wood < amount) return false;
                    Wood -= amount;
                    return true;
                case ResourceType.Food:
                    if (_food < amount) return false;
                    Food -= amount;
                    return true;
                case ResourceType.Pop:
                    if (_pop < amount) return false;
                    Pop -= amount;
                    return true;
                default:
                    return false;
            }
        }

        public int GetResource(ResourceType type)
        {
            return type switch
            {
                ResourceType.Gold => _gold,
                ResourceType.Wood => _wood,
                ResourceType.Food => _food,
                ResourceType.Pop  => _pop,
                _                 => 0,
            };
        }

        public bool CanAfford(int goldCost, int woodCost)
        {
            return _gold >= goldCost && _wood >= woodCost;
        }

        public bool SpendBuildCost(int goldCost, int woodCost)
        {
            if (!CanAfford(goldCost, woodCost)) return false;
            Gold -= goldCost;
            Wood -= woodCost;
            return true;
        }
    }

    public enum ResourceType
    {
        Gold,
        Wood,
        Food,
        Pop
    }
}

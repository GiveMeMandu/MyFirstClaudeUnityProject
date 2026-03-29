using System;

namespace UIStudy.UIToolkitLightweight
{
    /// <summary>
    /// Step 2: Plain C# Model — UI 참조 없음, C# event로만 변경 알림.
    /// VContainer/R3 없이 순수 C# 이벤트로 동작.
    /// </summary>
    public class ResourceModel
    {
        private int _gold;
        private int _wood;
        private int _food;

        public event Action<int> GoldChanged;
        public event Action<int> WoodChanged;
        public event Action<int> FoodChanged;

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

        public ResourceModel(int gold = 50, int wood = 30, int food = 20)
        {
            _gold = gold;
            _wood = wood;
            _food = food;
        }

        public void GainAll(int amount)
        {
            Gold += amount;
            Wood += amount;
            Food += amount;
        }

        public bool CanSpend(int amount)
        {
            return _gold >= amount && _wood >= amount && _food >= amount;
        }

        public bool SpendAll(int amount)
        {
            if (!CanSpend(amount)) return false;
            Gold -= amount;
            Wood -= amount;
            Food -= amount;
            return true;
        }

        public bool SpendGold(int amount)
        {
            if (_gold < amount) return false;
            Gold -= amount;
            return true;
        }

        public bool SpendWood(int amount)
        {
            if (_wood < amount) return false;
            Wood -= amount;
            return true;
        }

        public bool SpendFood(int amount)
        {
            if (_food < amount) return false;
            Food -= amount;
            return true;
        }

        public void GainGold(int amount) => Gold += amount;
        public void GainWood(int amount) => Wood += amount;
        public void GainFood(int amount) => Food += amount;
    }
}

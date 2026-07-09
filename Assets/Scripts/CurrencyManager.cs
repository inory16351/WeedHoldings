using System;
using UnityEngine;

namespace WeedHoldings
{
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        [Header("재화 설정")]
        public int startGold = GameConstants.STARTING_GOLD;

        public int CurrentGold { get; private set; }

        public event Action<int> OnGoldChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadGold();
        }

        void LoadGold()
        {
            CurrentGold = PlayerPrefs.GetInt(GameConstants.GOLD_SAVE_KEY, startGold);
            if (CurrentGold < 99999)
            {
                CurrentGold = 99999;
                SaveGold();
            }
        }

        void SaveGold()
        {
            PlayerPrefs.SetInt(GameConstants.GOLD_SAVE_KEY, CurrentGold);
            PlayerPrefs.Save();
        }

        public bool CanAfford(int amount)
        {
            return CurrentGold >= amount;
        }

        public bool SpendGold(int amount)
        {
            if (!CanAfford(amount))
                return false;

            CurrentGold -= amount;
            OnGoldChanged?.Invoke(CurrentGold);
            SaveGold();
            return true;
        }

        public void AddGold(int amount)
        {
            if (amount < 0)
            {
                SpendGold(-amount);
                return;
            }

            CurrentGold += amount;
            OnGoldChanged?.Invoke(CurrentGold);
            SaveGold();
        }

        public void SetGold(int amount)
        {
            CurrentGold = Mathf.Max(0, amount);
            OnGoldChanged?.Invoke(CurrentGold);
            SaveGold();
        }
    }
}

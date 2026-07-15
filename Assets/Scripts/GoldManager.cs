using UnityEngine;
using TMPro;

namespace WeedHoldings
{
    public class GoldManager : MonoBehaviour
    {
        public static GoldManager Instance { get; private set; }

        public TextMeshProUGUI goldText;
        public int gold = 1000;

        void Awake()
        {
            Instance = this;
            UpdateGoldDisplay();
        }

        public void AddGold(int amount)
        {
            gold += amount;
            UpdateGoldDisplay();
        }

        public bool SpendGold(int amount)
        {
            if (gold < amount) return false;
            gold -= amount;
            UpdateGoldDisplay();
            return true;
        }

        void UpdateGoldDisplay()
        {
            if (goldText != null)
                goldText.text = gold.ToString("N0");
        }
    }
}

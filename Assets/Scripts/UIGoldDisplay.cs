using UnityEngine;
using TMPro;

namespace WeedHoldings
{
    public class UIGoldDisplay : MonoBehaviour
    {
        public TextMeshProUGUI goldText;

        void Start()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnGoldChanged += UpdateDisplay;
                UpdateDisplay(CurrencyManager.Instance.CurrentGold);
            }
        }

        void OnDestroy()
        {
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnGoldChanged -= UpdateDisplay;
        }

        void UpdateDisplay(int gold)
        {
            if (goldText != null)
                goldText.text = $"G {gold:N0}";
        }
    }
}

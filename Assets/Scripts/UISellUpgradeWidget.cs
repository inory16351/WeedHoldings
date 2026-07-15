using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WeedHoldings
{
    /// <summary>
    /// Sell_Upgrade_Panel(연구소의 무역소 시설 업그레이드 위젯)에 부착.
    /// UIFarmUpgradeWidget/UIFactoryUpgradeWidget과 동일한 로직이며 데이터만 SellUpgradeManager로 바뀐다.
    /// </summary>
    public class UISellUpgradeWidget : MonoBehaviour
    {
        Button upgradeButton;
        TMP_Text levelText;
        TMP_Text goldText;

        void Awake()
        {
            upgradeButton = transform.Find("Sell_Upgrade")?.GetComponent<Button>();
            levelText = transform.Find("Upgrade_Level")?.GetComponent<TMP_Text>();
            goldText = transform.Find("Req_Gold")?.GetComponent<TMP_Text>();

            if (upgradeButton != null)
                upgradeButton.onClick.AddListener(OnUpgradeClicked);

            UIScrollListFactory.RepositionUpgradeInfoTexts(
                transform as RectTransform,
                transform.Find("Sell_Icon") as RectTransform,
                levelText != null ? levelText.rectTransform : null,
                goldText != null ? goldText.rectTransform : null,
                0.7f);

            ConfigureAutoSize(levelText, 12f, 20f);
            ConfigureAutoSize(goldText, 12f, 20f);

            var buttonText = upgradeButton != null ? upgradeButton.GetComponentInChildren<TMP_Text>() : null;
            if (buttonText != null) buttonText.text = "LV UP!";
            ConfigureAutoSize(buttonText, 8f, 13f);

            UIScrollListFactory.ApplyNotoSansFont(this);
        }

        void OnEnable()
        {
            if (SellUpgradeManager.Instance != null)
                SellUpgradeManager.Instance.OnLevelChanged += RefreshDisplay;
            RefreshDisplay();
        }

        void OnDisable()
        {
            if (SellUpgradeManager.Instance != null)
                SellUpgradeManager.Instance.OnLevelChanged -= RefreshDisplay;
        }

        void Update()
        {
            if (upgradeButton != null && SellUpgradeManager.Instance != null)
                upgradeButton.interactable = SellUpgradeManager.Instance.CanUpgrade();
        }

        void OnUpgradeClicked()
        {
            if (SellUpgradeManager.Instance == null) return;
            if (SellUpgradeManager.Instance.TryUpgrade())
                RefreshDisplay();
        }

        void RefreshDisplay()
        {
            if (SellUpgradeManager.Instance == null) return;

            int level = SellUpgradeManager.Instance.CurrentLevel;
            var next = SellUpgradeManager.Instance.GetNextUpgradeData();

            if (levelText != null)
                levelText.text = $"Lv.{level}";

            if (goldText != null)
                goldText.text = next != null ? $"{next.requiredGold:N0}G" : "MAX";

            if (upgradeButton != null)
                upgradeButton.interactable = next != null && SellUpgradeManager.Instance.CanUpgrade();
        }

        static void ConfigureAutoSize(TMP_Text text, float min, float max)
        {
            if (text == null) return;
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
        }
    }
}

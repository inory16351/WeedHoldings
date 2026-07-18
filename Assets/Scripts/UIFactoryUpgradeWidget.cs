using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WeedHoldings
{
    /// <summary>
    /// Factory_Upgrade_Panel(연구소의 제조 시설 업그레이드 위젯)에 부착.
    /// UIFarmUpgradeWidget과 동일한 로직이며 데이터만 LabUpgradeManager -> FactoryUpgradeManager로 바뀐다.
    /// </summary>
    public class UIFactoryUpgradeWidget : MonoBehaviour
    {
        Button upgradeButton;
        TMP_Text levelText;
        TMP_Text goldText;

        void Awake()
        {
            upgradeButton = transform.Find("Factory_Upgrade")?.GetComponent<Button>();
            levelText = transform.Find("Upgrade_Level")?.GetComponent<TMP_Text>();
            goldText = transform.Find("Req_Gold")?.GetComponent<TMP_Text>();

            if (upgradeButton != null)
                upgradeButton.onClick.AddListener(OnUpgradeClicked);

            UIScrollListFactory.RepositionUpgradeInfoTexts(
                transform as RectTransform,
                transform.Find("Factory_Icon") as RectTransform,
                levelText != null ? levelText.rectTransform : null,
                goldText != null ? goldText.rectTransform : null,
                0.7f);

            ConfigureAutoSize(levelText, 12f, 20f);
            ConfigureAutoSize(goldText, 12f, 20f);

            var buttonText = upgradeButton != null ? upgradeButton.GetComponentInChildren<TMP_Text>() : null;
            if (buttonText != null) buttonText.text = "LV UP!";
            ConfigureAutoSize(buttonText, 8f, 13f);
        }

        static void ConfigureAutoSize(TMP_Text text, float min, float max)
        {
            if (text == null) return;
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
        }

        void OnEnable()
        {
            if (FactoryUpgradeManager.Instance != null)
                FactoryUpgradeManager.Instance.OnLevelChanged += RefreshDisplay;
            RefreshDisplay();
        }

        void OnDisable()
        {
            if (FactoryUpgradeManager.Instance != null)
                FactoryUpgradeManager.Instance.OnLevelChanged -= RefreshDisplay;
        }

        void Update()
        {
            if (upgradeButton != null && FactoryUpgradeManager.Instance != null)
                upgradeButton.interactable = FactoryUpgradeManager.Instance.CanUpgrade();
        }

        void OnUpgradeClicked()
        {
            if (FactoryUpgradeManager.Instance == null) return;
            if (FactoryUpgradeManager.Instance.TryUpgrade())
            {
                SfxManager.Play("Factory01");
                RefreshDisplay();
            }
        }

        void RefreshDisplay()
        {
            if (FactoryUpgradeManager.Instance == null) return;

            int level = FactoryUpgradeManager.Instance.CurrentLevel;
            var next = FactoryUpgradeManager.Instance.GetNextUpgradeData();

            if (levelText != null)
                levelText.text = $"Lv.{level}";

            if (goldText != null)
                goldText.text = next != null ? $"{next.requiredGold:N0}G" : "MAX";

            if (upgradeButton != null)
                upgradeButton.interactable = next != null && FactoryUpgradeManager.Instance.CanUpgrade();
        }
    }
}

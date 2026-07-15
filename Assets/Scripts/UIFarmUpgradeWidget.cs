using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WeedHoldings
{
    /// <summary>
    /// Farm_Upgrade_Panel(연구소의 재배 시설 업그레이드 위젯)에 부착.
    /// Farm_Upgrade 버튼, Upgrade_Level / Req_Gold 텍스트를 자식 이름으로 찾아 연결한다.
    /// </summary>
    public class UIFarmUpgradeWidget : MonoBehaviour
    {
        Button upgradeButton;
        TMP_Text levelText;
        TMP_Text goldText;

        void Awake()
        {
            upgradeButton = transform.Find("Farm_Upgrade")?.GetComponent<Button>();
            levelText = transform.Find("Upgrade_Level")?.GetComponent<TMP_Text>();
            goldText = transform.Find("Req_Gold")?.GetComponent<TMP_Text>();

            if (upgradeButton != null)
                upgradeButton.onClick.AddListener(OnUpgradeClicked);

            UIScrollListFactory.RepositionUpgradeInfoTexts(
                transform as RectTransform,
                transform.Find("Farm_Icon") as RectTransform,
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
            if (LabUpgradeManager.Instance != null)
            {
                LabUpgradeManager.Instance.ReapplyGrowthBonus();
                LabUpgradeManager.Instance.OnLevelChanged += RefreshDisplay;
            }
            RefreshDisplay();
        }

        void OnDisable()
        {
            if (LabUpgradeManager.Instance != null)
                LabUpgradeManager.Instance.OnLevelChanged -= RefreshDisplay;
        }

        void Update()
        {
            // 골드 변동에 따라 매 프레임 버튼 활성 여부만 갱신 (WaterButtonHandler와 동일한 폴링 방식)
            if (upgradeButton != null && LabUpgradeManager.Instance != null)
                upgradeButton.interactable = LabUpgradeManager.Instance.CanUpgrade();
        }

        void OnUpgradeClicked()
        {
            if (LabUpgradeManager.Instance == null) return;
            if (LabUpgradeManager.Instance.TryUpgrade())
                RefreshDisplay();
        }

        void RefreshDisplay()
        {
            if (LabUpgradeManager.Instance == null) return;

            int level = LabUpgradeManager.Instance.CurrentLevel;
            var next = LabUpgradeManager.Instance.GetNextUpgradeData();

            if (levelText != null)
                levelText.text = $"Lv.{level}";

            if (goldText != null)
                goldText.text = next != null ? $"{next.requiredGold:N0}G" : "MAX";

            if (upgradeButton != null)
                upgradeButton.interactable = next != null && LabUpgradeManager.Instance.CanUpgrade();
        }
    }
}

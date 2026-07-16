using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WeedHoldings
{
    /// <summary>
    /// 보약 목록의 한 행(Potion_Panel_01 복제본). 해금된 보약은 정상 표시,
    /// 해금 안 된 보약은 실루엣(어두운 아이콘 + "???")으로 표시한다.
    /// </summary>
    public class UIPotionListSlot : MonoBehaviour
    {
        public Image icon;
        public TMP_Text nameText;
        public TMP_Text goldText;
        public TMP_Text timeText;
        public Button selectButton;

        PotionData potion;
        public PotionData CurrentPotion => potion;

        Color defaultBg = new Color(0.18f, 0.18f, 0.22f, 0.9f);
        Color selectedBg = new Color(0.25f, 0.45f, 0.85f, 0.9f);
        Color lockedBg = new Color(0.12f, 0.12f, 0.14f, 0.9f);
        Image background;

        void Awake()
        {
            background = GetComponent<Image>();
        }

        public void Setup(PotionData potionData, bool selected)
        {
            if (potionData == null) return;
            potion = potionData;

            bool unlocked = DataManager.Instance != null && DataManager.Instance.IsPotionUnlocked(potion.potionID);

            if (icon != null)
            {
                icon.sprite = potion.potionIcon;
                icon.color = unlocked ? Color.white : new Color(0.05f, 0.05f, 0.05f);
            }

            if (nameText != null)
            {
                nameText.text = unlocked ? potion.potionName : "???";
                ConfigureAutoSize(nameText, 10f, 16f);
            }

            if (goldText != null)
            {
                goldText.text = unlocked ? $"◉ {potion.sellGold:N0}" : $"필요 Lv.{potion.requiredUnlockLevel}";
                ConfigureAutoSize(goldText, 9f, 13f);
            }

            if (timeText != null)
            {
                int primaryPlantId = DataManager.Instance != null ? DataManager.Instance.GetPotionPrimaryMaterialPlantID(potion.potionID) : 0;
                float multiplier = FactoryUpgradeManager.Instance != null ? FactoryUpgradeManager.Instance.GetPotionTimeMultiplier(primaryPlantId) : 1f;
                timeText.text = unlocked ? $"🕐 {potion.potionTimeSeconds / multiplier:F0}초" : "";
                ConfigureAutoSize(timeText, 9f, 13f);
            }

            if (background != null)
                background.color = !unlocked ? lockedBg : (selected ? selectedBg : defaultBg);

            if (selectButton != null)
                selectButton.interactable = unlocked;
        }

        public void SetHighlight(bool selected)
        {
            bool unlocked = DataManager.Instance != null && potion != null && DataManager.Instance.IsPotionUnlocked(potion.potionID);
            if (background != null && unlocked)
                background.color = selected ? selectedBg : defaultBg;
        }

        static void ConfigureAutoSize(TMP_Text text, float min, float max)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
        }
    }
}

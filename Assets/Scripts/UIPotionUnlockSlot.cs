using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WeedHoldings
{
    /// <summary>
    /// 연구소 포션 언락 리스트의 한 행. UIPlantUnlockSlot과 동일한 로직이며 데이터만 PotionData/FactoryUpgradeManager로 바뀐다.
    /// </summary>
    public class UIPotionUnlockSlot : MonoBehaviour
    {
        public Image icon;
        public TMP_Text nameText;
        public TMP_Text infoText;
        public Button unlockButton;
        public TMP_Text unlockCostText;

        const string LockedDescription = "보약 해금 시 정보를 확인할 수 있습니다";

        PotionData potion;
        public int PotionID => potion != null ? potion.potionID : -1;
        public PotionData CurrentPotion => potion;

        static void SetAutoSize(TMP_Text text, float min, float max)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
        }

        public void Setup(PotionData potionData)
        {
            if (potionData == null) return;
            potion = potionData;

            bool unlocked = DataManager.Instance != null && DataManager.Instance.IsPotionUnlocked(potion.potionID);
            int currentLevel = FactoryUpgradeManager.Instance != null ? FactoryUpgradeManager.Instance.CurrentLevel : 1;
            bool levelMet = currentLevel >= potion.requiredUnlockLevel;
            bool goldEnough = GoldManager.Instance != null && GoldManager.Instance.gold >= potion.requiredUnlockGold;

            if (icon != null)
            {
                icon.sprite = potion.potionIcon;
                icon.preserveAspect = true;
                icon.color = unlocked ? Color.white : new Color(0.03f, 0.03f, 0.03f);
            }

            if (nameText != null)
            {
                SetAutoSize(nameText, 10f, 15f);
                nameText.text = unlocked
                    ? $"{potion.potionName}\n{potion.description}"
                    : $"???\n{LockedDescription}";
            }

            if (infoText != null)
            {
                SetAutoSize(infoText, 9f, 13f);
                infoText.text = unlocked
                    ? "해금 완료"
                    : $"공장 Lv.{potion.requiredUnlockLevel} 달성 시 해금!";
                infoText.color = !unlocked && !levelMet ? new Color(1f, 0.4f, 0.4f) : Color.white;
            }

            if (unlockCostText != null)
            {
                SetAutoSize(unlockCostText, 10f, 14f);
                unlockCostText.text = $"{potion.requiredUnlockGold:N0}G";
            }

            if (unlockButton != null)
            {
                unlockButton.gameObject.SetActive(!unlocked);
                unlockButton.interactable = !unlocked && levelMet && goldEnough;
            }
        }
    }
}

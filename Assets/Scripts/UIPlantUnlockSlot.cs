using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WeedHoldings
{
    /// <summary>
    /// 연구소 식물 언락 리스트의 한 행. 잠금 상태/레벨/골드 조건에 따라 표시를 갱신한다.
    /// </summary>
    public class UIPlantUnlockSlot : MonoBehaviour
    {
        public Image icon;
        public TMP_Text nameText;
        public TMP_Text infoText;
        public Button unlockButton;
        public TMP_Text unlockCostText;

        const string LockedDescription = "식물 해금 시 설명을 확인할 수 있습니다";

        PlantData plant;
        public int PlantID => plant != null ? plant.plantID : -1;
        public PlantData CurrentPlant => plant;

        static void SetAutoSize(TMP_Text text, float min, float max)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
        }

        public void Setup(PlantData plantData)
        {
            if (plantData == null) return;
            plant = plantData;

            bool unlocked = DataManager.Instance != null && DataManager.Instance.IsPlantUnlocked(plant.plantID);
            int currentLevel = LabUpgradeManager.Instance != null ? LabUpgradeManager.Instance.CurrentLevel : 1;
            bool levelMet = currentLevel >= plant.requiredHarvestLevel;
            bool goldEnough = GoldManager.Instance != null && GoldManager.Instance.gold >= plant.requiredGoldToUnlock;

            if (icon != null)
            {
                icon.sprite = plant.iconSprite;
                icon.preserveAspect = true;
                icon.color = unlocked ? Color.white : new Color(0.03f, 0.03f, 0.03f);
            }

            if (nameText != null)
            {
                SetAutoSize(nameText, 10f, 15f);
                nameText.text = unlocked
                    ? $"{plant.plantName}\n{plant.description}"
                    : $"???\n{LockedDescription}";
            }

            if (infoText != null)
            {
                SetAutoSize(infoText, 9f, 13f);
                infoText.text = unlocked
                    ? "해금 완료"
                    : $"농장 Lv.{plant.requiredHarvestLevel} 달성 시 해금!";
                infoText.color = !unlocked && !levelMet ? new Color(1f, 0.4f, 0.4f) : Color.white;
            }

            if (unlockCostText != null)
            {
                SetAutoSize(unlockCostText, 10f, 14f);
                unlockCostText.text = $"{plant.requiredGoldToUnlock:N0}G";
            }

            if (unlockButton != null)
            {
                unlockButton.gameObject.SetActive(!unlocked);
                unlockButton.interactable = !unlocked && levelMet && goldEnough;
            }
        }
    }
}

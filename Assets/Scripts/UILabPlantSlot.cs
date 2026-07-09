using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace WeedHoldings
{
    public class UILabPlantSlot : MonoBehaviour
    {
        public Image           iconImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI reqText;
        public Button          unlockButton;
        public TextMeshProUGUI btnText;

        PlantData targetPlant;
        public event Action<PlantData> OnUnlockClicked;

        public void Setup(PlantData pd, bool isUnlocked, int labLevel, bool canAfford)
        {
            targetPlant = pd;
            
            if (nameText != null)
                nameText.text = pd.plantName;

            if (iconImage != null)
            {
                if (pd.iconSprite != null)
                {
                    iconImage.sprite = pd.iconSprite;
                    iconImage.color = Color.white;
                }
                else
                {
                    iconImage.color = new Color(0.4f, 0.4f, 0.4f, 1f);
                }
            }

            if (isUnlocked)
            {
                if (reqText != null) reqText.text = "<color=#80FF80>연구 완료</color>";
                if (unlockButton != null) unlockButton.interactable = false;
                if (btnText != null) btnText.text = "완료";
            }
            else
            {
                if (labLevel >= pd.requiredHarvestLevel)
                {
                    if (reqText != null) reqText.text = "<color=#FFFF80>연구 가능</color>";
                    if (unlockButton != null) unlockButton.interactable = canAfford;
                    if (btnText != null) btnText.text = $"{pd.requiredGoldToUnlock:N0}G";
                }
                else
                {
                    if (reqText != null) reqText.text = $"연구소 Lv.{pd.requiredHarvestLevel} 필요";
                    if (unlockButton != null) unlockButton.interactable = false;
                    if (btnText != null) btnText.text = "잠김";
                }
            }

            if (unlockButton != null)
            {
                unlockButton.onClick.RemoveAllListeners();
                unlockButton.onClick.AddListener(() => OnUnlockClicked?.Invoke(targetPlant));
            }
        }
    }
}

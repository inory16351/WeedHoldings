using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WeedHoldings
{
    /// <summary>
    /// 무역소 Inventory_Slot_01 복제본 한 칸. 보유 중인 포션 1종을 표시하고 Plus/Minus로
    /// 판매할 수량(To_Sell)을 조절한다. 총 적재량 상한 검증은 UISellPanelController가 담당한다.
    /// </summary>
    public class UITradeInventorySlot : MonoBehaviour
    {
        public Image icon;
        public Button plusButton;
        public Button minusButton;
        public TMP_Text toSellText;
        public TMP_Text amountText;

        public int PotionID { get; private set; }
        public int OwnedAmount { get; private set; }
        public int SelectedAmount { get; private set; }

        public void Setup(PotionData potion, int owned, System.Action<UITradeInventorySlot> onPlus, System.Action<UITradeInventorySlot> onMinus)
        {
            PotionID = potion.potionID;
            OwnedAmount = owned;
            SelectedAmount = 0;

            if (icon != null)
            {
                icon.sprite = potion.potionIcon;
                icon.preserveAspect = true;
                // BindSlotComponent가 icon 필드를 채우는 시점이 Awake보다 뒤라서(복제 직후 코드로
                // 연결), Awake에서 처리하면 icon이 항상 null이라 슬롯 배경이 적용되지 않았다.
                CharacterCardVisuals.FixupSlotIconBackground(icon.transform);
            }
            RefreshTexts();

            if (plusButton != null)
            {
                plusButton.onClick.RemoveAllListeners();
                plusButton.onClick.AddListener(() => onPlus?.Invoke(this));
            }
            if (minusButton != null)
            {
                minusButton.onClick.RemoveAllListeners();
                minusButton.onClick.AddListener(() => onMinus?.Invoke(this));
            }
        }

        /// <summary>
        /// 지역/선박을 다시 선택해 인벤토리 슬롯이 통째로 재생성될 때, 이전에 골라뒀던 판매 수량을
        /// 그대로 복원하기 위한 용도. 보유 수량을 넘지 않도록 클램프한다.
        /// </summary>
        public void SetSelectedAmount(int amount)
        {
            SelectedAmount = Mathf.Clamp(amount, 0, OwnedAmount);
            RefreshTexts();
        }

        public bool TryIncrease()
        {
            if (SelectedAmount >= OwnedAmount) return false;
            SelectedAmount++;
            RefreshTexts();
            return true;
        }

        public bool TryDecrease()
        {
            if (SelectedAmount <= 0) return false;
            SelectedAmount--;
            RefreshTexts();
            return true;
        }

        void RefreshTexts()
        {
            if (toSellText != null) toSellText.text = SelectedAmount.ToString();
            if (amountText != null) amountText.text = $"보유 {OwnedAmount}";
        }
    }
}

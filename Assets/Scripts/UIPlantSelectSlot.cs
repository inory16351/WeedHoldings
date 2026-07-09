using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WeedHoldings
{
    /// <summary>
    /// 재배 화면 우측 식물 선택 리스트 슬롯 (1행).
    /// </summary>
    public class UIPlantSelectSlot : MonoBehaviour
    {
        [Header("UI 요소")]
        public Image              iconImage;
        public TextMeshProUGUI    nameText;
        public TextMeshProUGUI    stockText;    // 보유량
        public Button             selectButton;
        public Image              lockOverlay;
        public TextMeshProUGUI    reqLevelText; // 필요 연구소 레벨 (잠금 상태)

        PlantData currentPlantData;
        bool      isLocked;
        private Image rootImg;

        public event Action<PlantData> OnSlotClicked;

        void Start()
        {
            if (selectButton != null)
                selectButton.onClick.AddListener(HandleClick);
        }

        public PlantData GetPlantData() => currentPlantData;

        public void SetSelected(bool selected)
        {
            if (rootImg == null) rootImg = GetComponent<Image>();
            if (rootImg != null)
            {
                rootImg.color = selected 
                    ? new Color(0.28f, 0.40f, 0.60f) 
                    : new Color(0.22f, 0.25f, 0.32f);
            }
        }

        public void Setup(PlantData plantData, bool locked)
        {
            currentPlantData = plantData;
            isLocked         = locked;

            // 이름
            if (nameText != null)
                nameText.text = locked ? "???" : plantData.plantName;

            // 아이콘 색상 및 스프라이트
            if (iconImage != null)
            {
                Sprite iconSp = plantData.iconSprite;
                if (iconSp == null)
                {
                    int num = plantData.plantID - 10000;
                    if (num > 0 && num <= 99)
                    {
                        iconSp = Resources.Load<Sprite>($"Plants/Plant_Icon_{num:D2}");
                    }
                }

                if (iconSp != null)
                {
                    iconImage.sprite = iconSp;
                    iconImage.color  = locked ? new Color(0.35f, 0.35f, 0.35f) : Color.white;
                }
                else
                {
                    iconImage.sprite = null;
                    iconImage.color  = locked ? new Color(0.35f, 0.35f, 0.35f) : new Color(0.45f, 0.75f, 0.30f);
                }
            }

            // 보유량
            if (stockText != null)
            {
                if (locked)
                {
                    stockText.text = $"해금 {plantData.requiredGoldToUnlock:N0}G";
                }
                else
                {
                    int count = InventoryManager.Instance?.GetCount(plantData.plantID) ?? 0;
                    stockText.text = $"보유량: {count}";
                }
            }

            // 잠금 오버레이
            if (lockOverlay != null)
                lockOverlay.gameObject.SetActive(locked);

            if (reqLevelText != null)
            {
                reqLevelText.gameObject.SetActive(locked);
                if (locked)
                    reqLevelText.text = $"연구소 Lv.{plantData.requiredHarvestLevel} 필요";
            }

            // 버튼 색상 및 상태 설정
            if (selectButton != null)
            {
                var img = selectButton.GetComponent<Image>();
                var btnText = selectButton.GetComponentInChildren<TextMeshProUGUI>();

                if (!locked)
                {
                    if (img != null) img.color = new Color(0.40f, 0.72f, 0.28f);
                    if (btnText != null) btnText.text = "선택";
                    selectButton.interactable = true;
                }
                else
                {
                    bool canUnlock = LabManager.Instance != null && LabManager.Instance.IsPlantUnlockable(plantData.requiredHarvestLevel);
                    if (canUnlock)
                    {
                        if (img != null) img.color = new Color(0.85f, 0.55f, 0.15f);
                        if (btnText != null) btnText.text = "해금";
                        selectButton.interactable = true;
                    }
                    else
                    {
                        if (img != null) img.color = new Color(0.40f, 0.40f, 0.40f);
                        if (btnText != null) btnText.text = "잠금";
                        selectButton.interactable = false;
                    }
                }
            }
        }

        public void RefreshStock()
        {
            if (isLocked || currentPlantData == null) return;
            if (stockText == null) return;
            int count = InventoryManager.Instance?.GetCount(currentPlantData.plantID) ?? 0;
            stockText.text = $"보유량: {count}";
        }

        void HandleClick()
        {
            OnSlotClicked?.Invoke(currentPlantData);
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WeedHoldings
{
    public class UIAvPlantListSlot : MonoBehaviour
    {
        public Image plantIcon;
        public TextMeshProUGUI plantText;
        public Button selectButton;
        public Image background;

        PlantData currentPlantData;
        bool isLocked;

        Color defaultBg = new Color(0.15f, 0.15f, 0.2f, 0.9f);
        Color selectedBg = new Color(0.2f, 0.8f, 0.2f, 0.6f);
        Color lockedBg = new Color(0.25f, 0.25f, 0.25f, 0.9f);

        const string LockedDescription = "식물 해금 시 설명을 확인할 수 있습니다";

        void Awake()
        {
            ResolveReferences();
        }

        /// <summary>이 컴포넌트는 UIAvPlantListPanel이 런타임에 AddComponent로 붙이는 경우가 많아
        /// 인스펙터에서 미리 연결해둔 값이 없다. 그래서 비어 있으면 자식 이름으로 스스로 찾는다.
        /// (안 그러면 plantText가 계속 null이라 Setup()이 아무 것도 안 바꾸고, 씬에 예시로 박아둔
        /// "벽실낮잠꽃\n보유: 0개" 같은 더미 텍스트가 실제 데이터와 상관없이 그대로 남아 보였다.)</summary>
        void ResolveReferences()
        {
            if (plantIcon == null) plantIcon = transform.Find("Plant_Icon")?.GetComponent<Image>();
            if (plantText == null) plantText = transform.Find("Plant_Text")?.GetComponent<TextMeshProUGUI>();
            if (selectButton == null) selectButton = transform.Find("Select_Button")?.GetComponent<Button>();
            if (background == null) background = GetComponent<Image>();
        }

        public void Setup(PlantData plantData, bool locked = false)
        {
            ResolveReferences();
            currentPlantData = plantData;
            isLocked = locked;

            if (background != null)
                background.color = locked ? lockedBg : defaultBg;

            if (plantIcon != null)
            {
                if (plantData.iconSprite != null)
                {
                    plantIcon.sprite = plantData.iconSprite;
                    plantIcon.preserveAspect = true; // 비율 무시하고 박스에 맞춰 늘어나 찌그러지는 문제 방지
                    // 잠긴 식물은 검은 실루엣으로 표기
                    plantIcon.color = locked ? new Color(0.03f, 0.03f, 0.03f) : Color.white;
                }
                else
                {
                    plantIcon.color = locked ? new Color(0.03f, 0.03f, 0.03f) : new Color(0.5f, 0.7f, 0.3f);
                }
            }

            if (plantText != null)
            {
                plantText.text = locked
                    ? $"???\n{LockedDescription}"
                    : $"{plantData.plantName}\n보유: {(DataManager.Instance != null ? DataManager.Instance.GetPlantCount(plantData.plantID) : 0)}개";
                plantText.color = locked ? Color.gray : Color.white;
            }

            if (selectButton != null)
                selectButton.interactable = !locked;
        }

        public void SetHighlight(bool selected)
        {
            if (background != null && !isLocked)
                background.color = selected ? selectedBg : defaultBg;
        }
    }
}
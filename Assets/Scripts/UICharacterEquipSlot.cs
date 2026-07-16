using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WeedHoldings
{
    /// <summary>
    /// 캐릭터 패널의 농장/공장/무역소 장착 칸 하나(Charactor_01~03, 각 열 3개씩 총 9개).
    /// 부모 오브젝트 이름(Farm/Factory/Sell)과 자기 이름 끝자리 번호로 담당 분야/슬롯 번호를 스스로 정한다.
    /// Char_Icon을 누르면 공용 Char_equip 팝업을 열고, 팝업에서 장착이 확정되면 Refresh()로 다시 그린다.
    /// </summary>
    public class UICharacterEquipSlot : MonoBehaviour
    {
        EquipCategory category;
        int slotIndex;

        Button iconButton;
        Image iconImage;
        Image bgImage;
        TextMeshProUGUI infoText;

        UICharacterEquipPopup popup;

        void Awake()
        {
            var parentName = transform.parent != null ? transform.parent.name : "";
            category = parentName switch
            {
                "Farm" => EquipCategory.Farm,
                "Factory" => EquipCategory.Factory,
                "Sell" => EquipCategory.Sell,
                _ => EquipCategory.Farm,
            };

            var match = Regex.Match(name, @"(\d+)$");
            slotIndex = match.Success ? int.Parse(match.Value) - 1 : 0;

            var iconTransform = transform.Find("Char_Icon");
            iconButton = iconTransform?.GetComponent<Button>();
            if (iconButton == null && iconTransform != null) iconButton = iconTransform.gameObject.AddComponent<Button>();
            iconImage = iconTransform?.GetComponent<Image>();
            if (iconImage != null) iconImage.preserveAspect = true;
            bgImage = CharacterCardVisuals.MoveBackgroundBehindIcon(iconTransform)?.GetComponent<Image>();

            infoText = transform.Find("Info")?.GetComponent<TextMeshProUGUI>();

            UIScrollListFactory.ApplyNotoSansFont(transform);
            if (infoText != null)
            {
                infoText.enableAutoSizing = true;
                infoText.fontSizeMin = 7f;
                infoText.fontSizeMax = 14f;
                infoText.enableWordWrapping = true;
                infoText.overflowMode = TMPro.TextOverflowModes.Truncate;
            }

            if (iconButton != null)
            {
                if (iconButton.targetGraphic == null) iconButton.targetGraphic = iconImage;
                iconButton.interactable = true;
                iconButton.onClick.RemoveListener(OnIconClicked);
                iconButton.onClick.AddListener(OnIconClicked);
            }
        }

        void Start()
        {
            Refresh();
        }

        void OnIconClicked()
        {
            if (popup == null)
                popup = Object.FindFirstObjectByType<UICharacterEquipPopup>(FindObjectsInactive.Include);

            popup?.Open(category, slotIndex, Refresh);
        }

        public void Refresh()
        {
            if (CharacterEquipManager.Instance == null || DataManager.Instance == null) return;

            int characterId = CharacterEquipManager.Instance.GetEquippedCharacterID(category, slotIndex);
            var character = characterId != 0 ? DataManager.Instance.GetCharacterByID(characterId) : null;

            if (character == null)
            {
                // Button의 targetGraphic이 이 Image라서 enabled=false로 끄면 클릭 자체가 안 먹힌다.
                // 그래서 비활성화(disable) 대신 스프라이트를 비우고 살짝 흐리게만 해서 "미장착"을 표시한다.
                if (iconImage != null)
                {
                    iconImage.enabled = true;
                    iconImage.sprite = null;
                    iconImage.color = new Color(1f, 1f, 1f, 0.15f);
                }
                // 포션/식물 인벤토리 슬롯과 동일하게: 배경 이미지 자체를 "빈 슬롯"으로 바꿔서 보여준다
                // (별도 오버레이를 얹는 게 아니라 같은 자리의 스프라이트를 교체).
                if (bgImage != null)
                {
                    bgImage.enabled = true;
                    bgImage.sprite = CharacterEquipManager.GetEmptySlotIcon();
                    bgImage.color = Color.white;
                }
                if (infoText != null) infoText.text = "미장착\n칸을 눌러 캐릭터를 장착하세요";
                return;
            }

            if (iconImage != null)
            {
                iconImage.enabled = true;
                iconImage.sprite = character.characterIcon;
                iconImage.color = Color.white;
            }
            if (bgImage != null)
            {
                var frame = CharacterEquipManager.GetFrame(character.grade);
                if (frame != null)
                {
                    bgImage.enabled = true;
                    bgImage.sprite = frame;
                    bgImage.color = Color.white;
                }
            }
            if (infoText != null)
                infoText.text = $"{character.grade}\n{CharacterEquipManager.BuildEffectDescription(character, category)}";
        }
    }
}

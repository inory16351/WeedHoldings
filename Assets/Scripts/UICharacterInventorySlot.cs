using System;
using UnityEngine;
using UnityEngine.UI;

namespace WeedHoldings
{
    /// <summary>
    /// 캐릭터 장착 팝업(Char_equip) 안 Inventory 그리드에 늘어서는 캐릭터 슬롯 하나(Char_Slot).
    /// 원본 하나를 복제해서 보유 캐릭터 수만큼 만들어 쓴다. 클릭하면 팝업의 Char_Info에 상세 정보를 띄운다.
    /// Card_Bg는 등급별 카드 프레임(_card2, 투명 배경 크롭본)으로 채운다. Rating(1) 자리는 프레임 안에
    /// 등급 배지가 이미 그려져 있어 별도 사용하지 않고 숨긴다.
    /// </summary>
    public class UICharacterInventorySlot : MonoBehaviour
    {
        Button button;
        Image cardBg;
        Image charIcon;
        Image ratingImage;

        CharacterData character;
        Action<CharacterData> onSelected;

        void Awake()
        {
            cardBg = transform.Find("Card_Bg")?.GetComponent<Image>();
            charIcon = transform.Find("Char_Icon")?.GetComponent<Image>();
            if (charIcon != null) charIcon.preserveAspect = true;
            ratingImage = transform.Find("Rating (1)")?.GetComponent<Image>();

            if (cardBg == null || charIcon == null)
                Debug.LogError($"[CharEquip] {name}에서 Card_Bg/Char_Icon을 못 찾음: cardBg={(cardBg != null)}, charIcon={(charIcon != null)}");

            // Char_Slot 원본에는 Image만 있고 Button이 없어서 클릭이 전혀 감지되지 않았다 - 직접 추가한다.
            button = GetComponent<Button>();
            if (button == null) button = gameObject.AddComponent<Button>();

            var ownImage = GetComponent<Image>();
            if (button.targetGraphic == null) button.targetGraphic = ownImage != null ? ownImage : cardBg;
            button.interactable = true;

            button.onClick.RemoveListener(InvokeSelected);
            button.onClick.AddListener(InvokeSelected);
        }

        void InvokeSelected() => onSelected?.Invoke(character);

        public void Setup(CharacterData data, Action<CharacterData> onSelectedCallback)
        {
            character = data;
            onSelected = onSelectedCallback;

            // Awake()가 어떤 이유로든(오브젝트가 비활성 상태에서 복제된 직후라 자식 탐색이 아직 불안정했던
            // 경우 등) Card_Bg/Char_Icon을 못 찾았을 수 있으니, 여기서 다시 한번 확실하게 찾아 스스로 복구한다.
            if (cardBg == null) cardBg = transform.Find("Card_Bg")?.GetComponent<Image>();
            if (charIcon == null)
            {
                charIcon = transform.Find("Char_Icon")?.GetComponent<Image>();
                if (charIcon != null) charIcon.preserveAspect = true;
            }
            if (ratingImage == null) ratingImage = transform.Find("Rating (1)")?.GetComponent<Image>();

            var frame = CharacterEquipManager.GetFrame(data.grade);

            bool cardBgFoundOnThisClone = cardBg != null;
            bool charIconFoundOnThisClone = charIcon != null;

            if (cardBg != null)
            {
                cardBg.enabled = true;
                if (frame != null) cardBg.sprite = frame;
            }
            if (charIcon != null)
            {
                charIcon.enabled = data.characterIcon != null;
                charIcon.sprite = data.characterIcon;
            }

            // 각 항목을 따로따로 명확히 찍는다 - sprite?.name은 sprite가 null이든 이름이 빈 문자열이든
            // 똑같이 빈 칸으로 나와서 구분이 안 됐다(지난번 로그가 헷갈렸던 이유).
            Debug.Log(
                $"[CharEquip] {data.characterName}({data.grade}) 세팅: " +
                $"Card_Bg찾음={cardBgFoundOnThisClone}, Char_Icon찾음={charIconFoundOnThisClone}, " +
                $"frame데이터={(frame != null)}, icon데이터={(data.characterIcon != null)}, " +
                $"실제할당후 cardBg.sprite={(cardBg != null && cardBg.sprite != null)}, charIcon.sprite={(charIcon != null && charIcon.sprite != null)}, " +
                $"resourceName='{data.resourceName}'");

            // 등급 배지 이미지는 아직 에셋이 없다 - 있으면 붙이고, 없으면 그냥 빈 상태로 둔다(로직만 준비).
            if (ratingImage != null)
            {
                var badge = CharacterEquipManager.GetGradeBadge(data.grade);
                ratingImage.enabled = badge != null;
                if (badge != null) ratingImage.sprite = badge;
            }
        }
    }
}

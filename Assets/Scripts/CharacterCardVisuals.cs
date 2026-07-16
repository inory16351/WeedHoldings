using UnityEngine;
using UnityEngine.UI;

namespace WeedHoldings
{
    /// <summary>
    /// 아이콘 옆에 배경 이미지를 둔 슬롯들(캐릭터 장착 칸, 제조 트랙, 보약 상세, 무역소 인벤토리 등)에서
    /// 공용으로 쓰는 헬퍼. 씬마다 배경 오브젝트가 놓인 위치가 달라서(아이콘의 자식인 곳도, 아이콘과
    /// 형제인 곳도 있다) 두 경우 다 처리한다.
    /// </summary>
    public static class CharacterCardVisuals
    {
        /// <summary>
        /// Char_Bg를 찾아 Char_Icon보다 먼저 그려지도록(뒤에 깔리도록) 정리한다. Unity는 자식을 항상
        /// 부모보다 위(앞)에 그리고, 형제는 리스트 앞쪽일수록 먼저(뒤에) 그리므로 - Char_Bg가 Char_Icon의
        /// 자식으로 남아있으면 배경에 어떤 스프라이트를 넣든 캐릭터 아이콘을 완전히 가려버린다.
        /// </summary>
        public static Transform MoveBackgroundBehindIcon(Transform iconTransform)
        {
            if (iconTransform == null) return null;

            // 케이스 1: Char_Bg가 Char_Icon의 자식으로 들어있는 경우 - 부모 쪽 형제로 꺼내온다.
            var bg = iconTransform.Find("Char_Bg");
            if (bg != null)
            {
                var bgRect = bg as RectTransform;
                var iconRect = iconTransform as RectTransform;

                int iconIndex = iconTransform.GetSiblingIndex();
                bg.SetParent(iconTransform.parent, false);

                if (bgRect != null && iconRect != null)
                {
                    bgRect.anchorMin = iconRect.anchorMin;
                    bgRect.anchorMax = iconRect.anchorMax;
                    bgRect.pivot = iconRect.pivot;
                    bgRect.anchoredPosition = iconRect.anchoredPosition;
                    bgRect.sizeDelta = iconRect.sizeDelta;
                }

                bg.SetSiblingIndex(iconIndex);
                return bg;
            }

            // 케이스 2: Char_Bg가 이미 Char_Icon과 같은 부모 아래 형제로 있는 경우 - 순서만 바로잡는다.
            if (iconTransform.parent != null)
            {
                bg = iconTransform.parent.Find("Char_Bg");
                if (bg != null)
                {
                    int iconIndex = iconTransform.GetSiblingIndex();
                    if (bg.GetSiblingIndex() > iconIndex)
                        bg.SetSiblingIndex(iconIndex);
                    return bg;
                }
            }

            return null;
        }

        /// <summary>
        /// 아이콘 옆의 "Slot_Icon"(빈 슬롯 배경, Resources/Cards/slot)을 아이콘보다 뒤에서 그려지도록
        /// 정리하고, 스프라이트가 비어 있으면 채운다. 스프라이트가 없는 채로 남아있으면 기본 흰색
        /// Image로 렌더링되면서(자식이라 아이콘보다 위에 그려져) 실제 아이템 아이콘을 가려버린다.
        /// </summary>
        public static void FixupSlotIconBackground(Transform iconTransform)
        {
            if (iconTransform == null) return;

            var slot = iconTransform.Find("Slot_Icon");
            bool wasChild = slot != null;
            if (slot == null && iconTransform.parent != null)
                slot = iconTransform.parent.Find("Slot_Icon");
            if (slot == null) return;

            var slotImage = slot.GetComponent<Image>();
            if (slotImage == null) slotImage = slot.gameObject.AddComponent<Image>();

            int iconIndex = iconTransform.GetSiblingIndex();
            if (wasChild)
            {
                var slotRect = slot as RectTransform;
                var iconRect = iconTransform as RectTransform;
                slot.SetParent(iconTransform.parent, false);

                if (slotRect != null && iconRect != null)
                {
                    slotRect.anchorMin = iconRect.anchorMin;
                    slotRect.anchorMax = iconRect.anchorMax;
                    slotRect.pivot = iconRect.pivot;
                    slotRect.anchoredPosition = iconRect.anchoredPosition;
                    slotRect.sizeDelta = iconRect.sizeDelta;
                }
                slot.SetSiblingIndex(iconIndex);
            }
            else if (slot.GetSiblingIndex() > iconIndex)
            {
                slot.SetSiblingIndex(iconIndex);
            }

            if (slotImage.sprite == null)
                slotImage.sprite = CharacterEquipManager.GetEmptySlotIcon();
            slotImage.preserveAspect = true;
            slotImage.enabled = true;
        }
    }
}

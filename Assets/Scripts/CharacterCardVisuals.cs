using UnityEngine;
using UnityEngine.UI;

namespace WeedHoldings
{
    /// <summary>
    /// 캐릭터 아이콘 옆에 배경용 Char_Bg를 둔 슬롯들(Charactor_XX, Char_Slot, Char_Info)에서 공용으로 쓰는 헬퍼.
    /// 씬마다 Char_Bg가 놓인 위치가 달라서(어떤 곳은 Char_Icon의 자식, 어떤 곳은 Char_Icon과 형제) 두 경우 다 처리한다.
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
        /// Char_Icon과 같은 부모 아래에 "Slot_Icon"(빈 슬롯 배경, Resources/Cards/slot)을 준비해서 항상
        /// Char_Icon 뒤에 깔리도록 한다. 캐릭터 아이콘과 달리 이건 장착/선택 여부와 무관하게 계속 켜져
        /// 있는 배경 역할이라 - 없으면 새로 만들고, 있으면 그대로 재사용해서 위치/순서만 맞춘다.
        /// </summary>
        public static Image EnsureSlotIconBackground(Transform iconTransform)
        {
            if (iconTransform == null || iconTransform.parent == null) return null;

            var slotTransform = iconTransform.parent.Find("Slot_Icon");
            Image image;

            if (slotTransform == null)
            {
                var go = new GameObject("Slot_Icon", typeof(RectTransform));
                go.layer = iconTransform.gameObject.layer;
                go.transform.SetParent(iconTransform.parent, false);
                slotTransform = go.transform;
                image = go.AddComponent<Image>();

                if (slotTransform is RectTransform slotRect && iconTransform is RectTransform iconRect)
                {
                    slotRect.anchorMin = iconRect.anchorMin;
                    slotRect.anchorMax = iconRect.anchorMax;
                    slotRect.pivot = iconRect.pivot;
                    slotRect.anchoredPosition = iconRect.anchoredPosition;
                    slotRect.sizeDelta = iconRect.sizeDelta;
                }
            }
            else
            {
                image = slotTransform.GetComponent<Image>();
                if (image == null) image = slotTransform.gameObject.AddComponent<Image>();
            }

            // 캐릭터 아이콘 뒤에서 그려지도록(배경 역할) 형제 순서를 맞춘다.
            int iconIndex = iconTransform.GetSiblingIndex();
            if (slotTransform.GetSiblingIndex() > iconIndex)
                slotTransform.SetSiblingIndex(iconIndex);

            image.sprite = CharacterEquipManager.GetEmptySlotIcon();
            image.preserveAspect = true;
            image.enabled = true;

            return image;
        }
    }
}

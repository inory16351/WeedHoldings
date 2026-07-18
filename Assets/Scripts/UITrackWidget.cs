using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WeedHoldings
{
    /// <summary>
    /// Track_01~06 각각에 부착. PotionCraftManager의 트랙 상태(대기/제조중/완료)를 표시하고,
    /// 완료 상태일 때 Crafting 버튼을 눌러 결과물을 인벤토리로 수확한다.
    /// </summary>
    public class UITrackWidget : MonoBehaviour
    {
        public int trackIndex = -1; // 부트스트랩에서 Track_XX 이름으로부터 설정

        Image potionIcon;
        Image potionCrafting;
        TMP_Text potionNameText;
        TMP_Text timeText;
        Button craftingButton;
        TMP_Text craftingButtonText;
        Image craftingButtonImage;
        Image lockIcon;

        static Sprite cachedLockedSprite, cachedProgressSprite, cachedSuccessSprite, cachedFailSprite2;

        /// <summary>제조 트랙 버튼(Crafting)은 상태(잠김/제작 중/완료/실패)마다 문구가 바뀌는데,
        /// 예전에는 그때그때 TMP 텍스트를 갈아 끼웠다. 다른 버튼들처럼 폰트가 미리 구워진 이미지로
        /// 바꾸면서 텍스트 대신 상태별 이미지를 스와핑하는 방식으로 통일한다.</summary>
        static Sprite ResolveCraftSprite(string state)
        {
            return state switch
            {
                "Locked" => cachedLockedSprite ??= Resources.Load<Sprite>("UI/Craft_Locked"),
                "Progress" => cachedProgressSprite ??= Resources.Load<Sprite>("UI/Craft_Progress"),
                "Success" => cachedSuccessSprite ??= Resources.Load<Sprite>("UI/Craft_Success"),
                "Fail" => cachedFailSprite2 ??= Resources.Load<Sprite>("UI/Craft_Fail"),
                _ => null,
            };
        }

        static Sprite cachedFailSprite;

        /// <summary>Resources/Potions/Potion_Fail.png를 실패 아이콘으로 사용한다.</summary>
        static Sprite ResolveFailSprite()
        {
            if (cachedFailSprite == null)
                cachedFailSprite = Resources.Load<Sprite>("Potions/Potion_Fail");
            return cachedFailSprite;
        }

        static Sprite cachedLockSprite;

        /// <summary>Resources/UI/Rock.png를 잠금 아이콘으로 사용한다.</summary>
        static Sprite ResolveLockSprite()
        {
            if (cachedLockSprite == null)
                cachedLockSprite = Resources.Load<Sprite>("UI/Rock");
            return cachedLockSprite;
        }

        /// <summary>잠긴 트랙에 자물쇠 아이콘을 씌우기 위해 첫 호출 시 자식 Image를 하나 만들어 캐싱한다.</summary>
        Image EnsureLockIcon()
        {
            if (lockIcon != null) return lockIcon;

            var go = new GameObject("Lock_Icon", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            go.transform.SetAsLastSibling();

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(48f, 48f);
            rect.anchoredPosition = Vector2.zero;

            lockIcon = go.AddComponent<Image>();
            lockIcon.sprite = ResolveLockSprite();
            lockIcon.preserveAspect = true;
            lockIcon.raycastTarget = false;
            return lockIcon;
        }

        void Awake()
        {
            var potionIconTransform = transform.Find("Potion_Icon");
            potionIcon = potionIconTransform?.GetComponent<Image>();
            if (potionIcon != null) potionIcon.preserveAspect = true;
            CharacterCardVisuals.FixupSlotIconBackground(potionIconTransform);

            potionCrafting = transform.Find("Potion_Crafting")?.GetComponent<Image>();
            potionNameText = transform.Find("Potion_Name")?.GetComponent<TMP_Text>();
            timeText = transform.Find("Time")?.GetComponent<TMP_Text>();

            var craftingObj = transform.Find("Crafting");
            craftingButton = craftingObj?.GetComponent<Button>();
            craftingButtonText = craftingObj?.GetComponentInChildren<TMP_Text>();
            craftingButtonImage = craftingObj?.GetComponent<Image>();
            if (craftingButtonText != null) craftingButtonText.enabled = false;
            if (craftingButtonImage != null) craftingButtonImage.preserveAspect = true;

            // BG가 맨 마지막 자식(=가장 위에 렌더링)으로 되어 있어 Potion_Crafting을 가리는 문제 방지.
            var bg = transform.Find("BG");
            if (bg != null) bg.SetAsFirstSibling();

            if (craftingButton != null)
                craftingButton.onClick.AddListener(OnCraftingButtonClicked);

            UIScrollListFactory.ApplyNotoSansFont(this);
            ConfigureAutoSize(potionNameText, 10f, 16f);
            ConfigureAutoSize(timeText, 9f, 14f);
        }

        static void ConfigureAutoSize(TMP_Text text, float min, float max)
        {
            if (text == null) return;
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
        }

        void Update()
        {
            RefreshDisplay();
        }

        void RefreshDisplay()
        {
            bool unlocked = FactoryUpgradeManager.Instance == null || FactoryUpgradeManager.Instance.IsTrackUnlocked(trackIndex);
            var track = PotionCraftManager.Instance != null ? PotionCraftManager.Instance.GetTrack(trackIndex) : null;

            if (!unlocked)
            {
                SetIconsVisible(false);
                EnsureLockIcon().enabled = true;
                if (potionNameText != null) potionNameText.text = "잠김";
                if (timeText != null) timeText.text = "";
                if (craftingButton != null) craftingButton.interactable = false;
                SetCraftingButtonSprite("Locked");
                return;
            }
            if (lockIcon != null) lockIcon.enabled = false;

            if (track == null || track.state == TrackState.Idle)
            {
                SetIconsVisible(false);
                if (potionNameText != null) potionNameText.text = "대기 중";
                if (timeText != null) timeText.text = "";
                if (craftingButton != null) craftingButton.interactable = false;
                SetCraftingButtonSprite(null); // 대기 중에는 누를 것이 없으므로 버튼 이미지도 비운다.
                return;
            }

            if (track.state == TrackState.Failed)
            {
                SetIconsVisible(true);
                var failSprite = ResolveFailSprite();
                if (potionIcon != null) potionIcon.sprite = failSprite;
                if (potionCrafting != null) potionCrafting.sprite = failSprite;
                if (potionNameText != null) potionNameText.text = track.potion != null ? track.potion.potionName : "";
                if (timeText != null) timeText.text = "제작 실패....";
                if (craftingButton != null) craftingButton.interactable = true;
                SetCraftingButtonSprite("Fail");
                return;
            }

            SetIconsVisible(true);
            if (potionIcon != null) potionIcon.sprite = track.potion.potionIcon;
            if (potionCrafting != null) potionCrafting.sprite = track.potion.potionIcon;
            if (potionNameText != null) potionNameText.text = track.potion.potionName;

            if (track.state == TrackState.Crafting)
            {
                int minutes = Mathf.FloorToInt(track.remainingTime / 60f);
                int seconds = Mathf.FloorToInt(track.remainingTime % 60f);
                if (timeText != null) timeText.text = $"제작 중... {minutes:D2}:{seconds:D2}";
                if (craftingButton != null) craftingButton.interactable = false;
                SetCraftingButtonSprite("Progress");
            }
            else if (track.state == TrackState.ReadyToCollect)
            {
                if (timeText != null) timeText.text = "완성! 00:00";
                if (craftingButton != null) craftingButton.interactable = true;
                SetCraftingButtonSprite("Success");
            }
        }

        void SetIconsVisible(bool visible)
        {
            if (potionIcon != null) potionIcon.enabled = visible;
            if (potionCrafting != null) potionCrafting.enabled = visible;
        }

        void SetCraftingButtonSprite(string state)
        {
            if (craftingButtonImage == null) return;
            var sprite = state != null ? ResolveCraftSprite(state) : null;
            craftingButtonImage.sprite = sprite;
            craftingButtonImage.enabled = sprite != null;
        }

        void OnCraftingButtonClicked()
        {
            PotionCraftManager.Instance?.CollectTrack(trackIndex);
        }
    }
}

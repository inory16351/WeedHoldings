using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WeedHoldings
{
    /// <summary>
    /// 연구소의 "Potion_Unlock_List"에 부착. UIPlantUnlockPanel과 완전히 동일한 로직이며
    /// 데이터만 포션(PotionData/DataManager 포션 API/FactoryUpgradeManager)으로 바뀐다.
    /// 씬에 미리 만들어진 Potion_Panel_01 슬롯(Image/Potion_Text/Req_Factory_Level/Unlock)을
    /// 그대로 사용해 스크롤 리스트로 감싸고, 전체 포션(potionID 오름차순)을 슬롯 수에 맞춰 순서대로 배치한다.
    /// 슬롯이 포션 수보다 적으면 마지막 슬롯을 복제해 자동으로 채운다.
    /// </summary>
    public class UIPotionUnlockPanel : MonoBehaviour
    {
        const string SlotBaseName = "Potion_Panel_01";

        RectTransform contentRoot;
        readonly List<UIPotionUnlockSlot> slotList = new List<UIPotionUnlockSlot>();

        void Awake()
        {
            CreateScrollView();
        }

        void OnEnable()
        {
            if (FactoryUpgradeManager.Instance != null)
                FactoryUpgradeManager.Instance.OnLevelChanged += RefreshAll;
            EnsureSlotCountMatchesPotions();
            RefreshAll();
        }

        void OnDisable()
        {
            if (FactoryUpgradeManager.Instance != null)
                FactoryUpgradeManager.Instance.OnLevelChanged -= RefreshAll;
        }

        /// <summary>
        /// 씬에 미리 만들어진 슬롯 수보다 포션 데이터가 많으면 마지막 슬롯을 복제해 채운다.
        /// </summary>
        void EnsureSlotCountMatchesPotions()
        {
            if (DataManager.Instance == null || contentRoot == null || contentRoot.childCount == 0) return;

            int neededCount = DataManager.Instance.GetAllPotions().Count;
            if (slotList.Count >= neededCount) return;

            var template = contentRoot.GetChild(contentRoot.childCount - 1);
            int toAdd = neededCount - slotList.Count;
            for (int i = 0; i < toAdd; i++)
            {
                int newSlotIndex = slotList.Count + i;
                var clone = Instantiate(template.gameObject, contentRoot);
                clone.name = $"{SlotBaseName} ({newSlotIndex})";
            }

            BindSlots();
        }

        const float HeaderHeight = 32f;

        void CreateScrollView()
        {
            CreateHeaderLabel();

            var scrollRect = UIScrollListFactory.Create(transform, out contentRoot, 12f);
            var scrollRt = scrollRect.GetComponent<RectTransform>();
            // 헤더 라벨이 차지하는 만큼 스크롤 영역 상단을 안쪽으로 당긴다.
            scrollRt.offsetMax = new Vector2(scrollRt.offsetMax.x, -HeaderHeight);

            MoveExistingSlotsToContent();
            BindSlots();
        }

        void CreateHeaderLabel()
        {
            var headerGO = new GameObject("Header_Text", typeof(RectTransform));
            headerGO.transform.SetParent(transform, false);
            headerGO.layer = 5;

            var rt = headerGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0, HeaderHeight);

            var text = headerGO.AddComponent<TextMeshProUGUI>();
            text.text = "보약 연구";
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 20;
            text.color = Color.white;

            var font = ResolveNotoSansFont();
            if (font != null) text.font = font;
        }

        void MoveExistingSlotsToContent()
        {
            // Potion_Unlock_List 바로 아래 미리 만들어진 "Potion_Panel_01" 계열 오브젝트를 전부 Content로 옮긴다.
            var existing = new List<Transform>();
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith(SlotBaseName))
                    existing.Add(child);
            }
            foreach (var panelTransform in existing)
            {
                if (panelTransform.parent != contentRoot)
                    UIScrollListFactory.MoveIntoContent(panelTransform, contentRoot);
            }
        }

        void BindSlots()
        {
            slotList.Clear();

            var panels = new List<Transform>();
            foreach (Transform child in contentRoot)
            {
                if (child.name.StartsWith(SlotBaseName))
                    panels.Add(child);
            }
            panels.Sort((a, b) => ExtractIndex(a.name).CompareTo(ExtractIndex(b.name)));

            foreach (var panel in panels)
            {
                var slot = panel.GetComponent<UIPotionUnlockSlot>();
                if (slot == null) slot = panel.gameObject.AddComponent<UIPotionUnlockSlot>();

                slot.icon = panel.Find("Image")?.GetComponent<Image>();
                if (slot.icon != null)
                {
                    slot.icon.preserveAspect = true; // 아이콘 원본 비율 유지 (찌그러짐 방지)
                    slot.icon.type = Image.Type.Simple;
                    slot.icon.rectTransform.sizeDelta = new Vector2(118f, 118f);
                }
                slot.nameText = panel.Find("Potion_Text")?.GetComponent<TMP_Text>();
                slot.infoText = panel.Find("Req_Factory_Level")?.GetComponent<TMP_Text>();
                slot.unlockButton = panel.Find("Unlock")?.GetComponent<Button>();
                slot.unlockCostText = panel.Find("Unlock/Text (TMP)")?.GetComponent<TMP_Text>();

                ApplyNotoSansFont(slot);

                if (slot.unlockButton != null)
                {
                    slot.unlockButton.onClick.RemoveAllListeners();
                    var capturedSlot = slot;
                    slot.unlockButton.onClick.AddListener(() => OnUnlockClicked(capturedSlot));
                }

                slotList.Add(slot);
            }
        }

        static int ExtractIndex(string name)
        {
            var match = Regex.Match(name, @"\((\d+)\)$");
            return match.Success ? int.Parse(match.Groups[1].Value) + 1 : 0;
        }

        static TMP_FontAsset cachedNotoSansFont;

        /// <summary>
        /// 농장 선택 리스트(Av_Plant_Panel)가 이미 에디터에서 올바른 노토산스 SDF 폰트로 설정돼 있으므로
        /// 그걸 그대로 재사용한다. TMP 기본 폰트(LiberationSans)로 새로 생성된 텍스트가 한글을 못 그리는 문제 방지.
        /// </summary>
        static TMP_FontAsset ResolveNotoSansFont()
        {
            if (cachedNotoSansFont != null) return cachedNotoSansFont;

            var farmSlot = Object.FindFirstObjectByType<UIAvPlantListSlot>(FindObjectsInactive.Include);
            if (farmSlot != null && farmSlot.plantText != null)
                cachedNotoSansFont = farmSlot.plantText.font;

            return cachedNotoSansFont;
        }

        static void ApplyNotoSansFont(UIPotionUnlockSlot slot)
        {
            var font = ResolveNotoSansFont();
            if (font == null) return;

            if (slot.nameText != null) slot.nameText.font = font;
            if (slot.infoText != null) slot.infoText.font = font;
            if (slot.unlockCostText != null) slot.unlockCostText.font = font;
        }

        void OnUnlockClicked(UIPotionUnlockSlot slot)
        {
            var potion = slot.CurrentPotion;
            if (potion == null || DataManager.Instance == null) return;
            if (DataManager.Instance.IsPotionUnlocked(potion.potionID)) return;

            int currentLevel = FactoryUpgradeManager.Instance != null ? FactoryUpgradeManager.Instance.CurrentLevel : 1;
            if (currentLevel < potion.requiredUnlockLevel) return;
            if (GoldManager.Instance == null || !GoldManager.Instance.SpendGold(potion.requiredUnlockGold)) return;

            DataManager.Instance.UnlockPotion(potion.potionID);
            RefreshAll();

            var factoryPanel = Object.FindFirstObjectByType<UIFactoryPanelController>(FindObjectsInactive.Include);
            if (factoryPanel != null) factoryPanel.RefreshPotionList();
        }

        public void RefreshAll()
        {
            if (DataManager.Instance == null) return;

            // GetAllPotions()는 potionID 오름차순이므로 0번 슬롯 = 가장 먼저 해금되는 포션(기본 무료 해금분)
            var allPotions = DataManager.Instance.GetAllPotions();

            for (int i = 0; i < slotList.Count; i++)
            {
                if (i < allPotions.Count)
                {
                    slotList[i].gameObject.SetActive(true);
                    slotList[i].Setup(allPotions[i]);
                }
                else
                {
                    slotList[i].gameObject.SetActive(false);
                }
            }

            ForceLayoutAndMaskRefresh();
        }

        /// <summary>
        /// 런타임에 슬롯을 옮기고/복제하고/내용을 바꾼 직후에는 VerticalLayoutGroup의 위치 재계산과
        /// RectMask2D의 클립 영역 갱신이 그 프레임에 아직 반영 안 된 상태일 수 있다.
        /// 즉시 강제로 재계산해서 "스크롤 내리면 영역 밖의 패널도 보이는" 현상을 방지한다.
        /// </summary>
        void ForceLayoutAndMaskRefresh()
        {
            if (contentRoot == null) return;
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
            Canvas.ForceUpdateCanvases();
        }
    }
}

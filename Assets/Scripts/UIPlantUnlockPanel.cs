using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WeedHoldings
{
    /// <summary>
    /// 연구소의 "Plant_Unlock_List"에 부착.
    /// 씬에 미리 만들어진 Plant_Panel_01 ~ Plant_Panel_01 (8) 슬롯(Image/Plant_Text/Req_Farm_Level/Unlock)을
    /// 그대로 사용해 스크롤 리스트로 감싸고, 전체 식물(plantID 오름차순, 기본 해금분 포함)을 슬롯 수에 맞춰 순서대로 배치한다.
    /// 슬롯이 식물 수보다 적으면 마지막 슬롯을 복제해 자동으로 채운다.
    /// </summary>
    public class UIPlantUnlockPanel : MonoBehaviour
    {
        const string SlotBaseName = "Plant_Panel_01";
        const int SlotCount = 9;

        RectTransform contentRoot;
        readonly List<UIPlantUnlockSlot> slotList = new List<UIPlantUnlockSlot>();

        void Awake()
        {
            CreateScrollView();
        }

        void OnEnable()
        {
            if (LabUpgradeManager.Instance != null)
                LabUpgradeManager.Instance.OnLevelChanged += RefreshAll;
            EnsureSlotCountMatchesPlants();
            RefreshAll();
        }

        /// <summary>
        /// 씬에 미리 만들어진 슬롯 수보다 식물 데이터가 많으면 마지막 슬롯을 복제해 채운다.
        /// (예: 템플릿은 9개인데 전체 식물이 10종이면 1개를 복제해서 10번 슬롯을 만든다)
        /// </summary>
        void EnsureSlotCountMatchesPlants()
        {
            if (DataManager.Instance == null || contentRoot == null || contentRoot.childCount == 0) return;

            int neededCount = DataManager.Instance.GetAllPlants().Count;
            if (slotList.Count >= neededCount) return;

            // 슬롯 이름 규칙: 0번째는 접미사 없음("Plant_Panel_01"), n번째(n>=1)는 "(n)".
            var template = contentRoot.GetChild(contentRoot.childCount - 1);
            int toAdd = neededCount - slotList.Count;
            for (int i = 0; i < toAdd; i++)
            {
                int newSlotIndex = slotList.Count + i; // 항상 1 이상이므로 접미사 형태로만 생성
                var clone = Instantiate(template.gameObject, contentRoot);
                clone.name = $"{SlotBaseName} ({newSlotIndex})";
            }

            BindSlots();
        }

        void OnDisable()
        {
            if (LabUpgradeManager.Instance != null)
                LabUpgradeManager.Instance.OnLevelChanged -= RefreshAll;
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
            text.text = "식물 연구";
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 20;
            text.color = Color.white;

            var font = ResolveNotoSansFont();
            if (font != null) text.font = font;
        }

        void MoveExistingSlotsToContent()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                string panelName = i == 0 ? SlotBaseName : $"{SlotBaseName} ({i})";
                var panelTransform = transform.Find(panelName);
                if (panelTransform != null && panelTransform.parent != contentRoot)
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
                var slot = panel.GetComponent<UIPlantUnlockSlot>();
                if (slot == null) slot = panel.gameObject.AddComponent<UIPlantUnlockSlot>();

                slot.icon = panel.Find("Image")?.GetComponent<Image>();
                if (slot.icon != null)
                {
                    slot.icon.preserveAspect = true; // 아이콘 원본 비율 유지 (찌그러짐 방지)
                    slot.icon.type = Image.Type.Simple;
                    // 원래 크기(~98x99)가 작아 보여서 슬롯 높이(~138) 안에서 안전하게 키움
                    slot.icon.rectTransform.sizeDelta = new Vector2(118f, 118f);
                }
                slot.nameText = panel.Find("Plant_Text")?.GetComponent<TMP_Text>();
                slot.infoText = panel.Find("Req_Farm_Level")?.GetComponent<TMP_Text>();
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

        static void ApplyNotoSansFont(UIPlantUnlockSlot slot)
        {
            var font = ResolveNotoSansFont();
            if (font == null) return;

            if (slot.nameText != null) slot.nameText.font = font;
            if (slot.infoText != null) slot.infoText.font = font;
            if (slot.unlockCostText != null) slot.unlockCostText.font = font;
        }

        void OnUnlockClicked(UIPlantUnlockSlot slot)
        {
            var plant = slot.CurrentPlant;
            if (plant == null || DataManager.Instance == null) return;
            if (DataManager.Instance.IsPlantUnlocked(plant.plantID)) return;

            int currentLevel = LabUpgradeManager.Instance != null ? LabUpgradeManager.Instance.CurrentLevel : 1;
            if (currentLevel < plant.requiredHarvestLevel) return;
            if (GoldManager.Instance == null || !GoldManager.Instance.SpendGold(plant.requiredGoldToUnlock)) return;

            DataManager.Instance.UnlockPlant(plant.plantID);
            RefreshAll();

            var farmList = Object.FindFirstObjectByType<UIAvPlantListPanel>(FindObjectsInactive.Include);
            if (farmList != null) farmList.RefreshAll();
        }

        public void RefreshAll()
        {
            if (DataManager.Instance == null) return;

            // GetAllPlants()는 plantID 오름차순이므로 0번 슬롯 = 가장 먼저 해금되는 식물(기본 무료 해금 식물)
            var allPlants = DataManager.Instance.GetAllPlants();

            for (int i = 0; i < slotList.Count; i++)
            {
                if (i < allPlants.Count)
                {
                    slotList[i].gameObject.SetActive(true);
                    slotList[i].Setup(allPlants[i]);
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

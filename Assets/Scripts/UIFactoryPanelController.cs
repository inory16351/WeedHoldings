using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WeedHoldings
{
    /// <summary>
    /// Factory_Panel(공장 탭 좌측)에 부착. 보약 목록(해금/실루엣), 선택 상세(이미지/이름/재료/시간/골드),
    /// 보유 식물 인벤토리(가로 스크롤), 제조 버튼까지 이 화면의 UI 로직을 전부 담당한다.
    /// 실제 트랙/타이머/룰렛 로직은 PotionCraftManager가 담당하고 여기서는 호출만 한다.
    /// </summary>
    public class UIFactoryPanelController : MonoBehaviour
    {
        const string SlotBaseName = "Potion_Panel_01";

        RectTransform potionListContent;
        readonly List<UIPotionListSlot> potionSlots = new List<UIPotionListSlot>();
        int selectedIndex = -1;
        PotionData selectedPotion;

        // Resource_Slot_01~03 템플릿 (Plant_Icon + Plant_Amount)
        Transform[] resourceSlotRoots;
        Image[] resourceSlotIcons;
        TMP_Text[] resourceSlotAmounts;

        Image selectedPotionImage;
        TMP_Text selectedPotionNameText;
        TMP_Text timeText;
        TMP_Text goldText;
        TMP_Text warningText;
        TMP_Text reqPlantLabel;
        TMP_Text inventoryLabel;
        Button craftButton;

        RectTransform inventoryContent;
        Transform plantIconTemplate;
        readonly List<Transform> inventoryIconInstances = new List<Transform>();

        const string WarningHintText = "! 보약 제조 시 필요한 재료가 자동으로 사용됩니다.";

        void Awake()
        {
            CacheReferences();
            BuildPotionScrollList();
            BuildInventoryScrollRow();

            if (craftButton != null)
                craftButton.onClick.AddListener(OnCraftClicked);

            if (reqPlantLabel != null)
            {
                reqPlantLabel.text = "재료 식물";
                ConfigureAutoSize(reqPlantLabel, 10f, 16f);
            }
            if (inventoryLabel != null)
            {
                inventoryLabel.text = "보유 식물";
                ConfigureAutoSize(inventoryLabel, 10f, 16f);
            }

            ResetWarning();

            // 선택 전에도 재료 슬롯이 빈 상태로 항상 보이도록 초기화
            ClearResourceSlots();
            ClearSelectedPotionDetail();

            // 새로 만들거나 씬에서 바인딩한 텍스트가 TMP 기본 폰트(한글 깨짐)로 남지 않도록 일괄 적용
            UIScrollListFactory.ApplyNotoSansFont(this);
        }

        void OnEnable()
        {
            RefreshPotionList();
            RefreshInventoryRow();
            ResetWarning();

            if (selectedPotion != null)
                ShowSelectedPotionDetail(selectedPotion);
            else
                ClearSelectedPotionDetail();
        }

        void CacheReferences()
        {
            foreach (Transform child in transform)
            {
                switch (child.name)
                {
                    case "Warning":
                        warningText = child.GetComponent<TMP_Text>();
                        break;
                    case "Craft_Button":
                        craftButton = child.GetComponent<Button>();
                        break;
                }
            }

            // Time/Gold/Selected_Potion(아이콘+이름)은 "Potion_Info" 하위로 옮겨졌다.
            var potionInfo = transform.Find("Potion_Info");
            if (potionInfo != null)
            {
                foreach (Transform child in potionInfo)
                {
                    switch (child.name)
                    {
                        case "Time":
                            timeText = child.GetComponent<TMP_Text>();
                            break;
                        case "Gold":
                            goldText = child.GetComponent<TMP_Text>();
                            break;
                        case "Selected_Potion":
                            var img = child.GetComponent<Image>();
                            if (img != null) selectedPotionImage = img;
                            var txt = child.GetComponent<TMP_Text>();
                            if (txt != null) selectedPotionNameText = txt;
                            break;
                    }
                }
            }

            var resourceSlotParent = transform.Find("Resource_Slot");
            resourceSlotRoots = new Transform[3];
            resourceSlotIcons = new Image[3];
            resourceSlotAmounts = new TMP_Text[3];
            if (resourceSlotParent != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    var slot = resourceSlotParent.Find($"Resource_Slot_{i + 1:D2}");
                    resourceSlotRoots[i] = slot;
                    if (slot == null) continue;
                    resourceSlotIcons[i] = slot.Find("Plant_Icon")?.GetComponent<Image>();
                    resourceSlotAmounts[i] = slot.Find("Plant_Amount")?.GetComponent<TMP_Text>();
                }
                reqPlantLabel = resourceSlotParent.Find("Req_Plant")?.GetComponent<TMP_Text>();
            }

            var inventoryPlant = transform.Find("Inventory_Plant");
            if (inventoryPlant != null)
            {
                plantIconTemplate = inventoryPlant.Find("Plant_Icon");
                inventoryLabel = inventoryPlant.Find("Text (TMP)")?.GetComponent<TMP_Text>();
            }
        }

        static void ConfigureAutoSize(TMP_Text text, float min, float max)
        {
            if (text == null) return;
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
        }

        void ResetWarning()
        {
            if (warningText == null) return;
            warningText.text = WarningHintText;
            ConfigureAutoSize(warningText, 9f, 13f);
        }

        // ---------- 보약 목록 ----------
        void BuildPotionScrollList()
        {
            var listPanel = transform.Find("Potion_List_Panel");
            if (listPanel == null) return;

            var template = listPanel.Find(SlotBaseName);
            if (template == null) return;

            var scrollRect = UIScrollListFactory.Create(listPanel, out potionListContent, 6f);
            UIScrollListFactory.MoveIntoContent(template, potionListContent);

            int totalPotions = DataManager.Instance != null ? DataManager.Instance.GetAllPotions().Count : 1;
            var lastTemplate = template;
            for (int i = 1; i < totalPotions; i++)
            {
                var clone = Instantiate(lastTemplate.gameObject, potionListContent);
                clone.name = $"{SlotBaseName} ({i})";
            }

            BindPotionSlots();
        }

        void BindPotionSlots()
        {
            potionSlots.Clear();
            if (potionListContent == null) return;

            var panels = new List<Transform>();
            foreach (Transform child in potionListContent)
            {
                if (child.name.StartsWith(SlotBaseName))
                    panels.Add(child);
            }
            panels.Sort((a, b) => ExtractIndex(a.name).CompareTo(ExtractIndex(b.name)));

            for (int i = 0; i < panels.Count; i++)
            {
                var panel = panels[i];
                var slot = panel.GetComponent<UIPotionListSlot>();
                if (slot == null) slot = panel.gameObject.AddComponent<UIPotionListSlot>();

                slot.icon = panel.Find("Potion_Icon")?.GetComponent<Image>();
                if (slot.icon != null) slot.icon.preserveAspect = true;
                slot.nameText = panel.Find("Potion_Name")?.GetComponent<TMP_Text>();
                slot.goldText = panel.Find("Gold")?.GetComponent<TMP_Text>();
                slot.timeText = panel.Find("Time")?.GetComponent<TMP_Text>();

                var button = panel.GetComponent<Button>();
                if (button == null) button = panel.gameObject.AddComponent<Button>();
                var bg = panel.GetComponent<Image>();
                if (bg != null) button.targetGraphic = bg;
                slot.selectButton = button;

                int capturedIndex = i;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnPotionSlotClicked(capturedIndex));

                potionSlots.Add(slot);
            }
        }

        static int ExtractIndex(string name)
        {
            var match = Regex.Match(name, @"\((\d+)\)$");
            return match.Success ? int.Parse(match.Groups[1].Value) + 1 : 0;
        }

        public void RefreshPotionList()
        {
            if (DataManager.Instance == null) return;
            var allPotions = DataManager.Instance.GetAllPotions();

            for (int i = 0; i < potionSlots.Count; i++)
            {
                if (i < allPotions.Count)
                {
                    potionSlots[i].gameObject.SetActive(true);
                    potionSlots[i].Setup(allPotions[i], i == selectedIndex);
                }
                else
                {
                    potionSlots[i].gameObject.SetActive(false);
                }
            }
        }

        void OnPotionSlotClicked(int index)
        {
            if (index < 0 || index >= potionSlots.Count) return;
            var slot = potionSlots[index];
            if (slot.CurrentPotion == null) return;
            if (DataManager.Instance == null || !DataManager.Instance.IsPotionUnlocked(slot.CurrentPotion.potionID)) return;

            // 이미 선택된 보약을 다시 누르면 선택을 해제한다.
            if (selectedIndex == index)
            {
                selectedIndex = -1;
                selectedPotion = null;
                for (int i = 0; i < potionSlots.Count; i++)
                    potionSlots[i].SetHighlight(false);
                ClearSelectedPotionDetail();
                ClearResourceSlots();
                ResetWarning();
                return;
            }

            selectedIndex = index;
            selectedPotion = slot.CurrentPotion;

            for (int i = 0; i < potionSlots.Count; i++)
                potionSlots[i].SetHighlight(i == selectedIndex);

            ShowSelectedPotionDetail(selectedPotion);
        }

        // ---------- 선택된 보약 상세 ----------
        void ShowSelectedPotionDetail(PotionData potion)
        {
            if (selectedPotionImage != null)
            {
                selectedPotionImage.sprite = potion.potionIcon;
                selectedPotionImage.color = Color.white;
            }
            if (selectedPotionNameText != null)
            {
                selectedPotionNameText.text = potion.potionName;
                selectedPotionNameText.color = Color.white;
                ConfigureAutoSize(selectedPotionNameText, 12f, 24f);
            }
            if (timeText != null)
            {
                float multiplier = FactoryUpgradeManager.Instance != null ? FactoryUpgradeManager.Instance.GetPotionTimeMultiplier() : 1f;
                timeText.text = $"제작 시간: {potion.potionTimeSeconds / multiplier:F0}초";
                timeText.color = Color.white;
                ConfigureAutoSize(timeText, 10f, 16f);
            }
            if (goldText != null)
            {
                goldText.text = $"판매 가치: {potion.sellGold:N0} 골드";
                goldText.color = Color.white;
                ConfigureAutoSize(goldText, 10f, 16f);
            }
            ResetWarning();

            RefreshResourceSlots(potion);
        }

        static readonly Color EmptySlotColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        static readonly Color EmptyTextColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        /// <summary>
        /// 선택이 해제된 상태. 완전히 안 보이게 하는 대신 회색 빈 이미지/안내 텍스트로
        /// 영역 자체는 항상 눈에 보이게 유지한다.
        /// </summary>
        void ClearSelectedPotionDetail()
        {
            if (selectedPotionImage != null)
            {
                selectedPotionImage.sprite = null;
                selectedPotionImage.color = EmptySlotColor;
            }
            if (selectedPotionNameText != null)
            {
                selectedPotionNameText.text = "보약을 선택하세요";
                selectedPotionNameText.color = EmptyTextColor;
                ConfigureAutoSize(selectedPotionNameText, 12f, 24f);
            }
            if (timeText != null)
            {
                timeText.text = "제작 시간: -";
                timeText.color = EmptyTextColor;
                ConfigureAutoSize(timeText, 10f, 16f);
            }
            if (goldText != null)
            {
                goldText.text = "판매 가치: -";
                goldText.color = EmptyTextColor;
                ConfigureAutoSize(goldText, 10f, 16f);
            }
        }

        void RefreshResourceSlots(PotionData potion)
        {
            var materials = DataManager.Instance != null ? DataManager.Instance.GetPotionMaterials(potion.reqPlantGroup) : new List<(int, int)>();

            // 재료 정의가 하나도 없으면 데이터 누락 상태이므로 제작 불가로 취급한다.
            bool allEnough = materials.Count > 0;

            for (int i = 0; i < resourceSlotRoots.Length; i++)
            {
                if (resourceSlotRoots[i] == null) continue;

                // 슬롯 자체는 선택 여부와 무관하게 항상 켜져 있고, 재료가 있을 때만 내용을 채운다.
                resourceSlotRoots[i].gameObject.SetActive(true);

                if (i < materials.Count)
                {
                    var (plantId, amount) = materials[i];
                    var plant = DataManager.Instance.GetPlantByID(plantId);
                    int owned = DataManager.Instance.GetPlantCount(plantId);
                    bool enough = owned >= amount;
                    if (!enough) allEnough = false;

                    if (resourceSlotIcons[i] != null)
                    {
                        resourceSlotIcons[i].sprite = plant != null ? plant.iconSprite : null;
                        resourceSlotIcons[i].color = Color.white;
                        resourceSlotIcons[i].preserveAspect = true;
                    }
                    if (resourceSlotAmounts[i] != null)
                    {
                        // "필요갯수 / 인벤토리 수량"
                        resourceSlotAmounts[i].text = $"{amount} / {owned}";
                        resourceSlotAmounts[i].color = enough ? new Color(0.4f, 0.9f, 0.4f) : new Color(1f, 0.4f, 0.4f);
                        ConfigureAutoSize(resourceSlotAmounts[i], 10f, 16f);
                    }
                }
                else
                {
                    if (resourceSlotIcons[i] != null)
                    {
                        resourceSlotIcons[i].sprite = null;
                        resourceSlotIcons[i].color = new Color(1f, 1f, 1f, 0.15f);
                    }
                    if (resourceSlotAmounts[i] != null)
                        resourceSlotAmounts[i].text = "";
                }
            }

            // 필요한 재료가 인벤토리에 전부 있을 때만 제작 버튼을 활성화한다.
            if (craftButton != null)
                craftButton.interactable = allEnough;
        }

        /// <summary>아직 아무 보약도 선택하지 않은 초기 상태. 슬롯은 켜져 있되 빈 상태로 표시한다.</summary>
        void ClearResourceSlots()
        {
            for (int i = 0; i < resourceSlotRoots.Length; i++)
            {
                if (resourceSlotRoots[i] == null) continue;
                resourceSlotRoots[i].gameObject.SetActive(true);
                if (resourceSlotIcons[i] != null)
                {
                    resourceSlotIcons[i].sprite = null;
                    resourceSlotIcons[i].color = new Color(1f, 1f, 1f, 0.15f);
                }
                if (resourceSlotAmounts[i] != null)
                {
                    resourceSlotAmounts[i].text = "";
                    ConfigureAutoSize(resourceSlotAmounts[i], 10f, 16f);
                }
            }

            // 선택된 보약이 없으므로 제작 버튼도 비활성화한다.
            if (craftButton != null)
                craftButton.interactable = false;
        }

        // ---------- 보유 식물(인벤토리) 가로 스크롤 ----------
        // 주의: Inventory_Plant는 "보유 식물 목록이 나타나는 영역"이지 그 자체가 이미지가 되면 안 된다.
        // 기존 Plant_Icon 템플릿(아주 작은 아이콘 하나)을 그대로 늘려서 재사용하면 HorizontalLayoutGroup이
        // 폭 제약 없이 자식을 늘려버려 "영역 전체가 이미지 하나"처럼 보이는 문제가 있었다.
        // 그래서 Inventory_Plant 영역 안에, 작고 고정된 크기의 "패널"(배경+아이콘+수량 텍스트)을
        // 식물 개수만큼 코드로 새로 만들어 넣는 방식으로 바꿨다.
        const float InventorySlotWidth = 64f;
        const float InventorySlotHeight = 78f;

        void BuildInventoryScrollRow()
        {
            if (plantIconTemplate == null) return;

            var host = plantIconTemplate.parent as RectTransform; // "Inventory_Plant"
            var templateRt = plantIconTemplate as RectTransform;

            // Inventory_Plant 안에는 "보유 식물" 라벨(Text (TMP))도 같이 있으므로 전체를 덮지 않고,
            // 기존 Plant_Icon 템플릿이 있던 세로 위치를 스크롤 영역의 자리로 사용해 라벨과 겹치지 않게 한다.
            float rowCenterY = templateRt != null ? templateRt.anchoredPosition.y : 0f;

            var scrollHostGO = new GameObject("Inventory_ScrollHost", typeof(RectTransform));
            scrollHostGO.transform.SetParent(host, false);
            var scrollHostRt = scrollHostGO.GetComponent<RectTransform>();
            scrollHostRt.anchorMin = new Vector2(0f, 0.5f);
            scrollHostRt.anchorMax = new Vector2(1f, 0.5f);
            scrollHostRt.pivot = new Vector2(0.5f, 0.5f);
            scrollHostRt.anchoredPosition = new Vector2(0f, rowCenterY);
            scrollHostRt.sizeDelta = new Vector2(-16f, InventorySlotHeight + 12f);

            UIScrollListFactory.CreateHorizontal(scrollHostRt, out inventoryContent, 6f);

            // 원래 있던 Plant_Icon 템플릿 오브젝트는 더 이상 늘려 쓰지 않고 완전히 숨긴다.
            plantIconTemplate.gameObject.SetActive(false);
        }

        public void RefreshInventoryRow()
        {
            if (inventoryContent == null || DataManager.Instance == null) return;

            foreach (var instance in inventoryIconInstances)
            {
                if (instance != null) Destroy(instance.gameObject);
            }
            inventoryIconInstances.Clear();

            int realCount = 0;
            foreach (var plant in DataManager.Instance.GetAllPlants())
            {
                int count = DataManager.Instance.GetPlantCount(plant.plantID);
                if (count <= 0) continue;

                var slot = CreateInventorySlot(plant, count);
                inventoryIconInstances.Add(slot);
                realCount++;
            }

            // 보유 식물이 적어 줄이 휑해 보이지 않도록, 패널을 스크롤 없이 채울 수 있는 만큼 빈 칸을 더 넣는다.
            int targetCount = UIScrollListFactory.ComputeFillSlotCount(inventoryContent, InventorySlotWidth, 6f);
            for (int i = realCount; i < targetCount; i++)
                inventoryIconInstances.Add(CreateEmptyInventorySlot());

            UIScrollListFactory.ApplyNotoSansFont(this);
        }

        Transform CreateEmptyInventorySlot()
        {
            var slotGO = new GameObject("Inventory_Slot_Empty", typeof(RectTransform));
            slotGO.transform.SetParent(inventoryContent, false);
            slotGO.layer = 5;

            var layoutElement = slotGO.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = InventorySlotWidth;
            layoutElement.preferredHeight = InventorySlotHeight;

            var bg = slotGO.AddComponent<Image>();
            bg.color = new Color(0.16f, 0.16f, 0.2f, 0.4f);

            return slotGO.transform;
        }

        Transform CreateInventorySlot(PlantData plant, int count)
        {
            var slotGO = new GameObject($"Inventory_Slot_{plant.plantID}", typeof(RectTransform));
            slotGO.transform.SetParent(inventoryContent, false);
            slotGO.layer = 5;

            var layoutElement = slotGO.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = InventorySlotWidth;
            layoutElement.preferredHeight = InventorySlotHeight;

            var bg = slotGO.AddComponent<Image>();
            bg.color = new Color(0.16f, 0.16f, 0.2f, 0.9f);

            var layout = slotGO.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.spacing = 2;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var iconGO = new GameObject("Icon", typeof(RectTransform));
            iconGO.transform.SetParent(slotGO.transform, false);
            iconGO.layer = 5;
            var iconLayoutElement = iconGO.AddComponent<LayoutElement>();
            iconLayoutElement.preferredWidth = InventorySlotWidth - 16f;
            iconLayoutElement.preferredHeight = InventorySlotWidth - 16f;
            var icon = iconGO.AddComponent<Image>();
            icon.sprite = plant.iconSprite;
            icon.preserveAspect = true;

            var textGO = new GameObject("Amount", typeof(RectTransform));
            textGO.transform.SetParent(slotGO.transform, false);
            textGO.layer = 5;
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = $"{plant.plantName}\n{count}";
            text.fontSize = 11;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.enableAutoSizing = true;
            text.fontSizeMin = 8;
            text.fontSizeMax = 11;

            return slotGO.transform;
        }

        // ---------- 제조 버튼 ----------
        void OnCraftClicked()
        {
            if (selectedPotion == null)
            {
                ShowWarning("먼저 제작할 보약을 선택하세요.");
                return;
            }

            bool started = PotionCraftManager.Instance != null && PotionCraftManager.Instance.TryStartCraft(selectedPotion);
            if (!started)
            {
                ShowWarning("재료가 부족하거나 사용 가능한 트랙이 없습니다.");
                return;
            }

            ResetWarning();
            RefreshInventoryRow();
            RefreshResourceSlots(selectedPotion);
        }

        void ShowWarning(string message)
        {
            if (warningText != null)
            {
                warningText.text = message;
                ConfigureAutoSize(warningText, 9f, 13f);
            }
            Debug.LogWarning($"[UIFactoryPanelController] {message}");
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WeedHoldings
{
    /// <summary>
    /// SellPanel(무역소)에 부착. 선박 선택 -> 지역 선택 -> 판매할 화물(보약) 선택 -> 출항까지
    /// 이 화면의 UI 로직을 전부 담당한다. 실제 항해 타이머/골드 지급은 TradeManager, 해금 레벨은
    /// SellUpgradeManager가 담당하고 여기서는 호출/표시만 한다.
    /// </summary>
    public class UISellPanelController : MonoBehaviour
    {
        const string InventorySlotBaseName = "Inventory_Slot_01";
        const int RegionSlotCount = 5;

        // Ship_Track
        Transform[] shipTrackRoots = new Transform[TradeManager.ShipCount];
        Button[] shipTrackButtons = new Button[TradeManager.ShipCount];
        TMP_Text[] shipTrackTexts = new TMP_Text[TradeManager.ShipCount];

        // Region_Panel
        Transform regionPanel;
        Transform[] regionRoots = new Transform[RegionSlotCount];
        Button[] regionButtons = new Button[RegionSlotCount];
        TMP_Text[] regionTexts = new TMP_Text[RegionSlotCount];
        int[] regionIdBySlot = new int[RegionSlotCount];

        // Inventory_Slot
        RectTransform inventoryContent;
        Transform inventorySlotTemplate;
        readonly List<UITradeInventorySlot> inventorySlots = new List<UITradeInventorySlot>();
        readonly List<Transform> emptyCargoSlots = new List<Transform>();

        // Sell_Panel
        Button sellButton;
        TMP_Text sellButtonText;

        // Time_Panel
        TMP_Text timeText;

        // Sell_Info
        TMP_Text luggageKindText;
        TMP_Text luggageKindAmountText;
        TMP_Text luggageText;
        TMP_Text luggageAmountText;
        TMP_Text goldText;
        TMP_Text goldAmountText;

        int selectedShipIndex = -1;
        int selectedRegionId = -1;

        void Awake()
        {
            CacheReferences();
            ResizeRightColumnPanels();
            ResizeLeftColumnPanels();
            BindShipButtons();
            BindRegionButtons();
            BuildInventoryScroll();

            if (sellButton != null)
                sellButton.onClick.AddListener(OnSellClicked);

            SetRegionPanelInteractable(false);
            RefreshInventorySlots();
            RefreshSellInfo();
            ResetTimeDisplay();

            UIScrollListFactory.ApplyNotoSansFont(this);
        }

        void OnEnable()
        {
            selectedShipIndex = -1;
            selectedRegionId = -1;
            SetRegionPanelInteractable(false);
            RefreshInventorySlots();
            RefreshSellInfo();
            ResetTimeDisplay();
            RefreshShipButtons();
        }

        void Update()
        {
            RefreshShipButtons();
            RefreshRegionButtons();
            RefreshTimeDisplay();
        }

        // ---------- 참조 캐싱 ----------
        void CacheReferences()
        {
            var shipTrack = transform.Find("Ship_Track");
            if (shipTrack != null)
            {
                for (int i = 0; i < TradeManager.ShipCount; i++)
                {
                    var track = shipTrack.Find($"Ship_Track_{i + 1:D2}");
                    shipTrackRoots[i] = track;
                    if (track == null) continue;

                    var button = track.GetComponent<Button>();
                    if (button == null) button = track.gameObject.AddComponent<Button>();
                    shipTrackButtons[i] = button;
                    shipTrackTexts[i] = track.GetComponentInChildren<TMP_Text>();
                    ConfigureAutoSize(shipTrackTexts[i], 9f, 15f);
                }
            }

            regionPanel = transform.Find("Region_Panel");
            if (regionPanel != null)
            {
                for (int i = 0; i < RegionSlotCount; i++)
                {
                    var region = regionPanel.Find($"Region_{i + 1:D2}");
                    regionRoots[i] = region;
                    if (region == null) continue;

                    var button = region.GetComponent<Button>();
                    if (button == null) button = region.gameObject.AddComponent<Button>();
                    regionButtons[i] = button;
                    regionTexts[i] = region.GetComponentInChildren<TMP_Text>();
                    ConfigureAutoSize(regionTexts[i], 10f, 18f);
                }
            }

            var inventorySlot = transform.Find("Inventory_Slot");
            if (inventorySlot != null)
            {
                inventorySlotTemplate = inventorySlot.Find(InventorySlotBaseName);
                ConfigureAutoSize(inventorySlot.Find("Text (TMP)")?.GetComponent<TMP_Text>(), 12f, 22f);
            }

            ConfigureAutoSize(shipTrack?.Find("Text (TMP)")?.GetComponent<TMP_Text>(), 12f, 22f);
            ConfigureAutoSize(regionPanel?.Find("Text (TMP)")?.GetComponent<TMP_Text>(), 12f, 22f);

            var sellPanel = transform.Find("Sell_Panel");
            if (sellPanel != null)
            {
                sellButton = sellPanel.Find("Sell_Button")?.GetComponent<Button>();
                sellButtonText = sellButton != null ? sellButton.GetComponentInChildren<TMP_Text>() : null;
                ConfigureAutoSize(sellButtonText, 14f, 26f);
            }

            var timePanel = transform.Find("Time_Panel");
            if (timePanel != null)
            {
                timeText = timePanel.Find("Time")?.GetComponent<TMP_Text>();
                ConfigureAutoSize(timeText, 12f, 20f);
                ConfigureAutoSize(timePanel.Find("Text (TMP) (1)")?.GetComponent<TMP_Text>(), 10f, 18f);
            }

            var sellInfo = transform.Find("Sell_Info");
            if (sellInfo != null)
            {
                luggageKindText = sellInfo.Find("Luggage_Kind")?.GetComponent<TMP_Text>();
                luggageKindAmountText = sellInfo.Find("Luggage_Kind_Amount")?.GetComponent<TMP_Text>();
                luggageText = sellInfo.Find("Luggage_Text")?.GetComponent<TMP_Text>();
                luggageAmountText = sellInfo.Find("Luggage_Amount")?.GetComponent<TMP_Text>();
                goldText = sellInfo.Find("Gold_Text")?.GetComponent<TMP_Text>();
                goldAmountText = sellInfo.Find("Gold_Amount")?.GetComponent<TMP_Text>();

                ConfigureAutoSize(luggageKindText, 10f, 16f);
                ConfigureAutoSize(luggageKindAmountText, 10f, 16f);
                ConfigureAutoSize(luggageText, 10f, 16f);
                ConfigureAutoSize(luggageAmountText, 10f, 16f);
                ConfigureAutoSize(goldText, 10f, 16f);
                ConfigureAutoSize(goldAmountText, 10f, 16f);
                ConfigureAutoSize(sellInfo.Find("Text (TMP)")?.GetComponent<TMP_Text>(), 12f, 22f);
            }
        }

        static void ConfigureAutoSize(TMP_Text text, float min, float max)
        {
            if (text == null) return;
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
        }

        /// <summary>
        /// Region_Panel/Sell_Info/Time_Panel/Sell_Panel(출항 버튼) 4개를 Top_Panel(높이 114.301) 바로
        /// 아래부터 화면 우측 절반을 꽉 채우도록 재배치한다. 전부 SellPanel(캔버스 전체 크기 1920x1080)
        /// 기준 스트레치 앵커(0,0)-(1,1)라서 sizeDelta/anchoredPosition으로 화면좌표 사각형을 역산한다.
        /// </summary>
        void ResizeRightColumnPanels()
        {
            const float canvasW = 1920f, canvasH = 1080f;
            const float topPanelHeight = 114.301f;
            const float outerMargin = 20f;
            const float gap = 10f;

            float colXMin = canvasW / 2f + 10f;
            float colXMax = canvasW - outerMargin;

            float top = canvasH - topPanelHeight - outerMargin;
            float bottom = outerMargin;
            float usableHeight = top - bottom - gap * 3;

            float regionH = usableHeight * 0.35f;
            float infoH = usableHeight * 0.35f;
            float timeH = usableHeight * 0.10f;
            float sellH = usableHeight * 0.20f;

            float regionTop = top;
            float regionBottom = regionTop - regionH;
            float infoTop = regionBottom - gap;
            float infoBottom = infoTop - infoH;
            float timeTop = infoBottom - gap;
            float timeBottom = timeTop - timeH;
            float sellTop = timeBottom - gap;
            float sellBottom = sellTop - sellH;

            ApplyStretchRect(transform.Find("Region_Panel") as RectTransform, colXMin, colXMax, regionBottom, regionTop, canvasW, canvasH);
            ApplyStretchRect(transform.Find("Sell_Info") as RectTransform, colXMin, colXMax, infoBottom, infoTop, canvasW, canvasH);
            ApplyStretchRect(transform.Find("Time_Panel") as RectTransform, colXMin, colXMax, timeBottom, timeTop, canvasW, canvasH);

            var sellPanelRt = transform.Find("Sell_Panel") as RectTransform;
            ApplyStretchRect(sellPanelRt, colXMin, colXMax, sellBottom, sellTop, canvasW, canvasH);

            // Sell_Button은 Sell_Panel 안에서 중심 기준 고정 오프셋(center-anchored)으로 박혀 있어서,
            // Sell_Panel을 이렇게 훨씬 작게 줄이면 예전 오프셋(-144 등)이 새 패널 바깥(아래)으로 빠져버린다.
            // 축소된 패널 한가운데로 다시 맞춘다.
            var sellButtonRt = sellPanelRt != null ? sellPanelRt.Find("Sell_Button") as RectTransform : null;
            if (sellButtonRt != null)
                sellButtonRt.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Ship_Track/Inventory_Slot을 화면 좌측 절반에, ResizeRightColumnPanels()와 동일한 여백/간격
        /// 규칙으로 배치한다. 우측 4개 패널은 정교하게 재배치되는데 좌측 두 패널은 씬에 있던 원래
        /// 크기 그대로 방치돼 있어 서로 비율이 안 맞아 보이던 문제를 해결한다.
        /// Ship_Track과 Inventory_Slot은 각각 절반(50%)씩 차지한다.
        /// </summary>
        void ResizeLeftColumnPanels()
        {
            const float canvasW = 1920f, canvasH = 1080f;
            const float topPanelHeight = 114.301f;
            const float outerMargin = 20f;
            const float gap = 10f;

            float colXMin = outerMargin;
            float colXMax = canvasW / 2f - 10f;

            float top = canvasH - topPanelHeight - outerMargin;
            float bottom = outerMargin;
            float usableHeight = top - bottom - gap;

            float shipH = usableHeight * 0.5f;
            float inventoryH = usableHeight * 0.5f;

            float shipTop = top;
            float shipBottom = shipTop - shipH;
            float inventoryTop = shipBottom - gap;
            float inventoryBottom = inventoryTop - inventoryH;

            ApplyStretchRect(transform.Find("Ship_Track") as RectTransform, colXMin, colXMax, shipBottom, shipTop, canvasW, canvasH);
            ApplyStretchRect(transform.Find("Inventory_Slot") as RectTransform, colXMin, colXMax, inventoryBottom, inventoryTop, canvasW, canvasH);
        }

        static void ApplyStretchRect(RectTransform rt, float xMin, float xMax, float yMin, float yMax, float parentW, float parentH)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = new Vector2((xMax - xMin) - parentW, (yMax - yMin) - parentH);
            rt.anchoredPosition = new Vector2((xMin + xMax) / 2f - parentW / 2f, (yMin + yMax) / 2f - parentH / 2f);
        }

        // ---------- 선박(Ship_Track) ----------
        void BindShipButtons()
        {
            for (int i = 0; i < TradeManager.ShipCount; i++)
            {
                if (shipTrackButtons[i] == null) continue;
                int capturedIndex = i;
                shipTrackButtons[i].onClick.RemoveAllListeners();
                shipTrackButtons[i].onClick.AddListener(() => OnShipClicked(capturedIndex));
            }
        }

        void OnShipClicked(int index)
        {
            if (SellUpgradeManager.Instance != null && !SellUpgradeManager.Instance.IsShipTrackUnlocked(index)) return;
            var ship = TradeManager.Instance?.GetShip(index);
            if (ship != null && ship.state != ShipState.Idle) return; // 항해 중인 배는 재설정 불가

            // 이미 선택된 배를 다시 누르면 선택 해제.
            bool deselect = selectedShipIndex == index;
            selectedShipIndex = deselect ? -1 : index;
            selectedRegionId = -1;
            RefreshShipButtons();
            SetRegionPanelInteractable(!deselect);
            RefreshInventorySlots();
            RefreshSellInfo();
            ResetTimeDisplay();
        }

        void RefreshShipButtons()
        {
            for (int i = 0; i < TradeManager.ShipCount; i++)
            {
                if (shipTrackRoots[i] == null) continue;

                bool unlocked = SellUpgradeManager.Instance == null || SellUpgradeManager.Instance.IsShipTrackUnlocked(i);
                var ship = TradeManager.Instance?.GetShip(i);

                if (!unlocked)
                {
                    if (shipTrackTexts[i] != null) shipTrackTexts[i].text = "잠김";
                    if (shipTrackButtons[i] != null) shipTrackButtons[i].interactable = false;
                    SetSlotColor(shipTrackRoots[i], new Color(0.12f, 0.12f, 0.14f, 0.9f));
                    continue;
                }

                bool isVoyaging = ship != null && ship.state == ShipState.Voyaging;
                if (shipTrackButtons[i] != null)
                    shipTrackButtons[i].interactable = !isVoyaging;

                if (shipTrackTexts[i] != null)
                {
                    if (isVoyaging)
                    {
                        int minutes = Mathf.FloorToInt(ship.remainingTime / 60f);
                        int seconds = Mathf.FloorToInt(ship.remainingTime % 60f);
                        shipTrackTexts[i].text = $"무역선 Lv.1\n항해 중... {minutes:D2}:{seconds:D2}";
                    }
                    else
                    {
                        shipTrackTexts[i].text = "무역선 Lv.1\n상태: 대기중";
                    }
                }

                Color bg = isVoyaging ? new Color(0.55f, 0.45f, 0.15f, 0.9f)
                    : (i == selectedShipIndex ? new Color(0.25f, 0.45f, 0.85f, 0.9f) : new Color(0.18f, 0.18f, 0.22f, 0.9f));
                SetSlotColor(shipTrackRoots[i], bg);
            }
        }

        // ---------- 지역(Region_Panel) ----------
        void BindRegionButtons()
        {
            if (DataManager.Instance == null) return;
            var regions = DataManager.Instance.GetAllRegions();

            for (int i = 0; i < RegionSlotCount; i++)
            {
                if (regionButtons[i] == null) continue;
                if (i >= regions.Count)
                {
                    regionRoots[i]?.gameObject.SetActive(false);
                    continue;
                }

                regionIdBySlot[i] = regions[i].regionID;
                int capturedIndex = i;
                regionButtons[i].onClick.RemoveAllListeners();
                regionButtons[i].onClick.AddListener(() => OnRegionClicked(capturedIndex));
            }
        }

        void OnRegionClicked(int slotIndex)
        {
            if (selectedShipIndex < 0) return;

            int regionId = regionIdBySlot[slotIndex];
            if (regionId <= 0) return;
            if (SellUpgradeManager.Instance != null && !SellUpgradeManager.Instance.IsRegionUnlocked(regionId)) return;

            // 이미 선택된 지역을 다시 누르면 선택 해제.
            selectedRegionId = (selectedRegionId == regionId) ? -1 : regionId;
            RefreshRegionButtons();
            RefreshInventorySlots();
            RefreshSellInfo();
        }

        void SetRegionPanelInteractable(bool interactable)
        {
            if (regionPanel != null)
            {
                var cg = regionPanel.GetComponent<CanvasGroup>();
                if (cg == null) cg = regionPanel.gameObject.AddComponent<CanvasGroup>();
                cg.interactable = interactable;
                cg.blocksRaycasts = interactable;
                cg.alpha = interactable ? 1f : 0.5f;
            }
            RefreshRegionButtons();
        }

        void RefreshRegionButtons()
        {
            for (int i = 0; i < RegionSlotCount; i++)
            {
                if (regionRoots[i] == null) continue;

                int regionId = regionIdBySlot[i];
                bool unlocked = regionId > 0 && (SellUpgradeManager.Instance == null || SellUpgradeManager.Instance.IsRegionUnlocked(regionId));

                if (regionTexts[i] != null)
                {
                    var region = regionId > 0 ? DataManager.Instance?.GetRegionByID(regionId) : null;
                    regionTexts[i].text = unlocked ? (region != null ? region.regionName : "") : "???";
                }
                if (regionButtons[i] != null)
                    regionButtons[i].interactable = unlocked;

                Color bg = regionId == selectedRegionId ? new Color(0.25f, 0.45f, 0.85f, 0.9f)
                    : (unlocked ? new Color(0.18f, 0.18f, 0.22f, 0.9f) : new Color(0.12f, 0.12f, 0.14f, 0.9f));
                SetSlotColor(regionRoots[i], bg);
            }
        }

        static void SetSlotColor(Transform root, Color color)
        {
            if (root == null) return;
            var img = root.GetComponent<Image>();
            if (img != null) img.color = color;
        }

        const int InventoryRowCount = 2;

        // ---------- 보유 화물(Inventory_Slot) 가로 스크롤 (기획서처럼 위/아래 2행으로 균일하게 배치) ----------
        void BuildInventoryScroll()
        {
            if (inventorySlotTemplate == null) return;

            var host = inventorySlotTemplate.parent; // "Inventory_Slot"
            var templateRt = inventorySlotTemplate as RectTransform;
            NormalizeSlotTemplate(templateRt);

            // 카드 한 장의 고정 크기(NormalizeSlotTemplate이 계산한 실제 비율)를 그대로 셀 크기로 사용해서,
            // 예전처럼 1행짜리 HorizontalLayoutGroup이 카드를 세로로 늘려 비율이 깨지는 문제를 없앤다.
            var cellSize = templateRt != null ? templateRt.sizeDelta : new Vector2(140f, 90f);
            var scrollRect = UIScrollListFactory.CreateHorizontalGrid(host, out inventoryContent, cellSize, new Vector2(14f, 14f), InventoryRowCount);
            var scrollRt = scrollRect.GetComponent<RectTransform>();
            // 위쪽에 이미 있는 "판매할 화물 선택" 라벨과 겹치지 않도록 스크롤 영역 상단을 안쪽으로 당긴다.
            scrollRt.offsetMax = new Vector2(scrollRt.offsetMax.x, -70f);

            inventorySlotTemplate.SetParent(inventoryContent, false);

            var slot = inventorySlotTemplate.GetComponent<UITradeInventorySlot>();
            if (slot == null) slot = inventorySlotTemplate.gameObject.AddComponent<UITradeInventorySlot>();
            BindSlotComponent(slot, inventorySlotTemplate);

            // 템플릿 자체는 숨기고 이후 보유 포션 수만큼 복제해서 사용한다.
            inventorySlotTemplate.gameObject.SetActive(false);
        }

        /// <summary>
        /// Inventory_Slot_01은 원래 부모(거의 화면 전체 크기)를 그대로 채우도록 앵커가 잡혀있어서
        /// 아이콘/Plus/Minus/텍스트가 화면 한쪽 구석에 몰려 보였다. 각 자식의 크기는 그대로 두고
        /// (그대로 유지) 자식들의 바운딩 박스 중심이 (0,0)에 오도록 위치만 재정렬한 뒤,
        /// 그 바운딩 박스 크기를 카드 하나의 고정 크기로 사용한다(균일한 카드가 되도록).
        /// </summary>
        static void NormalizeSlotTemplate(RectTransform template)
        {
            if (template == null) return;

            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            var children = new List<RectTransform>();
            foreach (RectTransform child in template)
            {
                children.Add(child);
                Vector2 pos = child.anchoredPosition;
                Vector2 size = child.rect.size;
                minX = Mathf.Min(minX, pos.x - size.x / 2f);
                maxX = Mathf.Max(maxX, pos.x + size.x / 2f);
                minY = Mathf.Min(minY, pos.y - size.y / 2f);
                maxY = Mathf.Max(maxY, pos.y + size.y / 2f);
            }
            if (children.Count == 0) return;

            Vector2 center = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
            foreach (var child in children)
                child.anchoredPosition -= center;

            float width = (maxX - minX) + 24f;
            float height = (maxY - minY) + 24f;

            template.anchorMin = new Vector2(0.5f, 0.5f);
            template.anchorMax = new Vector2(0.5f, 0.5f);
            template.pivot = new Vector2(0.5f, 0.5f);
            template.sizeDelta = new Vector2(width, height);

            var le = template.GetComponent<LayoutElement>();
            if (le == null) le = template.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;
        }

        static void BindSlotComponent(UITradeInventorySlot slot, Transform root)
        {
            slot.icon = root.Find("Image (3)")?.GetComponent<Image>();
            slot.plusButton = root.Find("Plus")?.GetComponent<Button>();
            slot.minusButton = root.Find("Minus")?.GetComponent<Button>();
            slot.toSellText = root.Find("To_Sell")?.GetComponent<TMP_Text>();
            slot.amountText = root.Find("Amount")?.GetComponent<TMP_Text>();

            ConfigureAutoSize(slot.toSellText, 9f, 15f);
            ConfigureAutoSize(slot.amountText, 8f, 12f);

            var plusText = slot.plusButton != null ? slot.plusButton.GetComponentInChildren<TMP_Text>() : null;
            var minusText = slot.minusButton != null ? slot.minusButton.GetComponentInChildren<TMP_Text>() : null;
            ConfigureAutoSize(plusText, 10f, 18f);
            ConfigureAutoSize(minusText, 10f, 18f);
        }

        /// <summary>
        /// 인벤토리에 있는 보약은 지역 선택 여부와 무관하게 항상 전부 보여준다.
        /// 지역을 고르면 그 지역의 식물 보너스가 적용된 가격 기준으로 비싼 순으로 재정렬된다(지역 선택 전에는 기본 판매가 기준).
        /// </summary>
        void RefreshInventorySlots()
        {
            ClearInventorySlots(false);

            if (DataManager.Instance == null || inventoryContent == null || inventorySlotTemplate == null)
                return;

            var ownedPotions = DataManager.Instance.GetAllPotions()
                .Select(p => (potion: p, owned: DataManager.Instance.GetPotionCount(p.potionID)))
                .Where(x => x.owned > 0)
                .OrderByDescending(x => selectedRegionId > 0 && TradeManager.Instance != null
                    ? TradeManager.Instance.GetEffectiveSellPrice(x.potion.potionID, selectedRegionId)
                    : x.potion.sellGold)
                .ToList();

            foreach (var (potion, owned) in ownedPotions)
            {
                var clone = Instantiate(inventorySlotTemplate.gameObject, inventoryContent);
                clone.SetActive(true);
                clone.name = $"Inventory_Slot_{potion.potionID}";

                var slot = clone.GetComponent<UITradeInventorySlot>();
                if (slot == null) slot = clone.AddComponent<UITradeInventorySlot>();
                BindSlotComponent(slot, clone.transform);
                slot.Setup(potion, owned, OnSlotPlusClicked, OnSlotMinusClicked);

                inventorySlots.Add(slot);
            }

            // 보유 화물이 적어 줄이 휑해 보이지 않도록, 패널을 스크롤 없이 채울 수 있는 만큼 빈 칸을 더 넣는다.
            // (2행 그리드이므로 한 열에 카드가 2장씩 들어간다.)
            var templateRt = inventorySlotTemplate as RectTransform;
            float cardWidth = templateRt != null ? templateRt.sizeDelta.x : 0f;
            int targetCount = UIScrollListFactory.ComputeFillSlotCount(inventoryContent, cardWidth, 14f) * InventoryRowCount;
            for (int i = inventorySlots.Count; i < targetCount; i++)
                emptyCargoSlots.Add(CreateEmptyCargoSlot());

            // 빈 칸을 채워도 패딩 근사치 때문에 살짝 왼쪽으로 쏠려 보일 수 있어, 콘텐츠를 뷰포트
            // 가운데로 정렬한다(항목이 많아 스크롤이 필요하면 자동으로 원래 위치로 되돌아간다).
            UIScrollListFactory.CenterHorizontalContentIfUnderfilled(inventoryContent);

            UIScrollListFactory.ApplyNotoSansFont(this);
        }

        Transform CreateEmptyCargoSlot()
        {
            var clone = Instantiate(inventorySlotTemplate.gameObject, inventoryContent);
            clone.SetActive(true);
            clone.name = "Inventory_Slot_Empty";

            // 실제 화물이 아니라 빈 칸 채우기용이라 상호작용 로직은 떼어내고 시각적으로만 흐리게 남긴다.
            var comp = clone.GetComponent<UITradeInventorySlot>();
            if (comp != null) Destroy(comp);

            var icon = clone.transform.Find("Image (3)")?.GetComponent<Image>();
            if (icon != null)
            {
                icon.sprite = null;
                icon.color = new Color(1f, 1f, 1f, 0.12f);
            }
            clone.transform.Find("Plus")?.gameObject.SetActive(false);
            clone.transform.Find("Minus")?.gameObject.SetActive(false);
            var toSellText = clone.transform.Find("To_Sell")?.GetComponent<TMP_Text>();
            if (toSellText != null) toSellText.text = "";
            var amountText = clone.transform.Find("Amount")?.GetComponent<TMP_Text>();
            if (amountText != null) amountText.text = "";

            return clone.transform;
        }

        void ClearInventorySlots(bool refreshInfo = true)
        {
            foreach (var slot in inventorySlots)
            {
                if (slot != null) Destroy(slot.gameObject);
            }
            inventorySlots.Clear();

            foreach (var empty in emptyCargoSlots)
            {
                if (empty != null) Destroy(empty.gameObject);
            }
            emptyCargoSlots.Clear();

            if (refreshInfo) RefreshSellInfo();
        }

        void OnSlotPlusClicked(UITradeInventorySlot slot)
        {
            int capacity = SellUpgradeManager.Instance != null ? SellUpgradeManager.Instance.GetCargoCapacity() : 3;
            if (GetTotalSelectedAmount() >= capacity) return;

            if (slot.TryIncrease())
                RefreshSellInfo();
        }

        void OnSlotMinusClicked(UITradeInventorySlot slot)
        {
            if (slot.TryDecrease())
                RefreshSellInfo();
        }

        int GetTotalSelectedAmount()
        {
            int total = 0;
            foreach (var slot in inventorySlots) total += slot.SelectedAmount;
            return total;
        }

        // ---------- 판매 요약(Sell_Info) / 출항(Sell_Panel) ----------
        void RefreshSellInfo()
        {
            int capacity = SellUpgradeManager.Instance != null ? SellUpgradeManager.Instance.GetCargoCapacity() : 3;
            int totalAmount = GetTotalSelectedAmount();
            int kindCount = inventorySlots.Count(s => s.SelectedAmount > 0);

            int expectedGold = 0;
            if (selectedRegionId > 0 && TradeManager.Instance != null)
            {
                foreach (var slot in inventorySlots)
                {
                    if (slot.SelectedAmount <= 0) continue;
                    expectedGold += Mathf.RoundToInt(TradeManager.Instance.GetEffectiveSellPrice(slot.PotionID, selectedRegionId) * slot.SelectedAmount);
                }
            }

            if (luggageText != null) luggageText.text = "총 적재량";
            if (luggageAmountText != null) luggageAmountText.text = $"{totalAmount} / {capacity}";
            if (luggageKindText != null) luggageKindText.text = "선택 화물";
            if (luggageKindAmountText != null) luggageKindAmountText.text = $"{kindCount}종 / {totalAmount}개";
            if (goldText != null) goldText.text = "예상 수익";
            if (goldAmountText != null) goldAmountText.text = $"{expectedGold:N0} G";

            if (sellButton != null)
                sellButton.interactable = selectedShipIndex >= 0 && selectedRegionId > 0 && totalAmount > 0;
        }

        void OnSellClicked()
        {
            if (selectedShipIndex < 0 || selectedRegionId <= 0 || TradeManager.Instance == null) return;

            var cargo = new List<(int potionId, int amount)>();
            foreach (var slot in inventorySlots)
            {
                if (slot.SelectedAmount > 0)
                    cargo.Add((slot.PotionID, slot.SelectedAmount));
            }
            if (cargo.Count == 0) return;

            bool started = TradeManager.Instance.StartVoyage(selectedShipIndex, selectedRegionId, cargo);
            if (!started) return;

            selectedShipIndex = -1;
            selectedRegionId = -1;
            SetRegionPanelInteractable(false);
            RefreshInventorySlots();
            RefreshSellInfo();
            ResetTimeDisplay();
        }

        // ---------- 소요 시간(Time_Panel) ----------
        void ResetTimeDisplay()
        {
            if (timeText != null) timeText.text = "--:--:--";
        }

        void RefreshTimeDisplay()
        {
            if (timeText == null) return;

            if (selectedShipIndex >= 0)
            {
                var ship = TradeManager.Instance?.GetShip(selectedShipIndex);
                if (ship != null && ship.state == ShipState.Voyaging)
                {
                    int minutes = Mathf.FloorToInt(ship.remainingTime / 60f);
                    int seconds = Mathf.FloorToInt(ship.remainingTime % 60f);
                    timeText.text = $"{minutes:D2}:{seconds:D2}";
                    return;
                }
            }

            if (selectedRegionId > 0)
            {
                var region = DataManager.Instance?.GetRegionByID(selectedRegionId);
                if (region != null)
                {
                    int minutes = Mathf.FloorToInt(region.sellTimeSeconds / 60f);
                    int seconds = Mathf.FloorToInt(region.sellTimeSeconds % 60f);
                    timeText.text = $"예상 {minutes:D2}:{seconds:D2}";
                    return;
                }
            }

            timeText.text = "--:--:--";
        }
    }
}

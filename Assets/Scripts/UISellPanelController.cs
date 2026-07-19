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
        Image[] shipTrackLockIcons = new Image[TradeManager.ShipCount];

        // Region_Panel
        Transform regionPanel;
        Transform[] regionRoots = new Transform[RegionSlotCount];
        Button[] regionButtons = new Button[RegionSlotCount];
        TMP_Text[] regionTexts = new TMP_Text[RegionSlotCount];
        Image[] regionLabelImages = new Image[RegionSlotCount];
        Image[] regionLockIcons = new Image[RegionSlotCount];
        int[] regionIdBySlot = new int[RegionSlotCount];

        // Inventory_Slot
        RectTransform inventoryContent;
        Transform inventorySlotTemplate;
        readonly List<UITradeInventorySlot> inventorySlots = new List<UITradeInventorySlot>();
        readonly List<Transform> emptyCargoSlots = new List<Transform>();

        // 배/지역을 새로 고를 때마다 Inventory_Slot이 통째로 재생성되는데, 그때마다 이미 골라둔
        // 판매 수량이 0으로 초기화되지 않도록 potionID 기준으로 선택 수량을 별도 보관해둔다.
        readonly Dictionary<int, int> pendingSellAmounts = new Dictionary<int, int>();

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

            // ResizeLeftColumnPanels()가 방금 바꾼 anchor/sizeDelta가 실제 반영된 rect를 곧바로
            // 읽어야 하는데(BuildInventoryScroll/RefreshInventorySlots의 뷰포트 폭 계산),
            // SellPanel이 이번 프레임에 막 활성화된 상태라 유니티 레이아웃 재계산이 아직 안 돌아서
            // rect.width가 0으로 읽혀 "빈 슬롯 채우기(ComputeFillSlotCount)"가 0개로 계산되던 문제가 있었다.
            // 강제로 캔버스 레이아웃을 즉시 갱신해서 이후 계산이 정확한 크기를 읽도록 한다.
            Canvas.ForceUpdateCanvases();

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

            // Awake()와 같은 이유 - 패널이 막 켜진 시점이라 뷰포트 rect가 아직 갱신 전일 수 있다.
            Canvas.ForceUpdateCanvases();
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
                    if (regionTexts[i] != null) regionTexts[i].enabled = false;
                }
            }

            var inventorySlot = transform.Find("Inventory_Slot");
            if (inventorySlot != null)
            {
                inventorySlotTemplate = inventorySlot.Find(InventorySlotBaseName);
                SetHeaderText(inventorySlot.Find("Text (TMP)"), "보유 화물", 12f, 22f);
            }

            SetHeaderText(shipTrack?.Find("Text (TMP)"), "무역선", 12f, 22f);
            SetHeaderText(regionPanel?.Find("Text (TMP)"), "목적지", 12f, 22f);

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
                SetHeaderText(timePanel.Find("Text (TMP) (1)"), "예상 소요 시간", 10f, 18f);
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
                SetHeaderText(sellInfo.Find("Text (TMP)"), "거래 요약", 12f, 22f);

                var goldIcon = sellInfo.Find("Gold")?.GetComponent<Image>();
                if (goldIcon != null)
                {
                    goldIcon.sprite = Resources.Load<Sprite>("Gold");
                    goldIcon.preserveAspect = true;
                }

                var luggageIcon = sellInfo.Find("Luggage")?.GetComponent<Image>();
                if (luggageIcon != null)
                {
                    luggageIcon.sprite = Resources.Load<Sprite>("Luggage");
                    luggageIcon.preserveAspect = true;
                }
            }
        }

        static void ConfigureAutoSize(TMP_Text text, float min, float max)
        {
            if (text == null) return;
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
        }

        /// <summary>씬에 미리 만들어진 채 내용이 채워지지 않았던(TMP 기본값 "New Text") 구역 헤더에
        /// 실제 라벨을 채운다.</summary>
        static void SetHeaderText(Transform target, string label, float min, float max)
        {
            var text = target?.GetComponent<TMP_Text>();
            if (text == null) return;
            text.text = label;
            ConfigureAutoSize(text, min, max);
        }

        static readonly Color VoyagingTextColor = new Color(1f, 0.95f, 0.6f, 1f); // 밝은 크림 옐로우
        static readonly Color VoyagingOutlineColor = new Color(0.15f, 0.08f, 0f, 1f); // 짙은 갈색 외곽선

        /// <summary>항해 중 배경(황토색 틴트) 위에서 "항해 중" 텍스트가 잘 안 보인다는 피드백이 있었다.
        /// 밝은 크림색 + 굵게 + 짙은 외곽선을 줘서 어떤 배경 위에서도 또렷하게 읽히도록 한다.</summary>
        static void StyleVoyagingText(TMP_Text text)
        {
            text.color = VoyagingTextColor;
            text.fontStyle = FontStyles.Bold;
            text.outlineWidth = 0.25f;
            text.outlineColor = VoyagingOutlineColor;
        }

        static readonly Color IdleTextColor = new Color(0.08f, 0.08f, 0.1f, 1f); // 거의 검정

        /// <summary>항해 중이 아닐 때(대기중) Ship_Track 배경은 항상 흰색이라, 예전처럼 흰 글씨를
        /// 쓰면 배경과 겹쳐 글씨가 아예 안 보였다. 짙은(거의 검정) 색으로 바꿔 대비를 준다.</summary>
        static void ResetTextStyle(TMP_Text text)
        {
            text.color = IdleTextColor;
            text.fontStyle = FontStyles.Normal;
            text.outlineWidth = 0f;
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
                    if (shipTrackTexts[i] != null)
                    {
                        shipTrackTexts[i].text = "잠김";
                        // 자물쇠 아이콘이 슬롯 정중앙에 오므로, "잠김" 글자는 그 아래로 내려서 겹치지 않게 한다.
                        shipTrackTexts[i].margin = new Vector4(0f, 130f, 0f, 0f);
                        // 잠금 배경은 어두운 색이라 밝은 흰 글씨로 고정해 대비를 준다.
                        shipTrackTexts[i].color = Color.white;
                        shipTrackTexts[i].fontStyle = FontStyles.Normal;
                        shipTrackTexts[i].outlineWidth = 0f;
                    }
                    if (shipTrackButtons[i] != null) shipTrackButtons[i].interactable = false;
                    SetSlotColor(shipTrackRoots[i], new Color(0.12f, 0.12f, 0.14f, 0.9f));
                    EnsureLockIcon(shipTrackRoots[i], ref shipTrackLockIcons[i], Vector2.zero).enabled = true;
                    SetSlotSelectedOutline(shipTrackRoots[i], false);
                    continue;
                }
                if (shipTrackLockIcons[i] != null) shipTrackLockIcons[i].enabled = false;
                if (shipTrackTexts[i] != null) shipTrackTexts[i].margin = Vector4.zero;

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
                        StyleVoyagingText(shipTrackTexts[i]);
                    }
                    else
                    {
                        shipTrackTexts[i].text = "무역선 Lv.1\n상태: 대기중";
                        ResetTextStyle(shipTrackTexts[i]);
                    }
                }

                // 해금된 트랙은 선택 여부와 상관없이 항상 밝게(흰색) 표시하고, 선택 여부는 Region_Panel과
                // 동일하게 파란 테두리로만 표시한다. 항해 중일 때만 별도로 황토색 틴트를 남겨 상태를 구분한다.
                Color bg = isVoyaging ? new Color(0.55f, 0.45f, 0.15f, 0.9f) : Color.white;
                SetSlotColor(shipTrackRoots[i], bg);
                SetSlotSelectedOutline(shipTrackRoots[i], !isVoyaging && i == selectedShipIndex);
            }
        }

        // 무역소 테이블.xlsx "지역" 시트의 Region_ID(42001~42005) 기준 - 리소스/기타 리소스 2에 있던
        // 지역 배경 이미지 파일명과 1:1 매칭(42001=섬, 42002=극지방, 42003=사막, 42004=초원, 42005=황실).
        static readonly Dictionary<int, string> RegionImageNames = new Dictionary<int, string>
        {
            { 42001, "Island" },
            { 42002, "Polar" },
            { 42003, "Desert" },
            { 42004, "Grassland" },
            { 42005, "Royal" },
        };

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

                // 지역 배경 이미지는 선택/해금 상태에 따라 매 프레임 색만 덧입혀지므로(RefreshRegionButtons의
                // SetSlotColor) 스프라이트 자체는 여기서 한 번만 넣어주면 된다. 버튼 크기(세로로 긴 직사각형)에
                // 맞춰 늘리면 정사각형 원본 이미지가 찌그러지므로 preserveAspect로 비율을 유지한다.
                var regionImage = regionRoots[i]?.GetComponent<Image>();
                if (regionImage != null && RegionImageNames.TryGetValue(regions[i].regionID, out var imageName))
                {
                    // 버튼이 기본 유니티 UISprite(Sliced 타입)로 되어 있어서, 타입을 Simple로 바꾸지 않으면
                    // 정사각형 배경 사진이 9-슬라이스로 이상하게 늘어난다.
                    regionImage.sprite = Resources.Load<Sprite>($"Regions/{imageName}");
                    regionImage.type = Image.Type.Simple;
                    regionImage.preserveAspect = true;
                }
            }
        }

        void OnRegionClicked(int slotIndex)
        {
            if (selectedShipIndex < 0) return;

            int regionId = regionIdBySlot[slotIndex];
            if (regionId <= 0) return;
            if (SellUpgradeManager.Instance != null && !SellUpgradeManager.Instance.IsRegionUnlocked(regionId)) return;
            // 이미 다른 배가 항해 중인 목적지는 그 배가 돌아올 때까지 다시 선택할 수 없다.
            if (TradeManager.Instance != null && TradeManager.Instance.IsRegionInVoyage(regionId)) return;

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
                // 배를 아직 선택하지 않아 지역을 고를 수 없는 동안에도, 화면 자체는 흐려지지 않고
                // 항상 선명하게 보여야 한다(선택 불가는 클릭이 안 먹히는 것으로만 표시).
                cg.alpha = 1f;
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
                bool voyaging = unlocked && TradeManager.Instance != null && TradeManager.Instance.IsRegionInVoyage(regionId);

                // 지역명 텍스트는 사진 배경 위에서 잘 안 보이므로 제거하고, 대신 폰트를 미리 구운
                // 라벨 이미지(Region_Label_*)를 사용한다. 잠긴 지역은 "???" 라벨 + 중앙 자물쇠 아이콘.
                UpdateRegionLabelImage(i, regionId, unlocked);
                // 다른 잠긴 트랙들과 달리 지역 슬롯은 겹치는 텍스트가 없으므로 자물쇠를 중앙에 그대로 둔다.
                EnsureLockIcon(regionRoots[i], ref regionLockIcons[i], Vector2.zero).enabled = !unlocked;

                if (regionButtons[i] != null)
                    regionButtons[i].interactable = unlocked && !voyaging;

                // Ship_Track과 동일한 규칙: 해금된 지역은 선택 여부와 무관하게 항상 밝게(흰색) 유지해
                // 투명하게 죽어 보이지 않게 하고, 선택 표시만 파란 테두리로 한다. 다만 이미 다른 배가
                // 향하고 있는 지역이라면 Ship_Track의 "항해 중" 상태처럼 어둡게 틴트하고 가운데에
                // "항해 중" 텍스트를 띄워 지금은 고를 수 없는 목적지임을 알려준다.
                Color bg = !unlocked ? new Color(0.12f, 0.12f, 0.14f, 0.9f)
                    : (voyaging ? new Color(0.55f, 0.45f, 0.15f, 0.9f) : Color.white);
                SetSlotColor(regionRoots[i], bg);
                SetSlotSelectedOutline(regionRoots[i], unlocked && !voyaging && regionId == selectedRegionId);

                if (regionTexts[i] != null)
                {
                    regionTexts[i].enabled = voyaging;
                    if (voyaging)
                    {
                        regionTexts[i].text = "항해 중";
                        StyleVoyagingText(regionTexts[i]);
                    }
                }
            }
        }

        static Sprite cachedLockedRegionLabel;
        static readonly Dictionary<int, Sprite> cachedRegionLabels = new Dictionary<int, Sprite>();

        /// <summary>지역명을 사진 배경 위에 텍스트로 직접 그리면 잘 안 보이므로, 미리 구운 라벨
        /// 이미지(Resources/Regions/Region_Label_*)로 대체한다. 슬롯 하단에 작은 배지 형태로 붙인다.</summary>
        void UpdateRegionLabelImage(int slotIndex, int regionId, bool unlocked)
        {
            var root = regionRoots[slotIndex];
            if (root == null) return;

            var image = EnsureRegionLabelImage(root, ref regionLabelImages[slotIndex]);
            if (image == null) return;

            if (!unlocked)
            {
                if (cachedLockedRegionLabel == null)
                    cachedLockedRegionLabel = Resources.Load<Sprite>("Regions/Region_Label_Locked");
                image.sprite = cachedLockedRegionLabel;
                image.enabled = image.sprite != null;
                return;
            }

            if (regionId <= 0 || !RegionImageNames.TryGetValue(regionId, out var imageName))
            {
                image.enabled = false;
                return;
            }

            if (!cachedRegionLabels.TryGetValue(regionId, out var sprite))
            {
                sprite = Resources.Load<Sprite>($"Regions/Region_Label_{imageName}");
                cachedRegionLabels[regionId] = sprite;
            }
            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        /// <summary>지역 슬롯 하단에 라벨 배지 이미지를 붙이기 위해 첫 호출 시 자식 Image를 만들어 캐싱한다.</summary>
        static Image EnsureRegionLabelImage(Transform root, ref Image cached)
        {
            if (cached != null) return cached;
            if (root == null) return null;

            var go = new GameObject("Region_Label_Image", typeof(RectTransform));
            go.transform.SetParent(root, false);
            go.transform.SetAsLastSibling();

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(110f, 34f);
            rect.anchoredPosition = new Vector2(0f, 8f);

            cached = go.AddComponent<Image>();
            cached.preserveAspect = true;
            cached.raycastTarget = false;
            return cached;
        }

        static void SetSlotColor(Transform root, Color color)
        {
            if (root == null) return;
            var img = root.GetComponent<Image>();
            if (img != null) img.color = color;
        }

        static Sprite cachedLockSprite;

        /// <summary>Resources/UI/Rock.png를 잠금 아이콘으로 사용한다.</summary>
        static Sprite ResolveLockSprite()
        {
            if (cachedLockSprite == null)
                cachedLockSprite = Resources.Load<Sprite>("UI/Rock");
            return cachedLockSprite;
        }

        /// <summary>잠긴 슬롯에 자물쇠 아이콘을 씌우기 위해 첫 호출 시 자식 Image를 하나 만들어 캐싱한다.
        /// anchoredPosition을 지정하지 않으면 "잠김" 텍스트(슬롯 중앙)와 안 겹치게 위쪽에 배치한다.</summary>
        static Image EnsureLockIcon(Transform root, ref Image cached, Vector2? anchoredPosition = null)
        {
            if (cached != null) return cached;
            if (root == null) return null;

            var go = new GameObject("Lock_Icon", typeof(RectTransform));
            go.transform.SetParent(root, false);
            go.transform.SetAsLastSibling();

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(48f, 48f);
            rect.anchoredPosition = anchoredPosition ?? new Vector2(0f, 55f);

            cached = go.AddComponent<Image>();
            cached.sprite = ResolveLockSprite();
            cached.preserveAspect = true;
            cached.raycastTarget = false;
            return cached;
        }

        static readonly Color SelectedOutlineColor = new Color(0.2f, 0.55f, 1f, 1f);

        /// <summary>선택 상태를 색 틴트 대신 파란 테두리(Outline)로 표시한다.</summary>
        static void SetSlotSelectedOutline(Transform root, bool selected)
        {
            if (root == null) return;
            var outline = root.GetComponent<Outline>();
            if (outline == null)
            {
                outline = root.gameObject.AddComponent<Outline>();
                outline.effectColor = SelectedOutlineColor;
                outline.effectDistance = new Vector2(4f, 4f);
                outline.useGraphicAlpha = false;
            }
            outline.enabled = selected;
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

            // 예전엔 상하좌우 스트레치 앵커(0,0)-(1,1)에서 위쪽만 라벨 높이(70)만큼 잘라냈는데,
            // 그러면 스크롤 영역이 항상 패널 아래쪽에 붙어(하단 기준) 배치돼 Inventory_Slot 크기가
            // 바뀔 때마다 중심이 어긋났다. 대신 Inventory_Slot 중앙을 앵커/피벗 기준점으로 잡고,
            // 라벨 아래 남는 공간(라벨 제외 높이)의 정중앙에 스크롤 영역을 배치한다.
            const float labelHeight = 70f;
            var hostRt = host as RectTransform;
            float width = hostRt != null ? hostRt.rect.width : 0f;
            float height = hostRt != null ? Mathf.Max(0f, hostRt.rect.height - labelHeight) : 0f;

            scrollRt.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRt.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRt.pivot = new Vector2(0.5f, 0.5f);
            scrollRt.sizeDelta = new Vector2(width, height);
            scrollRt.anchoredPosition = new Vector2(0f, -labelHeight / 2f);

            // "판매할 화물 선택" 라벨이 Inventory_Slot 중앙 기준 고정 오프셋으로 박혀 있어서, 패널
            // 크기가 바뀌면(ResizeLeftColumnPanels) 라벨과 카드 줄의 왼쪽 기준선이 서로 어긋났다.
            // 라벨을 좌측 상단 고정 마진으로 재배치하고, 카드 그리드의 왼쪽 패딩도 같은 마진으로
            // 맞춰서 카드가 라벨 바로 아래, 같은 왼쪽 기준선에서 시작하도록 한다.
            const float labelMargin = 16f;
            var labelRt = host.Find("Text (TMP)") as RectTransform;
            if (labelRt != null)
            {
                labelRt.anchorMin = new Vector2(0f, 1f);
                labelRt.anchorMax = new Vector2(0f, 1f);
                labelRt.pivot = new Vector2(0f, 1f);
                labelRt.anchoredPosition = new Vector2(labelMargin, -labelMargin);
            }

            var gridLayout = inventoryContent != null ? inventoryContent.GetComponent<GridLayoutGroup>() : null;
            if (gridLayout != null)
                gridLayout.padding = new RectOffset((int)labelMargin, 4, 4, 4);

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

        static Sprite cachedPlusSprite;
        static Sprite cachedMinusSprite;

        static void BindSlotComponent(UITradeInventorySlot slot, Transform root)
        {
            slot.icon = root.Find("Potion_Icon")?.GetComponent<Image>();
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

            ApplyButtonIcon(slot.plusButton, ref cachedPlusSprite, "Plus", plusText);
            ApplyButtonIcon(slot.minusButton, ref cachedMinusSprite, "Minus", minusText);
        }

        /// <summary>
        /// Plus/Minus 버튼이 기본 유니티 UI 스프라이트 + "+"/"-" 텍스트로만 되어 있어서, 리소스의
        /// 실제 아이콘 이미지로 교체한다. 아이콘 자체에 기호가 그려져 있으므로 중복되지 않게 텍스트는 숨긴다.
        /// </summary>
        static void ApplyButtonIcon(Button button, ref Sprite cache, string resourceName, TMP_Text label)
        {
            if (button == null) return;
            if (cache == null) cache = Resources.Load<Sprite>(resourceName);
            if (cache == null) return;

            var image = button.GetComponent<Image>();
            if (image == null) return;

            image.sprite = cache;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            if (label != null) label.gameObject.SetActive(false);
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

                if (pendingSellAmounts.TryGetValue(potion.potionID, out int savedAmount) && savedAmount > 0)
                    slot.SetSelectedAmount(savedAmount);

                inventorySlots.Add(slot);
            }

            // 보유 화물이 적어 줄이 휑해 보이지 않도록, 패널을 스크롤 없이 채울 수 있는 만큼 빈 칸을 더 넣는다.
            // (2행 그리드이므로 한 열에 카드가 2장씩 들어간다.)
            // 뷰포트(inventoryContent.parent)가 이 프레임에 막 생성/활성화됐으면 rect.width가 아직 0으로
            // 읽혀 ComputeFillSlotCount가 0을 반환 - 그러면 보유 포션이 하나도 없을 때 빈 슬롯이 하나도
            // 안 채워져서 화면이 통째로 비어 보였다. 계산 직전에 강제로 레이아웃을 갱신해 정확한 폭을 읽는다.
            Canvas.ForceUpdateCanvases();
            var templateRt = inventorySlotTemplate as RectTransform;
            float cardWidth = templateRt != null ? templateRt.sizeDelta.x : 0f;
            int targetCount = UIScrollListFactory.ComputeFillSlotCount(inventoryContent, cardWidth, 14f) * InventoryRowCount;
            for (int i = inventorySlots.Count; i < targetCount; i++)
                emptyCargoSlots.Add(CreateEmptyCargoSlot());

            // 카드 줄은 라벨과 같은 왼쪽 기준선(그리드 왼쪽 패딩 = 라벨 마진)에서 시작해야 하므로
            // 더 이상 뷰포트 가운데로 재정렬하지 않는다(라벨 바로 아래, 왼쪽부터 채워지는 배치 유지).

            UIScrollListFactory.ApplyNotoSansFont(this);
        }

        /// <summary>
        /// 실제 화물 슬롯 템플릿을 복제해서 Plus/Minus/텍스트를 하나하나 지우는 대신, 제조 패널의
        /// 보유 식물 인벤토리(CreateEmptyInventorySlot)와 똑같이 빈 칸 채우기 전용 오브젝트를 코드로
        /// 새로 만든다. Inventory_Slot_01 템플릿 구조(Potion_Icon 이름 등)에 기대는 게 하나도 없어서
        /// 씬 쪽 이름이 바뀌어도 깨지지 않는다.
        /// </summary>
        Transform CreateEmptyCargoSlot()
        {
            var slotGO = new GameObject("Inventory_Slot_Empty", typeof(RectTransform));
            slotGO.transform.SetParent(inventoryContent, false);
            slotGO.layer = 5;

            var bg = slotGO.AddComponent<Image>();
            bg.sprite = CharacterEquipManager.GetEmptySlotIcon();
            bg.color = bg.sprite != null ? Color.white : new Color(0.16f, 0.16f, 0.2f, 0.4f);
            bg.preserveAspect = true;

            return slotGO.transform;
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
            {
                pendingSellAmounts[slot.PotionID] = slot.SelectedAmount;
                RefreshSellInfo();
            }
        }

        void OnSlotMinusClicked(UITradeInventorySlot slot)
        {
            if (slot.TryDecrease())
            {
                if (slot.SelectedAmount > 0) pendingSellAmounts[slot.PotionID] = slot.SelectedAmount;
                else pendingSellAmounts.Remove(slot.PotionID);
                RefreshSellInfo();
            }
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

        List<(int potionId, int amount)> BuildCargoFromSelection()
        {
            var cargo = new List<(int potionId, int amount)>();
            foreach (var slot in inventorySlots)
            {
                if (slot.SelectedAmount > 0)
                    cargo.Add((slot.PotionID, slot.SelectedAmount));
            }
            return cargo;
        }

        void OnSellClicked()
        {
            if (selectedShipIndex < 0 || selectedRegionId <= 0 || TradeManager.Instance == null) return;

            var cargo = BuildCargoFromSelection();
            if (cargo.Count == 0) return;

            bool started = TradeManager.Instance.StartVoyage(selectedShipIndex, selectedRegionId, cargo);
            if (!started) return;

            foreach (var (potionId, _) in cargo)
                pendingSellAmounts.Remove(potionId);

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

            if (selectedRegionId > 0 && TradeManager.Instance != null)
            {
                var region = DataManager.Instance?.GetRegionByID(selectedRegionId);
                if (region != null)
                {
                    // 화물을 아직 안 골랐어도 지역 기본 시간을, 골랐으면 캐릭터 보너스가 반영된
                    // 실제 예상 시간을 보여준다(연구소/공장 미리보기와 동일한 패턴).
                    var cargo = BuildCargoFromSelection();
                    float previewSeconds = cargo.Count > 0
                        ? TradeManager.Instance.GetVoyageTimeSeconds(cargo, selectedRegionId)
                        : region.sellTimeSeconds;

                    int minutes = Mathf.FloorToInt(previewSeconds / 60f);
                    int seconds = Mathf.FloorToInt(previewSeconds % 60f);
                    timeText.text = $"예상 {minutes:D2}:{seconds:D2}";
                    return;
                }
            }

            timeText.text = "--:--:--";
        }
    }
}

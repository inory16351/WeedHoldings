using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WeedHoldings
{
    public class UILobbyNavigation : MonoBehaviour
    {
        [Header("메인 로비 버튼")]
        public Button farmButton;
        public Button potionButton;
        public Button sellButton;
        public Button laboratoryButton;

        [Header("탑 패널")]
        public Button backButton;
        public GameObject navButtonsContainer;
        public Button farmNavButton;
        public Button potionNavButton;
        public Button sellNavButton;
        public Button laboratoryNavButton;

        [Header("UI 패널")]
        public GameObject farmPanel;
        public GameObject potionPanel;
        public GameObject sellPanel;
        public GameObject laboratoryPanel;

        [Header("캐릭터/뽑기 (씬에서 직접 배치 후 연결)")]
        public Button characterButton;
        public Button gachaButton;
        public Button characterNavButton;
        public Button gachaNavButton;
        public GameObject characterPanel;
        public GameObject gachaPanel;

        GameObject[] panels;
        Button[] navButtons;
        Button[] lobbyButtons;
        GameObject mainPanel;
        Color normalColor = Color.white;
        Color activeColor = new Color(0.5f, 0.5f, 0.5f, 1);

        void Start()
        {
            // 메인 로비 버튼(농장/공장/무역소/연구실)이 담긴 최상위 "Panel". 서브 패널이 열려 있는 동안
            // 인터랙션만 막고 화면에는 계속 남아있어서 서브 패널 뒤로 비쳐 보이는 문제가 있었다.
            mainPanel = transform.Find("Panel")?.gameObject;

            EnsureManager<CultivationManager>();
            EnsureManager<LabUpgradeManager>();
            EnsureManager<PotionCraftManager>();
            EnsureManager<FactoryUpgradeManager>();
            EnsureManager<TradeManager>();
            EnsureManager<SellUpgradeManager>();
            SetupLabWidgets();
            SetupFactoryWidgets();
            SetupSellWidgets();

            panels = new GameObject[] { farmPanel, potionPanel, sellPanel, laboratoryPanel, characterPanel, gachaPanel };
            navButtons = new Button[] { farmNavButton, potionNavButton, sellNavButton, laboratoryNavButton, characterNavButton, gachaNavButton };
            lobbyButtons = new Button[] { farmButton, potionButton, sellButton, laboratoryButton, characterButton, gachaButton };

            // Add CanvasGroup to each panel for raycast blocking when inactive
            foreach (var p in panels)
            {
                if (p != null)
                {
                    var cg = p.GetComponent<CanvasGroup>();
                    if (cg == null) cg = p.AddComponent<CanvasGroup>();
                }
            }

            if (backButton != null)
                backButton.onClick.AddListener(OnBack);

            if (farmButton != null)
                farmButton.onClick.AddListener(() => ShowPanel(0));
            if (potionButton != null)
                potionButton.onClick.AddListener(() => ShowPanel(1));
            if (sellButton != null)
                sellButton.onClick.AddListener(() => ShowPanel(2));
            if (laboratoryButton != null)
                laboratoryButton.onClick.AddListener(() => ShowPanel(3));
            if (characterButton != null)
                characterButton.onClick.AddListener(() => ShowPanel(4));
            if (gachaButton != null)
                gachaButton.onClick.AddListener(() => ShowPanel(5));

            if (farmNavButton != null)
                farmNavButton.onClick.AddListener(() => ShowPanel(0));
            if (potionNavButton != null)
                potionNavButton.onClick.AddListener(() => ShowPanel(1));
            if (sellNavButton != null)
                sellNavButton.onClick.AddListener(() => ShowPanel(2));
            if (laboratoryNavButton != null)
                laboratoryNavButton.onClick.AddListener(() => ShowPanel(3));
            if (characterNavButton != null)
                characterNavButton.onClick.AddListener(() => ShowPanel(4));
            if (gachaNavButton != null)
                gachaNavButton.onClick.AddListener(() => ShowPanel(5));

            OnBack();
        }

        static T EnsureManager<T>() where T : Component
        {
            var existing = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (existing != null) return existing;

            var go = new GameObject(typeof(T).Name);
            return go.AddComponent<T>();
        }

        void SetupLabWidgets()
        {
            if (laboratoryPanel != null)
            {
                var farmUpgradePanel = laboratoryPanel.transform.Find("Upgrade/Farm_Upgrade_Panel");
                if (farmUpgradePanel != null && farmUpgradePanel.GetComponent<UIFarmUpgradeWidget>() == null)
                    farmUpgradePanel.gameObject.AddComponent<UIFarmUpgradeWidget>();

                var factoryUpgradePanel = laboratoryPanel.transform.Find("Upgrade/Factory_Upgrade_Panel");
                if (factoryUpgradePanel != null && factoryUpgradePanel.GetComponent<UIFactoryUpgradeWidget>() == null)
                    factoryUpgradePanel.gameObject.AddComponent<UIFactoryUpgradeWidget>();

                var sellUpgradePanel = laboratoryPanel.transform.Find("Upgrade/Sell_Upgrade_Panel");
                if (sellUpgradePanel != null && sellUpgradePanel.GetComponent<UISellUpgradeWidget>() == null)
                    sellUpgradePanel.gameObject.AddComponent<UISellUpgradeWidget>();

                // 세 업그레이드 패널(농장/공장/무역소)이 좌측에 동일한 X, 균일한 간격으로 나란히 오도록 정렬.
                AlignUpgradePanelsVertically(
                    farmUpgradePanel as RectTransform,
                    factoryUpgradePanel as RectTransform,
                    sellUpgradePanel as RectTransform);

                CreateCornerLabel(farmUpgradePanel, "농장");
                CreateCornerLabel(factoryUpgradePanel, "공장");
                CreateCornerLabel(sellUpgradePanel, "무역소");

                var unlockList = laboratoryPanel.transform.Find("Plant_Unlock/Plant_Unlock_List");
                if (unlockList != null && unlockList.GetComponent<UIPlantUnlockPanel>() == null)
                    unlockList.gameObject.AddComponent<UIPlantUnlockPanel>();

                var potionUnlockList = laboratoryPanel.transform.Find("Potion_Unlock/Potion_Unlock_List");
                if (potionUnlockList != null && potionUnlockList.GetComponent<UIPotionUnlockPanel>() == null)
                    potionUnlockList.gameObject.AddComponent<UIPotionUnlockPanel>();
            }

            if (farmPanel != null && farmPanel.GetComponent<UIBulkActionWidget>() == null)
                farmPanel.AddComponent<UIBulkActionWidget>();

            if (farmPanel != null && farmPanel.GetComponent<UIFieldUnlockWidget>() == null)
                farmPanel.AddComponent<UIFieldUnlockWidget>();

            if (farmPanel != null && farmPanel.GetComponent<UIFieldPurchasePopup>() == null)
                farmPanel.AddComponent<UIFieldPurchasePopup>();

            var witherWarning = GetComponent<UIGlobalWitherWarning>();
            if (witherWarning == null) witherWarning = gameObject.AddComponent<UIGlobalWitherWarning>();
            witherWarning.farmPanel = farmPanel;
        }

        /// <summary>
        /// 세 업그레이드 패널을 동일한 X좌표, 동일한 세로 간격으로 왼쪽에 나란히 배치한다.
        /// 표시 순서(위->아래)는 기존 배치 순서(농장/공장/무역소)를 그대로 유지한다.
        /// </summary>
        static void AlignUpgradePanelsVertically(RectTransform farm, RectTransform factory, RectTransform sell)
        {
            var panels = new[] { farm, factory, sell };
            if (panels.Any(p => p == null)) return;

            const float x = -318f;
            const float spacing = 110f;
            const float centerY = -53f;

            float[] ys = { centerY + spacing, centerY, centerY - spacing };
            for (int i = 0; i < panels.Length; i++)
                panels[i].anchoredPosition = new Vector2(x, ys[i]);
        }

        /// <summary>업그레이드 패널 좌측 상단(이미지 위쪽)에 작은 이름표를 붙인다.</summary>
        static void CreateCornerLabel(Transform panel, string text)
        {
            if (panel == null || panel.Find("Panel_Title") != null) return;

            var go = new GameObject("Panel_Title", typeof(RectTransform));
            go.transform.SetParent(panel, false);
            go.layer = 5;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(2f, 2f);
            rt.sizeDelta = new Vector2(90f, 26f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.color = Color.white;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 10f;
            tmp.fontSizeMax = 16f;

            var font = UIScrollListFactory.ResolveNotoSansFont();
            if (font != null) tmp.font = font;
        }

        void SetupFactoryWidgets()
        {
            if (potionPanel == null) return;

            var factoryPanel = potionPanel.transform.Find("Factory_Panel");
            if (factoryPanel != null && factoryPanel.GetComponent<UIFactoryPanelController>() == null)
                factoryPanel.gameObject.AddComponent<UIFactoryPanelController>();

            var trackPanel = potionPanel.transform.Find("Track_Panel");
            if (trackPanel != null)
            {
                for (int i = 0; i < trackPanel.childCount; i++)
                {
                    var track = trackPanel.GetChild(i);
                    var widget = track.GetComponent<UITrackWidget>();
                    if (widget == null) widget = track.gameObject.AddComponent<UITrackWidget>();
                    widget.trackIndex = i; // Track_01 -> 0, Track_02 -> 1, ...
                }
            }
        }

        void SetupSellWidgets()
        {
            if (sellPanel != null && sellPanel.GetComponent<UISellPanelController>() == null)
                sellPanel.AddComponent<UISellPanelController>();
        }

        void OnBack()
        {
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] != null)
                {
                    panels[i].SetActive(false);
                    var cg = panels[i].GetComponent<CanvasGroup>();
                    if (cg != null) cg.blocksRaycasts = false;
                }
            }
            UpdateNavButtonColors(-1);
            if (navButtonsContainer != null)
                navButtonsContainer.SetActive(false);

            // 로비로 돌아왔을 때만 메인 패널(농장/공장/무역소/연구실 버튼)을 다시 보여준다.
            if (mainPanel != null)
                mainPanel.SetActive(true);

            // Clear plant selection when going back to lobby
            if (PlantSelectionManager.Instance != null)
                PlantSelectionManager.Instance.ClearSelection();

            // Re-enable lobby buttons when all panels closed
            SetLobbyButtonsInteractable(true);
        }

        void ShowPanel(int index)
        {
            // Clear plant selection when switching panels
            if (PlantSelectionManager.Instance != null)
                PlantSelectionManager.Instance.ClearSelection();

            for (int i = 0; i < panels.Length; i++)
            {
                bool active = i == index;
                if (panels[i] != null)
                {
                    panels[i].SetActive(active);
                    var cg = panels[i].GetComponent<CanvasGroup>();
                    if (cg != null) cg.blocksRaycasts = active;
                }
            }
            UpdateNavButtonColors(index);
            if (navButtonsContainer != null)
                navButtonsContainer.SetActive(true);

            // 서브 패널이 열려 있는 동안은 뒤에 메인 패널(로비 버튼들)이 비쳐 보이지 않도록 완전히 숨긴다.
            if (mainPanel != null)
                mainPanel.SetActive(false);

            // Disable lobby buttons when any panel is open
            SetLobbyButtonsInteractable(false);
        }

        void SetLobbyButtonsInteractable(bool interactable)
        {
            foreach (var btn in lobbyButtons)
            {
                if (btn != null) btn.interactable = interactable;
            }
        }

        void UpdateNavButtonColors(int activeIndex)
        {
            for (int i = 0; i < navButtons.Length; i++)
            {
                if (navButtons[i] == null) continue;

                Color target = (i == activeIndex) ? activeColor : normalColor;
                ColorBlock cb = navButtons[i].colors;
                cb.normalColor = target;
                navButtons[i].colors = cb;

                // Selectable은 다음 포인터 상태 전환(호버/클릭 해제 등)이 일어나기 전까지 normalColor 변경분을
                // 화면에 반영하지 않는다. 그래서 패널은 클릭 즉시 바뀌는데 버튼 하이라이트 색은 한 박자
                // 늦게(버튼을 다시 눌러야) 바뀌는 것처럼 보였다. 그래픽 색을 직접 즉시 적용해서 해결한다.
                if (navButtons[i].targetGraphic != null)
                    navButtons[i].targetGraphic.color = target;
            }
        }
    }
}

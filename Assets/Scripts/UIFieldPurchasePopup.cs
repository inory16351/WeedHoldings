using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WeedHoldings
{
    /// <summary>
    /// FarmPanel에 부착되는 밭 칸 구매 확인 팝업. 구매 가능한(잠긴) 밭을 클릭하면 표시되고,
    /// '예'를 누르면 FieldPlot.ConfirmPurchase()로 실제 구매를 진행한다.
    /// </summary>
    public class UIFieldPurchasePopup : MonoBehaviour
    {
        public static UIFieldPurchasePopup Instance { get; private set; }

        GameObject dialogRoot;
        TMP_Text priceText;
        FieldPlot pendingPlot;

        void Awake()
        {
            Instance = this;
            BuildUI();
            dialogRoot.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void BuildUI()
        {
            var blockerGO = new GameObject("Field_Purchase_Popup", typeof(RectTransform));
            blockerGO.transform.SetParent(transform, false);
            blockerGO.layer = 5;
            dialogRoot = blockerGO;

            var blockerRt = blockerGO.GetComponent<RectTransform>();
            blockerRt.anchorMin = Vector2.zero;
            blockerRt.anchorMax = Vector2.one;
            blockerRt.offsetMin = Vector2.zero;
            blockerRt.offsetMax = Vector2.zero;

            var blockerImg = blockerGO.AddComponent<Image>();
            blockerImg.color = new Color(0f, 0f, 0f, 0.6f);
            blockerImg.raycastTarget = true; // 뒤쪽 클릭 차단

            var boxGO = new GameObject("Box", typeof(RectTransform));
            boxGO.transform.SetParent(blockerGO.transform, false);
            boxGO.layer = 5;
            var boxRt = boxGO.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.5f, 0.5f);
            boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.pivot = new Vector2(0.5f, 0.5f);
            boxRt.sizeDelta = new Vector2(380f, 220f);
            boxRt.anchoredPosition = Vector2.zero;

            var boxImg = boxGO.AddComponent<Image>();
            boxImg.color = new Color(0.14f, 0.14f, 0.17f, 0.97f);

            var layout = boxGO.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // 질문 텍스트
            var questionGO = new GameObject("Question", typeof(RectTransform));
            questionGO.transform.SetParent(boxGO.transform, false);
            questionGO.layer = 5;
            questionGO.AddComponent<LayoutElement>().preferredHeight = 40f;
            var questionText = questionGO.AddComponent<TextMeshProUGUI>();
            questionText.text = "구매하시겠습니까?";
            questionText.alignment = TextAlignmentOptions.Center;
            questionText.fontSize = 22;
            questionText.color = Color.white;

            // 질문과 버튼 사이의 가격 텍스트
            var priceGO = new GameObject("Price", typeof(RectTransform));
            priceGO.transform.SetParent(boxGO.transform, false);
            priceGO.layer = 5;
            priceGO.AddComponent<LayoutElement>().preferredHeight = 32f;
            priceText = priceGO.AddComponent<TextMeshProUGUI>();
            priceText.alignment = TextAlignmentOptions.Center;
            priceText.fontSize = 20;
            priceText.color = new Color(1f, 0.85f, 0.3f);

            // 예 / 아니오 버튼
            var buttonRowGO = new GameObject("Buttons", typeof(RectTransform));
            buttonRowGO.transform.SetParent(boxGO.transform, false);
            buttonRowGO.layer = 5;
            buttonRowGO.AddComponent<LayoutElement>().preferredHeight = 56f;
            var rowLayout = buttonRowGO.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 16f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;

            CreateDialogButton(buttonRowGO.transform, "예", new Color(0.25f, 0.55f, 0.3f, 1f)).onClick.AddListener(OnYesClicked);
            CreateDialogButton(buttonRowGO.transform, "아니오", new Color(0.5f, 0.2f, 0.2f, 1f)).onClick.AddListener(OnNoClicked);

            UIScrollListFactory.ApplyNotoSansFont(this);
        }

        static Button CreateDialogButton(Transform parent, string label, Color color)
        {
            var go = new GameObject(label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = 5;

            var img = go.AddComponent<Image>();
            img.color = color;

            var button = go.AddComponent<Button>();
            button.targetGraphic = img;

            var textGO = new GameObject("Text (TMP)", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            textGO.layer = 5;
            var rt = textGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 20;
            text.color = Color.white;

            return button;
        }

        public void Show(FieldPlot plot, int cost)
        {
            pendingPlot = plot;
            if (priceText != null) priceText.text = $"{cost:N0}G";
            dialogRoot.SetActive(true);
            // 이 컴포넌트는 FarmPanel 자신에 붙어있어서(UILobbyNavigation.SetupLabWidgets 참고) "transform"이
            // 곧 FarmPanel의 트랜스폼이다. 예전엔 여기서 SetAsLastSibling()을 호출해서 FarmPanel 자신이
            // Canvas의 형제들(Top_Panel 포함) 맨 뒤로 옮겨져 상단바가 가려져 버렸다. 팝업을 다른 요소들
            // 위로 띄우려던 의도였으므로, FarmPanel이 아니라 팝업 자신(dialogRoot, FarmPanel의 자식)만
            // FarmPanel 안에서 맨 위로 옮긴다.
            dialogRoot.transform.SetAsLastSibling();
        }

        void Hide()
        {
            dialogRoot.SetActive(false);
            pendingPlot = null;
        }

        void OnYesClicked()
        {
            pendingPlot?.ConfirmPurchase();
            Hide();
        }

        void OnNoClicked()
        {
            Hide();
        }
    }
}

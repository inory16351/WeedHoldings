using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WeedHoldings
{
    /// <summary>
    /// GachaPanel의 내부 화면 전환 담당: Main(뽑기 메인) -> Gacha_Panel_Child(연출, 클릭 유도) ->
    /// Result_1_Panel(1회 결과) 또는 Result_10_Panel(10회 결과, 카드 10장). Gacha_Panel_Child/결과 패널에
    /// 있는 동안은 상단 Top_Panel을 숨겨서 로비 네비게이션을 가린다. 캐릭터 등장 확률은 Result_Button을
    /// 누르는 순간 전부 확정되고, 이후 카드를 클릭하면 확정된 결과를 뒤집기 연출로 공개한다.
    /// </summary>
    public class UIGachaController : MonoBehaviour
    {
        const int ResultCount = 10;
        // 캐릭터는 최종적으로 골드의 주 소모처가 되도록 뽑기 가격을 3배로 올렸다(느린 장기 진행 목표).
        const int SinglePullCost = 30000;
        const int MultiPullCost = 270000;

        Transform mainPanel;
        Transform gachaPanelChild;
        Transform result1Panel;
        Transform result10Panel;
        Transform topPanel;

        Button gacha1Button;
        Button gacha10Button;
        Button resultButton;
        Image resultButtonImage;
        Button back1Button;
        Button back10Button;

        UIGachaResultCard resultCard1;
        readonly UIGachaResultCard[] resultCards10 = new UIGachaResultCard[ResultCount];

        bool pendingIsMulti;
        int revealedCount1;
        int revealedCount10;

        void Awake()
        {
            mainPanel = transform.Find("Main");
            gachaPanelChild = transform.Find("Gacha_Panel_Child");
            result1Panel = transform.Find("Result_1_Panel");
            result10Panel = transform.Find("Result_10_Panel");
            topPanel = transform.parent != null ? transform.parent.Find("Top_Panel") : null;

            gacha1Button = mainPanel?.Find("Gacha_1")?.GetComponent<Button>();
            gacha10Button = mainPanel?.Find("Gacha_10")?.GetComponent<Button>();
            resultButton = gachaPanelChild?.Find("Result_Button")?.GetComponent<Button>();
            resultButtonImage = resultButton?.GetComponent<Image>();
            // 배 이미지가 다 채워주는 자리라 기본 버튼 라벨("Button")은 필요 없다.
            var resultButtonLabel = resultButton?.transform.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            if (resultButtonLabel != null) resultButtonLabel.text = "";
            back1Button = result1Panel?.Find("Back (1)")?.GetComponent<Button>();
            back10Button = result10Panel?.Find("Back")?.GetComponent<Button>();

            SetupPullButtonLabel(gacha1Button, "1회 뽑기", SinglePullCost);
            SetupPullButtonLabel(gacha10Button, "10회 뽑기", MultiPullCost);
            MatchButtonSize(gacha1Button, gacha10Button);

            if (result1Panel != null)
            {
                var slot = result1Panel.Find("Char_Result");
                if (slot != null)
                {
                    resultCard1 = slot.GetComponent<UIGachaResultCard>();
                    if (resultCard1 == null) resultCard1 = slot.gameObject.AddComponent<UIGachaResultCard>();
                    resultCard1.OnRevealed = OnCard1Revealed;
                }
            }

            if (result10Panel != null)
            {
                for (int i = 0; i < ResultCount; i++)
                {
                    var slot = result10Panel.Find($"Char_Result_{(i + 1):D2}");
                    if (slot == null) continue;

                    var card = slot.GetComponent<UIGachaResultCard>();
                    if (card == null) card = slot.gameObject.AddComponent<UIGachaResultCard>();
                    card.OnRevealed = OnCard10Revealed;
                    resultCards10[i] = card;
                }
            }

            gacha1Button?.onClick.AddListener(OnGacha1Clicked);
            gacha10Button?.onClick.AddListener(OnGacha10Clicked);
            resultButton?.onClick.AddListener(OnResultButtonClicked);
            back1Button?.onClick.AddListener(OnBackClicked);
            back10Button?.onClick.AddListener(OnBackClicked);
        }

        /// <summary>버튼 라벨을 "1회 뽑기 / G 10,000" 형태로 채우고 한글 깨짐이 없도록 노토산스를 적용한다.</summary>
        static void SetupPullButtonLabel(Button button, string title, int cost)
        {
            if (button == null) return;

            var label = button.transform.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            if (label == null) return;

            label.text = $"{title}\nG {cost:N0}";

            var notoSans = UIScrollListFactory.ResolveNotoSansFont();
            if (notoSans != null) label.font = notoSans;
        }

        /// <summary>1회/10회 버튼 크기와 세로 위치를 통일한다(가로 위치·간격은 유지).</summary>
        static void MatchButtonSize(Button a, Button b)
        {
            var rectA = a?.transform as RectTransform;
            var rectB = b?.transform as RectTransform;
            if (rectA == null || rectB == null) return;

            var size = new Vector2(
                Mathf.Max(rectA.sizeDelta.x, rectB.sizeDelta.x),
                Mathf.Max(rectA.sizeDelta.y, rectB.sizeDelta.y));
            rectA.sizeDelta = size;
            rectB.sizeDelta = size;

            float y = (rectA.anchoredPosition.y + rectB.anchoredPosition.y) * 0.5f;
            rectA.anchoredPosition = new Vector2(rectA.anchoredPosition.x, y);
            rectB.anchoredPosition = new Vector2(rectB.anchoredPosition.x, y);
        }

        void OnEnable()
        {
            ShowMain();
        }

        void ShowMain()
        {
            mainPanel?.gameObject.SetActive(true);
            gachaPanelChild?.gameObject.SetActive(false);
            result1Panel?.gameObject.SetActive(false);
            result10Panel?.gameObject.SetActive(false);
            SetTopPanelActive(true);
        }

        void OnGacha1Clicked()
        {
            if (GoldManager.Instance == null || !GoldManager.Instance.SpendGold(SinglePullCost))
            {
                Debug.Log("[Gacha] 골드가 부족합니다.");
                return;
            }

            var results = GachaManager.Instance != null
                ? GachaManager.Instance.RollMultiple(1)
                : new System.Collections.Generic.List<GachaManager.GachaResult>();

            if (resultCard1 != null)
            {
                if (results.Count > 0)
                    resultCard1.SetPendingResult(results[0].character, results[0].isDuplicate);
                resultCard1.ResetToBack();
            }

            ApplyResultShipImage(results);
            pendingIsMulti = false;
            EnterGachaChild();
        }

        void OnGacha10Clicked()
        {
            if (GoldManager.Instance == null || !GoldManager.Instance.SpendGold(MultiPullCost))
            {
                Debug.Log("[Gacha] 골드가 부족합니다.");
                return;
            }

            var results = GachaManager.Instance != null
                ? GachaManager.Instance.RollMultiple(ResultCount)
                : new System.Collections.Generic.List<GachaManager.GachaResult>();

            for (int i = 0; i < resultCards10.Length; i++)
            {
                if (resultCards10[i] == null) continue;
                if (i < results.Count)
                    resultCards10[i].SetPendingResult(results[i].character, results[i].isDuplicate);
                resultCards10[i].ResetToBack();
            }

            ApplyResultShipImage(results);
            pendingIsMulti = true;
            EnterGachaChild();
        }

        /// <summary>Result_Button의 배 이미지를 이번 뽑기 결과 중 가장 높은 등급에 맞춰 바꾼다.</summary>
        void ApplyResultShipImage(System.Collections.Generic.List<GachaManager.GachaResult> results)
        {
            if (resultButtonImage == null) return;

            var ship = GachaManager.GetShipSpriteForBestGrade(results);
            if (ship != null) resultButtonImage.sprite = ship;
        }

        void EnterGachaChild()
        {
            mainPanel?.gameObject.SetActive(false);
            gachaPanelChild?.gameObject.SetActive(true);
            result1Panel?.gameObject.SetActive(false);
            result10Panel?.gameObject.SetActive(false);
            SetTopPanelActive(false);
        }

        void OnResultButtonClicked()
        {
            gachaPanelChild?.gameObject.SetActive(false);
            result1Panel?.gameObject.SetActive(!pendingIsMulti);
            result10Panel?.gameObject.SetActive(pendingIsMulti);
            SetTopPanelActive(false);

            // 카드를 전부 확인하기 전에는 뒤로가기(나가기)를 못 누르게 잠근다.
            revealedCount1 = 0;
            revealedCount10 = 0;
            if (back1Button != null) back1Button.interactable = false;
            if (back10Button != null) back10Button.interactable = false;
        }

        void OnCard1Revealed()
        {
            revealedCount1++;
            if (back1Button != null) back1Button.interactable = revealedCount1 >= 1;
        }

        void OnCard10Revealed()
        {
            revealedCount10++;
            if (back10Button != null) back10Button.interactable = revealedCount10 >= ResultCount;
        }

        void OnBackClicked()
        {
            ShowMain();
        }

        void SetTopPanelActive(bool active)
        {
            if (topPanel != null) topPanel.gameObject.SetActive(active);
        }
    }
}

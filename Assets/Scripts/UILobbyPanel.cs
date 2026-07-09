using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WeedHoldings
{
    /// <summary>
    /// 메인 로비/로딩 화면 제어.
    /// 기획서 page1_img1.jpg의 레이아웃과 씬 전환 흐름을 담당합니다.
    /// </summary>
    public class UILobbyPanel : MonoBehaviour
    {
        public static UILobbyPanel Instance { get; private set; }

        [Header("메인 화면 패널")]
        public GameObject lobbyPanel;
        public GameObject farmPanel;
        public GameObject labPanel;

        [Header("로비 배너 버튼")]
        public Button farmButton;
        public Button labButton;
        public Button factoryButton;    // 제조 (준비중)
        public Button marketButton;     // 판매 (준비중)

        [Header("각 화면의 뒤로가기 버튼")]
        public Button farmBackButton;
        public Button labBackButton;

        [Header("알림창 (준비중 안내용)")]
        public GameObject alertWindow;
        public TextMeshProUGUI alertText;
        public Button alertCloseButton;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            // 런타임 참조 유실 대비 복구 탐색 (비활성 패널 자식 탐색)
            if (farmBackButton == null && farmPanel != null)
            {
                farmBackButton = farmPanel.GetComponentInChildren<Button>(true);
                // "BackButton" 또는 "FarmBackButton" 이름 매칭 확인
                foreach (var btn in farmPanel.GetComponentsInChildren<Button>(true))
                {
                    if (btn.gameObject.name == "FarmBackButton" || btn.gameObject.name == "BackButton")
                    {
                        farmBackButton = btn;
                        break;
                    }
                }
            }

            if (labBackButton == null && labPanel != null)
            {
                foreach (var btn in labPanel.GetComponentsInChildren<Button>(true))
                {
                    if (btn.gameObject.name == "LabBackButton" || btn.gameObject.name == "BackButton")
                    {
                        labBackButton = btn;
                        break;
                    }
                }
            }

            if (alertWindow != null && (alertText == null || alertCloseButton == null))
            {
                if (alertText == null) alertText = alertWindow.GetComponentInChildren<TextMeshProUGUI>(true);
                if (alertCloseButton == null) alertCloseButton = alertWindow.GetComponentInChildren<Button>(true);
            }

            // 배너 이벤트 바인딩
            if (farmButton != null) farmButton.onClick.AddListener(() => OpenPanel(farmPanel));
            if (labButton != null) labButton.onClick.AddListener(() => OpenPanel(labPanel));
            if (factoryButton != null) factoryButton.onClick.AddListener(() => ShowAlert("제조(공장) 시스템은 다음 업데이트에 추가됩니다!"));
            if (marketButton != null) marketButton.onClick.AddListener(() => ShowAlert("판매(시장) 시스템은 다음 업데이트에 추가됩니다!"));

            // 뒤로가기 이벤트 바인딩
            if (farmBackButton != null)
            {
                farmBackButton.onClick.RemoveAllListeners();
                farmBackButton.onClick.AddListener(BackToLobby);
            }
            if (labBackButton != null)
            {
                labBackButton.onClick.RemoveAllListeners();
                labBackButton.onClick.AddListener(BackToLobby);
            }

            if (alertCloseButton != null)
            {
                alertCloseButton.onClick.RemoveAllListeners();
                alertCloseButton.onClick.AddListener(HideAlert);
            }

            // 초기 상태 설정: 로비만 켜기
            BackToLobby();
        }

        public void OpenPanel(GameObject panel)
        {
            if (lobbyPanel != null) lobbyPanel.SetActive(false);
            if (farmPanel != null) farmPanel.SetActive(false);
            if (labPanel != null) labPanel.SetActive(false);

            if (panel != null) panel.SetActive(true);
        }

        public void BackToLobby()
        {
            if (farmPanel != null) farmPanel.SetActive(false);
            if (labPanel != null) labPanel.SetActive(false);
            if (lobbyPanel != null) lobbyPanel.SetActive(true);
        }

        void ShowAlert(string msg)
        {
            if (alertText != null) alertText.text = msg;
            if (alertWindow != null) alertWindow.SetActive(true);
        }

        void HideAlert()
        {
            if (alertWindow != null) alertWindow.SetActive(false);
        }
    }
}

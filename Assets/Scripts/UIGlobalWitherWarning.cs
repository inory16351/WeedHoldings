using UnityEngine;
using UnityEngine.UI;

namespace WeedHoldings
{
    /// <summary>
    /// Canvas 최상단에 부착. 농장 패널이 아닌 다른 화면을 보고 있는 동안 시들음(Withering) 상태인
    /// 식물이 하나라도 있으면 전체 화면에 빨간색 점멸 경고를 띄운다. 농장 패널로 돌아가거나
    /// 시들음 상태인 식물이 전부 사라지면 자동으로 꺼진다.
    /// </summary>
    public class UIGlobalWitherWarning : MonoBehaviour
    {
        public GameObject farmPanel;

        Image overlay;
        float blinkTimer;

        void Awake()
        {
            BuildOverlay();
        }

        void BuildOverlay()
        {
            var go = new GameObject("Global_Wither_Warning", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            go.layer = 5;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            overlay = go.AddComponent<Image>();
            overlay.color = new Color(1f, 0f, 0f, 0f);
            overlay.raycastTarget = false; // 아래쪽 버튼 클릭을 막지 않는다

            go.transform.SetAsLastSibling(); // 모든 패널 위에 그려지도록 항상 맨 마지막 자식으로
            go.SetActive(false);
        }

        void Update()
        {
            bool farmActive = farmPanel != null && farmPanel.activeInHierarchy;
            bool anyWithering = CultivationManager.Instance != null && CultivationManager.Instance.WitheringCount > 0;
            bool shouldShow = anyWithering && !farmActive;

            if (!shouldShow)
            {
                if (overlay.gameObject.activeSelf)
                {
                    overlay.gameObject.SetActive(false);
                    blinkTimer = 0f;
                    SfxManager.SetLooping("Siren01", false);
                }
                return;
            }

            if (!overlay.gameObject.activeSelf)
            {
                overlay.gameObject.SetActive(true);
                SfxManager.SetLooping("Siren01", true);
            }

            blinkTimer += Time.deltaTime;
            float alpha = Mathf.PingPong(blinkTimer * 2f, 0.35f);
            var c = overlay.color;
            c.a = alpha;
            overlay.color = c;
        }
    }
}

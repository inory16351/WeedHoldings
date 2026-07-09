using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WeedHoldings
{
    /// <summary>
    /// 상단 탭 (농장 / 연구실) 전환 관리
    /// </summary>
    public class UITabManager : MonoBehaviour
    {
        [System.Serializable]
        public class Tab
        {
            public string    tabName;
            public Button    tabButton;
            public GameObject panel;
        }

        public List<Tab> tabs = new List<Tab>();

        [Header("탭 색상")]
        public Color activeColor   = new Color(0.25f, 0.25f, 0.25f);
        public Color inactiveColor = new Color(0.80f, 0.80f, 0.80f);

        int currentTabIndex = 0;

        void Start()
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                int idx = i;
                if (tabs[i].tabButton != null)
                    tabs[i].tabButton.onClick.AddListener(() => SwitchTab(idx));
            }
            SwitchTab(0);
        }

        public void SwitchTab(int index)
        {
            if (index < 0 || index >= tabs.Count) return;
            currentTabIndex = index;

            for (int i = 0; i < tabs.Count; i++)
            {
                bool active = (i == index);

                if (tabs[i].panel != null)
                    tabs[i].panel.SetActive(active);

                if (tabs[i].tabButton != null)
                {
                    var img = tabs[i].tabButton.GetComponent<Image>();
                    if (img != null) img.color = active ? activeColor : inactiveColor;

                    var txt = tabs[i].tabButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null) txt.color = active ? Color.white : Color.black;
                }
            }
        }
    }
}

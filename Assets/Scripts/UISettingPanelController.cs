using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WeedHoldings
{
    /// <summary>
    /// 상단바에 이미 있던(비어 있던) "Setting" 버튼을 눌렀을 때 해상도/사운드 볼륨을 조절하는
    /// 팝업을 띄운다. 팝업 UI 자체는 씬에 미리 만들어져 있지 않으므로 Awake에서 코드로 생성한다.
    /// PlayerPrefs에 저장해 다음 실행 때도 적용된 해상도/볼륨을 그대로 불러온다.
    /// </summary>
    public class UISettingPanelController : MonoBehaviour
    {
        const string PrefResolutionIndex = "Setting_ResolutionIndex";
        const string PrefVolume = "Setting_Volume";

        List<(int width, int height)> resolutionOptions;
        int selectedResolutionIndex;

        GameObject popupRoot;
        TMP_Dropdown resolutionDropdown;
        Slider volumeSlider;
        TMP_Text volumeValueText;

        void Awake()
        {
            var canvas = FindCanvasRoot();
            if (canvas == null) return;

            var settingButton = FindRecursive(canvas, "Setting")?.GetComponent<Button>();
            if (settingButton == null) return;

            var label = settingButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.enabled = false;

            var settingButtonImage = settingButton.GetComponent<Image>();
            if (settingButtonImage != null)
            {
                settingButtonImage.sprite = Resources.Load<Sprite>("UI/Setting_Button");
                settingButtonImage.preserveAspect = true;
            }

            BuildResolutionOptions();
            ApplySavedSettings();

            settingButton.onClick.AddListener(OpenPopup);
            BuildPopup(canvas);
        }

        static Transform FindCanvasRoot()
        {
            var canvasGO = GameObject.Find("Canvas");
            return canvasGO != null ? canvasGO.transform : null;
        }

        static Transform FindRecursive(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindRecursive(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        void BuildResolutionOptions()
        {
            // 디스플레이가 지원하는 전체 해상도 목록(Screen.resolutions)은 기기마다 수십 개씩 나와서
            // 목록이 지저분해지므로, 실사용 비중이 가장 높은 16:9 해상도 5개만 고정으로 제공한다.
            resolutionOptions = new List<(int, int)>
            {
                (1280, 720), (1366, 768), (1920, 1080), (2560, 1440), (3840, 2160),
            };

            selectedResolutionIndex = resolutionOptions.FindIndex(r => r.width == Screen.width && r.height == Screen.height);
            if (selectedResolutionIndex < 0)
            {
                // 정확히 일치하는 항목이 없으면 현재 해상도와 가장 가까운(면적 차이가 가장 작은) 항목을 고른다.
                long currentArea = (long)Screen.width * Screen.height;
                selectedResolutionIndex = 0;
                long bestDiff = long.MaxValue;
                for (int i = 0; i < resolutionOptions.Count; i++)
                {
                    long diff = System.Math.Abs((long)resolutionOptions[i].width * resolutionOptions[i].height - currentArea);
                    if (diff < bestDiff) { bestDiff = diff; selectedResolutionIndex = i; }
                }
            }
        }

        void ApplySavedSettings()
        {
            float savedVolume = PlayerPrefs.GetFloat(PrefVolume, 1f);
            AudioListener.volume = savedVolume;

            if (PlayerPrefs.HasKey(PrefResolutionIndex))
            {
                int idx = PlayerPrefs.GetInt(PrefResolutionIndex, selectedResolutionIndex);
                if (idx >= 0 && idx < resolutionOptions.Count)
                {
                    selectedResolutionIndex = idx;
                    var (w, h) = resolutionOptions[idx];
                    if (w != Screen.width || h != Screen.height)
                        Screen.SetResolution(w, h, Screen.fullScreenMode);
                }
            }
        }

        void OpenPopup()
        {
            if (resolutionDropdown != null)
                resolutionDropdown.value = selectedResolutionIndex;
            if (volumeSlider != null)
                volumeSlider.value = AudioListener.volume;
            popupRoot.SetActive(true);
        }

        void ClosePopup() => popupRoot.SetActive(false);

        void ApplyAndClose()
        {
            if (resolutionDropdown != null)
            {
                selectedResolutionIndex = resolutionDropdown.value;
                var (w, h) = resolutionOptions[selectedResolutionIndex];
                Screen.SetResolution(w, h, Screen.fullScreenMode);
                PlayerPrefs.SetInt(PrefResolutionIndex, selectedResolutionIndex);
            }
            if (volumeSlider != null)
            {
                AudioListener.volume = volumeSlider.value;
                PlayerPrefs.SetFloat(PrefVolume, volumeSlider.value);
            }
            PlayerPrefs.Save();
            ClosePopup();
        }

        // ---------- 팝업 UI 생성 ----------

        void BuildPopup(Transform canvas)
        {
            popupRoot = new GameObject("Setting_Popup", typeof(RectTransform));
            popupRoot.transform.SetParent(canvas, false);
            popupRoot.transform.SetAsLastSibling(); // 가장 위에 그려지도록
            var popupRect = popupRoot.GetComponent<RectTransform>();
            popupRect.anchorMin = Vector2.zero;
            popupRect.anchorMax = Vector2.one;
            popupRect.offsetMin = Vector2.zero;
            popupRect.offsetMax = Vector2.zero;

            // 반투명 배경 (클릭 시 닫기)
            var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
            dim.transform.SetParent(popupRoot.transform, false);
            var dimRect = dim.GetComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;
            var dimImage = dim.GetComponent<Image>();
            dimImage.color = new Color(0f, 0f, 0f, 0.6f);
            var dimButton = dim.GetComponent<Button>();
            dimButton.transition = Selectable.Transition.None;
            dimButton.onClick.AddListener(ClosePopup);

            // 중앙 패널
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(popupRoot.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(620f, 560f);
            panelRect.anchoredPosition = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.14f, 0.97f);

            var title = CreateText(panel.transform, "설정", 34, FontStyles.Bold,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(480f, 60f));

            var resLabel = CreateText(panel.transform, "해상도", 24, FontStyles.Normal,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(70f, -140f), new Vector2(200f, 40f));
            resLabel.alignment = TextAlignmentOptions.MidlineLeft;

            resolutionDropdown = CreateDropdown(panel.transform, new Vector2(0f, -190f));
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(resolutionOptions.Select(r => $"{r.width} x {r.height}").ToList());

            var volLabel = CreateText(panel.transform, "사운드 볼륨", 24, FontStyles.Normal,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(70f, -280f), new Vector2(200f, 40f));
            volLabel.alignment = TextAlignmentOptions.MidlineLeft;

            volumeSlider = CreateSlider(panel.transform, new Vector2(-50f, -330f));
            volumeSlider.onValueChanged.AddListener(v =>
            {
                AudioListener.volume = v; // 슬라이더를 움직이는 동안 바로 들어보며 조절 가능하게 미리 적용
                if (volumeValueText != null) volumeValueText.text = Mathf.RoundToInt(v * 100f) + "%";
            });

            volumeValueText = CreateText(panel.transform, "100%", 22, FontStyles.Normal,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-50f, -330f), new Vector2(80f, 40f));

            // 슬라이더(대략 -330~-360)와 충분히 떨어지도록 버튼을 패널 하단 여백에 배치한다.
            var applyButton = CreateButton(panel.transform, "적용", new Color(0.2f, 0.55f, 1f, 1f),
                new Vector2(0.5f, 0f), new Vector2(140f, 70f));
            applyButton.onClick.AddListener(ApplyAndClose);

            var closeButton = CreateButton(panel.transform, "닫기", new Color(0.3f, 0.3f, 0.34f, 1f),
                new Vector2(0.5f, 0f), new Vector2(-140f, 70f));
            closeButton.onClick.AddListener(ClosePopup);

            popupRoot.SetActive(false);
        }

        static TMP_Text CreateText(Transform parent, string text, float fontSize, FontStyles style,
            Vector2 anchor, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            // TMP Settings의 기본 폰트가 이미 NeoDGM SDF로 지정돼 있어 별도 할당 없이도 적용된다.
            return tmp;
        }

        static TMP_Dropdown CreateDropdown(Transform parent, Vector2 anchoredPos)
        {
            var go = new GameObject("Dropdown", typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(440f, 56f);
            go.GetComponent<Image>().color = new Color(0.18f, 0.19f, 0.24f, 1f);

            var label = new GameObject("Label", typeof(RectTransform));
            label.transform.SetParent(go.transform, false);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(16f, 4f);
            labelRect.offsetMax = new Vector2(-16f, -4f);
            var labelText = label.AddComponent<TextMeshProUGUI>();
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.color = Color.white;
            labelText.fontSize = 24f;

            var template = BuildDropdownTemplate(go.transform);

            var dropdown = go.GetComponent<TMP_Dropdown>();
            dropdown.captionText = labelText;
            dropdown.template = template;
            dropdown.itemText = template.GetComponentInChildren<TextMeshProUGUI>(true);
            template.gameObject.SetActive(false);
            return dropdown;
        }

        static RectTransform BuildDropdownTemplate(Transform parent)
        {
            var template = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            template.transform.SetParent(parent, false);
            var rect = template.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, 2f);
            rect.sizeDelta = new Vector2(0f, 200f);
            template.GetComponent<Image>().color = new Color(0.14f, 0.15f, 0.2f, 0.98f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(template.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = Color.white;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 40f);

            var item = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            item.transform.SetParent(content.transform, false);
            var itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
            itemRect.sizeDelta = new Vector2(0f, 40f);

            var itemBg = new GameObject("Item Background", typeof(RectTransform), typeof(Image));
            itemBg.transform.SetParent(item.transform, false);
            var itemBgRect = itemBg.GetComponent<RectTransform>();
            itemBgRect.anchorMin = Vector2.zero;
            itemBgRect.anchorMax = Vector2.one;
            itemBgRect.offsetMin = Vector2.zero;
            itemBgRect.offsetMax = Vector2.zero;
            itemBg.GetComponent<Image>().color = new Color(0.25f, 0.45f, 0.85f, 0.5f);

            var itemLabel = new GameObject("Item Label", typeof(RectTransform));
            itemLabel.transform.SetParent(item.transform, false);
            var itemLabelRect = itemLabel.GetComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(16f, 2f);
            itemLabelRect.offsetMax = new Vector2(-16f, -2f);
            var itemLabelText = itemLabel.AddComponent<TextMeshProUGUI>();
            itemLabelText.alignment = TextAlignmentOptions.MidlineLeft;
            itemLabelText.color = Color.white;
            itemLabelText.fontSize = 22f;

            var toggle = item.GetComponent<Toggle>();
            toggle.targetGraphic = itemBg.GetComponent<Image>();
            toggle.graphic = itemBg.GetComponent<Image>();

            var scrollRect = template.GetComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;

            return rect;
        }

        static Slider CreateSlider(Transform parent, Vector2 anchoredPos)
        {
            var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(-160f, 30f);

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.5f);
            bgRect.anchorMax = new Vector2(1f, 0.5f);
            bgRect.sizeDelta = new Vector2(0f, 12f);
            bg.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f, 1f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
            fillAreaRect.sizeDelta = new Vector2(0f, 12f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = new Color(0.2f, 0.55f, 1f, 1f);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            var handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = new Vector2(0f, 0.5f);
            handleAreaRect.anchorMax = new Vector2(1f, 0.5f);
            handleAreaRect.sizeDelta = new Vector2(-20f, 0f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(24f, 24f);
            handle.GetComponent<Image>().color = Color.white;

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            return slider;
        }

        static Button CreateButton(Transform parent, string text, Color color, Vector2 anchor, Vector2 anchoredPos)
        {
            var go = new GameObject(text + "_Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(200f, 64f);
            rect.anchoredPosition = anchoredPos;
            go.GetComponent<Image>().color = color;

            var label = CreateText(go.transform, text, 26, FontStyles.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return go.GetComponent<Button>();
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WeedHoldings
{
    /// <summary>
    /// 세로/가로 스크롤 리스트(뷰포트 + 콘텐츠 + 스크롤바)를 런타임에 생성하는 공용 헬퍼.
    /// UIAvPlantListPanel, UIPlantUnlockPanel, UIFactoryPanelController 등에서 공유한다.
    /// </summary>
    public static class UIScrollListFactory
    {
        static TMP_FontAsset cachedNotoSansFont;

        /// <summary>
        /// 가로 스크롤 콘텐츠가 놓인 Viewport의 실제 폭을 기준으로, 스크롤 없이 한 화면(패널)에
        /// 꽉 채울 수 있는 칸 수를 계산한다. 항목이 적어 목록이 휑해 보일 때 빈 칸으로 채우는 용도.
        /// </summary>
        public static int ComputeFillSlotCount(RectTransform content, float slotWidth, float spacing, float horizontalPadding = 8f)
        {
            if (content == null || slotWidth <= 0f) return 0;
            var viewport = content.parent as RectTransform;
            float width = viewport != null ? viewport.rect.width : 0f;
            if (width <= 0f) return 0;
            return Mathf.Max(0, Mathf.FloorToInt((width - horizontalPadding) / (slotWidth + spacing)));
        }

        /// <summary>
        /// 농장/공장/무역소 업그레이드 패널의 Upgrade_Level/Req_Gold 텍스트 박스가 아이콘 이미지와
        /// 겹치지 않도록 아이콘 오른쪽 빈 공간으로 재배치한다. 세 패널 모두 아이콘/텍스트가 서로 다른
        /// 크기의 부모를 가정하고 만든 좌표를 그대로 복사해 써서 텍스트가 아이콘을 침범하고 있었다.
        /// 고정 수치 대신 실제 아이콘/패널 rect 크기를 읽어 계산하므로 값이 조금 달라져도 안전하다.
        /// </summary>
        public static void RepositionUpgradeInfoTexts(RectTransform panel, RectTransform icon, RectTransform levelRt, RectTransform goldRt, float iconScale = 1f)
        {
            if (panel == null || icon == null) return;

            if (!Mathf.Approximately(iconScale, 1f))
                icon.sizeDelta *= iconScale;

            float iconRight = icon.anchoredPosition.x + icon.rect.width / 2f;
            float panelRight = panel.rect.width / 2f;
            float width = Mathf.Max(50f, panelRight - iconRight - 12f);
            float centerX = iconRight + 4f + width / 2f;

            if (levelRt != null)
            {
                levelRt.sizeDelta = new Vector2(width, 24f);
                levelRt.anchoredPosition = new Vector2(centerX, levelRt.anchoredPosition.y);
            }
            if (goldRt != null)
            {
                goldRt.sizeDelta = new Vector2(width, 24f);
                goldRt.anchoredPosition = new Vector2(centerX, goldRt.anchoredPosition.y);
            }
        }

        /// <summary>
        /// 농장 선택 리스트(Av_Plant_Panel)가 이미 에디터에서 올바른 노토산스 SDF 폰트로 설정돼 있으므로
        /// 그걸 그대로 재사용한다. 새로 코드로 만들거나 바인딩하는 텍스트가 TMP 기본 폰트(LiberationSans)로
        /// 남아 한글이 깨지는 문제를 막기 위해 모든 신규/코드 바인딩 텍스트에 이걸 적용해야 한다.
        /// </summary>
        public static TMP_FontAsset ResolveNotoSansFont()
        {
            if (cachedNotoSansFont != null) return cachedNotoSansFont;

            var farmSlot = Object.FindFirstObjectByType<UIAvPlantListSlot>(FindObjectsInactive.Include);
            if (farmSlot != null && farmSlot.plantText != null)
                cachedNotoSansFont = farmSlot.plantText.font;

            return cachedNotoSansFont;
        }

        /// <summary>주어진 오브젝트 아래의 모든 TMP 텍스트에 노토산스 폰트를 강제 적용한다.</summary>
        public static void ApplyNotoSansFont(Component root)
        {
            var font = ResolveNotoSansFont();
            if (font == null || root == null) return;

            var texts = root.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in texts)
                t.font = font;
        }

        /// <summary>
        /// 기존 패널(수동으로 앵커/크기를 잡아 배치했던 것)을 Content 아래로 옮긴다.
        /// VerticalLayoutGroup은 LayoutElement가 없는 자식의 높이를 0으로 취급해 찌그러뜨리므로,
        /// 옮기기 전 원래 렌더링되던 실제 높이를 측정해 LayoutElement.preferredHeight로 고정한다.
        /// (이게 없으면 리스트가 원래 설정한 영역/크기와 다르게 보이는 원인이 된다)
        /// </summary>
        public static void MoveIntoContent(Transform panel, RectTransform content)
        {
            var rt = panel as RectTransform;
            float originalHeight = rt != null ? rt.rect.height : 0f;

            panel.SetParent(content, false);

            if (originalHeight > 1f)
            {
                var layoutElement = panel.GetComponent<LayoutElement>();
                if (layoutElement == null) layoutElement = panel.gameObject.AddComponent<LayoutElement>();
                layoutElement.preferredHeight = originalHeight;
            }

            // 에디터에서 만들어진 패널들의 Image/TMP 그래픽이 maskable=false로 저장되어 있는 경우가 있는데,
            // 이러면 Mask/RectMask2D를 뭘 쓰든 그 그래픽만 클리핑을 무시하고 항상 그려진다
            // (뷰포트 밖으로 나간 것들이 계속 보이는 버그의 실제 원인). 강제로 true로 맞춘다.
            var graphics = panel.GetComponentsInChildren<MaskableGraphic>(true);
            foreach (var graphic in graphics)
            {
                graphic.maskable = true;
            }
        }

        public static ScrollRect Create(Transform parent, out RectTransform content, float spacing = 8f)
        {
            var srObj = new GameObject("ScrollRect", typeof(RectTransform));
            srObj.transform.SetParent(parent, false);
            srObj.layer = 5;

            var srRt = srObj.GetComponent<RectTransform>();
            srRt.anchorMin = Vector2.zero;
            srRt.anchorMax = Vector2.one;
            srRt.offsetMin = Vector2.zero;
            srRt.offsetMax = Vector2.zero;

            var bgImg = srObj.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.12f, 0.85f);
            bgImg.raycastTarget = false;

            var scrollRect = srObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 25f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;

            // Viewport: Unity 기본 Scroll View(GameObject > UI > Scroll View)가 쓰는 것과
            // 동일한 Image + Mask 조합으로 클리핑한다. RectMask2D는 계속 클리핑 누락 문제가 있었다.
            // (Mask는 그래픽의 알파값 기반으로 클리핑하므로 Color.clear(알파 0)는 절대 쓰면 안 됨 —
            //  전체가 안 보이는 함정이 있음. 여기서는 showMaskGraphic=false로 사각형 자체는 숨기고
            //  불투명한 색만 유지해 클리핑 용도로 사용한다.)
            var vpObj = new GameObject("Viewport", typeof(RectTransform));
            vpObj.transform.SetParent(srObj.transform, false);
            vpObj.layer = 5;

            var vpRt = vpObj.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;

            var vpImg = vpObj.AddComponent<Image>();
            vpImg.color = Color.white;
            vpImg.raycastTarget = false;

            var vpMask = vpObj.AddComponent<Mask>();
            vpMask.showMaskGraphic = false;

            // Content
            var ctObj = new GameObject("Content", typeof(RectTransform));
            ctObj.transform.SetParent(vpObj.transform, false);
            ctObj.layer = 5;

            content = ctObj.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0, 0);

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.content = content;
            scrollRect.viewport = vpRt;

            // Scrollbar (오른쪽)
            var sbObj = new GameObject("ScrollbarVertical", typeof(RectTransform));
            sbObj.transform.SetParent(srObj.transform, false);
            sbObj.layer = 5;

            var sbRt = sbObj.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(1, 0);
            sbRt.anchorMax = Vector2.one;
            sbRt.pivot = new Vector2(0.5f, 0.5f);
            sbRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 16);
            sbRt.offsetMin = new Vector2(0, 0);
            sbRt.offsetMax = new Vector2(0, 0);

            var sbBg = sbObj.AddComponent<Image>();
            sbBg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            sbBg.raycastTarget = true;

            var sb = sbObj.AddComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.TopToBottom;
            sb.value = 1;
            sb.size = 0.2f;
            sb.numberOfSteps = 0;
            sb.transition = Selectable.Transition.ColorTint;

            var sbColors = sb.colors;
            sbColors.normalColor = new Color(0.35f, 0.35f, 0.35f, 1);
            sbColors.highlightedColor = new Color(0.5f, 0.5f, 0.5f, 1);
            sbColors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1);
            sb.colors = sbColors;

            var haObj = new GameObject("SlidingArea", typeof(RectTransform));
            haObj.transform.SetParent(sbObj.transform, false);
            haObj.layer = 5;

            var haRt = haObj.GetComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero;
            haRt.anchorMax = Vector2.one;
            haRt.offsetMin = new Vector2(2, 2);
            haRt.offsetMax = new Vector2(-2, -2);

            var hObj = new GameObject("Handle", typeof(RectTransform));
            hObj.transform.SetParent(haObj.transform, false);
            hObj.layer = 5;

            var hRt = hObj.GetComponent<RectTransform>();
            hRt.anchorMin = Vector2.zero;
            hRt.anchorMax = Vector2.one;
            hRt.offsetMin = Vector2.zero;
            hRt.offsetMax = Vector2.zero;

            var hImg = hObj.AddComponent<Image>();
            hImg.color = new Color(0.5f, 0.5f, 0.5f, 1);
            hImg.raycastTarget = true;

            sb.targetGraphic = hImg;
            sb.handleRect = hRt;
            scrollRect.verticalScrollbar = sb;
            // AutoHideAndExpandViewport는 스크롤바 표시 여부에 따라 Viewport 크기를 런타임에 동적으로
            // 다시 계산하는데, 동적으로 생성된 이 계층구조와 타이밍이 어긋나는 문제가 있어 Permanent로 고정.
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.verticalScrollbarSpacing = 2;

            return scrollRect;
        }

        /// <summary>
        /// Create()와 동일한 뷰포트/세로 스크롤바 구조지만 Content에 VerticalLayoutGroup 대신
        /// GridLayoutGroup(고정 열 개수)을 사용한다. Field_Panel처럼 칸 크기가 전부 균일한 격자용.
        /// </summary>
        public static ScrollRect CreateGrid(Transform parent, out RectTransform content, Vector2 cellSize, Vector2 spacing, int columns)
        {
            var srObj = new GameObject("ScrollRect", typeof(RectTransform));
            srObj.transform.SetParent(parent, false);
            srObj.layer = 5;

            var srRt = srObj.GetComponent<RectTransform>();
            srRt.anchorMin = Vector2.zero;
            srRt.anchorMax = Vector2.one;
            srRt.offsetMin = Vector2.zero;
            srRt.offsetMax = Vector2.zero;

            var scrollRect = srObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 25f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;

            var vpObj = new GameObject("Viewport", typeof(RectTransform));
            vpObj.transform.SetParent(srObj.transform, false);
            vpObj.layer = 5;

            var vpRt = vpObj.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = new Vector2(0, 0);
            vpRt.offsetMax = new Vector2(-16, 0); // 오른쪽 스크롤바 자리

            var vpImg = vpObj.AddComponent<Image>();
            vpImg.color = Color.white;
            vpImg.raycastTarget = false;

            var vpMask = vpObj.AddComponent<Mask>();
            vpMask.showMaskGraphic = false;

            var ctObj = new GameObject("Content", typeof(RectTransform));
            ctObj.transform.SetParent(vpObj.transform, false);
            ctObj.layer = 5;

            content = ctObj.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0, 0);

            var layout = content.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = cellSize;
            layout.spacing = spacing;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = columns;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.content = content;
            scrollRect.viewport = vpRt;

            // Scrollbar (오른쪽)
            var sbObj = new GameObject("ScrollbarVertical", typeof(RectTransform));
            sbObj.transform.SetParent(srObj.transform, false);
            sbObj.layer = 5;

            var sbRt = sbObj.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(1, 0);
            sbRt.anchorMax = Vector2.one;
            sbRt.pivot = new Vector2(0.5f, 0.5f);
            sbRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 16);
            sbRt.offsetMin = new Vector2(0, 0);
            sbRt.offsetMax = new Vector2(0, 0);

            var sbBg = sbObj.AddComponent<Image>();
            sbBg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            sbBg.raycastTarget = true;

            var sb = sbObj.AddComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.TopToBottom;
            sb.value = 1;
            sb.size = 0.2f;
            sb.numberOfSteps = 0;
            sb.transition = Selectable.Transition.ColorTint;

            var sbColors = sb.colors;
            sbColors.normalColor = new Color(0.35f, 0.35f, 0.35f, 1);
            sbColors.highlightedColor = new Color(0.5f, 0.5f, 0.5f, 1);
            sbColors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1);
            sb.colors = sbColors;

            var haObj = new GameObject("SlidingArea", typeof(RectTransform));
            haObj.transform.SetParent(sbObj.transform, false);
            haObj.layer = 5;

            var haRt = haObj.GetComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero;
            haRt.anchorMax = Vector2.one;
            haRt.offsetMin = new Vector2(2, 2);
            haRt.offsetMax = new Vector2(-2, -2);

            var hObj = new GameObject("Handle", typeof(RectTransform));
            hObj.transform.SetParent(haObj.transform, false);
            hObj.layer = 5;

            var hRt = hObj.GetComponent<RectTransform>();
            hRt.anchorMin = Vector2.zero;
            hRt.anchorMax = Vector2.one;
            hRt.offsetMin = Vector2.zero;
            hRt.offsetMax = Vector2.zero;

            var hImg = hObj.AddComponent<Image>();
            hImg.color = new Color(0.5f, 0.5f, 0.5f, 1);
            hImg.raycastTarget = true;

            sb.targetGraphic = hImg;
            sb.handleRect = hRt;
            scrollRect.verticalScrollbar = sb;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.verticalScrollbarSpacing = 2;

            return scrollRect;
        }

        /// <summary>
        /// CreateHorizontal()과 동일한 가로 스크롤 구조지만 Content에 HorizontalLayoutGroup 대신
        /// GridLayoutGroup(고정 행 개수)을 사용한다. 무역소 인벤토리처럼 카드를 위/아래 2행으로
        /// 채우면서 옆으로 스크롤해야 할 때, 카드 하나의 크기(cellSize)를 그대로 유지하기 위한 용도.
        /// (HorizontalLayoutGroup + childForceExpandHeight는 행이 1개뿐이라 카드가 뷰포트 높이에
        /// 맞춰 세로로 늘어나 비율이 깨졌다.)
        /// </summary>
        public static ScrollRect CreateHorizontalGrid(Transform parent, out RectTransform content, Vector2 cellSize, Vector2 spacing, int rows)
        {
            var srObj = new GameObject("ScrollRect", typeof(RectTransform));
            srObj.transform.SetParent(parent, false);
            srObj.layer = 5;

            var srRt = srObj.GetComponent<RectTransform>();
            srRt.anchorMin = Vector2.zero;
            srRt.anchorMax = Vector2.one;
            srRt.offsetMin = Vector2.zero;
            srRt.offsetMax = Vector2.zero;

            var scrollRect = srObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 25f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;

            var vpObj = new GameObject("Viewport", typeof(RectTransform));
            vpObj.transform.SetParent(srObj.transform, false);
            vpObj.layer = 5;

            var vpRt = vpObj.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = new Vector2(0, 14); // 하단 스크롤바 자리
            vpRt.offsetMax = Vector2.zero;

            var vpImg = vpObj.AddComponent<Image>();
            vpImg.color = Color.white;
            vpImg.raycastTarget = false;

            var vpMask = vpObj.AddComponent<Mask>();
            vpMask.showMaskGraphic = false;

            var ctObj = new GameObject("Content", typeof(RectTransform));
            ctObj.transform.SetParent(vpObj.transform, false);
            ctObj.layer = 5;

            content = ctObj.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0, 0.5f);
            content.anchorMax = new Vector2(0, 0.5f);
            content.pivot = new Vector2(0, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0, 0);

            var layout = content.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = cellSize;
            layout.spacing = spacing;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.startAxis = GridLayoutGroup.Axis.Vertical; // 위->아래로 채운 뒤 다음 열로 이동 (가로 스크롤)
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            layout.constraintCount = rows;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.content = content;
            scrollRect.viewport = vpRt;

            // Scrollbar (하단)
            var sbObj = new GameObject("ScrollbarHorizontal", typeof(RectTransform));
            sbObj.transform.SetParent(srObj.transform, false);
            sbObj.layer = 5;

            var sbRt = sbObj.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(0, 0);
            sbRt.anchorMax = new Vector2(1, 0);
            sbRt.pivot = new Vector2(0.5f, 0.5f);
            sbRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 12);
            sbRt.offsetMin = new Vector2(0, 0);
            sbRt.offsetMax = new Vector2(0, 0);

            var sbBg = sbObj.AddComponent<Image>();
            sbBg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            sbBg.raycastTarget = true;

            var sb = sbObj.AddComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.LeftToRight;
            sb.value = 0;
            sb.size = 0.2f;
            sb.numberOfSteps = 0;
            sb.transition = Selectable.Transition.ColorTint;

            var sbColors = sb.colors;
            sbColors.normalColor = new Color(0.35f, 0.35f, 0.35f, 1);
            sbColors.highlightedColor = new Color(0.5f, 0.5f, 0.5f, 1);
            sbColors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1);
            sb.colors = sbColors;

            var haObj = new GameObject("SlidingArea", typeof(RectTransform));
            haObj.transform.SetParent(sbObj.transform, false);
            haObj.layer = 5;

            var haRt = haObj.GetComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero;
            haRt.anchorMax = Vector2.one;
            haRt.offsetMin = new Vector2(2, 2);
            haRt.offsetMax = new Vector2(-2, -2);

            var hObj = new GameObject("Handle", typeof(RectTransform));
            hObj.transform.SetParent(haObj.transform, false);
            hObj.layer = 5;

            var hRt = hObj.GetComponent<RectTransform>();
            hRt.anchorMin = Vector2.zero;
            hRt.anchorMax = Vector2.one;
            hRt.offsetMin = Vector2.zero;
            hRt.offsetMax = Vector2.zero;

            var hImg = hObj.AddComponent<Image>();
            hImg.color = new Color(0.5f, 0.5f, 0.5f, 1);
            hImg.raycastTarget = true;

            sb.targetGraphic = hImg;
            sb.handleRect = hRt;
            scrollRect.horizontalScrollbar = sb;
            scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.horizontalScrollbarSpacing = 2;

            return scrollRect;
        }

        /// <summary>Create()와 동일하지만 가로 스크롤 리스트를 만든다 (Inventory_Plant처럼 옆으로 늘어놓는 용도).</summary>
        public static ScrollRect CreateHorizontal(Transform parent, out RectTransform content, float spacing = 8f)
        {
            var srObj = new GameObject("ScrollRect", typeof(RectTransform));
            srObj.transform.SetParent(parent, false);
            srObj.layer = 5;

            var srRt = srObj.GetComponent<RectTransform>();
            srRt.anchorMin = Vector2.zero;
            srRt.anchorMax = Vector2.one;
            srRt.offsetMin = Vector2.zero;
            srRt.offsetMax = Vector2.zero;

            var scrollRect = srObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 25f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;

            var vpObj = new GameObject("Viewport", typeof(RectTransform));
            vpObj.transform.SetParent(srObj.transform, false);
            vpObj.layer = 5;

            var vpRt = vpObj.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = new Vector2(0, 14); // 하단 스크롤바 자리
            vpRt.offsetMax = Vector2.zero;

            var vpImg = vpObj.AddComponent<Image>();
            vpImg.color = Color.white;
            vpImg.raycastTarget = false;

            var vpMask = vpObj.AddComponent<Mask>();
            vpMask.showMaskGraphic = false;

            var ctObj = new GameObject("Content", typeof(RectTransform));
            ctObj.transform.SetParent(vpObj.transform, false);
            ctObj.layer = 5;

            content = ctObj.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0, 0);
            content.anchorMax = new Vector2(0, 1);
            content.pivot = new Vector2(0, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0, 0);

            var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.content = content;
            scrollRect.viewport = vpRt;

            // Scrollbar (하단)
            var sbObj = new GameObject("ScrollbarHorizontal", typeof(RectTransform));
            sbObj.transform.SetParent(srObj.transform, false);
            sbObj.layer = 5;

            var sbRt = sbObj.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(0, 0);
            sbRt.anchorMax = new Vector2(1, 0);
            sbRt.pivot = new Vector2(0.5f, 0.5f);
            sbRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 12);
            sbRt.offsetMin = new Vector2(0, 0);
            sbRt.offsetMax = new Vector2(0, 0);

            var sbBg = sbObj.AddComponent<Image>();
            sbBg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            sbBg.raycastTarget = true;

            var sb = sbObj.AddComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.LeftToRight;
            sb.value = 0;
            sb.size = 0.2f;
            sb.numberOfSteps = 0;
            sb.transition = Selectable.Transition.ColorTint;

            var sbColors = sb.colors;
            sbColors.normalColor = new Color(0.35f, 0.35f, 0.35f, 1);
            sbColors.highlightedColor = new Color(0.5f, 0.5f, 0.5f, 1);
            sbColors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1);
            sb.colors = sbColors;

            var haObj = new GameObject("SlidingArea", typeof(RectTransform));
            haObj.transform.SetParent(sbObj.transform, false);
            haObj.layer = 5;

            var haRt = haObj.GetComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero;
            haRt.anchorMax = Vector2.one;
            haRt.offsetMin = new Vector2(2, 2);
            haRt.offsetMax = new Vector2(-2, -2);

            var hObj = new GameObject("Handle", typeof(RectTransform));
            hObj.transform.SetParent(haObj.transform, false);
            hObj.layer = 5;

            var hRt = hObj.GetComponent<RectTransform>();
            hRt.anchorMin = Vector2.zero;
            hRt.anchorMax = Vector2.one;
            hRt.offsetMin = Vector2.zero;
            hRt.offsetMax = Vector2.zero;

            var hImg = hObj.AddComponent<Image>();
            hImg.color = new Color(0.5f, 0.5f, 0.5f, 1);
            hImg.raycastTarget = true;

            sb.targetGraphic = hImg;
            sb.handleRect = hRt;
            scrollRect.horizontalScrollbar = sb;
            scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.horizontalScrollbarSpacing = 2;

            return scrollRect;
        }
    }
}

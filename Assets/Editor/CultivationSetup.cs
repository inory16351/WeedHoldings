using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;

namespace WeedHoldings.Editor
{
    /// <summary>
    /// proto_01 씬을 자동으로 구성하는 에디터 도구.
    /// UI임시리소스 기준: 상단 탭 / 좌측 밭 그리드 / 우측 식물 리스트 / 연구실 패널
    /// </summary>
    public class CultivationSetup
    {
        const string SCENE_PATH  = "Assets/Scenes/proto_01.unity";
        const string PREFAB_DIR  = "Assets/Prefabs";
        const string SPRITE_DIR  = "Assets/Sprites/Placeholders";
        const string FONT_TTF    = "Assets/Font/NotoSansKR-Medium.ttf";

        static TMP_FontAsset s_Font;

        // ─────────────────────────────────────────────────────────
        [MenuItem("WeedHoldings/재배 시스템 셋업 (Full)")]
        public static void Run()
        {
            // 새 씬 생성
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SetActiveScene(scene);

            LoadOrCreateFont();
            CreatePlaceholderSprites();

            SetupCamera();
            GameObject canvas = CreateCanvas();
            CreateEventSystem();

            // 매니저 오브젝트
            GameObject gm  = CreateGameManager();
            GameObject fm  = CreateFarmManager();

            // Canvas UI 구성
            CreateTopBar(canvas);
            (GameObject farmPanel, GameObject labPanel, GameObject lobbyPanel) = CreateMainPanels(canvas);

            // 로비 UI 구성
            CreateLobbyUI(lobbyPanel);

            // 재배 패널 내부
            GameObject farmGrid   = CreateFarmGridArea(farmPanel);
            GameObject plantList  = CreatePlantListPanel(farmPanel);
            CreateFarmBottomBar(farmPanel);

            // 농장 뒤로가기 버튼
            MakeButton(farmPanel, "FarmBackButton", new Vector2(60, 60), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Color(0.40f, 0.40f, 0.45f), "←", 32);

            // 연구실 패널
            CreateLabPanelUI(labPanel);

            // 연구실 뒤로가기 버튼
            MakeButton(labPanel, "LabBackButton", new Vector2(60, 60), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Color(0.40f, 0.40f, 0.45f), "←", 32);

            // 프리팹 생성
            CreatePlotCellPrefab();
            CreatePlantSlotPrefab();

            // 참조 연결
            LinkReferences(canvas, farmPanel, labPanel, lobbyPanel, farmGrid, plantList, gm, fm);

            // 씬 내의 모든 텍스트에 NotoSans 폰트 일괄 적용
            ApplyFontToAllTexts();

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), SCENE_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CultivationSetup] proto_01 씬 + 플레이스홀더 프리팹 생성 완료!");
            
            if (UnityEditorInternal.InternalEditorUtility.inBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        // ─── 폰트 ─────────────────────────────────────────────────
        static void LoadOrCreateFont()
        {
            // 이미 프로젝트 내에 생성되어 있는 NotoSansKR-Regular SDF.asset을 먼저 읽어옵니다.
            s_Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/NotoSansKR-Regular SDF.asset");
            if (s_Font != null) return;

            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset NotoSansKR");
            if (guids.Length > 0)
            {
                s_Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
                if (s_Font != null) return;
            }

            // 폴백: 기본 TMP 폰트 사용
            s_Font = TMP_Settings.defaultFontAsset;
        }

        // ─── 플레이스홀더 스프라이트 ─────────────────────────────
        static void CreatePlaceholderSprites()
        {
            Directory.CreateDirectory(SPRITE_DIR);
            MakeSolidSprite("plot_empty",   64, 64, new Color(0.72f, 0.56f, 0.36f));
            MakeSolidSprite("plot_growing", 64, 64, new Color(0.35f, 0.70f, 0.25f));
            MakeSolidSprite("plot_harvest", 64, 64, new Color(0.95f, 0.80f, 0.20f));
            MakeSolidSprite("plot_wither",  64, 64, new Color(0.80f, 0.30f, 0.20f));
            MakeSolidSprite("plot_locked",  64, 64, new Color(0.45f, 0.45f, 0.45f));
            MakeSolidSprite("icon_plant",   40, 40, new Color(0.45f, 0.75f, 0.30f));
            MakeSolidSprite("water_drop",   20, 28, new Color(0.30f, 0.60f, 1.00f));
            MakeSolidSprite("white_sq",     4,  4,  Color.white);
        }

        static Sprite LoadSprite(string name)
            => AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_DIR}/{name}.png");

        static void MakeSolidSprite(string name, int w, int h, Color color)
        {
            string path = $"{SPRITE_DIR}/{name}.png";
            if (File.Exists(path)) return;

            Texture2D tex = new Texture2D(w, h);
            Color[] px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = color;
            tex.SetPixels(px);
            tex.Apply();

            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);

            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType       = TextureImporterType.Sprite;
                imp.spritePixelsPerUnit = 100;
                imp.SaveAndReimport();
            }
        }

        // ─── 카메라 ───────────────────────────────────────────────
        static void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cam = go.GetComponent<Camera>();
                go.tag = "MainCamera";
            }
            cam.orthographic   = true;
            cam.orthographicSize = 5f;
            cam.clearFlags     = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.18f, 0.18f, 0.22f);
            cam.transform.position = new Vector3(0, 0, -10f);
        }

        // ─── Canvas ──────────────────────────────────────────────
        static GameObject CreateCanvas()
        {
            GameObject go = new GameObject("CultivationCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            Canvas c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 0;

            CanvasScaler cs = go.GetComponent<CanvasScaler>();
            cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            cs.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            cs.matchWidthOrHeight  = 0.5f;

            return go;
        }

        static void CreateEventSystem()
        {
            var es = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es != null)
            {
                if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                {
                    var old = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    if (old != null) Object.DestroyImmediate(old);
                    es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                }
            }
            else
            {
                new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            }
        }

        // ─── 게임 매니저 ─────────────────────────────────────────
        static GameObject CreateGameManager()
        {
            GameObject go = new GameObject("GameManager");
            go.AddComponent<PlantDatabase>();
            go.AddComponent<DemoDataInitializer>();
            go.AddComponent<CurrencyManager>();
            go.AddComponent<InventoryManager>();
            go.AddComponent<LabManager>();
            return go;
        }

        static GameObject CreateFarmManager()
        {
            GameObject go = new GameObject("FarmManager");
            go.AddComponent<FarmGridManager>();
            return go;
        }

        // ─── 최상단 바 (골드 + 탭) ───────────────────────────────
        static void CreateTopBar(GameObject canvas)
        {
            // 상단 바 배경
            GameObject bar = MakeImg(canvas, "TopBar",
                new Vector2(1920, 64), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0.5f, 1), Vector2.zero,
                new Color(0.15f, 0.15f, 0.18f));

            // 사운드 토글 버튼 (골드패널 좌측에 배치)
            GameObject soundBtn = MakeButton(bar, "SoundToggleButton",
                new Vector2(180, 48), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(1, 0.5f), new Vector2(-312, 0),
                new Color(0.25f, 0.25f, 0.30f), "사운드 ON", 20);
            soundBtn.AddComponent<UISoundToggle>();

            // 골드 표시 (우측)
            GameObject goldPanel = MakeImg(bar, "GoldPanel",
                new Vector2(280, 48), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(1, 0.5f), new Vector2(-16, 0),
                new Color(0.10f, 0.10f, 0.12f));
            RoundedCorner(goldPanel);

            var goldText = MakeTMP(goldPanel, "GoldText",
                new Vector2(260, 40), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, "G 0", 30, TextAlignmentOptions.Right);
            goldText.color = new Color(1f, 0.85f, 0.20f);
        }

        // ─── 메인 패널 (농장 / 연구실 / 로비) ──────────────────────
        static (GameObject, GameObject, GameObject) CreateMainPanels(GameObject canvas)
        {
            // 공통 컨테이너 (탭 아래 전체 영역)
            GameObject container = MakeImg(canvas, "MainContainer",
                new Vector2(1920, 1016), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0.5f, 0), new Vector2(0, 0),
                new Color(0.12f, 0.12f, 0.15f));

            // 로비 패널
            GameObject lobbyPanel = MakeImg(container, "LobbyPanel",
                new Vector2(1920, 1016), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Color(0, 0, 0, 0));

            // 농장 패널 (초기 비활성)
            GameObject farmPanel = MakeImg(container, "FarmPanel",
                new Vector2(1920, 1016), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Color(0, 0, 0, 0));
            farmPanel.SetActive(false);

            // 연구실 패널 (초기 비활성)
            GameObject labPanel = MakeImg(container, "LabPanel",
                new Vector2(1920, 1016), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Color(0, 0, 0, 0));
            labPanel.SetActive(false);

            return (farmPanel, labPanel, lobbyPanel);
        }

        static void CreateLobbyUI(GameObject lobbyPanel)
        {
            // 로비 타이틀
            MakeTMP(lobbyPanel, "LobbyTitle", new Vector2(800, 80), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -60), "슬기로운 재배생활", 54, TextAlignmentOptions.Center);

            // 4개 배너 버튼 (재배, 제조, 판매, 업그레이드)
            GameObject btnFarm = MakeButton(lobbyPanel, "LobbyBtnFarm", new Vector2(400, 220), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-220, 80), new Color(0.20f, 0.50f, 0.20f), "재배\n(농장)", 36);
            AddOutline(btnFarm, new Color(0.35f, 0.65f, 0.35f));

            GameObject btnFactory = MakeButton(lobbyPanel, "LobbyBtnFactory", new Vector2(400, 220), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(220, 80), new Color(0.20f, 0.35f, 0.50f), "제조\n(공장)", 36);
            AddOutline(btnFactory, new Color(0.35f, 0.50f, 0.65f));

            GameObject btnMarket = MakeButton(lobbyPanel, "LobbyBtnMarket", new Vector2(400, 220), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-220, -180), new Color(0.55f, 0.40f, 0.15f), "판매\n(시장)", 36);
            AddOutline(btnMarket, new Color(0.70f, 0.55f, 0.30f));

            GameObject btnLab = MakeButton(lobbyPanel, "LobbyBtnLab", new Vector2(400, 220), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(220, -180), new Color(0.35f, 0.25f, 0.50f), "업그레이드\n(연구실)", 36);
            AddOutline(btnLab, new Color(0.50f, 0.40f, 0.70f));

            // 준비중 알림창
            GameObject alert = MakeImg(lobbyPanel, "LobbyAlertWindow", new Vector2(600, 320), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Color(0.15f, 0.15f, 0.18f));
            AddOutline(alert, new Color(0.60f, 0.30f, 0.30f));
            alert.SetActive(false);

            MakeTMP(alert, "Title", new Vector2(560, 50), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), "알 림", 30, TextAlignmentOptions.Center);
            var alertTxt = MakeTMP(alert, "MessageText", new Vector2(520, 120), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), "시스템 준비 중입니다.", 24, TextAlignmentOptions.Center);
            alertTxt.color = new Color(0.90f, 0.90f, 0.90f);

            MakeButton(alert, "AlertCloseButton", new Vector2(200, 50), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 20), new Color(0.40f, 0.40f, 0.45f), "확인", 22);
        }

        // ─── 재배 패널: 밭 그리드 영역 (좌측) ───────────────────
        static GameObject CreateFarmGridArea(GameObject farmPanel)
        {
            // 좌측 밭 컨테이너
            GameObject gridArea = MakeImg(farmPanel, "GridArea",
                new Vector2(1180, 900), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(0, 0.5f), new Vector2(20, 0),
                new Color(0.18f, 0.22f, 0.18f));
            AddOutline(gridArea, new Color(0.35f, 0.55f, 0.25f));

            // GridLayoutGroup 컨테이너
            GameObject gridContainer = new GameObject("GridContainer", typeof(RectTransform));
            gridContainer.transform.SetParent(gridArea.transform, false);
            RectTransform grt = gridContainer.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(0.5f, 0.5f);
            grt.anchorMax = new Vector2(0.5f, 0.5f);
            grt.pivot     = new Vector2(0.5f, 0.5f);
            grt.sizeDelta = new Vector2(1120, 820);
            grt.anchoredPosition = Vector2.zero;

            GridLayoutGroup glg  = gridContainer.AddComponent<GridLayoutGroup>();
            glg.cellSize         = new Vector2(340, 180);
            glg.spacing          = new Vector2(12, 12);
            glg.padding          = new RectOffset(10, 10, 10, 10);
            glg.constraint       = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount  = 3;
            glg.childAlignment   = TextAnchor.MiddleCenter;

            // UIFarmGridView 컴포넌트 추가
            UIFarmGridView gridView = gridArea.AddComponent<UIFarmGridView>();
            gridView.gridContainer = gridContainer.transform;

            return gridArea;
        }

        // ─── 재배 패널: 식물 선택 리스트 (우측) ─────────────────
        static GameObject CreatePlantListPanel(GameObject farmPanel)
        {
            // 우측 패널
            GameObject panel = MakeImg(farmPanel, "PlantListPanel",
                new Vector2(700, 900), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(1, 0.5f), new Vector2(-20, 0),
                new Color(0.16f, 0.18f, 0.22f));
            AddOutline(panel, new Color(0.30f, 0.35f, 0.50f));

            // 패널 타이틀
            MakeTMP(panel, "ListTitle",
                new Vector2(660, 50), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0.5f, 1), new Vector2(0, -10),
                "식물 선택", 30, TextAlignmentOptions.Center);

            // 스크롤 뷰
            GameObject scrollRoot = new GameObject("PlantScrollView", typeof(RectTransform));
            scrollRoot.transform.SetParent(panel.transform, false);
            RectTransform srt = scrollRoot.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0);
            srt.anchorMax = new Vector2(1, 1);
            srt.offsetMin = new Vector2(10, 10);
            srt.offsetMax = new Vector2(-10, -60);

            ScrollRect scroll     = scrollRoot.AddComponent<ScrollRect>();
            Image scrollImg       = scrollRoot.AddComponent<Image>();
            scrollImg.color       = new Color(0, 0, 0, 0);
            scroll.horizontal     = false;
            scroll.vertical       = true;
            scroll.scrollSensitivity = 30;

            // Viewport
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollRoot.transform, false);
            RectTransform vrt = viewport.GetComponent<RectTransform>();
            vrt.anchorMin  = Vector2.zero;
            vrt.anchorMax  = Vector2.one;
            vrt.offsetMin  = Vector2.zero;
            vrt.offsetMax  = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = vrt;

            // Content (VerticalLayoutGroup)
            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform crt = content.GetComponent<RectTransform>();
            crt.anchorMin  = new Vector2(0, 1);
            crt.anchorMax  = new Vector2(1, 1);
            crt.pivot      = new Vector2(0.5f, 1);
            crt.sizeDelta  = new Vector2(0, 0);
            crt.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup vlg  = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing             = 8;
            vlg.padding             = new RectOffset(8, 8, 8, 8);
            vlg.childAlignment      = TextAnchor.UpperCenter;
            vlg.childControlWidth   = true;
            vlg.childControlHeight  = false;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf  = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = crt;

            // UIPlantSelectPanel 연결
            UIPlantSelectPanel psp = panel.AddComponent<UIPlantSelectPanel>();
            psp.slotContainer      = content.transform;

            return panel;
        }

        // ─── 재배 패널: 하단 버튼 바 ────────────────────────────
        static void CreateFarmBottomBar(GameObject farmPanel)
        {
            GameObject bar = MakeImg(farmPanel, "FarmBottomBar",
                new Vector2(1180, 80), new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(0, 0), new Vector2(20, 10),
                new Color(0.13f, 0.17f, 0.13f));

            MakeButton(bar, "WaterAllButton",   new Vector2(350, 60), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10,  0), new Color(0.20f, 0.50f, 0.90f), "한번에 물주기", 26);
            MakeButton(bar, "PlantAllButton",   new Vector2(350, 60), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(375, 0), new Color(0.30f, 0.65f, 0.25f), "한번에 심기",   26);
            MakeButton(bar, "HarvestAllButton", new Vector2(350, 60), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(740, 0), new Color(0.85f, 0.55f, 0.15f), "한번에 수확하기", 26);

            // 상태 텍스트
            MakeTMP(bar, "StatusText",
                new Vector2(1100, 40), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 1), new Vector2(0, 0),
                "밭: 0칸 | 빈칸: 0 | 성장중: 0 | 수확가능: 0", 22, TextAlignmentOptions.Left)
                .gameObject.name = "StatusText";
        }

        // ─── 연구실 패널 UI ──────────────────────────────────────
        static void CreateLabPanelUI(GameObject labPanel)
        {
            // 중앙 카드
            GameObject card = MakeImg(labPanel, "LabCard",
                new Vector2(900, 780), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero,
                new Color(0.18f, 0.20f, 0.28f));
            AddOutline(card, new Color(0.40f, 0.45f, 0.70f));

            // 타이틀
            MakeTMP(card, "LabTitle",
                new Vector2(860, 60), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0.5f, 1), new Vector2(0, -24),
                "🔬 연구소", 40, TextAlignmentOptions.Center);

            // 구분선
            MakeImg(card, "Divider1",
                new Vector2(820, 2), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0.5f, 1), new Vector2(0, -90),
                new Color(0.40f, 0.45f, 0.70f));

            // 레벨
            var levelText = MakeTMP(card, "LevelText",
                new Vector2(820, 56), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0.5f, 1), new Vector2(0, -110),
                "연구소 Lv. 0 / 20", 36, TextAlignmentOptions.Center);
            levelText.color = new Color(0.80f, 0.90f, 1.00f);

            // 보너스
            var bonusText = MakeTMP(card, "BonusText",
                new Vector2(820, 44), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0.5f, 1), new Vector2(0, -170),
                "성장 속도 보너스 +0%", 28, TextAlignmentOptions.Center);
            bonusText.color = new Color(0.50f, 0.90f, 0.50f);

            // 배율
            var effectText = MakeTMP(card, "EffectText",
                new Vector2(820, 36), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0.5f, 1), new Vector2(0, -220),
                "성장 1.00× 빠름", 24, TextAlignmentOptions.Center);
            effectText.color = new Color(0.70f, 0.70f, 0.70f);

            // 구분선
            MakeImg(card, "Divider2",
                new Vector2(820, 2), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0.5f, 1), new Vector2(0, -270),
                new Color(0.40f, 0.45f, 0.70f));

            // 업그레이드 정보
            var upgradeInfoText = MakeTMP(card, "UpgradeInfoText",
                new Vector2(820, 80), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0.5f, 1), new Vector2(0, -290),
                "다음 레벨 효과를 확인하세요.", 22, TextAlignmentOptions.Center);
            upgradeInfoText.color = new Color(0.80f, 0.80f, 0.60f);

            // 업그레이드 버튼
            GameObject upgradeBtn = MakeButton(card, "UpgradeButton",
                new Vector2(500, 70), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0.5f, 1), new Vector2(0, -390),
                new Color(0.50f, 0.30f, 0.90f), "업그레이드", 30);

            var upgradeCostText = MakeTMP(upgradeBtn, "UpgradeCostText",
                new Vector2(490, 30), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0.5f, 0), new Vector2(0, 5),
                "업그레이드 200G", 20, TextAlignmentOptions.Center);
            upgradeCostText.color = Color.white;

            // 구분선
            MakeImg(card, "Divider3",
                new Vector2(820, 2), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0.5f, 1), new Vector2(0, -480),
                new Color(0.40f, 0.45f, 0.70f));

            // 그룹 정보
            var groupInfoText = MakeTMP(card, "GroupInfoText",
                new Vector2(820, 100), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0.5f, 1), new Vector2(0, -500),
                "한번에 심기/수확 비활성 (Lv.5 이상)", 20, TextAlignmentOptions.Center);
            groupInfoText.color = new Color(0.70f, 0.80f, 1.00f);

            // 식물 해금 안내
            var plantUnlockText = MakeTMP(card, "PlantUnlockText",
                new Vector2(820, 44), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0.5f, 1), new Vector2(0, -610),
                "해금 식물: 0 / 10", 24, TextAlignmentOptions.Center);
            plantUnlockText.color = new Color(0.90f, 0.80f, 0.50f);

            // UILabPanel 컴포넌트
            UILabPanel uiLab         = card.AddComponent<UILabPanel>();
            uiLab.levelText          = levelText;
            uiLab.bonusText          = bonusText;
            uiLab.effectText         = effectText;
            uiLab.upgradeButton      = upgradeBtn.GetComponent<Button>();
            uiLab.upgradeCostText    = upgradeCostText;
            uiLab.upgradeInfoText    = upgradeInfoText;
            uiLab.groupInfoText      = groupInfoText;
            uiLab.plantUnlockText    = plantUnlockText;
        }

        // ─── 밭 셀 프리팹 ────────────────────────────────────────
        static void CreatePlotCellPrefab()
        {
            Directory.CreateDirectory(PREFAB_DIR);
            string path = $"{PREFAB_DIR}/FarmPlotCell.prefab";
            if (File.Exists(path)) return;

            // 루트
            GameObject root = new GameObject("FarmPlotCell", typeof(RectTransform));
            Image rootImg = root.AddComponent<Image>();
            rootImg.sprite = LoadSprite("plot_empty");
            rootImg.color  = new Color(0.72f, 0.56f, 0.36f);

            Button btn = root.AddComponent<Button>();
            btn.targetGraphic = rootImg;

            // 식물 이미지 오브젝트 추가 (중앙에 식물 외형을 그리기 위함)
            GameObject plantImgGO = new GameObject("PlantImage", typeof(RectTransform), typeof(Image));
            plantImgGO.transform.SetParent(root.transform, false);
            Image plantImg = plantImgGO.GetComponent<Image>();
            plantImg.color = Color.white;
            var piRT = plantImgGO.GetComponent<RectTransform>();
            piRT.anchorMin = new Vector2(0.5f, 0.5f);
            piRT.anchorMax = new Vector2(0.5f, 0.5f);
            piRT.pivot     = new Vector2(0.5f, 0.5f);
            piRT.sizeDelta = new Vector2(100, 100);
            piRT.anchoredPosition = new Vector2(0, 15);
            plantImgGO.SetActive(false);

            // 식물 이름
            GameObject nameGO = new GameObject("PlantNameText", typeof(RectTransform));
            nameGO.transform.SetParent(root.transform, false);
            var nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0.70f);
            nameRT.anchorMax = new Vector2(1, 1);
            nameRT.offsetMin = new Vector2(4, 0);
            nameRT.offsetMax = new Vector2(-4, -4);
            TextMeshProUGUI nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
            nameTMP.text      = "";
            nameTMP.fontSize  = 18;
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.alignment = TextAlignmentOptions.Top;
            nameTMP.color     = Color.white;
            if (s_Font != null) nameTMP.font = s_Font;

            // 타이머
            GameObject timerGO = new GameObject("TimerText", typeof(RectTransform));
            timerGO.transform.SetParent(root.transform, false);
            var timerRT = timerGO.GetComponent<RectTransform>();
            timerRT.anchorMin = new Vector2(0, 0.15f);
            timerRT.anchorMax = new Vector2(1, 0.45f);
            timerRT.offsetMin = new Vector2(4, 0);
            timerRT.offsetMax = new Vector2(-4, 0);
            TextMeshProUGUI timerTMP = timerGO.AddComponent<TextMeshProUGUI>();
            timerTMP.text      = "";
            timerTMP.fontSize  = 15;
            timerTMP.alignment = TextAlignmentOptions.Center;
            timerTMP.color     = Color.white;
            if (s_Font != null) timerTMP.font = s_Font;

            // 성장 프로그레스 바 (초록)
            GameObject pbBg = new GameObject("ProgressBarBg", typeof(RectTransform), typeof(Image));
            pbBg.transform.SetParent(root.transform, false);
            pbBg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.6f);
            var pbBgRT = pbBg.GetComponent<RectTransform>();
            pbBgRT.anchorMin = new Vector2(0.05f, 0.08f);
            pbBgRT.anchorMax = new Vector2(0.95f, 0.16f);
            pbBgRT.offsetMin = Vector2.zero;
            pbBgRT.offsetMax = Vector2.zero;

            GameObject pbFill = new GameObject("ProgressBarFill", typeof(RectTransform), typeof(Image));
            pbFill.transform.SetParent(pbBg.transform, false);
            Image fillImg = pbFill.GetComponent<Image>();
            fillImg.color    = new Color(0.30f, 0.75f, 0.25f);
            fillImg.type     = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0f;
            var pfRT = pbFill.GetComponent<RectTransform>();
            pfRT.anchorMin = Vector2.zero;
            pfRT.anchorMax = Vector2.one;
            pfRT.offsetMin = Vector2.zero;
            pfRT.offsetMax = Vector2.zero;

            // 고사 바 (빨강)
            GameObject wbBg = new GameObject("WitherBarBg", typeof(RectTransform), typeof(Image));
            wbBg.transform.SetParent(root.transform, false);
            wbBg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.6f);
            var wbBgRT = wbBg.GetComponent<RectTransform>();
            wbBgRT.anchorMin = new Vector2(0.05f, 0.02f);
            wbBgRT.anchorMax = new Vector2(0.95f, 0.07f);
            wbBgRT.offsetMin = Vector2.zero;
            wbBgRT.offsetMax = Vector2.zero;

            GameObject wbFill = new GameObject("WitherBarFill", typeof(RectTransform), typeof(Image));
            wbFill.transform.SetParent(wbBg.transform, false);
            Image wfImg = wbFill.GetComponent<Image>();
            wfImg.color      = new Color(0.85f, 0.25f, 0.15f);
            wfImg.type       = Image.Type.Filled;
            wfImg.fillMethod = Image.FillMethod.Horizontal;
            wfImg.fillAmount = 0f;
            var wfRT = wbFill.GetComponent<RectTransform>();
            wfRT.anchorMin = Vector2.zero;
            wfRT.anchorMax = Vector2.one;
            wfRT.offsetMin = Vector2.zero;
            wfRT.offsetMax = Vector2.zero;

            // 잠금 오버레이
            GameObject lockGO = new GameObject("LockOverlay", typeof(RectTransform), typeof(Image));
            lockGO.transform.SetParent(root.transform, false);
            lockGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
            var lockRT = lockGO.GetComponent<RectTransform>();
            lockRT.anchorMin = Vector2.zero;
            lockRT.anchorMax = Vector2.one;
            lockRT.offsetMin = Vector2.zero;
            lockRT.offsetMax = Vector2.zero;
            lockGO.SetActive(false);

            GameObject lockTxtGO = new GameObject("LockCostText", typeof(RectTransform));
            lockTxtGO.transform.SetParent(lockGO.transform, false);
            TextMeshProUGUI lockTMP = lockTxtGO.AddComponent<TextMeshProUGUI>();
            lockTMP.text      = $"🔒 {GameConstants.PLOT_UNLOCK_BASE_PRICE}G";
            lockTMP.fontSize  = 20;
            lockTMP.alignment = TextAlignmentOptions.Center;
            lockTMP.color     = Color.white;
            if (s_Font != null) lockTMP.font = s_Font;
            var lckRT = lockTxtGO.GetComponent<RectTransform>();
            lckRT.anchorMin = Vector2.zero;
            lckRT.anchorMax = Vector2.one;
            lckRT.offsetMin = Vector2.zero;
            lckRT.offsetMax = Vector2.zero;

            // 수확 가능 글로우
            GameObject glowGO = new GameObject("HarvestReadyGlow", typeof(RectTransform), typeof(Image));
            glowGO.transform.SetParent(root.transform, false);
            glowGO.GetComponent<Image>().color = new Color(1f, 0.85f, 0.10f, 0.25f);
            var glRT = glowGO.GetComponent<RectTransform>();
            glRT.anchorMin = Vector2.zero;
            glRT.anchorMax = Vector2.one;
            glRT.offsetMin = Vector2.zero;
            glRT.offsetMax = Vector2.zero;
            glowGO.SetActive(false);

            // UIFarmPlotCell 컴포넌트 연결
            UIFarmPlotCell cell = root.AddComponent<UIFarmPlotCell>();
            cell.bgImage          = rootImg;
            cell.plantImage       = plantImg;
            cell.progressBarFill  = fillImg;
            cell.witherBarFill    = wfImg;
            cell.plantNameText    = nameTMP;
            cell.timerText        = timerTMP;
            cell.lockOverlay      = lockGO.GetComponent<Image>();
            cell.lockCostText     = lockTMP;
            cell.harvestReadyGlow = glowGO.GetComponent<Image>();

            // 클릭 연결
            btn.onClick.AddListener(cell.OnCellClicked);

            // 저장
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        // ─── 식물 선택 슬롯 프리팹 ──────────────────────────────
        static void CreatePlantSlotPrefab()
        {
            Directory.CreateDirectory(PREFAB_DIR);
            string path = $"{PREFAB_DIR}/PlantSelectSlot.prefab";

            GameObject root = new GameObject("PlantSelectSlot", typeof(RectTransform));
            Image rootImg = root.AddComponent<Image>();
            rootImg.color = new Color(0.22f, 0.25f, 0.32f);

            LayoutElement le  = root.AddComponent<LayoutElement>();
            le.preferredHeight = 90;

            Button btn = root.AddComponent<Button>();
            btn.targetGraphic = rootImg;

            // 아이콘
            GameObject iconGO = new GameObject("IconImage", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(root.transform, false);
            Image iconImg = iconGO.GetComponent<Image>();
            iconImg.color  = new Color(0.45f, 0.75f, 0.30f);
            iconImg.sprite = LoadSprite("icon_plant");
            var iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0, 0.5f);
            iconRT.anchorMax = new Vector2(0, 0.5f);
            iconRT.pivot     = new Vector2(0, 0.5f);
            iconRT.sizeDelta = new Vector2(60, 60);
            iconRT.anchoredPosition = new Vector2(16, 0);

            // 잠금 오버레이
            GameObject lockOvGO = new GameObject("LockOverlay", typeof(RectTransform), typeof(Image));
            lockOvGO.transform.SetParent(iconGO.transform, false);
            lockOvGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            var loRT = lockOvGO.GetComponent<RectTransform>();
            loRT.anchorMin = Vector2.zero;
            loRT.anchorMax = Vector2.one;
            loRT.offsetMin = Vector2.zero;
            loRT.offsetMax = Vector2.zero;
            lockOvGO.SetActive(false);

            // 이름
            GameObject nameGO = new GameObject("NameText", typeof(RectTransform));
            nameGO.transform.SetParent(root.transform, false);
            TextMeshProUGUI nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
            nameTMP.text      = "식물명";
            nameTMP.fontSize  = 22;
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.color     = Color.white;
            if (s_Font != null) nameTMP.font = s_Font;
            var nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0.5f);
            nameRT.anchorMax = new Vector2(0.55f, 0.5f);
            nameRT.pivot     = new Vector2(0, 0.5f);
            nameRT.sizeDelta = new Vector2(0, 32);
            nameRT.anchoredPosition = new Vector2(90, 14);

            // 보유량
            GameObject stockGO = new GameObject("StockText", typeof(RectTransform));
            stockGO.transform.SetParent(root.transform, false);
            TextMeshProUGUI stockTMP = stockGO.AddComponent<TextMeshProUGUI>();
            stockTMP.text      = "보유량: 0";
            stockTMP.fontSize  = 18;
            stockTMP.color     = new Color(0.75f, 0.85f, 0.75f);
            if (s_Font != null) stockTMP.font = s_Font;
            var stockRT = stockGO.GetComponent<RectTransform>();
            stockRT.anchorMin = new Vector2(0, 0.5f);
            stockRT.anchorMax = new Vector2(0.55f, 0.5f);
            stockRT.pivot     = new Vector2(0, 0.5f);
            stockRT.sizeDelta = new Vector2(0, 26);
            stockRT.anchoredPosition = new Vector2(90, -14);

            // 필요 연구소 레벨 (잠금 상태)
            GameObject reqGO = new GameObject("ReqLevelText", typeof(RectTransform));
            reqGO.transform.SetParent(root.transform, false);
            TextMeshProUGUI reqTMP = reqGO.AddComponent<TextMeshProUGUI>();
            reqTMP.text      = "연구소 Lv.0 필요";
            reqTMP.fontSize  = 16;
            reqTMP.color     = new Color(1f, 0.60f, 0.30f);
            if (s_Font != null) reqTMP.font = s_Font;
            var reqRT = reqGO.GetComponent<RectTransform>();
            reqRT.anchorMin = new Vector2(0, 0);
            reqRT.anchorMax = new Vector2(0.55f, 0);
            reqRT.pivot     = new Vector2(0, 0);
            reqRT.sizeDelta = new Vector2(0, 22);
            reqRT.anchoredPosition = new Vector2(90, 8);
            reqGO.SetActive(false);

            // 선택 버튼 (우측)
            GameObject selBtnGO = new GameObject("SelectButton", typeof(RectTransform), typeof(Image), typeof(Button));
            selBtnGO.transform.SetParent(root.transform, false);
            selBtnGO.GetComponent<Image>().color = new Color(0.30f, 0.65f, 0.25f);
            Button selBtn = selBtnGO.GetComponent<Button>();
            selBtn.targetGraphic = selBtnGO.GetComponent<Image>();
            var selRT = selBtnGO.GetComponent<RectTransform>();
            selRT.anchorMin = new Vector2(1, 0.5f);
            selRT.anchorMax = new Vector2(1, 0.5f);
            selRT.pivot     = new Vector2(1, 0.5f);
            selRT.sizeDelta = new Vector2(100, 56);
            selRT.anchoredPosition = new Vector2(-12, 0);

            GameObject selTxtGO = new GameObject("BtnText", typeof(RectTransform));
            selTxtGO.transform.SetParent(selBtnGO.transform, false);
            TextMeshProUGUI selTMP = selTxtGO.AddComponent<TextMeshProUGUI>();
            selTMP.text      = "선택";
            selTMP.fontSize  = 22;
            selTMP.fontStyle = FontStyles.Bold;
            selTMP.alignment = TextAlignmentOptions.Center;
            selTMP.color     = Color.white;
            if (s_Font != null) selTMP.font = s_Font;
            var selTxtRT = selTxtGO.GetComponent<RectTransform>();
            selTxtRT.anchorMin = Vector2.zero;
            selTxtRT.anchorMax = Vector2.one;
            selTxtRT.offsetMin = Vector2.zero;
            selTxtRT.offsetMax = Vector2.zero;

            // UIPlantSelectSlot 연결
            UIPlantSelectSlot slot = root.AddComponent<UIPlantSelectSlot>();
            slot.iconImage    = iconImg;
            slot.nameText     = nameTMP;
            slot.stockText    = stockTMP;
            slot.selectButton = selBtn;
            slot.lockOverlay  = lockOvGO.GetComponent<Image>();
            slot.reqLevelText = reqTMP;

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        // ─── 참조 연결 ─────────────────────────────────────────
        static void LinkReferences(GameObject canvas, GameObject farmPanel, GameObject labPanel, GameObject lobbyPanel,
                                   GameObject gridArea, GameObject plantListPanel,
                                   GameObject gm, GameObject fm)
        {
            // UIFarmGridView ← 셀 프리팹
            UIFarmGridView gridView = gridArea.GetComponent<UIFarmGridView>();
            if (gridView != null)
            {
                string cellPrefabPath = $"{PREFAB_DIR}/FarmPlotCell.prefab";
                gridView.plotCellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cellPrefabPath);
            }

            // UIPlantSelectPanel ← 슬롯 프리팹
            UIPlantSelectPanel psp = plantListPanel.GetComponent<UIPlantSelectPanel>();
            if (psp != null)
            {
                string slotPrefabPath = $"{PREFAB_DIR}/PlantSelectSlot.prefab";
                psp.plantSlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(slotPrefabPath);
            }

            // CultivationUIManager 구성 (FarmPanel에 추가)
            CultivationUIManager cuim = farmPanel.AddComponent<CultivationUIManager>();
            cuim.farmGrid        = fm.GetComponent<FarmGridManager>();
            cuim.plantSelectPanel = psp;

            var goldTxt    = GameObject.Find("GoldText")?.GetComponent<TextMeshProUGUI>();
            var statusTxt  = GameObject.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
            cuim.statusText = statusTxt;

            // UIGoldDisplay (TopBar에 추가)
            UIGoldDisplay ugd = canvas.AddComponent<UIGoldDisplay>();
            ugd.goldText = goldTxt;
            cuim.goldDisplay = ugd;

            // 하단 버튼 연결
            cuim.waterAllButton   = GameObject.Find("WaterAllButton")?.GetComponent<Button>();
            cuim.plantAllButton   = GameObject.Find("PlantAllButton")?.GetComponent<Button>();
            cuim.harvestAllButton = GameObject.Find("HarvestAllButton")?.GetComponent<Button>();

            // UILobbyPanel (Canvas에 추가)
            UILobbyPanel lobbyCtrl = canvas.AddComponent<UILobbyPanel>();
            lobbyCtrl.lobbyPanel = lobbyPanel;
            lobbyCtrl.farmPanel  = farmPanel;
            lobbyCtrl.labPanel   = labPanel;

            lobbyCtrl.farmButton    = GameObject.Find("LobbyBtnFarm")?.GetComponent<Button>();
            lobbyCtrl.labButton     = GameObject.Find("LobbyBtnLab")?.GetComponent<Button>();
            lobbyCtrl.factoryButton = GameObject.Find("LobbyBtnFactory")?.GetComponent<Button>();
            lobbyCtrl.marketButton  = GameObject.Find("LobbyBtnMarket")?.GetComponent<Button>();

            lobbyCtrl.farmBackButton = GameObject.Find("FarmBackButton")?.GetComponent<Button>();
            lobbyCtrl.labBackButton  = GameObject.Find("LabBackButton")?.GetComponent<Button>();

            lobbyCtrl.alertWindow      = GameObject.Find("LobbyAlertWindow");
            lobbyCtrl.alertText        = lobbyCtrl.alertWindow?.transform.Find("MessageText")?.GetComponent<TextMeshProUGUI>();
            lobbyCtrl.alertCloseButton = lobbyCtrl.alertWindow?.transform.Find("AlertCloseButton")?.GetComponent<Button>();
        }

        // ─── 유틸 헬퍼 ─────────────────────────────────────────
        static GameObject MakeImg(GameObject parent, string name,
            Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos,
            Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent.transform, false);
            Image img = go.GetComponent<Image>();
            img.color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin       = anchorMin;
            rt.anchorMax       = anchorMax;
            rt.pivot           = pivot;
            rt.sizeDelta       = size;
            rt.anchoredPosition = anchoredPos;
            return go;
        }

        static TextMeshProUGUI MakeTMP(GameObject parent, string name,
            Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos,
            string text, float fontSize, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.alignment = align;
            tmp.color     = Color.white;
            if (s_Font != null) tmp.font = s_Font;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin       = anchorMin;
            rt.anchorMax       = anchorMax;
            rt.pivot           = pivot;
            rt.sizeDelta       = size;
            rt.anchoredPosition = anchoredPos;
            return tmp;
        }

        static GameObject MakeButton(GameObject parent, string name,
            Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos,
            Color color, string label, float fontSize)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent.transform, false);
            Image img = go.GetComponent<Image>();
            img.color = color;
            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin       = anchorMin;
            rt.anchorMax       = anchorMax;
            rt.pivot           = pivot;
            rt.sizeDelta       = size;
            rt.anchoredPosition = anchoredPos;

            if (!string.IsNullOrEmpty(label))
                MakeTMP(go, "Text", size, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), Vector2.zero, label, fontSize, TextAlignmentOptions.Center);

            return go;
        }

        static void AddOutline(GameObject go, Color color)
        {
            Outline ol = go.AddComponent<Outline>();
            ol.effectColor    = color;
            ol.effectDistance = new Vector2(2, -2);
        }

        static void RoundedCorner(GameObject go)
        {
            // Unity 기본 UI에는 둥근 모서리가 없어 Outline으로 대체
            AddOutline(go, new Color(0.5f, 0.45f, 0.20f));
        }

        static void ApplyFontToAllTexts()
        {
            if (s_Font == null) return;
            // 씬 내의 비활성화된 텍스트 컴포넌트들까지 포함하여 전부 탐색
            var allTMPs = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var tmp in allTMPs)
            {
                tmp.font = s_Font;
            }
            Debug.Log($"[CultivationSetup] 씬 내부의 총 {allTMPs.Length}개 TextMeshProUGUI 요소에 NotoSans 폰트 강제 적용 완료!");
        }
    }
}

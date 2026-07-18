using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace WeedHoldings.EditorTools
{
    /// <summary>
    /// 새로 만든 버튼 이미지 16개 + 잠금 아이콘(Rock) + 배경음악(BGM) + 새 ssr_ship 아트를
    /// 프로젝트/씬에 한 번에 연결하는 배치용 에디터 스크립트.
    /// </summary>
    public static class GameAssetSetupTool
    {
        const string ScenePath = "Assets/Scenes/Proto_03.unity";
        const string ButtonsDir = "Assets/UI/Buttons";
        const string RockPath = "Assets/Resources/UI/Rock.png";
        const string BgmPath = "Assets/Audio/BGM/Soft Morning Breeze.mp3";
        const string SsrShipPath = "Assets/Resources/Cards/ssr_ship.png";

        // GameObject 이름 -> Canvas 기준 계층 경로. 씬이 비활성 패널을 갖고 있어도
        // Transform.Find는(GameObject.Find와 달리) 비활성 오브젝트도 찾아낸다.
        static readonly (string path, string imageFile)[] ButtonTargets = new[]
        {
            ("Panel/Farm_Button", "Top_Farm_Button.png"),
            ("Panel/Potion_Button", "Top_Factory_Button.png"),
            ("Panel/Sell_Button", "Top_Market_Button.png"),
            ("Panel/Laboratory_Button", "Top_Lab_Button.png"),
            ("Panel/Gacha_Button", "Top_Gacha_Button.png"),
            ("Panel/Charactor_Button", "Top_Character_Button.png"),
            ("PotionPanel/Factory_Panel/Craft_Button", "Crafting_Medicine_Button.png"),
            ("FarmPanel/All_Harvest", "Harvest_the_Plants_Button.png"),
            ("SellPanel/Sell_Panel/Sell_Button", "Set_sail_Button.png"),
            ("FarmPanel/ALL_Plant", "Sow_the_Seeds_Button.png"),
            ("FarmPanel/Water_Button", "Water_the_Plants_Button.png"),
            ("LaboratoryPanel/Upgrade/Factory_Upgrade_Panel/Factory_Upgrade", "Upgrading_Factory_Button.png"),
            ("LaboratoryPanel/Upgrade/Farm_Upgrade_Panel/Farm_Upgrade", "Upgrading_Farm_Button.png"),
            ("LaboratoryPanel/Upgrade/Sell_Upgrade_Panel/Sell_Upgrade", "Upgrading_Market_Button.png"),
            ("GachaPanel/Main/Gacha_1", "Gacha_1_Button.png"),
            ("GachaPanel/Main/Gacha_10", "Gacha_10_Button.png"),
        };

        [MenuItem("Tools/게임 에셋 일괄 설정/전체 실행")]
        public static void SetupAll()
        {
            try
            {
                ImportButtonSprites();
                ImportRockSprite();
                ReimportSsrShip();
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

                int applied = ApplyButtonImages();
                SetupBgm();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[GameAssetSetupTool] 완료: 버튼 이미지 {applied}/{ButtonTargets.Length}개 연결, BGM 설정 완료.");
                Succeed();
            }
            catch (Exception e)
            {
                Debug.LogError("[GameAssetSetupTool] 예외 발생: " + e);
                Fail();
            }
        }

        static void ImportButtonSprites()
        {
            foreach (var (_, imageFile) in ButtonTargets)
            {
                string path = ButtonsDir + "/" + imageFile;
                ForceSpriteImport(path);
            }
        }

        static void ImportRockSprite()
        {
            ForceSpriteImport(RockPath);
        }

        static void ReimportSsrShip()
        {
            AssetDatabase.ImportAsset(SsrShipPath, ImportAssetOptions.ForceUpdate);
        }

        static void ForceSpriteImport(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("[GameAssetSetupTool] TextureImporter를 못 찾음: " + assetPath);
                return;
            }
            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
            if (importer.alphaIsTransparency == false) { importer.alphaIsTransparency = true; changed = true; }
            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        static Transform FindCanvasChild(Transform canvas, string path)
        {
            return canvas.Find(path);
        }

        static int ApplyButtonImages()
        {
            var canvasGO = GameObject.Find("Canvas");
            if (canvasGO == null)
            {
                Debug.LogError("[GameAssetSetupTool] Canvas를 찾지 못함");
                return 0;
            }
            Transform canvas = canvasGO.transform;

            int applied = 0;
            foreach (var (path, imageFile) in ButtonTargets)
            {
                Transform target = FindCanvasChild(canvas, path);
                if (target == null)
                {
                    Debug.LogWarning($"[GameAssetSetupTool] 대상을 못 찾음: Canvas/{path}");
                    continue;
                }
                var image = target.GetComponent<Image>();
                if (image == null)
                {
                    Debug.LogWarning($"[GameAssetSetupTool] Image 컴포넌트 없음: Canvas/{path}");
                    continue;
                }
                string assetPath = ButtonsDir + "/" + imageFile;
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite == null)
                {
                    Debug.LogWarning($"[GameAssetSetupTool] 스프라이트 로드 실패: {assetPath}");
                    continue;
                }
                image.sprite = sprite;
                image.preserveAspect = true; // 비율 유지 - 늘어나거나 찌그러지지 않게
                image.type = Image.Type.Simple;
                EditorUtility.SetDirty(image);
                applied++;
            }
            return applied;
        }

        static void SetupBgm()
        {
            var canvasGO = GameObject.Find("Canvas");
            if (canvasGO == null)
            {
                Debug.LogError("[GameAssetSetupTool] Canvas를 찾지 못해 BGM 설정 실패");
                return;
            }

            AssetDatabase.ImportAsset(BgmPath, ImportAssetOptions.ForceUpdate);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(BgmPath);
            if (clip == null)
            {
                Debug.LogError("[GameAssetSetupTool] BGM 클립 로드 실패: " + BgmPath);
                return;
            }

            var source = canvasGO.GetComponent<AudioSource>();
            if (source == null) source = canvasGO.AddComponent<AudioSource>();

            source.clip = clip;
            source.loop = true;
            source.playOnAwake = true;
            source.volume = 0.4f;
            source.spatialBlend = 0f; // 2D
            EditorUtility.SetDirty(source);
        }

        static void Succeed()
        {
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void Fail()
        {
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }

        // =====================================================================================
        // Wave 2: 네비게이션 버튼 이미지, 버튼 캡션 제거, Ship/Factory Track 배경, 로비 배경,
        //         업그레이드 아이콘, 밭 배경, 빈 패널 색 채우기
        // =====================================================================================

        const string BackgroundsDir = "Assets/UI/Backgrounds";
        const string IconsDir = "Assets/UI/Icons";

        static readonly (string name, string imageFile)[] NavButtonTargets = new[]
        {
            ("Farm_Nav_Button", "Top_Farm_Button.png"),
            ("Potion_Nav_Button", "Top_Factory_Button.png"),
            ("Sell_Nav_Button", "Top_Market_Button.png"),
            ("Lab_Nav_Button", "Top_Lab_Button.png"),
            ("Gacha_Nav_Button", "Top_Gacha_Button.png"),
            ("Char_Nav_Button", "Top_Character_Button.png"),
        };

        static readonly string[] ShipTrackNames = { "Ship_Track_01", "Ship_Track_02", "Ship_Track_03", "Ship_Track_04" };
        static readonly string[] FactoryTrackNames = { "Track_01", "Track_02", "Track_03", "Track_04", "Track_05", "Track_06" };

        [MenuItem("Tools/게임 에셋 일괄 설정/2차 - 배경+아이콘+텍스트정리")]
        public static void SetupWave2()
        {
            try
            {
                ImportWave2Sprites();
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

                var canvasGO = GameObject.Find("Canvas");
                if (canvasGO == null) throw new Exception("Canvas를 찾지 못함");
                Transform canvas = canvasGO.transform;

                int navApplied = ApplyNavButtonImages(canvas);
                int captionsOff = DisableButtonCaptions(canvas);
                int shipBgApplied = ApplyShipTrackBackgrounds(canvas);
                int trackBgApplied = ApplyFactoryTrackBackgrounds(canvas);
                bool lobbyOk = ApplyLobbyBackground(canvas);
                int iconsApplied = ApplyUpgradeIcons(canvas);
                int fieldsApplied = ApplyFieldBackgrounds(canvas);
                int panelsApplied = ApplyEmptyPanelBackgrounds(canvas);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[GameAssetSetupTool] Wave2 완료: 네비버튼 {navApplied}/6, 캡션제거 {captionsOff}, " +
                          $"쉽트랙 {shipBgApplied}/4, 팩토리트랙 {trackBgApplied}/6, 로비배경 {lobbyOk}, " +
                          $"업그레이드아이콘 {iconsApplied}/3, 밭 {fieldsApplied}개, 빈패널 {panelsApplied}개.");
                Succeed();
            }
            catch (Exception e)
            {
                Debug.LogError("[GameAssetSetupTool] Wave2 예외: " + e);
                Fail();
            }
        }

        static void ImportWave2Sprites()
        {
            foreach (var (_, imageFile) in NavButtonTargets)
                ForceSpriteImport(ButtonsDir + "/" + imageFile);

            foreach (var name in new[] { "Ship_Track_BG", "Factory_Track_BG", "Main_Panel_BG", "Field_BG",
                "Farm_Panel_BG", "Potion_Panel_BG", "Sell_Panel_BG", "Laboratory_Panel_BG", "Gacha_Panel_BG", "Charactor_Panel_BG" })
                ForceSpriteImport(BackgroundsDir + "/" + name + ".png");

            foreach (var name in new[] { "Farm_Panel", "Factory_Panel", "Sell_Panel", "Lab_Panel" })
                ForceSpriteImport(BackgroundsDir + "/" + name + ".png");
            ForceSpriteImport(BackgroundsDir + "/Char_Panel.jpeg");

            foreach (var name in new[] { "Farm_Icon", "Sell_Icon", "Factory_Icon" })
                ForceSpriteImport(IconsDir + "/" + name + ".png");
        }

        /// <summary>이름으로 전체 하위 트리를 재귀 탐색(비활성 오브젝트 포함).</summary>
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

        static int ApplyNavButtonImages(Transform canvas)
        {
            int applied = 0;
            foreach (var (name, imageFile) in NavButtonTargets)
            {
                Transform target = FindRecursive(canvas, name);
                if (target == null)
                {
                    Debug.LogWarning($"[GameAssetSetupTool] 네비버튼 대상을 못 찾음: Canvas/.../{name}");
                    continue;
                }
                var image = target.GetComponent<Image>();
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonsDir + "/" + imageFile);
                if (image == null || sprite == null) continue;
                image.sprite = sprite;
                image.preserveAspect = true;
                EditorUtility.SetDirty(image);
                applied++;
            }
            return applied;
        }

        /// <summary>16개 버튼 자신의 정적 캡션(이제 이미지에 그려져 있어 중복)만 끈다.
        /// Ship_Track/Track/Field 처럼 상태에 따라 바뀌는 정보성 텍스트는 이 목록에 없으므로 안전하다.</summary>
        static int DisableButtonCaptions(Transform canvas)
        {
            int count = 0;
            foreach (var (path, _) in ButtonTargets)
            {
                Transform target = canvas.Find(path);
                if (target == null) continue;
                var label = target.GetComponentInChildren<TMP_Text>(true);
                if (label == null) continue;
                label.enabled = false;
                EditorUtility.SetDirty(label);
                count++;
            }

            // 상단바 네비게이션 버튼 6개는 ButtonTargets 목록에 없어서 위 루프에서 빠져 있었다 -
            // 이미지 자체에 이미 아이콘/문구가 그려져 있으므로 남아 있는 기본 캡션을 마저 지운다.
            foreach (var (name, _) in NavButtonTargets)
            {
                Transform target = FindRecursive(canvas, name);
                if (target == null) continue;
                var label = target.GetComponentInChildren<TMP_Text>(true);
                if (label == null) continue;
                label.enabled = false;
                EditorUtility.SetDirty(label);
                count++;
            }
            return count;
        }

        static int ApplyShipTrackBackgrounds(Transform canvas)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundsDir + "/Ship_Track_BG.png");
            if (sprite == null) return 0;
            int applied = 0;
            foreach (var name in ShipTrackNames)
            {
                var target = FindRecursive(canvas, name);
                var image = target?.GetComponent<Image>();
                if (image == null) continue;
                image.sprite = sprite;
                image.preserveAspect = true;
                EditorUtility.SetDirty(image);
                applied++;
            }
            return applied;
        }

        static int ApplyFactoryTrackBackgrounds(Transform canvas)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundsDir + "/Factory_Track_BG.png");
            if (sprite == null) return 0;
            int applied = 0;
            foreach (var name in FactoryTrackNames)
            {
                var track = FindRecursive(canvas, name);
                var bg = track?.Find("BG");
                var image = bg?.GetComponent<Image>();
                if (image == null) continue;
                image.sprite = sprite;
                image.preserveAspect = true;
                EditorUtility.SetDirty(image);
                applied++;
            }
            return applied;
        }

        static bool ApplyLobbyBackground(Transform canvas)
        {
            var lobby = canvas.Find("Panel");
            if (lobby == null) return false;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundsDir + "/Main_Panel_BG.png");
            if (sprite == null) return false;

            var image = lobby.GetComponent<Image>();
            if (image == null) image = lobby.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = false;
            image.type = Image.Type.Simple;
            EditorUtility.SetDirty(image);
            return true;
        }

        static int ApplyUpgradeIcons(Transform canvas)
        {
            var targets = new (string path, string iconFile)[]
            {
                ("LaboratoryPanel/Upgrade/Farm_Upgrade_Panel/Farm_Icon", "Farm_Icon"),
                ("LaboratoryPanel/Upgrade/Factory_Upgrade_Panel/Factory_Icon", "Factory_Icon"),
                ("LaboratoryPanel/Upgrade/Sell_Upgrade_Panel/Sell_Icon", "Sell_Icon"),
            };
            int applied = 0;
            foreach (var (path, iconFile) in targets)
            {
                var target = canvas.Find(path);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconsDir + "/" + iconFile + ".png");
                if (target == null || sprite == null)
                {
                    Debug.LogWarning($"[GameAssetSetupTool] 업그레이드 아이콘 대상을 못 찾음: Canvas/{path}");
                    continue;
                }
                var image = target.GetComponent<Image>();
                if (image == null) image = target.gameObject.AddComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                EditorUtility.SetDirty(image);
                applied++;
            }
            return applied;
        }

        /// <summary>
        /// FieldPlot 스크립트는 FieldManager가 런타임(Awake)에서야 AddComponent로 붙이기 때문에
        /// 에디터(편집 모드)에는 그 컴포넌트가 아예 존재하지 않는다 - 대신 "Field_XX"라는 이름의
        /// 밭 칸 GameObject를 이름으로 직접 찾아 그 Image를 바꾼다.
        /// </summary>
        static int ApplyFieldBackgrounds(Transform canvas)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundsDir + "/Field_BG.png");
            if (sprite == null) return 0;

            int applied = 0;
            var all = canvas.GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                if (!t.name.StartsWith("Field_")) continue;
                var image = t.GetComponent<Image>();
                if (image == null) continue;
                image.sprite = sprite;
                EditorUtility.SetDirty(image);
                applied++;
            }
            return applied;
        }

        static int ApplyEmptyPanelBackgrounds(Transform canvas)
        {
            // 사용자가 각 패널에 맞는 고해상도 배경 원화(Farm_Panel/Factory_Panel/Lab_Panel/Sell_Panel/
            // Char_Panel)를 직접 추가해줘서, 기존에 자리만 채우던 저해상도 *_Panel_BG 플레이스홀더 대신
            // 이 그림들을 쓴다. GachaPanel은 Wave5에서 이미 Gacha_Panel_01로 교체됐으므로 여기서 건드리지 않는다.
            var targets = new (string path, string bgFile, bool addIfMissing)[]
            {
                ("FarmPanel", "Farm_Panel", true),
                ("PotionPanel", "Factory_Panel", true),
                ("SellPanel", "Sell_Panel", true),
                ("LaboratoryPanel", "Lab_Panel", true),
                ("CharactorPanel", "Char_Panel", false),
            };
            int applied = 0;
            foreach (var (path, bgFile, addIfMissing) in targets)
            {
                var target = canvas.Find(path);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundsDir + "/" + bgFile + (bgFile == "Char_Panel" ? ".jpeg" : ".png"));
                if (target == null || sprite == null)
                {
                    Debug.LogWarning($"[GameAssetSetupTool] 빈 패널 대상을 못 찾음: Canvas/{path}");
                    continue;
                }
                var image = target.GetComponent<Image>();
                if (image == null)
                {
                    if (!addIfMissing) continue;
                    image = target.gameObject.AddComponent<Image>();
                }
                image.sprite = sprite;
                image.color = Color.white;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                EditorUtility.SetDirty(image);
                applied++;
            }
            return applied;
        }

        // =====================================================================================
        // Wave 3: 로비 버튼 6개를 Main_Panel 배경 그림 속 건물 위치/크기에 맞춰 재배치하고,
        //         농장 패널 하단 일괄작업 버튼 3개를 화면 비율에 맞게 확대.
        // =====================================================================================

        /// <summary>사용자가 제공한 로비 화면 참고 이미지("Assets/UI/로비 화면.png", 2808x1605)에서
        /// 실제 버튼 캡슐 위치를 실측한 픽셀 영역을 Unity 앵커 좌표(0~1, Y축은 위가 1)로 변환한 값.
        /// 참고 이미지의 건물 배치가 Main_Panel_BG.png와 동일한 원화이고 Panel 배경이 꽉 채워
        /// 늘어나 있으므로, 이미지 속 좌표 비율이 곧 Panel 앵커 비율과 같다.</summary>
        // 참고 이미지 실측 캡슐 크기 그대로는 화면에서 작아 보인다는 피드백을 받아, 같은 중심점을
        // 기준으로 가로/세로 35% 확대했다(이웃 버튼과 안 겹치는 선까지).
        static readonly (string name, float xMin, float yMin, float xMax, float yMax)[] LobbyButtonLayout = new[]
        {
            ("Farm_Button", 0.1191f, 0.4462f, 0.3811f, 0.5850f),
            ("Potion_Button", 0.3995f, 0.4462f, 0.6012f, 0.5850f),
            ("Sell_Button", 0.6747f, 0.4499f, 0.8815f, 0.5845f),
            ("Laboratory_Button", 0.0906f, 0.0777f, 0.3670f, 0.1997f),
            ("Charactor_Button", 0.3768f, 0.0777f, 0.6436f, 0.1997f),
            ("Gacha_Button", 0.6653f, 0.0777f, 0.9035f, 0.1997f),
        };

        static readonly string[] FarmBulkButtonNames = { "ALL_Plant", "All_Harvest", "Water_Button" };

        [MenuItem("Tools/게임 에셋 일괄 설정/3차 - 로비 배치 + 농장버튼 크기")]
        public static void SetupWave3()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var canvasGO = GameObject.Find("Canvas");
                if (canvasGO == null) throw new Exception("Canvas를 찾지 못함");
                Transform canvas = canvasGO.transform;

                var lobbyPanel = canvas.Find("Panel");
                int lobbyApplied = 0;
                if (lobbyPanel != null)
                {
                    foreach (var (name, xMin, yMin, xMax, yMax) in LobbyButtonLayout)
                    {
                        // Sell_Button은 로비/무역소 두 곳에 이름이 겹치므로 반드시 Panel 바로 아래에서만 찾는다.
                        var target = lobbyPanel.Find(name);
                        var rect = target as RectTransform;
                        if (rect == null) continue;

                        rect.anchorMin = new Vector2(xMin, yMin);
                        rect.anchorMax = new Vector2(xMax, yMax);
                        rect.anchoredPosition = Vector2.zero;
                        rect.sizeDelta = new Vector2(-6f, -6f); // 앵커 박스보다 살짝만 안쪽으로
                        EditorUtility.SetDirty(rect);
                        lobbyApplied++;
                    }
                }

                var farmPanel = canvas.Find("FarmPanel");
                int farmApplied = 0;
                if (farmPanel != null)
                {
                    foreach (var name in FarmBulkButtonNames)
                    {
                        var target = farmPanel.Find(name);
                        var rect = target as RectTransform;
                        if (rect == null) continue;

                        rect.sizeDelta = new Vector2(340f, 286f); // 기존 205x97 대비 큰 정사각형에 가깝게(버튼 이미지 비율)
                        EditorUtility.SetDirty(rect);
                        farmApplied++;
                    }
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);

                Debug.Log($"[GameAssetSetupTool] Wave3 완료: 로비버튼 배치 {lobbyApplied}/6, 농장 일괄버튼 확대 {farmApplied}/3.");
                Succeed();
            }
            catch (Exception e)
            {
                Debug.LogError("[GameAssetSetupTool] Wave3 예외: " + e);
                Fail();
            }
        }

        // =====================================================================================
        // Wave 4: 뽑기 연출 패널 반투명 오버레이 완화, 뽑기 버튼 2개 확대, 밭 칸 이미지를
        //         큰 그림 대신 단색 매쉬로 교체.
        // =====================================================================================

        static readonly string[] FieldObjectPrefix = { "Field_" };

        [MenuItem("Tools/게임 에셋 일괄 설정/4차 - 뽑기 연출 투명도 + 뽑기버튼 크기 + 밭 매쉬화")]
        public static void SetupWave4()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var canvasGO = GameObject.Find("Canvas");
                if (canvasGO == null) throw new Exception("Canvas를 찾지 못함");
                Transform canvas = canvasGO.transform;

                bool overlayFixed = LightenGachaRevealOverlay(canvas);
                int gachaResized = ResizeGachaPullButtons(canvas);
                int fieldsFlattened = FlattenFieldBackgrounds(canvas);
                bool settingWired = EnsureSettingPanelController(canvasGO);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);

                Debug.Log($"[GameAssetSetupTool] Wave4 완료: 뽑기연출 오버레이 완화 {overlayFixed}, " +
                          $"뽑기버튼 크기조절 {gachaResized}/2, 밭 매쉬화 {fieldsFlattened}개, 설정팝업 연결 {settingWired}.");
                Succeed();
            }
            catch (Exception e)
            {
                Debug.LogError("[GameAssetSetupTool] Wave4 예외: " + e);
                Fail();
            }
        }

        /// <summary>GachaPanel/Gacha_Panel_Child는 결과 공개 연출 중 화면을 어둡게 깔아주는
        /// 오버레이인데, 알파가 너무 짙어(0.392) 그 위의 배(카드) 이미지까지 흐릿해 보인다.
        /// 연출 분위기는 유지하되 훨씬 옅게 낮춘다.</summary>
        static bool LightenGachaRevealOverlay(Transform canvas)
        {
            var gachaPanel = canvas.Find("GachaPanel");
            var overlay = gachaPanel != null ? gachaPanel.Find("Gacha_Panel_Child") : null;
            var image = overlay != null ? overlay.GetComponent<Image>() : null;
            if (image == null) return false;

            var c = image.color;
            c.a = 0.12f;
            image.color = c;
            EditorUtility.SetDirty(image);
            return true;
        }

        /// <summary>UI 기획서(뽑기화면) 참고 비율대로 1회/10회 뽑기 버튼을 키운다. 실제 실행 중에는
        /// UIGachaController.MatchButtonSize()가 두 버튼 중 큰 쪽으로 다시 맞추므로 두 값을 동일하게 준다.</summary>
        static int ResizeGachaPullButtons(Transform canvas)
        {
            var mainPanel = canvas.Find("GachaPanel/Main");
            if (mainPanel == null) return 0;

            int applied = 0;
            foreach (var name in new[] { "Gacha_1", "Gacha_10" })
            {
                var rect = mainPanel.Find(name) as RectTransform;
                if (rect == null) continue;
                rect.sizeDelta = new Vector2(270f, 118f);
                EditorUtility.SetDirty(rect);
                applied++;
            }
            return applied;
        }

        static readonly string[] RegionLabelFiles =
        {
            "Region_Label_Island", "Region_Label_Polar", "Region_Label_Desert",
            "Region_Label_Grassland", "Region_Label_Royal", "Region_Label_Locked",
        };

        [MenuItem("Tools/게임 에셋 일괄 설정/지역 라벨 이미지 스프라이트로 가져오기")]
        public static void ImportRegionLabelSprites()
        {
            foreach (var name in RegionLabelFiles)
                ForceSpriteImport("Assets/Resources/Regions/" + name + ".png");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[GameAssetSetupTool] 지역 라벨 스프라이트 가져오기 완료: {RegionLabelFiles.Length}개.");
            Succeed();
        }

        static readonly string[] NewUiResourceSprites =
        {
            "Back_Button", "Game_End", "Craft_Locked", "Craft_Progress", "Craft_Success", "Craft_Fail", "Setting_Button",
        };

        [MenuItem("Tools/게임 에셋 일괄 설정/뒤로가기+제조트랙+설정 버튼 이미지 스프라이트로 가져오기")]
        public static void ImportNewUiResourceSprites()
        {
            foreach (var name in NewUiResourceSprites)
                ForceSpriteImport("Assets/Resources/UI/" + name + ".png");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[GameAssetSetupTool] 새 UI 리소스 스프라이트 가져오기 완료: {NewUiResourceSprites.Length}개.");
            Succeed();
        }

        /// <summary>상단바의 빈 "Setting" 버튼에 해상도/사운드 팝업 로직(UISettingPanelController)을
        /// 연결한다. 팝업 UI 자체는 그 스크립트가 런타임에 직접 생성하므로 씬에는 컴포넌트만 붙이면 된다.</summary>
        static bool EnsureSettingPanelController(GameObject canvasGO)
        {
            if (canvasGO.GetComponent<WeedHoldings.UISettingPanelController>() == null)
                canvasGO.AddComponent<WeedHoldings.UISettingPanelController>();
            EditorUtility.SetDirty(canvasGO);
            return true;
        }

        /// <summary>Field.png 큰 이미지를 걷어내고 FieldPlot이 상태별로 입히는 색(흰색/주황/빨강/갈색/
        /// 회색) 틴트만 남은 단색 매쉬로 되돌린다.</summary>
        static int FlattenFieldBackgrounds(Transform canvas)
        {
            int applied = 0;
            var all = canvas.GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                bool isField = false;
                foreach (var prefix in FieldObjectPrefix)
                {
                    if (t.name.StartsWith(prefix)) { isField = true; break; }
                }
                if (!isField) continue;

                var image = t.GetComponent<Image>();
                if (image == null) continue;
                image.sprite = null;
                EditorUtility.SetDirty(image);
                applied++;
            }
            return applied;
        }

        // =====================================================================================
        // Wave 5: 가챠 메인 배경 저해상도 흐림 수정, Av_Char/Gacha_Panel_01 삽입, 밭 이미지 복원.
        // =====================================================================================

        const string AvCharPath = IconsDir + "/Av_Char.png";
        const string GachaPanel01Path = BackgroundsDir + "/Gacha_Panel_01.png";

        [MenuItem("Tools/게임 에셋 일괄 설정/5차 - 가챠배경 교체 + AvChar + 밭 복원")]
        public static void SetupWave5()
        {
            try
            {
                ForceSpriteImport(AvCharPath);
                ForceSpriteImport(GachaPanel01Path);

                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var canvasGO = GameObject.Find("Canvas");
                if (canvasGO == null) throw new Exception("Canvas를 찾지 못함");
                Transform canvas = canvasGO.transform;

                bool gachaBgFixed = ReplaceGachaMainBackground(canvas);
                bool charEquipBgApplied = ApplyCharEquipBackground(canvas);
                bool avCharApplied = ApplyCharAvImage(canvas);
                int fieldsRestored = ApplyFieldBackgrounds(canvas);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[GameAssetSetupTool] Wave5 완료: 가챠배경 교체 {gachaBgFixed}, " +
                          $"캐릭터확인화면 배경 {charEquipBgApplied}, Char_AV 이미지 {avCharApplied}, 밭 이미지 복원 {fieldsRestored}개.");
                Succeed();
            }
            catch (Exception e)
            {
                Debug.LogError("[GameAssetSetupTool] Wave5 예외: " + e);
                Fail();
            }
        }

        /// <summary>GachaPanel 루트 배경이 512x512짜리 저해상도 이미지(Gacha_Panel_BG)를 화면 전체로
        /// 늘려써서 흐리게 보였다. 실제 항구 원화인 Gacha_Panel_01(1672x941)로 교체해 해상도 문제를
        /// 근본적으로 해결한다. Gacha_Panel_Child(연출 오버레이)는 이 배경 위에 얇은 어둠만 덧씌우므로
        /// 뽑기 연출 화면에서도 같은 배경이 자연스럽게 비쳐 보인다.</summary>
        static bool ReplaceGachaMainBackground(Transform canvas)
        {
            var gachaPanel = canvas.Find("GachaPanel");
            var image = gachaPanel != null ? gachaPanel.GetComponent<Image>() : null;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GachaPanel01Path);
            if (image == null || sprite == null) return false;

            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            EditorUtility.SetDirty(image);
            return true;
        }

        /// <summary>CharactorPanel/Char_equip("캐릭터 확인" 상세/장착 화면)은 기본 회색 반투명
        /// 백드롭만 있었다. 같은 항구 원화를 배경으로 깔아 준다.</summary>
        static bool ApplyCharEquipBackground(Transform canvas)
        {
            var target = canvas.Find("CharactorPanel/Char_equip");
            var image = target != null ? target.GetComponent<Image>() : null;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GachaPanel01Path);
            if (image == null || sprite == null) return false;

            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            EditorUtility.SetDirty(image);
            return true;
        }

        /// <summary>뽑기 버튼 위의 빈 이미지 오브젝트(Char_AV)에 확률표 이미지(Av_Char)를 끼워 넣는다.</summary>
        static bool ApplyCharAvImage(Transform canvas)
        {
            var target = canvas.Find("GachaPanel/Main/Char_AV");
            var image = target != null ? target.GetComponent<Image>() : null;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AvCharPath);
            if (image == null || sprite == null) return false;

            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            EditorUtility.SetDirty(image);
            return true;
        }

        // =====================================================================================
        // Wave 6: PotionPanel/Factory_Panel의 Title 텍스트 정리 + 보약/식물 연구 리스트에 남아있던
        //         중복 헤더(New Text) 비활성화.
        // =====================================================================================

        [MenuItem("Tools/게임 에셋 일괄 설정/6차 - Factory_Panel Title 정리 + 중복 헤더 비활성화")]
        public static void SetupWave6()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var canvasGO = GameObject.Find("Canvas");
                if (canvasGO == null) throw new Exception("Canvas를 찾지 못함");
                Transform canvas = canvasGO.transform;

                bool titleFixed = FixFactoryPanelTitle(canvas);
                int staleHeadersDisabled = DisableStaleUnlockListHeaders(canvas);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);

                Debug.Log($"[GameAssetSetupTool] Wave6 완료: Factory_Panel Title 정리 {titleFixed}, " +
                          $"중복 헤더 비활성화 {staleHeadersDisabled}개.");
                Succeed();
            }
            catch (Exception e)
            {
                Debug.LogError("[GameAssetSetupTool] Wave6 예외: " + e);
                Fail();
            }
        }

        /// <summary>PotionPanel/Factory_Panel/Title은 "New Text" 그대로에 Factory_Panel 정중앙에
        /// 200x50 크기로 박혀 있어 다른 UI에 가려 보이지도 않았다. "보약 제조"로 내용을 채우고
        /// 좌상단 코너에 맞는 앵커/크기로 옮긴다.</summary>
        static bool FixFactoryPanelTitle(Transform canvas)
        {
            var title = canvas.Find("PotionPanel/Factory_Panel/Title");
            var text = title != null ? title.GetComponent<TMP_Text>() : null;
            var rect = title as RectTransform;
            if (text == null || rect == null) return false;

            text.text = "보약 제조";
            text.enableAutoSizing = true;
            text.fontSizeMin = 18f;
            text.fontSizeMax = 30f;
            text.alignment = TextAlignmentOptions.TopLeft;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(18f, -14f);
            rect.sizeDelta = new Vector2(220f, 44f);

            EditorUtility.SetDirty(text);
            EditorUtility.SetDirty(rect);
            return true;
        }

        /// <summary>UIPotionUnlockPanel/UIPlantUnlockPanel이 코드에서 만드는 작은 상단 "Header_Text"
        /// ("보약 종류"/"식물 종류")와는 별개로, 씬에 미리 준비돼 있던 큰 부제 텍스트("Text (TMP) (1)",
        /// 497x79)가 있다 - 원래 "보약 합성 연구"/"식물 품종 연구"라는 내용이 들어갈 자리였는데 내용이
        /// 비어 있어("New Text") 이전에 실수로 꺼버렸다. 다시 켜고 원래 채워졌어야 할 문구를 넣는다.</summary>
        static int DisableStaleUnlockListHeaders(Transform canvas)
        {
            var entries = new (string path, string text)[]
            {
                ("LaboratoryPanel/Potion_Unlock/Potion_Unlock_List/Text (TMP) (1)", "보약 합성 연구"),
                ("LaboratoryPanel/Plant_Unlock/Plant_Unlock_List/Text (TMP) (1)", "식물 품종 연구"),
            };
            int count = 0;
            foreach (var (path, label) in entries)
            {
                var target = canvas.Find(path);
                var text = target != null ? target.GetComponent<TMP_Text>() : null;
                if (target == null || text == null) continue;

                target.gameObject.SetActive(true);
                text.text = label;
                EditorUtility.SetDirty(target.gameObject);
                EditorUtility.SetDirty(text);
                count++;
            }
            return count;
        }

        // =====================================================================================
        // Wave 7: 상단바 네비게이션 퀵이동 버튼 6개를 비율 유지한 채 25% 확대.
        // =====================================================================================

        [MenuItem("Tools/게임 에셋 일괄 설정/7차 - 상단바 네비 버튼 25% 확대")]
        public static void SetupWave7()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var canvasGO = GameObject.Find("Canvas");
                if (canvasGO == null) throw new Exception("Canvas를 찾지 못함");
                Transform canvas = canvasGO.transform;

                int resized = ResizeNavButtons(canvas);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);

                Debug.Log($"[GameAssetSetupTool] Wave7 완료: 네비 버튼 25% 확대 {resized}/6.");
                Succeed();
            }
            catch (Exception e)
            {
                Debug.LogError("[GameAssetSetupTool] Wave7 예외: " + e);
                Fail();
            }
        }

        const float NavButtonScale = 1.25f;

        /// <summary>네비 버튼 6개는 전부 같은 크기(140x68)로 170px 간격을 두고 왼쪽부터 나란히 배치돼
        /// 있다. 첫 버튼의 왼쪽 끝(로컬 x=30)을 기준점으로 삼아 위치/크기를 함께 1.25배 하면, 버튼
        /// 사이 간격까지 통째로 커져서 비율이 그대로 유지된 채 25% 확대된 것처럼 보인다.</summary>
        static int ResizeNavButtons(Transform canvas)
        {
            const float pivotX = 30f; // 원래 Farm_Nav_Button 좌측 끝(center 100 - half-width 70)

            int applied = 0;
            foreach (var (name, _) in NavButtonTargets)
            {
                var target = FindRecursive(canvas, name);
                var rect = target as RectTransform;
                if (rect == null) continue;

                var pos = rect.anchoredPosition;
                var size = rect.sizeDelta;

                pos.x = pivotX + (pos.x - pivotX) * NavButtonScale;
                size *= NavButtonScale;

                rect.anchoredPosition = pos;
                rect.sizeDelta = size;
                EditorUtility.SetDirty(rect);
                applied++;
            }
            return applied;
        }

        // =====================================================================================
        // Wave 8: 커스텀 배경 없이 유니티 기본 Background 스프라이트(알파 0.392)만 걸려있어 흐릿하게
        //         비쳐 보이던 하위 패널들을 불투명한 회색 톤으로 바꿔 가독성을 높인다.
        // =====================================================================================

        static readonly string[] TransparentPanelPaths =
        {
            "SellPanel/Ship_Track",
            "SellPanel/Region_Panel",
            "SellPanel/Sell_Info",
            "SellPanel/Time_Panel",
            "SellPanel/Inventory_Slot",
            "SellPanel/Sell_Panel", // 출항하기 버튼이 있는 하위 패널(최상위 SellPanel과 이름이 겹침)
            "PotionPanel/Track_Panel", // Wave9에서 SellPanel -> PotionPanel로 이동 정정됨
            "PotionPanel/Factory_Panel",
            "PotionPanel/Factory_Panel/Inventory_Plant",
            "PotionPanel/Factory_Panel/Potion_List_Panel",
            "PotionPanel/Factory_Panel/Potion_Info",
            "LaboratoryPanel/Upgrade/Farm_Upgrade_Panel",
            "LaboratoryPanel/Upgrade/Factory_Upgrade_Panel",
            "LaboratoryPanel/Upgrade/Sell_Upgrade_Panel",
            "LaboratoryPanel/Potion_Unlock/Potion_Unlock_List",
            "LaboratoryPanel/Plant_Unlock/Plant_Unlock_List",
            "CharactorPanel/Farm",
            "CharactorPanel/Factory",
            "CharactorPanel/Sell",
            // 열(Farm/Factory/Sell) 배경만으로는 그 위에 캐릭터 장착 칸(Charactor_01~03) 각자의
            // 기본 반투명 배경이 덮고 있어서 회색이 잘 안 보였다 - 칸 하나하나도 같이 칠한다.
            "CharactorPanel/Farm/Charactor_01",
            "CharactorPanel/Farm/Charactor_02",
            "CharactorPanel/Farm/Charactor_03",
            "CharactorPanel/Factory/Charactor_01",
            "CharactorPanel/Factory/Charactor_02",
            "CharactorPanel/Factory/Charactor_03",
            "CharactorPanel/Sell/Charactor_01",
            "CharactorPanel/Sell/Charactor_02",
            "CharactorPanel/Sell/Charactor_03",
            "CharactorPanel/Char_equip/Inventory",
            "CharactorPanel/Char_equip/Char_Info",
        };

        static readonly Color PanelGray = new Color(0.2f, 0.2f, 0.22f, 0.92f);

        [MenuItem("Tools/게임 에셋 일괄 설정/8차 - 투명 패널 회색으로 정리")]
        public static void SetupWave8()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var canvasGO = GameObject.Find("Canvas");
                if (canvasGO == null) throw new Exception("Canvas를 찾지 못함");
                Transform canvas = canvasGO.transform;

                int applied = 0;
                foreach (var path in TransparentPanelPaths)
                {
                    var target = canvas.Find(path);
                    var image = target != null ? target.GetComponent<Image>() : null;
                    if (image == null)
                    {
                        Debug.LogWarning($"[GameAssetSetupTool] 패널을 못 찾음: Canvas/{path}");
                        continue;
                    }
                    image.color = PanelGray;
                    EditorUtility.SetDirty(image);
                    applied++;
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);

                Debug.Log($"[GameAssetSetupTool] Wave8 완료: 투명 패널 회색 적용 {applied}/{TransparentPanelPaths.Length}.");
                Succeed();
            }
            catch (Exception e)
            {
                Debug.LogError("[GameAssetSetupTool] Wave8 예외: " + e);
                Fail();
            }
        }

        // =====================================================================================
        // Wave 9: 잘못 SellPanel 밑에 있던 Track_Panel(제조 트랙 6개)을 원래 있어야 할 PotionPanel
        //         밑으로 옮긴다. UILobbyNavigation.SetupFactoryWidgets()가 potionPanel.transform.Find
        //         ("Track_Panel")로 찾기 때문에, 지금 위치에서는 제조 트랙 위젯이 아예 연결되지 않는다.
        // =====================================================================================

        [MenuItem("Tools/게임 에셋 일괄 설정/9차 - Track_Panel을 PotionPanel 밑으로 이동")]
        public static void SetupWave9()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var canvasGO = GameObject.Find("Canvas");
                if (canvasGO == null) throw new Exception("Canvas를 찾지 못함");
                Transform canvas = canvasGO.transform;

                var trackPanel = canvas.Find("SellPanel/Track_Panel");
                var potionPanel = canvas.Find("PotionPanel");
                if (trackPanel == null) throw new Exception("SellPanel/Track_Panel을 찾지 못함(이미 옮겨졌을 수 있음)");
                if (potionPanel == null) throw new Exception("PotionPanel을 찾지 못함");

                trackPanel.SetParent(potionPanel, false);
                EditorUtility.SetDirty(trackPanel.gameObject);
                EditorUtility.SetDirty(potionPanel.gameObject);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);

                Debug.Log("[GameAssetSetupTool] Wave9 완료: Track_Panel을 PotionPanel 밑으로 이동함.");
                Succeed();
            }
            catch (Exception e)
            {
                Debug.LogError("[GameAssetSetupTool] Wave9 예외: " + e);
                Fail();
            }
        }

        static readonly string[] CharacterCardFiles =
        {
            "card_back", "r_card", "r_card2", "sr_card", "sr_card2", "ssr_card", "ssr_card2",
        };

        /// <summary>사용자가 투명 여백을 잘라 다시 넣어준 카드 프레임 이미지들을 다시 임포트한다.
        /// 크기가 파일마다 달라졌으므로(예: sr_card 200x293, ssr_card2 376x443) 이걸 쓰는 모든
        /// Image는 preserveAspect=true가 돼 있어야 비율이 안 깨진다(스크립트 쪽에서 이미 처리).</summary>
        [MenuItem("Tools/게임 에셋 일괄 설정/캐릭터 카드 이미지 다시 가져오기")]
        public static void ImportCharacterCardSprites()
        {
            foreach (var name in CharacterCardFiles)
                ForceSpriteImport("Assets/Resources/Cards/" + name + ".png");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[GameAssetSetupTool] 캐릭터 카드 이미지 재가져오기 완료: {CharacterCardFiles.Length}개.");
            Succeed();
        }

        // =====================================================================================
        // Wave 10: 네비 버튼 6개가 25% 확대 후 마지막(뽑기) 버튼이 Gold_Panel과 겹쳐서, 가로는
        //          줄이고 세로는 늘린 비율로 다시 배치한다.
        // =====================================================================================

        // 세로로 좀 더 긴 비율(160x95, 기존 140x68 대비 폭은 좁고 높이는 큼)로 간격 24px씩 나란히.
        // NavButtonsContainer 로컬 기준 첫 버튼 중심 x=110(왼쪽 끝 30에서 반너비 80만큼 안쪽).
        static readonly (string name, float centerX)[] NavButtonLayoutV2 =
        {
            ("Farm_Nav_Button", 110f),
            ("Potion_Nav_Button", 294f),
            ("Sell_Nav_Button", 478f),
            ("Lab_Nav_Button", 662f),
            ("Char_Nav_Button", 846f),
            ("Gacha_Nav_Button", 1030f),
        };
        static readonly Vector2 NavButtonSizeV2 = new Vector2(160f, 95f);

        [MenuItem("Tools/게임 에셋 일괄 설정/10차 - 네비 버튼 세로 비율로 재조정(골드패널 안 가림)")]
        public static void SetupWave10()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var canvasGO = GameObject.Find("Canvas");
                if (canvasGO == null) throw new Exception("Canvas를 찾지 못함");
                Transform canvas = canvasGO.transform;

                int applied = 0;
                foreach (var (name, centerX) in NavButtonLayoutV2)
                {
                    var target = FindRecursive(canvas, name);
                    var rect = target as RectTransform;
                    if (rect == null) continue;

                    var pos = rect.anchoredPosition;
                    pos.x = centerX;
                    rect.anchoredPosition = pos;
                    rect.sizeDelta = NavButtonSizeV2;
                    EditorUtility.SetDirty(rect);
                    applied++;
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);

                Debug.Log($"[GameAssetSetupTool] Wave10 완료: 네비 버튼 재배치 {applied}/6.");
                Succeed();
            }
            catch (Exception e)
            {
                Debug.LogError("[GameAssetSetupTool] Wave10 예외: " + e);
                Fail();
            }
        }

        // =====================================================================================
        // Wave 11: 밭 그리드 전체가 앉는 컨테이너(Field_Panel) 자체의 배경을 교체한다. 사용자가
        //          직접 프로젝트에 넣어준 Field_Panel.png(558x447, 나무/집/해바라기 장식이 테두리를
        //          둘러싼 그림)을 쓴다. Top_Panel(상단바)에 가리거나 위쪽이 잘리지 않도록 패널을
        //          Top_Panel 아래 가시 영역 안에 온전히 들어오는 최대 크기로 배치한다.
        // =====================================================================================

        [MenuItem("Tools/게임 에셋 일괄 설정/11차 - 밭 패널 컨테이너 배경 교체")]
        public static void SetupWave11()
        {
            try
            {
                const string bgPath = BackgroundsDir + "/Field_Panel.png";
                ForceSpriteImport(bgPath);

                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var canvasGO = GameObject.Find("Canvas");
                if (canvasGO == null) throw new Exception("Canvas를 찾지 못함");
                Transform canvas = canvasGO.transform;

                var fieldPanel = canvas.Find("FarmPanel/Field_Panel") ?? FindRecursive(canvas, "Field_Panel");
                var image = fieldPanel != null ? fieldPanel.GetComponent<Image>() : null;
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgPath);
                bool applied = false;
                if (image != null && sprite != null)
                {
                    image.sprite = sprite;
                    image.color = Color.white;
                    image.type = Image.Type.Simple;
                    image.preserveAspect = false;
                    EditorUtility.SetDirty(image);
                    applied = true;
                }

                // 아래쪽/왼쪽 모서리는 이웃 버튼/식물 리스트와 거의 맞닿아 있어 고정하고, 위쪽은
                // Top_Panel(상단바) 바로 아래까지, 오른쪽은 식물 선택 리스트 앞까지 최대한 키운다.
                // 24칸 격자는 이 그림의 장식(나무/집/해바라기 등)을 피한 가운데 빈 잔디 영역에
                // 맞춰 훨씬 작은 칸 크기(FieldManager.CellSize)로 다시 배치된다.
                bool resized = false;
                var fieldRect = fieldPanel as RectTransform;
                if (fieldRect != null)
                {
                    fieldRect.anchoredPosition = new Vector2(-307.6f, 57.33f);
                    fieldRect.sizeDelta = new Vector2(-843f, -345f);
                    EditorUtility.SetDirty(fieldRect);
                    resized = true;
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[GameAssetSetupTool] Wave11 완료: Field_Panel 배경 교체 {applied}, 크기 재조정 {resized}.");
                Succeed();
            }
            catch (Exception e)
            {
                Debug.LogError("[GameAssetSetupTool] Wave11 예외: " + e);
                Fail();
            }
        }
    }
}

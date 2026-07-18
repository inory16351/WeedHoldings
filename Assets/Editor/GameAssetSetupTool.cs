using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

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
    }
}

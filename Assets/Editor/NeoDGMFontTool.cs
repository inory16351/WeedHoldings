using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.TextCore.LowLevel;

namespace WeedHoldings.EditorTools
{
    /// <summary>
    /// neodgm.ttf(네오둥근모)를 TMP SDF 폰트 에셋으로 구워서 프로젝트 전체(씬 + 프리팹 + TMP 기본폰트)의
    /// TextMeshPro 폰트를 전부 교체한다. 프로젝트에 레거시 UnityEngine.UI.Text는 쓰이지 않고
    /// 전부 TextMeshProUGUI라서 TMP_Text만 처리하면 된다.
    /// </summary>
    public static class NeoDGMFontTool
    {
        const string SourceFontPath = "Assets/Fonts/NeoDGM/neodgm.ttf";
        const string OutputFontAssetPath = "Assets/Fonts/NeoDGM/NeoDGM SDF.asset";
        const string ScenePath = "Assets/Scenes/Proto_03.unity";
        const string PrefabPath = "Assets/Resources/Prefabs/PlantSelectSlot.prefab";

        // 프로젝트의 xlsx 데이터 테이블 + C# 스크립트 문자열 리터럴에서 실제로 쓰이는 문자를 전부 모아
        // 미리 구워 넣는 시드 문자 집합(ASCII + 게임에 등장하는 한글 전체). 폰트 에셋 자체는
        // Dynamic Atlas라서 여기 없는 글자가 나와도 런타임에 자동으로 추가되지만, 미리 구워두면
        // 첫 프레임에 빈 글자가 잠깐 보이는 현상을 막을 수 있다.
        const string SeedCharacters = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~◉가각간갈감값같개객갯거건걷것게겠겨격결겼경계고곤골공과괴구굴귀그극근글금급기긴김까깐깨꺼께꼬꽃꾸꾼꿈꿰끈끔끝끼낀나난날남낮내낸냄너넋널네녀녹농놓뇌누눈눌느는늘능늬니닌다단달담당대더던덩데도독돈동돼되된됨됩두둥뒤드득든들듯등디딪딱딸떠뗀또뜀라락랑래랙량러런럼럽렁레렌려력렷로록롤롭롯롱료루룹류률르른를름리릭린릴립릿마막만말맑맛망맡매맨머먼멍메며면명몇모목몰몸못몽묘무문묻물뭉미밀바박밖반발밟밤밧방밭배백버번벌법벤벨벼별보복본부분불블비빈빙빚빠빨빼뽑뿌사삭산살삼상새색샘생서석섞선설섬섯성세셀션소속손솜솟송수순스슬습시식신실심싹쏟씨씬씹아악안앉않알앞애액야약얀양어억업없었에엑엔엘여역연였영예오온와완왕외요용우운울웃워원위윈유윤율으은을음의이익인일임있잎자작잔잠장재저적전절접정젖제져조족종좋주준줄중쥔즐즙증지직진질짐집짚짜짝쩍쭉쯤찌찔차착찬참찾채책챙처천청체첼쳐쳤초촉촌총추출충취측치친카칸캐코콘쿨크큰클큼타탐탑탕태택터테텍템토통투튕트튼틀팅파판팡패팹퍼페편펼평포폭폰푹풀품풍프플피핀필핑하한할함합항해했행향헤혀현호혹화확환활황회획효후훤휘휴흐흑희히힘";

        [MenuItem("Tools/NeoDGM 폰트/폰트 에셋 굽기 + 전체 적용")]
        public static void BakeAndApply()
        {
            try
            {
                TMP_FontAsset fontAsset = BakeFontAsset();
                if (fontAsset == null)
                {
                    Debug.LogError("[NeoDGMFontTool] 폰트 에셋 생성 실패");
                    FailBatch();
                    return;
                }

                ApplyAsDefault(fontAsset);
                int sceneCount = ApplyToScene(fontAsset);
                int prefabCount = ApplyToPrefab(fontAsset);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[NeoDGMFontTool] 완료: 씬 TMP_Text {sceneCount}개, 프리팹 TMP_Text {prefabCount}개, TMP 기본폰트까지 NeoDGM으로 교체됨.");
                SucceedBatch();
            }
            catch (Exception e)
            {
                Debug.LogError("[NeoDGMFontTool] 예외 발생: " + e);
                FailBatch();
            }
        }

        static TMP_FontAsset BakeFontAsset()
        {
            AssetDatabase.ImportAsset(SourceFontPath, ImportAssetOptions.ForceUpdate);
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (sourceFont == null)
            {
                Debug.LogError("[NeoDGMFontTool] 원본 폰트를 로드하지 못했습니다: " + SourceFontPath);
                return null;
            }

            // 재실행 대비: 기존에 구워둔 에셋이 있으면 지우고 새로 만든다.
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputFontAssetPath) != null)
                AssetDatabase.DeleteAsset(OutputFontAssetPath);

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                48,                         // samplingPointSize: 도트 폰트라 크게 잡을 필요 없음
                2,                          // atlasPadding: 도트 폰트는 여백을 작게
                GlyphRenderMode.SDFAA,
                2048, 2048,
                AtlasPopulationMode.Dynamic, // 시드에 없는 문자가 나와도 런타임에 자동 추가
                true
            );

            if (fontAsset == null)
            {
                Debug.LogError("[NeoDGMFontTool] TMP_FontAsset.CreateFontAsset 실패 (폰트 페이스 로드 실패)");
                return null;
            }

            fontAsset.name = "NeoDGM SDF";
            AssetDatabase.CreateAsset(fontAsset, OutputFontAssetPath);
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            fontAsset.atlasTexture.name = "NeoDGM SDF Atlas";
            fontAsset.material.name = "NeoDGM SDF Material";

            bool allFound = fontAsset.TryAddCharacters(SeedCharacters, out string missing);
            if (!allFound && !string.IsNullOrEmpty(missing))
                Debug.LogWarning("[NeoDGMFontTool] neodgm.ttf에 없는 문자(참고용, 무시 가능): " + missing);

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();

            return fontAsset;
        }

        static void ApplyAsDefault(TMP_FontAsset fontAsset)
        {
            TMP_Settings.defaultFontAsset = fontAsset;
            EditorUtility.SetDirty(TMP_Settings.instance);
        }

        static int ApplyToScene(TMP_FontAsset fontAsset)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var texts = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in texts)
            {
                t.font = fontAsset;
                EditorUtility.SetDirty(t);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return texts.Length;
        }

        static int ApplyToPrefab(TMP_FontAsset fontAsset)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);

            foreach (var go in root.GetComponentsInChildren<Transform>(true))
            {
                int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go.gameObject);
                if (missingCount > 0)
                {
                    Debug.LogWarning($"[NeoDGMFontTool] 누락된 스크립트 {missingCount}개 발견 및 제거: GameObject '{GetPath(go)}'");
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go.gameObject);
                }
            }

            var texts = root.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in texts)
            {
                t.font = fontAsset;
                EditorUtility.SetDirty(t);
            }
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            return texts.Length;
        }

        static string GetPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }

        static void SucceedBatch()
        {
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        static void FailBatch()
        {
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
        }
    }
}

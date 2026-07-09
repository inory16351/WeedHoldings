using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;


namespace WeedHoldings.Editor
{
    public class ExcelAutoImporter : AssetPostprocessor
    {
        [MenuItem("WeedHoldings/엑셀 데이터 임포트")]
        public static void ImportManually()
        {
            string defaultPath = @"c:\Project\슬기로운재배생활\슬기로운재배생활_재배테이블.xlsx";
            if (File.Exists(defaultPath))
            {
                ImportExcelData(defaultPath);
                EditorUtility.DisplayDialog("임포트 완료", "엑셀 데이터 연동이 성공적으로 완료되었습니다!", "확인");
            }
            else
            {
                EditorUtility.DisplayDialog("에러", $"엑셀 파일을 찾을 수 없습니다:\n{defaultPath}", "확인");
            }
        }
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string assetPath in importedAssets)
            {
                // "재배테이블" 단어가 들어가고 확장자가 .xlsx 인 파일 감지
                if (assetPath.EndsWith(".xlsx") && assetPath.Contains("재배테이블"))
                {
                    Debug.Log($"[ExcelAutoImporter] Excel table modification detected: {assetPath}. Starting auto-parse...");
                    ImportExcelData(assetPath);
                }
            }
        }

        public static void ImportExcelData(string xlsxPath)
        {
            string outputJsonPath = "Assets/Editor/excel_data.json";
            
            // 1. Python 스크립트 실행하여 Excel 데이터를 JSON으로 파싱
            string pythonCmd = "python";
            string args = $"\"Assets/Editor/parse_excel.py\" \"{xlsxPath}\" \"{outputJsonPath}\"";

            System.Diagnostics.ProcessStartInfo start = new System.Diagnostics.ProcessStartInfo
            {
                FileName = pythonCmd,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(start))
                {
                    string stdout = process.StandardOutput.ReadToEnd();
                    string stderr = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        Debug.LogError($"[ExcelAutoImporter] Python script failed with exit code {process.ExitCode}.\nError: {stderr}");
                        return;
                    }
                    else
                    {
                        Debug.Log($"[ExcelAutoImporter] Python output: {stdout}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ExcelAutoImporter] Failed to run python parser. Make sure python and openpyxl are installed.\nException: {ex.Message}");
                return;
            }

            // 2. 파싱 완료된 JSON 데이터 읽기
            if (!File.Exists(outputJsonPath))
            {
                Debug.LogError($"[ExcelAutoImporter] Output JSON not found at: {outputJsonPath}");
                return;
            }

            string jsonContent = File.ReadAllText(outputJsonPath);
            JsonData data = JsonUtility.FromJson<JsonData>(jsonContent);

            if (data == null)
            {
                Debug.LogError("[ExcelAutoImporter] Failed to deserialize JSON data.");
                return;
            }

            // 3. 식물 데이터 (PlantData) ScriptableObject 생성/업데이트
            ImportPlants(data.plants);

            // 4. 연구소 업그레이드 데이터 (LabUpgradeData) ScriptableObject 생성/업데이트
            ImportUpgrades(data.upgrades);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ExcelAutoImporter] Auto-import of raw plants and lab upgrades completed successfully!");
        }

        private static void ImportPlants(List<JsonPlant> plants)
        {
            string dir = "Assets/Resources/Plants";
            Directory.CreateDirectory(dir);

            // 기존 PlantData 에셋 검색 후 삭제 (덮어씌우기를 위해)
            foreach (var guid in AssetDatabase.FindAssets("t:PlantData", new[] { dir }))
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
            }

            foreach (var entry in plants)
            {
                PlantData plant = ScriptableObject.CreateInstance<PlantData>();
                plant.plantID = entry.id;
                plant.plantName = entry.name;
                plant.growTimeSeconds = entry.growTime;
                plant.requiredHarvestLevel = entry.reqLevel;
                plant.requiredGoldToUnlock = entry.reqGold;
                plant.witherTimeSeconds = entry.waterTime > 0 ? entry.waterTime : 30f;
                plant.harvestGroupID = entry.groupId;

                // 수확량 & 가격 공식 자동 계산
                plant.harvestMinCount = 1;
                if (entry.id <= 10003)
                    plant.harvestMaxCount = 3;
                else if (entry.id <= 10007)
                    plant.harvestMaxCount = 2;
                else
                    plant.harvestMaxCount = 1;

                plant.sellGoldValue = entry.reqGold > 0 ? entry.reqGold / 10 : 10;

                plant.babyPhaseRatio = 0.3f;
                plant.growingPhaseRatio = 0.4f;
                plant.maturePhaseRatio = 0.3f;
                plant.description = $"{entry.name} 씨앗입니다. 밭에 심어 수확하여 약재로 활용할 수 있습니다.";

                // 이미지 리소스 바인딩 (파일명 패딩 불일치 수정 지원)
                plant.iconSprite = LoadPlantSprite(entry.icon);
                plant.babySprite = LoadPlantSprite(entry.baby);
                plant.fullGrowSprite = LoadPlantSprite(entry.fullGrow);

                string assetPath = $"{dir}/Plant_{entry.id:D5}_{entry.name}.asset";
                AssetDatabase.CreateAsset(plant, assetPath);
            }
            Debug.Log($"[ExcelAutoImporter] Imported {plants.Count} plants to {dir}.");
        }

        private static void ImportUpgrades(List<JsonUpgrade> upgrades)
        {
            string dir = "Assets/Resources/LabUpgrades";
            Directory.CreateDirectory(dir);

            // 기존 LabUpgradeData 에셋 검색 후 삭제
            foreach (var guid in AssetDatabase.FindAssets("t:LabUpgradeData", new[] { dir }))
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
            }

            foreach (var entry in upgrades)
            {
                LabUpgradeData upgrade = ScriptableObject.CreateInstance<LabUpgradeData>();
                upgrade.upgradeID = entry.upgradeID;
                upgrade.upgradeLevel = entry.level;
                upgrade.growBonusPercent = entry.growBonus;
                upgrade.requiredGold = entry.reqGold;
                
                // Unlock_Field 값을 unlockFieldRow에 매핑하여 추가 해금 가능 칸 수로 사용
                upgrade.unlockFieldRow = entry.unlockField;
                upgrade.harvestGroupID = entry.harvestGroupID;

                string assetPath = $"{dir}/LabUpgrade_{entry.level:D2}.asset";
                AssetDatabase.CreateAsset(upgrade, assetPath);
            }
            Debug.Log($"[ExcelAutoImporter] Imported {upgrades.Count} upgrades to {dir}.");
        }

        private static Sprite LoadPlantSprite(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            string path = $"Assets/Resources/Plants/{name}.png";
            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) return sp;

            // 파일명 보정 지원 (예: Plant_Full_Grow_10 -> Plant_Full_Grow_010.png)
            if (name.Contains("Full_Grow_") || name.Contains("Icon_"))
            {
                string[] parts = name.Split('_');
                if (parts.Length > 0 && int.TryParse(parts[parts.Length - 1], out int num))
                {
                    string prefix = string.Join("_", parts, 0, parts.Length - 1);
                    string altName1 = $"{prefix}_{num:D2}";
                    string altName2 = $"{prefix}_{num:D3}";
                    sp = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/Plants/{altName1}.png");
                    if (sp != null) return sp;
                    sp = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/Plants/{altName2}.png");
                    if (sp != null) return sp;
                }
            }
            return null;
        }

        // --- JSON 매핑 구조체 ---
        [System.Serializable]
        public class JsonPlant
        {
            public int id;
            public string name;
            public float growTime;
            public int reqLevel;
            public int reqGold;
            public string icon;
            public string baby;
            public string fullGrow;
            public float waterTime;
            public int groupId;
        }

        [System.Serializable]
        public class JsonUpgrade
        {
            public int upgradeID;
            public int level;
            public float growBonus;
            public int reqGold;
            public int unlockField;
            public int harvestGroupID;
        }

        [System.Serializable]
        public class JsonData
        {
            public List<JsonPlant> plants;
            public List<JsonUpgrade> upgrades;
        }
    }
}

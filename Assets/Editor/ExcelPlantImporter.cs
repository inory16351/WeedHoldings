using UnityEngine;
using UnityEditor;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;

namespace WeedHoldings.Editor
{
    public class ExcelPlantImporter : EditorWindow
    {
        string xlsxPath = "";
        string status = "";

        public static void RunImport()
        {
            string defaultPath = @"c:\Project\슬기로운재배생활\슬기로운재배생활_재배테이블.xlsx";
            if (File.Exists(defaultPath))
            {
                var importer = CreateInstance<ExcelPlantImporter>();
                importer.ImportPlants(defaultPath);
                Debug.Log("[ExcelPlantImporter] Batchmode import executed successfully.");
            }
            else
            {
                Debug.LogError($"[ExcelPlantImporter] Excel file not found at: {defaultPath}");
            }
        }

        void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("슬기로운재배생활_재배테이블.xlsx → PlantData", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("엑셀 파일 선택", GUILayout.Height(30)))
            {
                string path = EditorUtility.OpenFilePanel("식물 테이블 선택", Application.dataPath, "xlsx");
                if (!string.IsNullOrEmpty(path))
                {
                    xlsxPath = path;
                }
            }

            if (!string.IsNullOrEmpty(xlsxPath))
            {
                GUILayout.Label($"선택: {Path.GetFileName(xlsxPath)}");
            }

            GUILayout.Space(10);

            GUI.enabled = !string.IsNullOrEmpty(xlsxPath) && File.Exists(xlsxPath);
            if (GUILayout.Button("데이터 가져오기", GUILayout.Height(40)))
            {
                ImportPlants(xlsxPath);
            }
            GUI.enabled = true;

            if (!string.IsNullOrEmpty(status))
            {
                GUILayout.Space(10);
                GUILayout.Label(status);
            }
        }

        void ImportPlants(string xlsxPath)
        {
            // Sharing violation 우회를 위해 임시 파일 복사
            string tempPath = Path.Combine(Path.GetTempPath(), "temp_import_" + Path.GetRandomFileName() + ".xlsx");
            try
            {
                File.Copy(xlsxPath, tempPath, true);

                List<PlantEntry> plants = ParseXlsx(tempPath);
                if (plants.Count == 0)
                {
                    status = "데이터가 없습니다. 시트 형식을 확인하세요.";
                    return;
                }

                string dir = "Assets/Resources/Plants";
                Directory.CreateDirectory(dir);

                // 기존 스크립터블 오브젝트 에셋만 삭제 (png 파일 등 스프라이트 원본은 유지)
                foreach (var old in AssetDatabase.FindAssets("t:PlantData", new[] { dir }))
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(old));
                }

                foreach (var entry in plants)
                {
                    var plant = ScriptableObject.CreateInstance<PlantData>();
                    plant.plantID = entry.id;
                    plant.plantName = entry.name;
                    plant.growTimeSeconds = entry.growTime;
                    plant.requiredHarvestLevel = entry.harvestLevel;
                    plant.requiredGoldToUnlock = entry.requiredGold;
                    plant.harvestMinCount = entry.minHarvest;
                    plant.harvestMaxCount = entry.maxHarvest;
                    plant.sellGoldValue = entry.sellGold;
                    plant.witherTimeSeconds = entry.waterTime > 0 ? entry.waterTime : 30f;
                    
                    // 그룹 ID 셋업 (1001, 1002, 1003 분기 규칙 적용)
                    int groupID = 1001;
                    if (entry.id >= 10004 && entry.id <= 10007) groupID = 1002;
                    else if (entry.id >= 10008) groupID = 1003;
                    plant.harvestGroupID = groupID;

                    plant.babyPhaseRatio = 0.3f;
                    plant.growingPhaseRatio = 0.4f;
                    plant.maturePhaseRatio = 0.3f;
                    plant.description = $"{entry.name} 씨앗입니다.";

                    // 이미지 스프라이트 로드 및 바인딩
                    plant.iconSprite = LoadPlantSprite(entry.iconImageName);
                    plant.babySprite = LoadPlantSprite(entry.babyImageName);
                    plant.fullGrowSprite = LoadPlantSprite(entry.fullImageName);

                    string path = $"{dir}/Plant_{entry.id:D5}_{entry.name}.asset";
                    AssetDatabase.CreateAsset(plant, path);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                status = $"성공! {plants.Count}개 식물 데이터를 가져왔습니다.\nAssets/Resources/Plants/ 에 저장됨";
                Debug.Log($"[ExcelPlantImporter] {plants.Count}개 식물 임포트 완료 및 스프라이트 연동 완료");
            }
            catch (System.Exception e)
            {
                status = $"오류: {e.Message}";
                Debug.LogError($"[ExcelPlantImporter] {e}");
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        static Sprite LoadPlantSprite(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            string path = $"Assets/Resources/Plants/{name}.png";
            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) return sp;

            // 파일명 보정 (예: Plant_Full_Grow_10 -> Plant_Full_Grow_010.png)
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

        struct PlantEntry
        {
            public int id;
            public string name;
            public float growTime;
            public int harvestLevel;
            public int requiredGold;
            public int minHarvest;
            public int maxHarvest;
            public int sellGold;
            public string iconImageName;
            public string babyImageName;
            public string fullImageName;
            public float waterTime;
        }

        List<PlantEntry> ParseXlsx(string path)
        {
            var entries = new List<PlantEntry>();
            var sharedStrings = new Dictionary<int, string>();

            using (var archive = ZipFile.OpenRead(path))
            {
                XDocument sharedStringsDoc = null;
                var ssEntry = archive.GetEntry("xl/sharedStrings.xml");
                if (ssEntry != null)
                {
                    using (var sr = new StreamReader(ssEntry.Open()))
                        sharedStringsDoc = XDocument.Parse(sr.ReadToEnd());
                    var siElements = sharedStringsDoc?.Root?.Elements();
                    if (siElements != null)
                    {
                        int idx = 0;
                        foreach (var si in siElements)
                        {
                            var t = si.Element(XName.Get("t", si.GetDefaultNamespace().NamespaceName));
                            sharedStrings[idx++] = t?.Value ?? "";
                        }
                    }
                }

                var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml");
                if (sheetEntry == null)
                {
                    Debug.LogError("sheet1.xml not found in xlsx");
                    return entries;
                }

                XDocument sheetDoc;
                using (var sr = new StreamReader(sheetEntry.Open()))
                    sheetDoc = XDocument.Parse(sr.ReadToEnd());

                var ns = sheetDoc.Root.GetDefaultNamespace();
                var rows = sheetDoc.Root
                    .Element(XName.Get("sheetData", ns.NamespaceName))
                    ?.Elements(XName.Get("row", ns.NamespaceName));

                if (rows == null) return entries;

                bool header = true;
                foreach (var row in rows)
                {
                    var cells = row.Elements(XName.Get("c", ns.NamespaceName)).ToList();
                    if (cells.Count < 2) continue;

                    if (header)
                    {
                        header = false;
                        continue;
                    }

                    var entry = new PlantEntry();

                    // I열(인덱스 8)까지 파싱하기 위해 반복 범위 조절
                    for (int i = 0; i < cells.Count; i++)
                    {
                        var cell = cells[i];
                        string colRef = cell.Attribute("r")?.Value ?? "";
                        string type = cell.Attribute("t")?.Value ?? "";
                        string rawValue = cell.Element(XName.Get("v", ns.NamespaceName))?.Value ?? "";

                        string value = type == "s" && int.TryParse(rawValue, out int si)
                            ? (sharedStrings.ContainsKey(si) ? sharedStrings[si] : "")
                            : rawValue;

                        int colIdx = GetColIndex(colRef);

                        switch (colIdx)
                        {
                            case 0: int.TryParse(value, out entry.id); break;
                            case 1: entry.name = value; break;
                            case 2: float.TryParse(value, System.Globalization.NumberStyles.Any,
                                        System.Globalization.CultureInfo.InvariantCulture, out entry.growTime); break;
                            case 3: int.TryParse(value, out entry.harvestLevel); break;
                            case 4: int.TryParse(value, out entry.requiredGold); break;
                            case 5: entry.iconImageName = value; break;
                            case 6: entry.babyImageName = value; break;
                            case 7: entry.fullImageName = value; break;
                            case 8: float.TryParse(value, System.Globalization.NumberStyles.Any,
                                        System.Globalization.CultureInfo.InvariantCulture, out entry.waterTime); break;
                        }
                    }

                    if (entry.id > 0 && !string.IsNullOrEmpty(entry.name))
                    {
                        entry.minHarvest = 1;
                        entry.maxHarvest = 3;
                        entry.sellGold = entry.requiredGold > 0 ? entry.requiredGold / 10 : 10;
                        entries.Add(entry);
                    }
                }
            }

            return entries;
        }

        int GetColIndex(string cellRef)
        {
            string col = new string(cellRef.TakeWhile(char.IsLetter).ToArray());
            int result = 0;
            foreach (char c in col)
                result = result * 26 + (c - 'A' + 1);
            return result - 1;
        }
    }
}

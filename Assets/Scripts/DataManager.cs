using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WeedHoldings
{
    public class DataManager : MonoBehaviour
    {
        public static DataManager Instance { get; private set; }

        [Header("런타임 JSON 데이터 파일 (Resources)")]
        public string plantDataJsonPath = "plant_data";
        public string upgradeDataJsonPath = "upgrade_data";

        [Header("엑셀 파일 경로 (에디터 전용)")]
        public string excelFilePath = "../슬기로운재배생활/슬기로운재배생활_재배테이블.xlsx";
        public string potionExcelFilePath = "../슬기로운재배생활/제조테이블.xlsx";
        public string tradeExcelFilePath = "../슬기로운재배생활/무역소 테이블.xlsx";
        public string characterExcelFilePath = "../슬기로운재배생활/캐릭터테이블.xlsx";

        Dictionary<int, PlantData> plantDict = new Dictionary<int, PlantData>();
        Dictionary<int, LabUpgradeData> upgradeDict = new Dictionary<int, LabUpgradeData>();
        Dictionary<int, List<int>> harvestGroups = new Dictionary<int, List<int>>();

        List<int> unlockedPlantIDs = new List<int>();
        Dictionary<int, int> plantInventory = new Dictionary<int, int>();

        Dictionary<int, PotionData> potionDict = new Dictionary<int, PotionData>();
        Dictionary<int, FactoryUpgradeData> factoryUpgradeDict = new Dictionary<int, FactoryUpgradeData>();
        Dictionary<int, List<(int plantId, int amount)>> potionMaterialGroups = new Dictionary<int, List<(int, int)>>();
        List<int> unlockedPotionIDs = new List<int>();
        Dictionary<int, int> potionInventory = new Dictionary<int, int>();

        Dictionary<int, RegionData> regionDict = new Dictionary<int, RegionData>();
        Dictionary<int, List<(int plantId, float bonusPercent)>> regionBonusGroups = new Dictionary<int, List<(int, float)>>();
        Dictionary<int, SellUpgradeData> sellUpgradeDict = new Dictionary<int, SellUpgradeData>();

        Dictionary<int, CharacterData> characterDict = new Dictionary<int, CharacterData>();
        HashSet<int> ownedCharacterIDs = new HashSet<int>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 다른 매니저(LabUpgradeManager 등)의 Start()에서 바로 참조할 수 있도록
            // Unity의 전역 Awake 단계에서 데이터를 로드한다 (Start 단계는 실행 순서가 보장되지 않음)
            LoadFromScriptableObjects();
            if (unlockedPlantIDs.Count == 0 && plantDict.Count > 0)
            {
                var first = plantDict.Values.OrderBy(p => p.requiredGoldToUnlock).First();
                unlockedPlantIDs.Add(first.plantID);
            }

#if UNITY_EDITOR
            // 보약(포션) 데이터는 별도 .asset을 만들지 않고 매 실행마다 엑셀에서 직접 읽는다.
            LoadPotionTableFromExcel();
            LoadTradeTableFromExcel();
            LoadCharacterTableFromExcel();
#endif
            if (unlockedPotionIDs.Count == 0 && potionDict.Count > 0)
            {
                var first = potionDict.Values.OrderBy(p => p.requiredUnlockGold).First();
                unlockedPotionIDs.Add(first.potionID);
            }
        }

        void LoadFromScriptableObjects()
        {
            plantDict.Clear();
            var plants = Resources.LoadAll<PlantData>("Plants");
            Debug.Log($"[DataManager] Loaded {plants.Length} PlantData from Resources/Plants");
            foreach (var p in plants)
            {
                Debug.Log($"[DataManager] Loaded plant: {p.plantName} (ID: {p.plantID}), iconSprite={p.iconSprite?.name ?? "NULL"}");
                if (!plantDict.ContainsKey(p.plantID))
                    plantDict.Add(p.plantID, p);
            }

            upgradeDict.Clear();
            var upgrades = Resources.LoadAll<LabUpgradeData>("LabUpgrades");
            Debug.Log($"[DataManager] Loaded {upgrades.Length} LabUpgradeData from Resources/LabUpgrades");
            foreach (var u in upgrades)
                if (!upgradeDict.ContainsKey(u.upgradeID))
                    upgradeDict.Add(u.upgradeID, u);
        }

        public void ImportFromExcel()
        {
#if UNITY_EDITOR
            string path = System.IO.Path.GetFullPath(excelFilePath);
            if (!System.IO.File.Exists(path))
            {
                Debug.LogError($"Excel file not found: {path}");
                return;
            }

            var plantRows = ExcelParser.ParseSheetByName(path, "원료 식물");
            if (plantRows != null) ImportPlantRows(plantRows);

            var upgradeRows = ExcelParser.ParseSheetByName(path, "시설 업그레이드");
            if (upgradeRows != null) ImportUpgradeRows(upgradeRows);

            var groupRows = ExcelParser.ParseSheetByName(path, "한번에심기_한번에수확 그룹");
            if (groupRows != null) ImportHarvestGroupRows(groupRows);

            Debug.Log("[DataManager] Excel import complete.");
#else
            Debug.LogWarning("Excel import is only available in Editor.");
#endif
        }

#if UNITY_EDITOR
        void ImportPlantRows(List<string[]> rows)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r.Length < 9 || !IsDataRow(r[0])) continue;

                var plant = ScriptableObject.CreateInstance<PlantData>();
                plant.plantID = ParseInt(r[0]);
                plant.plantName = r[1] ?? "";
                plant.growTimeSeconds = ParseFloat(r[2]);
                plant.requiredHarvestLevel = ParseInt(r[3]);
                plant.requiredGoldToUnlock = ParseInt(r[4]);
                plant.witherTimeSeconds = ParseFloat(r[8]);
                plant.babyPhaseRatio = 0.3f;
                plant.growingPhaseRatio = 0.4f;
                plant.maturePhaseRatio = 0.3f;

                if (!plantDict.ContainsKey(plant.plantID))
                    plantDict.Add(plant.plantID, plant);
            }
        }

        void ImportUpgradeRows(List<string[]> rows)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r.Length < 4 || !IsDataRow(r[0])) continue;

                var upgrade = ScriptableObject.CreateInstance<LabUpgradeData>();
                upgrade.upgradeID = ParseInt(r[0]);
                upgrade.upgradeLevel = ParseInt(r[1]);
                upgrade.growBonusPercent = (int)ParseFloat(r[2]);
                upgrade.requiredGold = ParseInt(r[3]);
                upgrade.unlockFieldRow = ParseInt(r[4]);
                upgrade.harvestGroupID = ParseInt(r[5]);

                if (!upgradeDict.ContainsKey(upgrade.upgradeID))
                    upgradeDict.Add(upgrade.upgradeID, upgrade);
            }
        }

        void ImportHarvestGroupRows(List<string[]> rows)
        {
            harvestGroups.Clear();
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r.Length < 3 || !IsDataRow(r[1])) continue;

                int groupId = ParseInt(r[1]);
                int plantId = ParseInt(r[2]);

                if (!harvestGroups.ContainsKey(groupId))
                    harvestGroups[groupId] = new List<int>();
                harvestGroups[groupId].Add(plantId);
            }
        }

        int ParseInt(string val)
        {
            if (int.TryParse(val, out int result)) return result;
            return 0;
        }

        float ParseFloat(string val)
        {
            if (float.TryParse(val, out float result)) return result;
            return 0f;
        }

        /// <summary>
        /// 헤더/타입 행인지 데이터 행인지 판별한다. 시트마다 헤더 행 수가 다를 수 있어(예: 타입 행이
        /// 없는 시트) 고정된 시작 인덱스(예: i=3)로는 실제 데이터 행을 건너뛰는 버그가 생길 수 있으므로,
        /// 첫 번째 셀이 정수로 파싱되는지로 실제 데이터 행을 판별한다.
        /// </summary>
        static bool IsDataRow(string cell)
        {
            return !string.IsNullOrEmpty(cell) && int.TryParse(cell, out _);
        }

        /// <summary>
        /// 제조테이블.xlsx("보약", "제조 필요 원료 식물 그룹", "제조 시설 업그레이드" 시트)를 읽어
        /// potionDict / potionMaterialGroups / factoryUpgradeDict를 채운다.
        /// PlantData/LabUpgradeData와 달리 .asset 파일을 만들지 않고 실행할 때마다 직접 파싱한다.
        /// </summary>
        void LoadPotionTableFromExcel()
        {
            string path = System.IO.Path.GetFullPath(potionExcelFilePath);
            if (!System.IO.File.Exists(path))
            {
                Debug.LogWarning($"[DataManager] 제조테이블 파일을 찾을 수 없습니다: {path}");
                return;
            }

            var potionRows = ExcelParser.ParseSheetByName(path, "보약");
            if (potionRows != null) ImportPotionRows(potionRows);

            var materialRows = ExcelParser.ParseSheetByName(path, "제조 필요 원료 식물 그룹");
            if (materialRows != null) ImportPotionMaterialRows(materialRows);

            var factoryRows = ExcelParser.ParseSheetByName(path, "제조 시설 업그레이드");
            if (factoryRows != null) ImportFactoryUpgradeRows(factoryRows);

            Debug.Log($"[DataManager] 제조테이블 로드 완료: 보약 {potionDict.Count}개, 재료그룹 {potionMaterialGroups.Count}개, 제조시설업그레이드 {factoryUpgradeDict.Count}개");
        }

        void ImportPotionRows(List<string[]> rows)
        {
            potionDict.Clear();
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r.Length < 8 || !IsDataRow(r[0])) continue;

                var potion = new PotionData
                {
                    potionID = ParseInt(r[0]),
                    potionName = r[1] ?? "",
                    potionTimeSeconds = ParseFloat(r[2]),
                    failProbBase = ParseFloat(r[3]),
                    reqPlantGroup = ParseInt(r[4]),
                    requiredUnlockLevel = ParseInt(r[5]),
                    requiredUnlockGold = ParseInt(r[6]),
                    sellGold = ParseInt(r[7]),
                };

                // Potion_Icon 컬럼(9번째)에 실제 Resources/Potions 파일명이 그대로 들어있다
                // (예: "Potion_Icon_01"). ID 기준이 아니라 테이블에 적힌 이름으로 직접 로드한다.
                string iconName = r.Length > 8 ? r[8] : null;
                if (!string.IsNullOrEmpty(iconName))
                    potion.potionIcon = Resources.Load<Sprite>($"Potions/{iconName}");

                if (!potionDict.ContainsKey(potion.potionID))
                    potionDict.Add(potion.potionID, potion);
            }
        }

        void ImportPotionMaterialRows(List<string[]> rows)
        {
            potionMaterialGroups.Clear();
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r.Length < 4 || !IsDataRow(r[1])) continue;

                int groupId = ParseInt(r[1]);
                int plantId = ParseInt(r[2]);
                int amount = ParseInt(r[3]);

                if (!potionMaterialGroups.ContainsKey(groupId))
                    potionMaterialGroups[groupId] = new List<(int, int)>();
                potionMaterialGroups[groupId].Add((plantId, amount));
            }
        }

        void ImportFactoryUpgradeRows(List<string[]> rows)
        {
            factoryUpgradeDict.Clear();
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r.Length < 4 || !IsDataRow(r[1])) continue;

                var upgrade = new FactoryUpgradeData
                {
                    upgradeLevel = ParseInt(r[1]),
                    failProbBonus = ParseFloat(r[2]),
                    potionTimeBonus = ParseFloat(r[3]),
                    requiredGold = r.Length > 4 ? ParseInt(r[4]) : 0,
                    unlockTrack = r.Length > 5 ? ParseInt(r[5]) : 0,
                };

                if (!factoryUpgradeDict.ContainsKey(upgrade.upgradeLevel))
                    factoryUpgradeDict.Add(upgrade.upgradeLevel, upgrade);
            }
        }

        /// <summary>
        /// 무역소 테이블.xlsx("지역", "지역 보너스", "시설 업그레이드" 시트)를 읽어
        /// regionDict / regionBonusGroups / sellUpgradeDict를 채운다.
        /// </summary>
        void LoadTradeTableFromExcel()
        {
            string path = System.IO.Path.GetFullPath(tradeExcelFilePath);
            if (!System.IO.File.Exists(path))
            {
                Debug.LogWarning($"[DataManager] 무역소 테이블 파일을 찾을 수 없습니다: {path}");
                return;
            }

            var regionRows = ExcelParser.ParseSheetByName(path, "지역");
            if (regionRows != null) ImportRegionRows(regionRows);

            var bonusRows = ExcelParser.ParseSheetByName(path, "지역 보너스");
            if (bonusRows != null) ImportRegionBonusRows(bonusRows);

            var sellUpgradeRows = ExcelParser.ParseSheetByName(path, "시설 업그레이드");
            if (sellUpgradeRows != null) ImportSellUpgradeRows(sellUpgradeRows);

            Debug.Log($"[DataManager] 무역소테이블 로드 완료: 지역 {regionDict.Count}개, 지역보너스그룹 {regionBonusGroups.Count}개, 무역시설업그레이드 {sellUpgradeDict.Count}개");
        }

        void ImportRegionRows(List<string[]> rows)
        {
            regionDict.Clear();
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r.Length < 3 || !IsDataRow(r[0])) continue;

                var region = new RegionData
                {
                    regionID = ParseInt(r[0]),
                    regionName = r[1] ?? "",
                    sellTimeSeconds = ParseFloat(r[2]),
                    regionBonusGroupID = r.Length > 3 ? ParseInt(r[3]) : 0,
                };

                if (!regionDict.ContainsKey(region.regionID))
                    regionDict.Add(region.regionID, region);
            }
        }

        void ImportRegionBonusRows(List<string[]> rows)
        {
            regionBonusGroups.Clear();
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r.Length < 4 || !IsDataRow(r[1])) continue;

                int groupId = ParseInt(r[1]);
                int plantId = ParseInt(r[2]);
                float bonus = ParseFloat(r[3]);

                if (!regionBonusGroups.ContainsKey(groupId))
                    regionBonusGroups[groupId] = new List<(int, float)>();
                regionBonusGroups[groupId].Add((plantId, bonus));
            }
        }

        void ImportSellUpgradeRows(List<string[]> rows)
        {
            sellUpgradeDict.Clear();
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r.Length < 2 || !IsDataRow(r[1])) continue;

                var upgrade = new SellUpgradeData
                {
                    upgradeLevel = ParseInt(r[1]),
                    goldBonusPercent = r.Length > 2 ? ParseFloat(r[2]) : 0f,
                    requiredGold = r.Length > 3 ? ParseInt(r[3]) : 0,
                    unlockRegionID = r.Length > 4 ? ParseInt(r[4]) : 0,
                    cargoCapacity = r.Length > 5 ? ParseInt(r[5]) : 0,
                    unlockShipTrack = r.Length > 6 ? ParseInt(r[6]) : 0,
                };

                if (!sellUpgradeDict.ContainsKey(upgrade.upgradeLevel))
                    sellUpgradeDict.Add(upgrade.upgradeLevel, upgrade);
            }
        }

        /// <summary>
        /// 캐릭터테이블.xlsx("캐릭터테이블" 시트)를 읽어 characterDict를 채운다.
        /// 뽑기확률(Draw_Chance) 컬럼은 전체 합이 100이 되도록 설계되어 있어 그대로 가중치로 쓸 수 있다.
        /// </summary>
        void LoadCharacterTableFromExcel()
        {
            string path = System.IO.Path.GetFullPath(characterExcelFilePath);
            if (!System.IO.File.Exists(path))
            {
                Debug.LogWarning($"[DataManager] 캐릭터테이블 파일을 찾을 수 없습니다: {path}");
                return;
            }

            var rows = ExcelParser.ParseSheetByName(path, "캐릭터테이블");
            if (rows != null) ImportCharacterRows(rows);

            Debug.Log($"[DataManager] 캐릭터테이블 로드 완료: 캐릭터 {characterDict.Count}개");
        }

        void ImportCharacterRows(List<string[]> rows)
        {
            characterDict.Clear();
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r.Length < 6 || !IsDataRow(r[0])) continue;

                var character = new CharacterData
                {
                    characterID = ParseInt(r[0]),
                    characterName = r[1] ?? "",
                    grade = r[2] ?? "",
                    explain = r[3] ?? "",
                    resourceName = r[4] ?? "",
                    drawChance = ParseFloat(r[5]),
                    farmEffectAllNum = r.Length > 6 ? ParseFloat(r[6]) : 0f,
                    farmEffectTarget = r.Length > 7 ? ParseInt(r[7]) : 0,
                    farmEffectNum = r.Length > 8 ? ParseInt(r[8]) : 0,
                    farmEffectChance = r.Length > 9 ? ParseFloat(r[9]) : 0f,
                    factoryEffectAllNum = r.Length > 10 ? ParseFloat(r[10]) : 0f,
                    factoryEffectTarget = r.Length > 11 ? ParseInt(r[11]) : 0,
                    factoryEffectNum = r.Length > 12 ? ParseInt(r[12]) : 0,
                    factoryEffectChance = r.Length > 13 ? ParseFloat(r[13]) : 0f,
                    sellEffectAllNum = r.Length > 14 ? ParseFloat(r[14]) : 0f,
                    sellEffectTarget = r.Length > 15 ? ParseInt(r[15]) : 0,
                    sellEffectNum = r.Length > 16 ? ParseInt(r[16]) : 0,
                    sellEffectChance = r.Length > 17 ? ParseFloat(r[17]) : 0f,
                };

                if (!string.IsNullOrEmpty(character.resourceName))
                    character.characterIcon = Resources.Load<Sprite>($"Characters/{character.resourceName}");

                if (!characterDict.ContainsKey(character.characterID))
                    characterDict.Add(character.characterID, character);
            }
        }
#endif

        public PlantData GetPlantByID(int id)
        {
            plantDict.TryGetValue(id, out var plant);
            return plant;
        }

        public LabUpgradeData GetUpgradeByID(int id)
        {
            upgradeDict.TryGetValue(id, out var upgrade);
            return upgrade;
        }

        public LabUpgradeData GetUpgradeByLevel(int level)
        {
            return upgradeDict.Values.FirstOrDefault(u => u.upgradeLevel == level);
        }

        public List<PlantData> GetAllPlants()
        {
            return plantDict.Values.OrderBy(p => p.plantID).ToList();
        }

        public List<PlantData> GetUnlockedPlants()
        {
            return unlockedPlantIDs
                .Select(id => GetPlantByID(id))
                .Where(p => p != null)
                .ToList();
        }

        public bool IsPlantUnlocked(int id)
        {
            return unlockedPlantIDs.Contains(id);
        }

        public void UnlockPlant(int id)
        {
            if (!unlockedPlantIDs.Contains(id))
                unlockedPlantIDs.Add(id);
        }

        public List<PlantData> GetLockedPlants()
        {
            return GetAllPlants().Where(p => !unlockedPlantIDs.Contains(p.plantID)).ToList();
        }

        public List<int> GetPlantsInHarvestGroup(int groupId)
        {
            return harvestGroups.TryGetValue(groupId, out var list) ? list : new List<int>();
        }

        public int GetPlantCount(int plantID)
        {
            return plantInventory.TryGetValue(plantID, out int count) ? count : 0;
        }

        public void AddPlantToInventory(int plantID, int amount = 1)
        {
            if (!plantInventory.ContainsKey(plantID))
                plantInventory[plantID] = 0;
            plantInventory[plantID] += amount;
        }

        public bool HasPlantInInventory(int plantID, int amount = 1)
        {
            return GetPlantCount(plantID) >= amount;
        }

        public void RemovePlantFromInventory(int plantID, int amount = 1)
        {
            if (!plantInventory.ContainsKey(plantID)) return;
            plantInventory[plantID] = Mathf.Max(0, plantInventory[plantID] - amount);
        }

        // ---------- 보약(포션) ----------

        public PotionData GetPotionByID(int id)
        {
            potionDict.TryGetValue(id, out var potion);
            return potion;
        }

        public List<PotionData> GetAllPotions()
        {
            return potionDict.Values.OrderBy(p => p.potionID).ToList();
        }

        public bool IsPotionUnlocked(int id)
        {
            return unlockedPotionIDs.Contains(id);
        }

        public void UnlockPotion(int id)
        {
            if (!unlockedPotionIDs.Contains(id))
                unlockedPotionIDs.Add(id);
        }

        public FactoryUpgradeData GetFactoryUpgradeByLevel(int level)
        {
            factoryUpgradeDict.TryGetValue(level, out var data);
            return data;
        }

        /// <summary>reqPlantGroup에 속한 (식물ID, 필요수량) 목록. 없으면 빈 리스트.</summary>
        public List<(int plantId, int amount)> GetPotionMaterials(int reqPlantGroup)
        {
            return potionMaterialGroups.TryGetValue(reqPlantGroup, out var list) ? list : new List<(int, int)>();
        }

        public int GetPotionCount(int potionID)
        {
            return potionInventory.TryGetValue(potionID, out int count) ? count : 0;
        }

        public void AddPotionToInventory(int potionID, int amount = 1)
        {
            if (!potionInventory.ContainsKey(potionID))
                potionInventory[potionID] = 0;
            potionInventory[potionID] += amount;
        }

        public void RemovePotionFromInventory(int potionID, int amount = 1)
        {
            if (!potionInventory.ContainsKey(potionID)) return;
            potionInventory[potionID] = Mathf.Max(0, potionInventory[potionID] - amount);
        }

        /// <summary>포션의 대표 재료 식물 ID(여러 개면 첫 번째). 없으면 0.</summary>
        public int GetPotionPrimaryMaterialPlantID(int potionId)
        {
            var potion = GetPotionByID(potionId);
            if (potion == null) return 0;
            var materials = GetPotionMaterials(potion.reqPlantGroup);
            return materials.Count > 0 ? materials[0].plantId : 0;
        }

        // ---------- 무역소(지역/판매) ----------

        public List<RegionData> GetAllRegions()
        {
            return regionDict.Values.OrderBy(r => r.regionID).ToList();
        }

        public RegionData GetRegionByID(int id)
        {
            regionDict.TryGetValue(id, out var region);
            return region;
        }

        /// <summary>해당 지역에서 특정 식물이 받는 판매 보너스(%). 없으면 0.</summary>
        public float GetPlantBonusPercent(int regionId, int plantId)
        {
            var region = GetRegionByID(regionId);
            if (region == null) return 0f;
            if (!regionBonusGroups.TryGetValue(region.regionBonusGroupID, out var list)) return 0f;
            foreach (var (pid, bonus) in list)
                if (pid == plantId) return bonus;
            return 0f;
        }

        public SellUpgradeData GetSellUpgradeByLevel(int level)
        {
            sellUpgradeDict.TryGetValue(level, out var data);
            return data;
        }

        // ---------- 캐릭터(뽑기) ----------

        public List<CharacterData> GetAllCharacters()
        {
            return characterDict.Values.OrderBy(c => c.characterID).ToList();
        }

        public CharacterData GetCharacterByID(int id)
        {
            characterDict.TryGetValue(id, out var character);
            return character;
        }

        public bool IsCharacterOwned(int id)
        {
            return ownedCharacterIDs.Contains(id);
        }

        /// <summary>캐릭터를 인벤토리에 추가한다. 이미 보유 중이면 아무 일도 일어나지 않는다(중복 획득 없음).</summary>
        public void AddCharacterToInventory(int id)
        {
            ownedCharacterIDs.Add(id);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WeedHoldings
{
    public class PlantDatabase : MonoBehaviour
    {
        public static PlantDatabase Instance { get; private set; }

        [Header("식물 데이터 목록")]
        public List<PlantData> allPlants = new List<PlantData>();

        [Header("해금된 식물 ID 목록")]
        public List<int> unlockedPlantIDs = new List<int>();

        private Dictionary<int, PlantData> plantDict = new Dictionary<int, PlantData>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadFromResources();
            InitializeDatabase();
        }

        void LoadFromResources()
        {
            var loaded = Resources.LoadAll<PlantData>("Plants");
            if (loaded != null && loaded.Length > 0)
            {
                allPlants.Clear();
                allPlants.AddRange(loaded);
                allPlants.Sort((a, b) => a.plantID.CompareTo(b.plantID));
                Debug.Log($"[PlantDatabase] Loaded {allPlants.Count} plants from Resources/Plants");
            }
        }

        public void InitializeDatabase()
        {
            plantDict.Clear();
            foreach (var plant in allPlants)
            {
                if (plant != null && !plantDict.ContainsKey(plant.plantID))
                {
                    plantDict.Add(plant.plantID, plant);
                }
            }

            LoadUnlockedPlants();
        }

        public void SaveUnlockedPlants()
        {
            string idsStr = string.Join(",", unlockedPlantIDs);
            PlayerPrefs.SetString("UnlockedPlantIDs", idsStr);
            PlayerPrefs.Save();
        }

        public void LoadUnlockedPlants()
        {
            unlockedPlantIDs.Clear();
            string idsStr = PlayerPrefs.GetString("UnlockedPlantIDs", "");
            if (!string.IsNullOrEmpty(idsStr))
            {
                string[] split = idsStr.Split(',');
                foreach (string s in split)
                {
                    if (int.TryParse(s, out int id))
                        unlockedPlantIDs.Add(id);
                }
            }

            if (unlockedPlantIDs.Count == 0 && allPlants.Count > 0)
            {
                var firstPlant = allPlants.OrderBy(p => p.requiredGoldToUnlock).FirstOrDefault();
                if (firstPlant != null)
                {
                    unlockedPlantIDs.Add(firstPlant.plantID);
                }
                SaveUnlockedPlants();
            }
        }

        public PlantData GetPlantByID(int id)
        {
            plantDict.TryGetValue(id, out PlantData plant);
            return plant;
        }

        public List<PlantData> GetUnlockedPlants()
        {
            List<PlantData> result = new List<PlantData>();
            foreach (int id in unlockedPlantIDs)
            {
                PlantData plant = GetPlantByID(id);
                if (plant != null) result.Add(plant);
            }
            return result;
        }

        public bool IsPlantUnlocked(int id)
        {
            return unlockedPlantIDs.Contains(id);
        }

        public void UnlockPlant(int id)
        {
            if (!unlockedPlantIDs.Contains(id))
            {
                unlockedPlantIDs.Add(id);
                SaveUnlockedPlants();
            }
        }

        public List<PlantData> GetLockedPlants()
        {
            List<PlantData> result = new List<PlantData>();
            foreach (var plant in allPlants)
            {
                if (!unlockedPlantIDs.Contains(plant.plantID))
                {
                    result.Add(plant);
                }
            }
            return result;
        }
    }
}

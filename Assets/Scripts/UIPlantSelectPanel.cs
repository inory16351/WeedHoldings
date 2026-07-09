using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WeedHoldings
{
    /// <summary>
    /// 재배 화면 우측: 식물 선택 리스트 패널.
    /// </summary>
    public class UIPlantSelectPanel : MonoBehaviour
    {
        [Header("리스트 설정")]
        public GameObject      plantSlotPrefab;
        public Transform       slotContainer;

        [Header("선택된 식물 표시")]
        public PlantData       selectedPlant;

        List<UIPlantSelectSlot> slotList = new List<UIPlantSelectSlot>();

        void Start()
        {
            RefreshPlantList();

            if (InventoryManager.Instance != null)
                InventoryManager.Instance.OnInventoryChanged += RefreshStocks;
        }

        void OnDestroy()
        {
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.OnInventoryChanged -= RefreshStocks;
        }

        public void RefreshPlantList()
        {
            ClearSlots();
            if (PlantDatabase.Instance == null) return;

            // 해금된 식물 → 잠긴 식물 순서로 표시
            foreach (PlantData pd in PlantDatabase.Instance.allPlants)
            {
                bool locked = !PlantDatabase.Instance.IsPlantUnlocked(pd.plantID);
                AddSlot(pd, locked);
            }

            // 기본 선택: 해금된 식물 중 첫 번째 식물 기본 선택
            if (selectedPlant == null && PlantDatabase.Instance.allPlants.Count > 0)
            {
                var unlocked = PlantDatabase.Instance.GetUnlockedPlants();
                if (unlocked != null && unlocked.Count > 0)
                {
                    selectedPlant = unlocked[0];
                }
            }

            UpdateSelectionVisuals();
        }

        void AddSlot(PlantData pd, bool locked)
        {
            if (plantSlotPrefab == null || slotContainer == null) return;

            GameObject go = Instantiate(plantSlotPrefab, slotContainer);
            UIPlantSelectSlot slot = go.GetComponent<UIPlantSelectSlot>();
            if (slot == null) return;

            slot.Setup(pd, locked);
            slot.OnSlotClicked += HandleSlotClicked;
            slotList.Add(slot);
        }

        void HandleSlotClicked(PlantData pd)
        {
            if (pd == null) return;

            // 잠긴 식물: 연구소 레벨 조건 + 골드 소모로 해금
            if (!PlantDatabase.Instance.IsPlantUnlocked(pd.plantID))
            {
                LabManager lab = LabManager.Instance;
                if (lab != null && !lab.IsPlantUnlockable(pd.requiredHarvestLevel))
                {
                    Debug.Log($"[UIPlantSelectPanel] {pd.plantName} 해금 불가: 연구소 Lv.{pd.requiredHarvestLevel} 필요");
                    return;
                }

                if (CurrencyManager.Instance != null &&
                    CurrencyManager.Instance.CanAfford(pd.requiredGoldToUnlock))
                {
                    CurrencyManager.Instance.SpendGold(pd.requiredGoldToUnlock);
                    PlantDatabase.Instance.UnlockPlant(pd.plantID);
                    RefreshPlantList();
                }
                return;
            }

            // 해금된 식물: 선택 (CultivationUIManager가 사용)
            selectedPlant = pd;
            Debug.Log($"[UIPlantSelectPanel] 선택: {pd.plantName}");
            UpdateSelectionVisuals();
        }

        void UpdateSelectionVisuals()
        {
            foreach (var s in slotList)
            {
                if (s != null)
                {
                    s.SetSelected(selectedPlant != null && s.GetPlantData() != null && s.GetPlantData().plantID == selectedPlant.plantID);
                }
            }
        }

        void ClearSlots()
        {
            foreach (var s in slotList)
            {
                if (s != null)
                {
                    s.OnSlotClicked -= HandleSlotClicked;
                    Destroy(s.gameObject);
                }
            }
            slotList.Clear();
            selectedPlant = null;
        }

        void RefreshStocks()
        {
            foreach (var s in slotList) s?.RefreshStock();
        }

        public PlantData GetSelectedPlant() => selectedPlant;
    }
}

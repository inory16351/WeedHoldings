using UnityEngine;
using UnityEngine.UI;

namespace WeedHoldings
{
    /// <summary>
    /// FarmPanel의 ALL_Plant(한번에 심기) / All_Harvest(한번에 수확) 버튼 담당.
    /// 연구소 업그레이드 레벨에 따라 해금되는 harvestGroupID 그룹 소속 식물에 한해 동작한다.
    /// </summary>
    public class UIBulkActionWidget : MonoBehaviour
    {
        Button allPlantButton;
        Button allHarvestButton;

        void Awake()
        {
            allPlantButton = transform.Find("ALL_Plant")?.GetComponent<Button>();
            allHarvestButton = transform.Find("All_Harvest")?.GetComponent<Button>();

            if (allPlantButton != null)
                allPlantButton.onClick.AddListener(OnAllPlantClicked);
            if (allHarvestButton != null)
                allHarvestButton.onClick.AddListener(OnAllHarvestClicked);
        }

        void Update()
        {
            RefreshButtons();
        }

        void RefreshButtons()
        {
            if (allPlantButton != null)
            {
                bool canPlant = PlantSelectionManager.Instance != null
                    && PlantSelectionManager.Instance.HasSelection
                    && LabUpgradeManager.Instance != null
                    && LabUpgradeManager.Instance.IsHarvestGroupUnlocked(PlantSelectionManager.Instance.selectedPlant.harvestGroupID);
                allPlantButton.interactable = canPlant;
            }

            if (allHarvestButton != null)
            {
                allHarvestButton.interactable = HasAnyHarvestable();
            }
        }

        /// <summary>밭 전체가 수확 가능 상태일 필요는 없다. 현재 수확 가능한 밭이 하나라도 있으면 된다.</summary>
        bool HasAnyHarvestable()
        {
            if (CultivationManager.Instance == null) return false;

            foreach (var plot in CultivationManager.Instance.GetAllPlots())
            {
                if (plot != null && plot.GrowthState == GrowthState.ReadyToHarvest && plot.plantedPlant != null)
                    return true;
            }
            return false;
        }

        void OnAllPlantClicked()
        {
            if (PlantSelectionManager.Instance == null || !PlantSelectionManager.Instance.HasSelection) return;
            if (CultivationManager.Instance == null || LabUpgradeManager.Instance == null) return;

            var selected = PlantSelectionManager.Instance.selectedPlant;
            if (!LabUpgradeManager.Instance.IsHarvestGroupUnlocked(selected.harvestGroupID)) return;

            // 아무것도 심기지 않은 밭 전체에 선택된 식물을 심는다 (해금 안 된 밭은 제외).
            foreach (var plot in CultivationManager.Instance.GetAllPlots())
            {
                if (plot != null && plot.IsUnlocked && plot.GrowthState == GrowthState.Empty)
                    plot.PlantPlant(selected);
            }
        }

        void OnAllHarvestClicked()
        {
            if (CultivationManager.Instance == null) return;

            // 밭 전체가 수확 가능 상태가 될 때까지 기다리지 않고, 지금 수확 가능한 밭만 골라 전부 수확한다.
            foreach (var plot in CultivationManager.Instance.GetAllPlots())
            {
                if (plot != null && plot.GrowthState == GrowthState.ReadyToHarvest && plot.plantedPlant != null)
                    plot.Harvest();
            }
        }
    }
}

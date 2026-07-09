using System;
using UnityEngine;

namespace WeedHoldings
{
    public enum PlotState
    {
        Locked,          // 미해금
        Empty,           // 빈칸
        Growing,         // 성장중
        ReadyToHarvest,  // 수확 가능
        Withered         // 시들음
    }

    /// <summary>
    /// 밭 1칸의 데이터 및 동작 처리.
    /// 시각적 표현은 UIFarmPlotCell(Canvas UI)이 담당합니다.
    /// </summary>
    public class FarmPlot : MonoBehaviour
    {
        public int plotIndex;
        public int plotRow;   // 밭 행 번호 (0-based)
        public int plotCol;   // 밭 열 번호

        public PlotState plotState = PlotState.Locked;

        public PlantGrowthManager growthManager;

        public event Action<int> OnPlotClicked;
        public event Action<int, int, PlantData> OnPlotHarvested;   // index, amount, plantData
        public event Action<int> OnPlotStateChanged;

        void Awake()
        {
            if (growthManager == null)
                growthManager = GetComponent<PlantGrowthManager>();

            if (growthManager != null)
            {
                growthManager.OnPhaseChanged         += HandlePhaseChange;
                growthManager.OnPlantReadyToHarvest  += HandleReadyToHarvest;
                growthManager.OnPlantDied            += HandlePlantDied;
            }
        }

        void HandlePhaseChange(GrowthPhase phase)
        {
            switch (phase)
            {
                case GrowthPhase.Empty:
                    if (plotState != PlotState.Locked)
                        plotState = PlotState.Empty;
                    break;
                case GrowthPhase.Planted:
                case GrowthPhase.Baby:
                case GrowthPhase.Growing:
                    plotState = PlotState.Growing;
                    break;
                case GrowthPhase.Mature:
                    plotState = PlotState.ReadyToHarvest;
                    break;
                case GrowthPhase.Withered:
                    plotState = PlotState.Withered;
                    break;
            }
            OnPlotStateChanged?.Invoke(plotIndex);
        }

        void HandleReadyToHarvest()
        {
            plotState = PlotState.ReadyToHarvest;
            OnPlotStateChanged?.Invoke(plotIndex);
        }

        void HandlePlantDied()
        {
            plotState = PlotState.Empty;
            OnPlotStateChanged?.Invoke(plotIndex);
        }

        public void Initialize(int index, int row, int col)
        {
            plotIndex = index;
            plotRow   = row;
            plotCol   = col;
            name      = $"FarmPlot_{index:D2}";
        }

        /// <summary>밭 해금 (잠금 → 빈칸)</summary>
        public void Unlock()
        {
            if (plotState != PlotState.Locked) return;
            plotState = PlotState.Empty;
            OnPlotStateChanged?.Invoke(plotIndex);
        }

        public void PlantSeed(PlantData seedData)
        {
            if (growthManager == null || plotState != PlotState.Empty) return;
            growthManager.PlantSeed(seedData);
            plotState = PlotState.Growing;
            OnPlotStateChanged?.Invoke(plotIndex);
        }

        public void Water()
        {
            if (growthManager != null)
                growthManager.WaterPlant();
        }

        public int TryHarvest()
        {
            if (growthManager == null || !growthManager.CanHarvest()) return 0;
            PlantData pd = growthManager.currentPlantData;
            int amount = growthManager.Harvest();
            if (amount > 0)
            {
                plotState = PlotState.Empty;
                OnPlotHarvested?.Invoke(plotIndex, amount, pd);
                OnPlotStateChanged?.Invoke(plotIndex);
            }
            return amount;
        }

        public void ClearWithered()
        {
            if (plotState == PlotState.Withered)
                growthManager?.ClearPlot();
        }

        public void NotifyClicked() => OnPlotClicked?.Invoke(plotIndex);

        void OnDestroy()
        {
            if (growthManager != null)
            {
                growthManager.OnPhaseChanged        -= HandlePhaseChange;
                growthManager.OnPlantReadyToHarvest -= HandleReadyToHarvest;
                growthManager.OnPlantDied           -= HandlePlantDied;
            }
        }
    }
}

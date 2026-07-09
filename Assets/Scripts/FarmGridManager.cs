using System.Collections.Generic;
using UnityEngine;

namespace WeedHoldings
{
    /// <summary>
    /// 밭 전체 격자(4×6)를 관리합니다.
    /// - 기본 3행(행 0~2)은 처음부터 해금
    /// - 나머지 행은 LabManager 업그레이드로 해금
    /// - UIFarmPlotCell이 시각적 표현을 담당
    /// </summary>
    public class FarmGridManager : MonoBehaviour
    {
        public static FarmGridManager Instance { get; private set; }

        [Header("밭 설정")]
        public int gridColumns = GameConstants.FARM_GRID_COLS;   // 6
        public int gridRows    = GameConstants.FARM_GRID_ROWS;   // 4

        [Header("밭 목록 (런타임 생성)")]
        public List<FarmPlot> farmPlots = new List<FarmPlot>();

        public int GetUnlockCostForNextPlot()
        {
            int purchasedCount = 0;
            foreach (var p in farmPlots)
            {
                if (p != null && p.plotRow >= GameConstants.DEFAULT_UNLOCKED_ROWS && p.plotState != PlotState.Locked)
                {
                    purchasedCount++;
                }
            }
            return 500 + purchasedCount * 500;
        }

        public int TotalPlots   => farmPlots.Count;
        public int UnlockedPlotCount
        {
            get
            {
                int c = 0;
                foreach (var p in farmPlots)
                    if (p.plotState != PlotState.Locked) c++;
                return c;
            }
        }
        public int EmptyPlotCount
        {
            get
            {
                int c = 0;
                foreach (var p in farmPlots)
                    if (p.plotState == PlotState.Empty) c++;
                return c;
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            GeneratePlots();
        }

        void Start()
        {
            ApplyInitialLockState();
            LoadPlots();
        }

        void OnDestroy()
        {
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SavePlots();
        }

        void OnApplicationQuit()
        {
            SavePlots();
        }

        /// <summary>FarmPlot 오브젝트만 생성 (UI 셀은 UIFarmPlotCell에서 생성)</summary>
        void GeneratePlots()
        {
            farmPlots.Clear();
            for (int row = 0; row < gridRows; row++)
            {
                for (int col = 0; col < gridColumns; col++)
                {
                    int idx = row * gridColumns + col;

                    GameObject go = new GameObject($"FarmPlot_{idx:D2}");
                    go.transform.SetParent(transform);

                    PlantGrowthManager pgm  = go.AddComponent<PlantGrowthManager>();
                    FarmPlot fp             = go.AddComponent<FarmPlot>();
                    fp.growthManager        = pgm;
                    fp.Initialize(idx, row, col);
                    fp.plotState            = PlotState.Locked;  // 초기엔 전부 잠금

                    fp.OnPlotHarvested  += HandlePlotHarvested;

                    farmPlots.Add(fp);
                }
            }
        }

        /// <summary>LabManager 현재 레벨을 기준으로 초기 해금 상태 적용</summary>
        void ApplyInitialLockState()
        {
            LabManager lab = LabManager.Instance;
            for (int row = 0; row < gridRows; row++)
            {
                bool rowUnlocked = (lab != null)
                    ? lab.IsFieldRowUnlocked(row)
                    : (row < GameConstants.DEFAULT_UNLOCKED_ROWS);

                for (int col = 0; col < gridColumns; col++)
                {
                    FarmPlot fp = GetPlot(row * gridColumns + col);
                    if (fp == null) continue;

                    if (rowUnlocked && fp.plotState == PlotState.Locked)
                        fp.Unlock();
                }
            }
        }



        void HandlePlotHarvested(int idx, int amount, PlantData pd)
        {
            if (pd == null) return;
            int gold = amount * pd.sellGoldValue;
            CurrencyManager.Instance?.AddGold(gold);
            InventoryManager.Instance?.AddItem(pd.plantID, amount);
        }

        // ── 편의 메서드 ──

        public FarmPlot GetPlot(int index)
        {
            if (index >= 0 && index < farmPlots.Count)
                return farmPlots[index];
            return null;
        }

        public FarmPlot GetFirstEmptyPlot()
        {
            foreach (var p in farmPlots)
                if (p.plotState == PlotState.Empty) return p;
            return null;
        }

        public List<FarmPlot> GetPlotsByState(PlotState state)
        {
            var result = new List<FarmPlot>();
            foreach (var p in farmPlots)
                if (p.plotState == state) result.Add(p);
            return result;
        }

        public void PlantInFirstEmptyPlot(PlantData plantData)
        {
            var p = GetFirstEmptyPlot();
            p?.PlantSeed(plantData);
        }

        /// <summary>그룹 ID에 속한 식물의 모든 빈 칸에 한번에 심기</summary>
        public int PlantAllEmpty(PlantData plantData)
        {
            int count = 0;
            foreach (var p in farmPlots)
            {
                if (p.plotState == PlotState.Empty)
                {
                    p.PlantSeed(plantData);
                    count++;
                }
            }
            if (count > 0) SavePlots();
            return count;
        }

        public void WaterAllPlots()
        {
            foreach (var p in farmPlots)
                p.Water();
            SavePlots();
        }

        /// <summary>수확 가능한 모든 밭 수확 → 합산 골드 반환</summary>
        public int HarvestAll()
        {
            int totalGold = 0;
            var ready = GetPlotsByState(PlotState.ReadyToHarvest);
            foreach (var p in ready)
            {
                PlantData pd = p.growthManager?.currentPlantData;
                int amount = p.TryHarvest();
                if (amount > 0 && pd != null)
                    totalGold += amount * pd.sellGoldValue;
            }
            if (ready.Count > 0) SavePlots();
            return totalGold;
        }

        /// <summary>밭 칸 개별 클릭 처리 (UIFarmPlotCell에서 호출)</summary>
        public void OnPlotCellClicked(int plotIndex)
        {
            FarmPlot fp = GetPlot(plotIndex);
            if (fp == null) return;

            switch (fp.plotState)
            {
                case PlotState.Empty:
                    var selectPanel = FindFirstObjectByType<UIPlantSelectPanel>();
                    PlantData pd = selectPanel?.GetSelectedPlant();
                    if (pd != null)
                    {
                        fp.PlantSeed(pd);
                        var gridView = FindFirstObjectByType<UIFarmGridView>();
                        gridView?.RefreshCell(plotIndex);
                        SavePlots();
                    }
                    break;
                case PlotState.Growing:
                    fp.Water();
                    SavePlots();
                    break;
                case PlotState.ReadyToHarvest:
                    fp.TryHarvest();
                    SavePlots();
                    break;
                case PlotState.Withered:
                    fp.ClearWithered();
                    SavePlots();
                    break;
                case PlotState.Locked:
                    TryUnlockPlot(fp);
                    break;
            }
        }

        void TryUnlockPlot(FarmPlot fp)
        {
            // 이전 행이 모두 해금되어 있는지 확인
            if (fp.plotRow > 0)
            {
                int prevRowStart = (fp.plotRow - 1) * gridColumns;
                for (int c = 0; c < gridColumns; c++)
                {
                    var prev = GetPlot(prevRowStart + c);
                    if (prev != null && prev.plotState == PlotState.Locked) return;
                }
            }

            // 골드로 추가 해금(구매)한 칸의 누적 개수 계산 (기본 3행 외 영역)
            int purchasedCount = 0;
            foreach (var p in farmPlots)
            {
                if (p != null && p.plotRow >= GameConstants.DEFAULT_UNLOCKED_ROWS && p.plotState != PlotState.Locked)
                {
                    purchasedCount++;
                }
            }

            // 시설 업그레이드 레벨에 따른 최대 추가 해금 가능 칸수 제한 확인
            int maxAllowedPurchased = LabManager.Instance != null ? LabManager.Instance.GetMaxUnlockablePlots() : 0;
            if (purchasedCount >= maxAllowedPurchased)
            {
                Debug.LogWarning($"[FarmGridManager] 연구소 레벨 부족으로 추가 밭을 해금할 수 없습니다. (현재 해금: {purchasedCount} / 허용: {maxAllowedPurchased})");
                return;
            }

            // 가격 공식: 500 + (구매 전 구매한 칸의 수 * 500)
            int cost = 500 + purchasedCount * 500;

            if (CurrencyManager.Instance != null && CurrencyManager.Instance.SpendGold(cost))
            {
                fp.Unlock();
                var gridView = FindFirstObjectByType<UIFarmGridView>();
                gridView?.RefreshCell(fp.plotIndex);
                gridView?.RefreshAll();
                SavePlots();
                Debug.Log($"[FarmGridManager] 밭 칸 해금 완료! 소모골드: {cost}, 현재 추가해금수: {purchasedCount + 1}");
            }
        }

        public void SavePlots()
        {
            for (int i = 0; i < farmPlots.Count; i++)
            {
                var fp = farmPlots[i];
                if (fp == null) continue;

                PlayerPrefs.SetInt($"Plot_{i}_State", (int)fp.plotState);
                if (fp.growthManager != null && fp.growthManager.currentPlantData != null)
                {
                    PlayerPrefs.SetInt($"Plot_{i}_PlantID", fp.growthManager.currentPlantData.plantID);
                    PlayerPrefs.SetFloat($"Plot_{i}_PlantTime", fp.growthManager.plantTime);
                    PlayerPrefs.SetFloat($"Plot_{i}_TimeSinceLastWater", fp.growthManager.timeSinceLastWater);
                }
                else
                {
                    PlayerPrefs.SetInt($"Plot_{i}_PlantID", 0);
                    PlayerPrefs.SetFloat($"Plot_{i}_PlantTime", 0f);
                    PlayerPrefs.SetFloat($"Plot_{i}_TimeSinceLastWater", 0f);
                }
            }
            PlayerPrefs.Save();
        }

        public void LoadPlots()
        {
            for (int i = 0; i < farmPlots.Count; i++)
            {
                var fp = farmPlots[i];
                if (fp == null) continue;

                PlotState defaultState = fp.plotRow < GameConstants.DEFAULT_UNLOCKED_ROWS ? PlotState.Empty : PlotState.Locked;
                fp.plotState = (PlotState)PlayerPrefs.GetInt($"Plot_{i}_State", (int)defaultState);

                int plantID = PlayerPrefs.GetInt($"Plot_{i}_PlantID", 0);
                if (plantID > 0 && PlantDatabase.Instance != null)
                {
                    var pd = PlantDatabase.Instance.GetPlantByID(plantID);
                    if (pd != null && fp.growthManager != null)
                    {
                        fp.growthManager.PlantSeed(pd);
                        fp.growthManager.plantTime = PlayerPrefs.GetFloat($"Plot_{i}_PlantTime", 0f);
                        fp.growthManager.timeSinceLastWater = PlayerPrefs.GetFloat($"Plot_{i}_TimeSinceLastWater", 0f);

                        if (fp.plotState == PlotState.ReadyToHarvest)
                            fp.growthManager.currentPhase = GrowthPhase.Mature;
                        else if (fp.plotState == PlotState.Withered)
                            fp.growthManager.currentPhase = GrowthPhase.Withered;
                        else if (fp.plotState == PlotState.Growing)
                            fp.growthManager.currentPhase = GrowthPhase.Growing;
                    }
                }
                else
                {
                    fp.growthManager?.ClearPlot();
                }
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WeedHoldings
{
    /// <summary>
    /// 재배 관리자 - 모든 밭 객체의 성장/시들음 시간 통합 관리
    /// </summary>
    public class CultivationManager : MonoBehaviour
    {
        public static CultivationManager Instance { get; private set; }

        [Header("재배 설정")]
        [SerializeField] private bool useRealTime = true;           // 실제 시간 사용 여부
        [SerializeField] private float globalGrowthSpeed = 1f;      // 전역 성장 속도 배율
        [SerializeField] private bool enableWitherSystem = true;    // 고사 시스템 활성화

        [Header("밭 프리팹")]
        [SerializeField] private GameObject fieldPlotPrefab;

        private List<FieldPlot> allPlots = new List<FieldPlot>();
        private Dictionary<int, FieldPlot> plotDict = new Dictionary<int, FieldPlot>();

        // 이벤트
        public System.Action<FieldPlot, GrowthState> OnPlotGrowthStateChanged;
        public System.Action<FieldPlot, WitherState> OnPlotWitherStateChanged;
        public System.Action<FieldPlot> OnPlotReadyToHarvest;
        public System.Action<FieldPlot> OnPlotWithering;     // 시들음 시작
        public System.Action<FieldPlot> OnPlotWithered;      // 고사
        public System.Action<FieldPlot> OnPlotPlanted;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            // 씬 내 모든 FieldPlot 자동 등록
            RegisterAllPlotsInScene();
        }

        void Update()
        {
            if (!useRealTime) return;
            if (!enableWitherSystem) return;

            // 성장 속도 보너스는 매 프레임 배율을 곱하는 방식이 아니라, PlantPlant() 시점에
            // growTimeRemaining 자체에 미리 나눠서 반영한다(아래 GetCurrentGrowthSpeedMultiplier 참고).
            // 여기서는 항상 실제 시간(1배속)으로만 감소시킨다 — 심는 순간 이미 배율이 확정되어 있으므로.
            for (int i = allPlots.Count - 1; i >= 0; i--)
            {
                var plot = allPlots[i];
                if (plot != null && plot.IsGrowing)
                {
                    plot.UpdateGrowthTime(Time.deltaTime);
                }
            }
        }

        /// <summary>
        /// 현재 연구소 레벨 기준 성장 속도 배율. FieldPlot.PlantPlant()가 심는 순간
        /// growTimeRemaining = growTimeSeconds / 이 배율 로 미리 계산해 확정한다.
        /// (재적용 순서 문제를 피하려고 매번 LabUpgradeManager에서 직접 최신값을 읽는다)
        /// </summary>
        public float GetCurrentGrowthSpeedMultiplier()
        {
            if (LabUpgradeManager.Instance == null) return globalGrowthSpeed;

            var data = LabUpgradeManager.Instance.GetCurrentUpgradeData();
            int bonus = data != null ? data.growBonusPercent : 0;
            return Mathf.Max(0.01f, (100f + bonus) / 100f);
        }

        /// <summary>
        /// 씬 내 모든 FieldPlot 자동 등록
        /// </summary>
        public void RegisterAllPlotsInScene()
        {
            // FindObjectsInactive.Include 필수: UILobbyNavigation.Start()가 CultivationManager 생성 직후
            // OnBack()으로 FarmPanel을 비활성화하는데, 이 등록 스윕은 그보다 늦게(같은 프레임 뒤쪽) 실행되므로
            // 기본 옵션(활성 오브젝트만 탐색)으로는 FarmPanel 하위의 밭들이 전부 누락된다.
            var plots = FindObjectsByType<FieldPlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var plot in plots)
            {
                RegisterPlot(plot);
            }
            Debug.Log($"[CultivationManager] {allPlots.Count}개 밭 객체 등록 완료");
        }

        /// <summary>
        /// 개별 밭 등록
        /// </summary>
        public void RegisterPlot(FieldPlot plot)
        {
            if (plot == null) return;
            if (plotDict.ContainsKey(plot.PlotIndex)) return;

            plotDict[plot.PlotIndex] = plot;
            if (!allPlots.Contains(plot))
                allPlots.Add(plot);

            // 이벤트 구독
            plot.OnGrowthStateChanged += HandleGrowthStateChanged;
            plot.OnWitherStateChanged += HandleWitherStateChanged;
        }

        /// <summary>
        /// 밭 등록 해제
        /// </summary>
        public void UnregisterPlot(FieldPlot plot)
        {
            if (plot == null) return;
            plotDict.Remove(plot.PlotIndex);
            allPlots.Remove(plot);
            plot.OnGrowthStateChanged -= HandleGrowthStateChanged;
            plot.OnWitherStateChanged -= HandleWitherStateChanged;
        }

        /// <summary>
        /// 성장 상태 변경 이벤트 처리
        /// </summary>
        private void HandleGrowthStateChanged(FieldPlot plot, GrowthState newState)
        {
            OnPlotGrowthStateChanged?.Invoke(plot, newState);

            switch (newState)
            {
                case GrowthState.Planted:
                    OnPlotPlanted?.Invoke(plot);
                    break;
                case GrowthState.ReadyToHarvest:
                    OnPlotReadyToHarvest?.Invoke(plot);
                    break;
            }
        }

        /// <summary>
        /// 시들음 상태 변경 이벤트 처리
        /// </summary>
        private void HandleWitherStateChanged(FieldPlot plot, WitherState newState)
        {
            switch (newState)
            {
                case WitherState.Withering:
                    // 시들음 시작 (10초 유예 시작)
                    OnPlotWithering?.Invoke(plot);
                    break;
                case WitherState.Withered:
                    // 고사 확정
                    OnPlotWithered?.Invoke(plot);
                    break;
            }
        }

        /// <summary>
        /// 특정 인덱스의 밭 가져오기
        /// </summary>
        public FieldPlot GetPlot(int index)
        {
            plotDict.TryGetValue(index, out var plot);
            return plot;
        }

        /// <summary>
        /// 모든 밭 가져오기
        /// </summary>
        public IReadOnlyList<FieldPlot> GetAllPlots() => allPlots.AsReadOnly();

        /// <summary>
        /// 성장 중인 밭 개수
        /// </summary>
        public int GrowingPlotCount => allPlots.Count(p => p != null && p.IsGrowing);

        /// <summary>
        /// 수확 가능 밭 개수
        /// </summary>
        public int ReadyToHarvestCount => allPlots.Count(p => p != null && p.GrowthState == GrowthState.ReadyToHarvest);

        /// <summary>
        /// 시들음 중인 밭 개수
        /// </summary>
        public int WitheringCount => allPlots.Count(p => p != null && p.WitherState == WitherState.Withering);

        /// <summary>
        /// 고사된 밭 개수
        /// </summary>
        public int WitheredCount => allPlots.Count(p => p != null && p.WitherState == WitherState.Withered);

        /// <summary>
        /// 빈 밭 개수
        /// </summary>
        public int EmptyPlotCount => allPlots.Count(p => p != null && p.GrowthState == GrowthState.Empty);

        /// <summary>
        /// 전역 성장 속도 설정
        /// </summary>
        public void SetGlobalGrowthSpeed(float speed)
        {
            globalGrowthSpeed = Mathf.Max(0.01f, speed);
        }

        /// <summary>
        /// 고사 시스템 토글
        /// </summary>
        public void SetWitherSystemEnabled(bool enabled)
        {
            enableWitherSystem = enabled;
        }

        /// <summary>
        /// 모든 밭 강제 수확
        /// </summary>
        public void ForceHarvestAll()
        {
            foreach (var plot in allPlots)
            {
                if (plot != null && plot.GrowthState == GrowthState.ReadyToHarvest)
                {
                    // 하베스트는 FieldPlot의 Harvest 메서드 직접 호출
                }
            }
        }

        /// <summary>
        /// 모든 밭 리셋
        /// </summary>
        public void ResetAllPlots()
        {
            foreach (var plot in allPlots)
            {
                if (plot != null)
                {
                    plot.ResetPlot();
                }
            }
        }

        /// <summary>
        /// 특정 밭에 식물 심기 (매니저 레벨)
        /// </summary>
        public bool PlantAt(int plotIndex, PlantData plantData)
        {
            var plot = GetPlot(plotIndex);
            if (plot == null || plot.GrowthState != GrowthState.Empty) return false;

            plot.PlantPlant(plantData);
            return true;
        }

        /// <summary>
        /// 밭 수확 (매니저 레벨)
        /// </summary>
        public bool HarvestAt(int plotIndex)
        {
            var plot = GetPlot(plotIndex);
            if (plot == null || plot.GrowthState != GrowthState.ReadyToHarvest) return false;

            // Harvest는 FieldPlot의 Harvest 메서드 직접 호출 필요
            return true;
        }

        /// <summary>
        /// 고사된 밭 정리
        /// </summary>
        public bool ClearWitheredAt(int plotIndex)
        {
            var plot = GetPlot(plotIndex);
            if (plot == null || plot.WitherState != WitherState.Withered) return false;

            // ClearWithered는 FieldPlot의 ClearWithered 메서드 직접 호출 필요
            return true;
        }
    }
}
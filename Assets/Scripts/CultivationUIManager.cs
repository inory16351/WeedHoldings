using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WeedHoldings
{
    /// <summary>
    /// 재배 화면 전체 UI 제어.
    /// 탭 전환은 UITabManager가 담당하며, 이 클래스는 재배 탭 내 버튼 동작을 처리합니다.
    /// </summary>
    public class CultivationUIManager : MonoBehaviour
    {
        [Header("골드 / 상태")]
        public UIGoldDisplay    goldDisplay;
        public TextMeshProUGUI  statusText;   // 밭 상태 요약

        [Header("하단 액션 버튼")]
        public Button waterAllButton;
        public Button plantAllButton;   // 한번에 심기
        public Button harvestAllButton; // 한번에 수확

        [Header("식물 선택 패널 (우측)")]
        public UIPlantSelectPanel plantSelectPanel;

        [Header("밭 정보")]
        public FarmGridManager farmGrid;

        void Start()
        {
            if (farmGrid == null)
                farmGrid = FindFirstObjectByType<FarmGridManager>();

            if (waterAllButton   != null) waterAllButton.onClick.AddListener(OnWaterAll);
            if (plantAllButton   != null) plantAllButton.onClick.AddListener(OnPlantAll);
            if (harvestAllButton != null) harvestAllButton.onClick.AddListener(OnHarvestAll);

            InvokeRepeating(nameof(UpdateStatusText), 0f, 0.5f);
        }

        void UpdateStatusText()
        {
            if (statusText == null || farmGrid == null) return;

            int total    = farmGrid.UnlockedPlotCount;
            int empty    = farmGrid.EmptyPlotCount;
            int growing  = farmGrid.GetPlotsByState(PlotState.Growing).Count;
            int ready    = farmGrid.GetPlotsByState(PlotState.ReadyToHarvest).Count;
            int withered = farmGrid.GetPlotsByState(PlotState.Withered).Count;

            statusText.text =
                $"해금: {total}칸 | 빈칸: {empty} | 성장중: {growing} | 수확가능: {ready}" +
                (withered > 0 ? $" | <color=#FF4040>시들음: {withered}</color>" : "");

            // 하단 버튼 활성화 조건 (재배시스템 기획서 기준: 그룹 일괄 기능이 활성화되었을 때만)
            bool hasGroup = false;
            PlantData sel = plantSelectPanel?.GetSelectedPlant();
            if (sel != null && LabManager.Instance != null && sel.harvestGroupID > 0)
                hasGroup = LabManager.Instance.IsGroupActive(sel.harvestGroupID);

            if (plantAllButton != null)
                plantAllButton.interactable = (sel != null && empty > 0 && hasGroup);

            if (harvestAllButton != null)
                harvestAllButton.interactable = (ready > 0 && hasGroup);
        }

        void OnWaterAll()
        {
            farmGrid?.WaterAllPlots();
        }

        void OnPlantAll()
        {
            if (farmGrid == null) return;
            PlantData pd = plantSelectPanel?.GetSelectedPlant();
            if (pd == null)
            {
                Debug.Log("[CultivationUIManager] 식물을 먼저 선택하세요.");
                return;
            }

            // 그룹 한번에 심기 활성화 여부 확인
            LabManager lab = LabManager.Instance;
            if (pd.harvestGroupID > 0 && lab != null && lab.IsGroupActive(pd.harvestGroupID))
            {
                int planted = farmGrid.PlantAllEmpty(pd);
                Debug.Log($"[CultivationUIManager] 한번에 심기: {planted}칸에 {pd.plantName} 심음");
            }
            else
            {
                // 한번에 심기 미활성 → 빈칸 하나에만 심기
                farmGrid.PlantInFirstEmptyPlot(pd);
            }
        }

        void OnHarvestAll()
        {
            if (farmGrid == null) return;
            PlantData sel = plantSelectPanel?.GetSelectedPlant();

            // 선택한 식물의 그룹 한번에 수확 활성화 여부
            LabManager lab = LabManager.Instance;
            bool groupHarvest = sel != null && sel.harvestGroupID > 0
                                && lab != null && lab.IsGroupActive(sel.harvestGroupID);

            int totalGold = farmGrid.HarvestAll();
            if (totalGold > 0)
                Debug.Log($"[CultivationUIManager] 전체 수확 완료. 획득 골드: {totalGold}G");
        }
    }
}

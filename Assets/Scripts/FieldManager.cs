using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

namespace WeedHoldings
{
    public class FieldManager : MonoBehaviour
    {
        public static FieldManager Instance { get; private set; }

        /// <summary>기본으로 무료 제공되는 밭 칸(3x3, 좌상단). 재배 시스템 기획서: "기본적으로 제공되는 3x3의 밭".</summary>
        public const int FreeBlockSize = 3;
        public const int FreeFieldCount = FreeBlockSize * FreeBlockSize;

        [Header("밭 설정")]
        public List<FieldPlot> plots = new List<FieldPlot>();

        [Header("UI 설정")]
        public TMP_FontAsset timeFont;
        public Color timeTextColor = Color.white;
        public int timeFontSize = 16;

        // 재배 화면 기획(재배 화면.png): 6열 x 4행 = 24칸이 스크롤 없이 패널 안에 전부 들어와야 한다.
        // Field_Panel 실제 크기(≈997.7 x 656.4)에 맞춰 칸 크기/간격을 역산했다.
        public const int GridColumns = 6;
        const int GridRows = 4;
        static readonly Vector2 CellSize = new Vector2(150f, 145f);
        static readonly Vector2 CellSpacing = new Vector2(12f, 12f);

        // 밭 칸 구매 가격 공식: 1200G + 이미 구매한 칸 수 x 2000G (전체 경제 재조정에 맞춰 기존 대비 4배)
        const int FieldUnlockBaseCost = 1200;
        const int FieldUnlockCostPerPurchase = 2000;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializePlots();
        }

        // 성장/고사 타이머는 CultivationManager가 전담한다 (중복 틱으로 인한 성장 속도 오차 방지).

        void InitializePlots()
        {
            plots.Clear();

            // 씬에 수동 배치돼 있던 24개 밭 버튼을 기존 순서(6열 그리드 순서와 동일) 그대로 두고,
            // GridLayoutGroup으로 패널 크기에 맞는 6x4 격자에 다시 배치한다(스크롤 불필요, 패널 안에 꽉 참).
            var existingButtons = new List<Button>(GetComponentsInChildren<Button>(true));

            var grid = gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = CellSize;
            grid.spacing = CellSpacing;
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = GridColumns;

            for (int i = 0; i < existingButtons.Count; i++)
            {
                FieldPlot plot = existingButtons[i].GetComponent<FieldPlot>();
                if (plot == null)
                    plot = existingButtons[i].gameObject.AddComponent<FieldPlot>();

                plot.Initialize(i);
                plots.Add(plot);
            }
        }

        public FieldPlot GetPlot(int index)
        {
            if (index >= 0 && index < plots.Count)
                return plots[index];
            return null;
        }

        /// <summary>다음 밭 칸 구매 가격. 이미 구매한(무료 9칸 제외) 칸 수에 비례해 오른다.</summary>
        public int GetFieldUnlockCost()
        {
            int purchasedCount = 0;
            foreach (var plot in plots)
            {
                if (plot != null && plot.IsPurchased) purchasedCount++;
            }
            return FieldUnlockBaseCost + FieldUnlockCostPerPurchase * purchasedCount;
        }

        /// <summary>
        /// 연구소 재배 시설 업그레이드 레벨이 정하는 "총 보유 가능 밭 칸 수" 상한과 인접 여부를 기준으로
        /// 각 밭 칸의 구매 가능 여부를 다시 계산한다. 고정된 순서가 아니라 이미 해금된 칸과 붙어 있는 칸이면
        /// 유저가 원하는 걸 골라 살 수 있다(기획서: "원하는 밭 칸의 해금"). 레벨업 시, 개별 밭 구매 직후
        /// (이웃 칸이 새로 구매 가능해질 수 있으므로) 호출된다.
        /// </summary>
        public void RefreshUnlockStates()
        {
            int ceiling = LabUpgradeManager.Instance != null
                ? LabUpgradeManager.Instance.GetUnlockedFieldCount()
                : FreeFieldCount;

            int unlockedCount = 0;
            foreach (var plot in plots)
            {
                if (plot != null && plot.IsUnlocked) unlockedCount++;
            }
            bool roomAvailable = unlockedCount < ceiling;

            for (int i = 0; i < plots.Count; i++)
            {
                var plot = plots[i];
                if (plot == null) continue;

                bool availableToPurchase = roomAvailable && HasUnlockedNeighbor(i);
                plot.ApplyUnlockState(availableToPurchase);
            }
        }

        /// <summary>6열 격자 기준으로 상하좌우 이웃 중 이미 해금된(무료 또는 구매됨) 칸이 있는지 확인한다.</summary>
        bool HasUnlockedNeighbor(int index)
        {
            int row = index / GridColumns;
            int col = index % GridColumns;

            return IsNeighborUnlocked(row - 1, col) || IsNeighborUnlocked(row + 1, col) ||
                   IsNeighborUnlocked(row, col - 1) || IsNeighborUnlocked(row, col + 1);
        }

        bool IsNeighborUnlocked(int row, int col)
        {
            if (row < 0 || col < 0 || col >= GridColumns) return false;
            var plot = GetPlot(row * GridColumns + col);
            return plot != null && plot.IsUnlocked;
        }

        public void ResetAllPlots()
        {
            foreach (var plot in plots)
            {
                if (plot != null)
                {
                    plot.ResetPlot();
                }
            }
        }
    }
}
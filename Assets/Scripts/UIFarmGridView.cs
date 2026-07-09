using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WeedHoldings
{
    /// <summary>
    /// 밭 그리드 전체 UI(Canvas 기반).
    /// FarmGridManager의 FarmPlot들을 UIFarmPlotCell로 시각화합니다.
    /// </summary>
    public class UIFarmGridView : MonoBehaviour
    {
        [Header("셀 프리팹 및 컨테이너")]
        public GameObject plotCellPrefab;
        public Transform  gridContainer;   // GridLayoutGroup이 붙은 Transform

        private List<UIFarmPlotCell> cells = new List<UIFarmPlotCell>();

        void Start()
        {
            BuildGrid();
        }

        public void BuildGrid()
        {
            foreach (Transform child in gridContainer)
                Destroy(child.gameObject);
            cells.Clear();

            FarmGridManager fgm = FarmGridManager.Instance;
            if (fgm == null || plotCellPrefab == null) return;

            foreach (FarmPlot fp in fgm.farmPlots)
            {
                GameObject go   = Instantiate(plotCellPrefab, gridContainer);
                UIFarmPlotCell cell = go.GetComponent<UIFarmPlotCell>();
                if (cell != null)
                    cell.Setup(fp);
                cells.Add(cell);
            }
        }

        public void RefreshAll()
        {
            foreach (var cell in cells)
                cell?.Refresh();
        }

        public void RefreshCell(int plotIndex)
        {
            if (plotIndex >= 0 && plotIndex < cells.Count)
                cells[plotIndex]?.Refresh();
        }
    }
}

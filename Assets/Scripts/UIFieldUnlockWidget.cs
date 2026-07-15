using UnityEngine;

namespace WeedHoldings
{
    /// <summary>
    /// FarmPanel에 부착. 연구소 재배 시설 업그레이드 레벨이 바뀔 때마다
    /// FieldManager.RefreshUnlockStates()를 다시 호출해 밭 칸 구매 가능 상태를 갱신한다.
    /// (개별 밭 칸을 실제로 사는 로직/팝업은 FieldPlot과 UIFieldPurchasePopup이 담당)
    /// </summary>
    public class UIFieldUnlockWidget : MonoBehaviour
    {
        void OnEnable()
        {
            if (LabUpgradeManager.Instance != null)
                LabUpgradeManager.Instance.OnLevelChanged += RefreshFieldUnlockStates;
            RefreshFieldUnlockStates();
        }

        void OnDisable()
        {
            if (LabUpgradeManager.Instance != null)
                LabUpgradeManager.Instance.OnLevelChanged -= RefreshFieldUnlockStates;
        }

        void RefreshFieldUnlockStates()
        {
            FieldManager.Instance?.RefreshUnlockStates();
        }
    }
}

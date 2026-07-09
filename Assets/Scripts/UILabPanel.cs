using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WeedHoldings
{
    /// <summary>
    /// 연구실(업그레이드) UI 패널.
    /// </summary>
    public class UILabPanel : MonoBehaviour
    {
        [Header("현재 상태 표시")]
        public TextMeshProUGUI levelText;       // "연구소 Lv. 5 / 20"
        public TextMeshProUGUI bonusText;       // "성장 속도 보너스 +25%"
        public TextMeshProUGUI effectText;      // "실제 성장 시간: 기본 × 0.80"

        [Header("업그레이드 버튼")]
        public Button          upgradeButton;
        public TextMeshProUGUI upgradeCostText; // "업그레이드: 2,700G"
        public TextMeshProUGUI upgradeInfoText; // "다음 레벨 효과"

        [Header("해금 그룹 정보")]
        public TextMeshProUGUI groupInfoText;   // 활성화된 그룹/밭 정보

        [Header("식물 잠금 해제 안내")]
        public TextMeshProUGUI plantUnlockText;

        [Header("식물 해금 리스트")]
        public Transform       plantUnlockContainer;
        public GameObject      plantUnlockSlotPrefab;

        void Start()
        {
            if (upgradeButton != null)
                upgradeButton.onClick.AddListener(OnUpgradeClicked);

            if (LabManager.Instance != null)
                LabManager.Instance.OnLevelChanged += _ => Refresh();

            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnGoldChanged += _ => Refresh();

            Refresh();
        }

        void OnDestroy()
        {
            if (LabManager.Instance != null)
                LabManager.Instance.OnLevelChanged -= _ => Refresh();
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnGoldChanged -= _ => Refresh();
        }

        public void Refresh()
        {
            LabManager lab = LabManager.Instance;
            if (lab == null) return;

            int   lv    = lab.CurrentLevel;
            float bonus = lab.GrowBonusPercent;

            // 레벨 표시
            if (levelText != null)
                levelText.text = $"연구소 Lv. {lv} / {LabManager.MAX_LEVEL}";

            // 보너스 표시
            if (bonusText != null)
                bonusText.text = $"성장 속도 보너스 +{bonus:F0}%";

            // 실제 성장 배율
            if (effectText != null)
            {
                float mult = (100f + bonus) / 100f;
                effectText.text = $"성장 {mult:F2}× 빠름";
            }

            // 업그레이드 버튼
            if (lab.IsMaxLevel)
            {
                if (upgradeButton != null) upgradeButton.interactable = false;
                if (upgradeCostText != null) upgradeCostText.text = "최대 레벨";
                if (upgradeInfoText != null) upgradeInfoText.text = "";
            }
            else
            {
                var next = lab.GetNextUpgradeData();
                int cost = lab.GetNextUpgradeCost();
                bool canAfford = CurrencyManager.Instance?.CanAfford(cost) ?? false;

                if (upgradeButton != null) upgradeButton.interactable = canAfford;
                if (upgradeCostText != null) upgradeCostText.text = $"업그레이드 {cost:N0}G";

                if (upgradeInfoText != null && next != null)
                {
                    string info = $"Lv.{next.upgradeLevel} → 보너스 +{next.growBonusPercent:F0}%";
                    if (next.harvestGroupID > 0)
                        info += $"\n한번에 심기/수확 그룹 {next.harvestGroupID} 활성화";
                    if (next.unlockFieldRow > 0)
                        info += $"\n밭 {next.unlockFieldRow + 1}행 해금";
                    upgradeInfoText.text = info;
                }
            }

            // 그룹 정보
            if (groupInfoText != null)
            {
                string g = "";
                if (lab.IsGroupActive(1001)) g += "· 그룹1 (대박초/몽롱귀비/환각초) 활성\n";
                if (lab.IsGroupActive(1002)) g += "· 그룹2 (설강/마글달/환각타이나/하니봉) 활성\n";
                if (lab.IsGroupActive(1003)) g += "· 그룹3 (엔/솔깔란마/태풀) 활성\n";
                if (string.IsNullOrEmpty(g)) g = "한번에 심기/수확 비활성 (Lv.5 이상)";
                groupInfoText.text = g.TrimEnd();
            }

            // 식물 해금 안내
            if (plantUnlockText != null)
            {
                var db = PlantDatabase.Instance;
                if (db != null)
                {
                    int unlocked = db.GetUnlockedPlants().Count;
                    int total    = db.allPlants.Count;
                    plantUnlockText.text = $"해금 식물: {unlocked} / {total}";
                }
            }

            // 식물 해금 리스트 갱신
            RefreshPlantUnlockList();
        }

        public void RefreshPlantUnlockList()
        {
            if (plantUnlockContainer == null) return;

            // 기존 슬롯 삭제
            for (int i = plantUnlockContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(plantUnlockContainer.GetChild(i).gameObject);
            }

            var db = PlantDatabase.Instance;
            if (db == null) return;

            LabManager lab = LabManager.Instance;
            int labLevel = lab != null ? lab.CurrentLevel : 0;

            foreach (PlantData pd in db.allPlants)
            {
                bool isUnlocked = db.IsPlantUnlocked(pd.plantID);
                bool canAfford = CurrencyManager.Instance?.CanAfford(pd.requiredGoldToUnlock) ?? false;

                if (plantUnlockSlotPrefab != null)
                {
                    GameObject go = Instantiate(plantUnlockSlotPrefab, plantUnlockContainer);
                    var slot = go.GetComponent<UILabPlantSlot>();
                    if (slot != null)
                    {
                        slot.Setup(pd, isUnlocked, labLevel, canAfford);
                        slot.OnUnlockClicked += HandlePlantUnlock;
                    }
                }
            }
        }

        void HandlePlantUnlock(PlantData pd)
        {
            if (pd == null) return;
            var db = PlantDatabase.Instance;
            if (db == null || db.IsPlantUnlocked(pd.plantID)) return;

            LabManager lab = LabManager.Instance;
            if (lab == null || !lab.IsPlantUnlockable(pd.requiredHarvestLevel))
            {
                Debug.Log($"[UILabPanel] {pd.plantName} 해금 불가: 연구소 레벨 부족");
                return;
            }

            if (CurrencyManager.Instance != null && CurrencyManager.Instance.SpendGold(pd.requiredGoldToUnlock))
            {
                db.UnlockPlant(pd.plantID);
                Debug.Log($"[UILabPanel] {pd.plantName} 해금 완료!");
                Refresh();
                
                // 재배 패널의 식물 리스트도 함께 갱신
                FindFirstObjectByType<UIPlantSelectPanel>()?.RefreshPlantList();
            }
        }

        void OnUpgradeClicked()
        {
            if (LabManager.Instance != null)
            {
                bool ok = LabManager.Instance.TryUpgrade();
                if (!ok)
                    Debug.Log("[UILabPanel] 업그레이드 실패 (골드 부족 또는 최대 레벨)");
            }
        }
    }
}

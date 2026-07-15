using System;
using UnityEngine;

namespace WeedHoldings
{
    /// <summary>
    /// 연구소 재배 시설 업그레이드 레벨 관리 (ResearchMgr)
    /// - 업그레이드 시 식물 성장 속도 테이블(LabUpgradeData)에 맞춰 CultivationManager의 전역 성장 속도를 갱신
    /// - 식물 언락 리스트의 레벨 게이팅 기준(CurrentLevel)을 제공
    /// </summary>
    public class LabUpgradeManager : MonoBehaviour
    {
        public static LabUpgradeManager Instance { get; private set; }

        public int CurrentLevel { get; private set; } = 1;

        public event Action OnLevelChanged;

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
            ReapplyGrowthBonus();
        }

        public LabUpgradeData GetCurrentUpgradeData()
        {
            return DataManager.Instance != null ? DataManager.Instance.GetUpgradeByLevel(CurrentLevel) : null;
        }

        public LabUpgradeData GetNextUpgradeData()
        {
            return DataManager.Instance != null ? DataManager.Instance.GetUpgradeByLevel(CurrentLevel + 1) : null;
        }

        public bool CanUpgrade()
        {
            var next = GetNextUpgradeData();
            if (next == null) return false;
            return GoldManager.Instance != null && GoldManager.Instance.gold >= next.requiredGold;
        }

        public bool TryUpgrade()
        {
            if (!CanUpgrade()) return false;

            var next = GetNextUpgradeData();
            if (!GoldManager.Instance.SpendGold(next.requiredGold)) return false;

            CurrentLevel = next.upgradeLevel;
            ReapplyGrowthBonus();
            OnLevelChanged?.Invoke();
            Debug.Log($"[LabUpgradeManager] 재배 시설 업그레이드! Lv.{CurrentLevel} (성장 보너스 {next.growBonusPercent}%)");
            return true;
        }

        /// <summary>
        /// 현재 레벨의 성장 보너스를 CultivationManager에 다시 반영한다.
        /// 매니저들의 초기화 순서와 무관하게 안전하도록 외부(UI)에서도 재호출할 수 있게 public으로 노출.
        /// </summary>
        public void ReapplyGrowthBonus()
        {
            if (CultivationManager.Instance == null) return;

            var data = GetCurrentUpgradeData();
            int bonus = data != null ? data.growBonusPercent : 0;
            CultivationManager.Instance.SetGlobalGrowthSpeed((100f + bonus) / 100f);
        }

        /// <summary>
        /// 현재 레벨에서 운용 가능한 밭 칸 수(테이블의 Unlock_Field). 레벨이 오를 때마다 누적되는 값이 아니라
        /// 테이블에 그 레벨 기준으로 이미 "현재까지 총 몇 칸"인지 적혀 있으므로 현재 레벨 값을 그대로 쓴다.
        /// </summary>
        public int GetUnlockedFieldCount()
        {
            var data = GetCurrentUpgradeData();
            return data != null && data.unlockFieldRow > 0 ? data.unlockFieldRow : 9;
        }

        /// <summary>
        /// 한번에 심기/한번에 수확 그룹(harvestGroupID)이 현재 레벨까지의 업그레이드 테이블에서
        /// 등장한 적이 있으면 해금된 것으로 본다. (재배테이블: 그룹 1001은 Lv.5+, 1002는 Lv.10+, 1003은 Lv.15+)
        /// </summary>
        public bool IsHarvestGroupUnlocked(int harvestGroupId)
        {
            if (harvestGroupId <= 0 || DataManager.Instance == null) return false;

            for (int level = 1; level <= CurrentLevel; level++)
            {
                var data = DataManager.Instance.GetUpgradeByLevel(level);
                if (data != null && data.harvestGroupID == harvestGroupId)
                    return true;
            }
            return false;
        }
    }
}

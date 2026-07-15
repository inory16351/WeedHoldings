using System;
using UnityEngine;

namespace WeedHoldings
{
    /// <summary>
    /// 공장(제조) 시설 업그레이드 레벨 관리. LabUpgradeManager(재배 시설)와 동일한 역할을
    /// 제조 트랙/실패확률/제조시간 쪽에 대해 수행한다.
    /// 업그레이드를 실제로 구매하는 UI(공장 시설 업그레이드 패널)는 아직 씬에 없어서
    /// 지금은 기본 레벨(1)로 고정되어 있다. Farm_Upgrade_Panel과 동일한 패턴으로
    /// 나중에 UI가 만들어지면 TryUpgrade()를 그대로 연결하면 된다.
    /// </summary>
    public class FactoryUpgradeManager : MonoBehaviour
    {
        public static FactoryUpgradeManager Instance { get; private set; }

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

        public FactoryUpgradeData GetCurrentUpgradeData()
        {
            return DataManager.Instance != null ? DataManager.Instance.GetFactoryUpgradeByLevel(CurrentLevel) : null;
        }

        public FactoryUpgradeData GetNextUpgradeData()
        {
            return DataManager.Instance != null ? DataManager.Instance.GetFactoryUpgradeByLevel(CurrentLevel + 1) : null;
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
            OnLevelChanged?.Invoke();
            Debug.Log($"[FactoryUpgradeManager] 제조 시설 업그레이드! Lv.{CurrentLevel} ({next.requiredGold:N0}G 소모)");
            return true;
        }

        /// <summary>현재 레벨까지 등장한 적 있는 트랙 번호는 전부 해금 상태로 본다. 트랙 1(인덱스 0)은 항상 기본 해금.</summary>
        public bool IsTrackUnlocked(int trackIndex)
        {
            if (trackIndex <= 0) return true; // 첫 트랙은 기본 제공

            if (DataManager.Instance == null) return false;
            for (int level = 1; level <= CurrentLevel; level++)
            {
                var data = DataManager.Instance.GetFactoryUpgradeByLevel(level);
                if (data != null && data.unlockTrack == trackIndex)
                    return true;
            }
            return false;
        }

        /// <summary>현재 레벨의 실패확률 보정치 누적 반영. (표 기준 "이 레벨의" 값이며 누적합이 아님)</summary>
        public float GetFailProbBonus()
        {
            var data = GetCurrentUpgradeData();
            return data != null ? data.failProbBonus : 0f;
        }

        public float GetPotionTimeMultiplier()
        {
            var data = GetCurrentUpgradeData();
            int bonus = data != null ? (int)data.potionTimeBonus : 0;
            return Mathf.Max(0.01f, (100f + bonus) / 100f);
        }
    }
}

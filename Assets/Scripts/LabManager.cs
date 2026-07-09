using System;
using System.Collections.Generic;
using UnityEngine;

namespace WeedHoldings
{
    /// <summary>
    /// 연구소(시설 업그레이드) 매니저.
    /// 레벨 관리, 성장 보너스 계산, 밭 행 해금, 그룹 활성화를 담당합니다.
    /// </summary>
    public class LabManager : MonoBehaviour
    {
        public static LabManager Instance { get; private set; }

        public const int MAX_LEVEL = 20;

        [Header("업그레이드 테이블 (20레벨 분량)")]
        public List<LabUpgradeData> upgradeTable = new List<LabUpgradeData>();

        [Header("현재 레벨")]
        [SerializeField] private int currentLevel = 0;  // 0 = 업그레이드 없음

        public int CurrentLevel => currentLevel;
        public float GrowBonusPercent => GetBonusAt(currentLevel);
        public bool IsMaxLevel => currentLevel >= MAX_LEVEL;

        // 현재 레벨에서 해금된 그룹 ID 세트
        private HashSet<int> activeGroupIDs = new HashSet<int>();
        // 현재 레벨에서 해금된 추가 밭 행 번호
        private HashSet<int> unlockedFieldRows = new HashSet<int>();

        public event Action<int> OnLevelChanged;
        public event Action<int> OnFieldRowUnlocked;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadUpgradeTableFromResources();
            LoadLevel();
        }

        void LoadUpgradeTableFromResources()
        {
            var loaded = Resources.LoadAll<LabUpgradeData>("LabUpgrades");
            if (loaded != null && loaded.Length > 0)
            {
                upgradeTable.Clear();
                upgradeTable.AddRange(loaded);
                upgradeTable.Sort((a, b) => a.upgradeLevel.CompareTo(b.upgradeLevel));
                Debug.Log($"[LabManager] Loaded {upgradeTable.Count} upgrades from Resources/LabUpgrades");
            }
        }

        void LoadLevel()
        {
            currentLevel = PlayerPrefs.GetInt(GameConstants.LAB_LEVEL_SAVE_KEY, 0);
            RebuildActiveState();
        }

        void SaveLevel()
        {
            PlayerPrefs.SetInt(GameConstants.LAB_LEVEL_SAVE_KEY, currentLevel);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 현재 레벨까지 누적하여 활성화된 그룹/밭 행을 재계산합니다.
        /// </summary>
        void RebuildActiveState()
        {
            activeGroupIDs.Clear();
            unlockedFieldRows.Clear();

            for (int lv = 1; lv <= currentLevel; lv++)
            {
                var data = GetDataAtLevel(lv);
                if (data == null) continue;
                if (data.harvestGroupID > 0)
                    activeGroupIDs.Add(data.harvestGroupID);
                if (data.unlockFieldRow > 0)
                    unlockedFieldRows.Add(data.unlockFieldRow);
            }
        }

        LabUpgradeData GetDataAtLevel(int level)
        {
            foreach (var d in upgradeTable)
                if (d.upgradeLevel == level) return d;
            return null;
        }

        float GetBonusAt(int level)
        {
            if (level <= 0) return 0f;
            var data = GetDataAtLevel(level);
            return data != null ? data.growBonusPercent : level * 5f;
        }

        public LabUpgradeData GetNextUpgradeData()
        {
            if (IsMaxLevel) return null;
            return GetDataAtLevel(currentLevel + 1);
        }

        public int GetNextUpgradeCost()
        {
            var data = GetNextUpgradeData();
            if (data != null) return data.requiredGold;
            // fallback: 200 + level * 500
            return 200 + (currentLevel + 1) * 500;
        }

        /// <summary>
        /// 업그레이드 시도. 골드가 부족하면 false 반환.
        /// </summary>
        public bool TryUpgrade()
        {
            if (IsMaxLevel) return false;
            int cost = GetNextUpgradeCost();
            if (!CurrencyManager.Instance.SpendGold(cost)) return false;

            currentLevel++;
            RebuildActiveState();
            SaveLevel();

            var data = GetDataAtLevel(currentLevel);
            if (data != null && data.unlockFieldRow > 0)
                OnFieldRowUnlocked?.Invoke(data.unlockFieldRow);

            OnLevelChanged?.Invoke(currentLevel);
            return true;
        }

        /// <summary>
        /// 해당 그룹 ID가 현재 레벨에서 활성화되었는지 확인
        /// </summary>
        public bool IsGroupActive(int groupID) => activeGroupIDs.Contains(groupID);

        /// <summary>
        /// 식물이 현재 레벨에서 해금 가능한지 확인
        /// </summary>
        public bool IsPlantUnlockable(int requiredLevel) => currentLevel >= requiredLevel;

        /// <summary>
        /// 성장 속도 배수 계산: 기본 성장 시간 / ((100 + 보너스%) / 100)
        /// </summary>
        public float GetEffectiveGrowTime(float baseGrowTime)
        {
            float bonus = GrowBonusPercent;
            return baseGrowTime / ((100f + bonus) / 100f);
        }

        public bool IsFieldRowUnlocked(int row) => row < GameConstants.DEFAULT_UNLOCKED_ROWS;

        /// <summary>
        /// 현재 레벨까지 업그레이드하면서 해금할 수 있게 된 추가 밭 칸의 총 개수를 반환합니다.
        /// </summary>
        public int GetMaxUnlockablePlots()
        {
            int total = 0;
            for (int lv = 1; lv <= currentLevel; lv++)
            {
                var d = GetDataAtLevel(lv);
                if (d != null && d.unlockFieldRow > 0)
                {
                    total += d.unlockFieldRow;
                }
            }
            return total;
        }
    }
}

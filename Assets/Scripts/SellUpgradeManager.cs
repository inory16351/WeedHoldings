using System;
using System.Collections.Generic;
using UnityEngine;

namespace WeedHoldings
{
    /// <summary>
    /// 무역소(판매) 시설 업그레이드 레벨 관리. LabUpgradeManager/FactoryUpgradeManager와 동일한 역할을
    /// 무역소의 지역 해금/적재량/선박 트랙/전역 판매가 보너스에 대해 수행한다.
    /// </summary>
    public class SellUpgradeManager : MonoBehaviour
    {
        public static SellUpgradeManager Instance { get; private set; }

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

        public SellUpgradeData GetCurrentUpgradeData()
        {
            return DataManager.Instance != null ? DataManager.Instance.GetSellUpgradeByLevel(CurrentLevel) : null;
        }

        public SellUpgradeData GetNextUpgradeData()
        {
            return DataManager.Instance != null ? DataManager.Instance.GetSellUpgradeByLevel(CurrentLevel + 1) : null;
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
            Debug.Log($"[SellUpgradeManager] 무역소 시설 업그레이드! Lv.{CurrentLevel} ({next.requiredGold:N0}G 소모)");
            return true;
        }

        /// <summary>현재 레벨까지 등장한 적 있는 지역(Region_ID)은 전부 해금 상태로 본다.</summary>
        public bool IsRegionUnlocked(int regionId)
        {
            if (DataManager.Instance == null) return false;
            for (int level = 1; level <= CurrentLevel; level++)
            {
                var data = DataManager.Instance.GetSellUpgradeByLevel(level);
                if (data != null && data.unlockRegionID == regionId)
                    return true;
            }
            return false;
        }

        /// <summary>현재 레벨까지 등장한 적 있는 선박 트랙 번호는 전부 해금 상태로 본다. 트랙 1(인덱스 0)은 항상 기본 해금.</summary>
        public bool IsShipTrackUnlocked(int trackIndex)
        {
            if (trackIndex <= 0) return true;

            if (DataManager.Instance == null) return false;
            for (int level = 1; level <= CurrentLevel; level++)
            {
                var data = DataManager.Instance.GetSellUpgradeByLevel(level);
                if (data != null && data.unlockShipTrack == trackIndex)
                    return true;
            }
            return false;
        }

        /// <summary>현재 레벨의 적재량(총 화물 개수 상한).</summary>
        public int GetCargoCapacity()
        {
            var data = GetCurrentUpgradeData();
            return data != null && data.cargoCapacity > 0 ? data.cargoCapacity : 3;
        }

        /// <summary>현재 레벨의 전역 판매가 보너스(%). 지역/식물 보너스에 가산된다.</summary>
        public float GetGoldBonusPercent()
        {
            var data = GetCurrentUpgradeData();
            return data != null ? data.goldBonusPercent : 0f;
        }

        /// <summary>
        /// 무역소에 장착된 캐릭터 보너스를 반영한 판매(항해) 속도 배율. 무역소 시설 업그레이드 표에는
        /// 아직 판매 속도 보너스 컬럼이 없어 시설 쪽 기여는 0으로 취급하고 캐릭터 보너스만 반영한다.
        /// cargoPlantIds는 이번 항해에 실은 보약들의 주 재료 식물 ID 목록(여러 종류 가능).
        /// </summary>
        public float GetSellSpeedMultiplier(ICollection<int> cargoPlantIds)
        {
            int bonus = 0;
            if (CharacterEquipManager.Instance != null)
                bonus += Mathf.RoundToInt(CharacterEquipManager.Instance.GetSpeedBonusPercent(EquipCategory.Sell, cargoPlantIds));

            return Mathf.Max(0.01f, (100f + bonus) / 100f);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace WeedHoldings
{
    public enum ShipState { Idle, Voyaging }

    /// <summary>
    /// 4개 선박 트랙(Ship_Track_01~04)의 상태를 관리한다. 화물 적재, 판매 시간 타이머,
    /// 완료 시 자동 골드 획득까지 담당하는 순수 게임 로직 매니저.
    /// (PotionCraftManager가 제조 트랙을 관리하는 것과 동일한 역할을 무역선에 대해 수행)
    /// 무역은 실패 확률이 없어 시간이 지나면 항상 성공한다.
    /// </summary>
    public class TradeManager : MonoBehaviour
    {
        public static TradeManager Instance { get; private set; }

        public class ShipSlot
        {
            public int trackIndex;
            public ShipState state = ShipState.Idle;
            public int regionId;
            public float totalTime;
            public float remainingTime;
            public int rewardGold;
        }

        public const int ShipCount = 4;
        ShipSlot[] ships;

        public event Action OnShipsChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            ships = new ShipSlot[ShipCount];
            for (int i = 0; i < ShipCount; i++)
                ships[i] = new ShipSlot { trackIndex = i };
        }

        void Update()
        {
            bool changed = false;
            foreach (var ship in ships)
            {
                if (ship.state != ShipState.Voyaging) continue;

                ship.remainingTime -= Time.deltaTime;
                if (ship.remainingTime <= 0f)
                {
                    ship.remainingTime = 0f;
                    GoldManager.Instance?.AddGold(ship.rewardGold);
                    Debug.Log($"[TradeManager] 선박{ship.trackIndex + 1} 무역 완료: {ship.rewardGold:N0}G 획득");
                    ClearShip(ship);
                }
                changed = true;
            }
            if (changed) OnShipsChanged?.Invoke();
        }

        void ClearShip(ShipSlot ship)
        {
            ship.state = ShipState.Idle;
            ship.regionId = 0;
            ship.totalTime = 0f;
            ship.remainingTime = 0f;
            ship.rewardGold = 0;
        }

        public ShipSlot GetShip(int index)
        {
            return (ships != null && index >= 0 && index < ships.Length) ? ships[index] : null;
        }

        /// <summary>선택한 지역/식물 보너스가 반영된 포션 1개당 예상 판매가.</summary>
        public float GetEffectiveSellPrice(int potionId, int regionId)
        {
            if (DataManager.Instance == null) return 0f;
            var potion = DataManager.Instance.GetPotionByID(potionId);
            if (potion == null) return 0f;

            int plantId = DataManager.Instance.GetPotionPrimaryMaterialPlantID(potionId);
            float plantBonus = DataManager.Instance.GetPlantBonusPercent(regionId, plantId);
            float goldBonus = SellUpgradeManager.Instance != null ? SellUpgradeManager.Instance.GetGoldBonusPercent() : 0f;

            return potion.sellGold * (1f + (plantBonus + goldBonus) / 100f);
        }

        /// <summary>화물 목록의 예상 총 수익(반올림).</summary>
        public int GetExpectedTotalGold(List<(int potionId, int amount)> cargo, int regionId)
        {
            float total = 0f;
            foreach (var (potionId, amount) in cargo)
                total += GetEffectiveSellPrice(potionId, regionId) * amount;
            return Mathf.RoundToInt(total);
        }

        /// <summary>선택한 트랙에 화물을 싣고 출항시킨다. 재고를 즉시 차감하고 골드는 도착 시 지급한다.</summary>
        public bool StartVoyage(int shipIndex, int regionId, List<(int potionId, int amount)> cargo)
        {
            var ship = GetShip(shipIndex);
            if (ship == null || ship.state != ShipState.Idle) return false;
            if (cargo == null || cargo.Count == 0) return false;
            if (DataManager.Instance == null) return false;

            var region = DataManager.Instance.GetRegionByID(regionId);
            if (region == null) return false;

            foreach (var (potionId, amount) in cargo)
            {
                if (amount <= 0 || DataManager.Instance.GetPotionCount(potionId) < amount)
                    return false;
            }

            int totalGold = GetExpectedTotalGold(cargo, regionId);
            foreach (var (potionId, amount) in cargo)
                DataManager.Instance.RemovePotionFromInventory(potionId, amount);

            ship.state = ShipState.Voyaging;
            ship.regionId = regionId;
            ship.totalTime = region.sellTimeSeconds;
            ship.remainingTime = region.sellTimeSeconds;
            ship.rewardGold = totalGold;

            Debug.Log($"[TradeManager] 선박{shipIndex + 1} {region.regionName}(으)로 출항 ({region.sellTimeSeconds:F0}초, 예상 {totalGold:N0}G)");
            OnShipsChanged?.Invoke();
            return true;
        }
    }
}

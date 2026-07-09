using System;
using UnityEngine;

namespace WeedHoldings
{
    public enum GrowthPhase
    {
        Empty,
        Planted,
        Baby,
        Growing,
        Mature,
        Withered
    }

    public class PlantGrowthManager : MonoBehaviour
    {
        public GrowthPhase currentPhase = GrowthPhase.Empty;

        public PlantData currentPlantData;
        public float timeSinceLastWater = 0f;

        public float plantTime;
        public float totalGrowDuration;   // LabManager 보너스 적용 후 실제 성장 시간
        public bool isPaused;

        public event Action<GrowthPhase> OnPhaseChanged;
        public event Action<float>       OnGrowthProgressUpdated;
        public event Action              OnPlantReadyToHarvest;
        public event Action              OnPlantWithered;
        public event Action              OnPlantDied;  // 고사 유예 후 완전 소멸

        public bool IsWarningActive => currentPlantData != null && timeSinceLastWater >= currentPlantData.witherTimeSeconds;

        void Awake() { }

        void Update()
        {
            if (currentPhase == GrowthPhase.Empty)
                return;

            if (isPaused)
                return;

            float delta = Time.deltaTime;

            // 수확 가능 상태는 고사하지 않음
            if (currentPhase == GrowthPhase.Mature)
            {
                timeSinceLastWater = 0f;
                return;
            }

            // 고사 타이머 업데이트
            timeSinceLastWater += delta;
            float witherLimit = currentPlantData != null ? currentPlantData.witherTimeSeconds : 60f;

            if (timeSinceLastWater >= witherLimit)
            {
                // 고사 경고 상태 돌입
                if (currentPhase != GrowthPhase.Withered)
                {
                    currentPhase = GrowthPhase.Withered;
                    OnPhaseChanged?.Invoke(GrowthPhase.Withered);
                    OnPlantWithered?.Invoke();
                }

                // 10초 유예 경과 시 사망
                if (timeSinceLastWater >= witherLimit + GameConstants.WITHER_GRACE_SECONDS)
                {
                    ClearPlot();
                    OnPlantDied?.Invoke();
                    return;
                }
            }

            // 성장 진행
            plantTime += delta;
            float progress = Mathf.Clamp01(plantTime / totalGrowDuration);
            OnGrowthProgressUpdated?.Invoke(progress);

            UpdateGrowthPhase(progress);

            if (progress >= 1f && currentPhase != GrowthPhase.Mature && currentPhase != GrowthPhase.Withered)
            {
                currentPhase = GrowthPhase.Mature;
                timeSinceLastWater = 0f;
                OnPhaseChanged?.Invoke(GrowthPhase.Mature);
                OnPlantReadyToHarvest?.Invoke();
            }
        }

        void UpdateGrowthPhase(float progress)
        {
            if (currentPlantData == null || currentPhase == GrowthPhase.Withered) return;

            GrowthPhase expected;
            if (progress < currentPlantData.babyPhaseRatio)
                expected = GrowthPhase.Baby;
            else if (progress < currentPlantData.babyPhaseRatio + currentPlantData.growingPhaseRatio)
                expected = GrowthPhase.Growing;
            else
                expected = GrowthPhase.Mature;

            if (expected != currentPhase && currentPhase != GrowthPhase.Empty)
            {
                currentPhase = expected;
                OnPhaseChanged?.Invoke(currentPhase);
            }
        }

        /// <summary>씨앗 심기. LabManager 보너스를 반영한 실제 성장 시간을 계산합니다.</summary>
        public void PlantSeed(PlantData plantData)
        {
            if (currentPhase != GrowthPhase.Empty) return;

            currentPlantData = plantData;

            // 연구소 보너스 적용
            float baseGrow = plantData.growTimeSeconds;
            totalGrowDuration = LabManager.Instance != null
                ? LabManager.Instance.GetEffectiveGrowTime(baseGrow)
                : baseGrow;

            plantTime = 0f;
            timeSinceLastWater = 0f;
            isPaused = false;
            currentPhase = GrowthPhase.Planted;
            OnPhaseChanged?.Invoke(GrowthPhase.Planted);
        }

        public void WaterPlant()
        {
            if (currentPhase == GrowthPhase.Empty)
                return;
            if (currentPhase == GrowthPhase.Mature) return;

            timeSinceLastWater = 0f;
            if (currentPhase == GrowthPhase.Withered)
            {
                currentPhase = GrowthPhase.Growing;
                OnPhaseChanged?.Invoke(GrowthPhase.Growing);
            }
        }

        public bool CanHarvest() => currentPhase == GrowthPhase.Mature;

        public int Harvest()
        {
            if (!CanHarvest()) return 0;
            int amount = UnityEngine.Random.Range(
                currentPlantData.harvestMinCount,
                currentPlantData.harvestMaxCount + 1);
            ClearPlot();
            return amount;
        }

        public void ClearPlot()
        {
            currentPlantData = null;
            plantTime = 0f;
            timeSinceLastWater = 0f;
            currentPhase = GrowthPhase.Empty;
            OnPhaseChanged?.Invoke(GrowthPhase.Empty);
        }

        public void SetPaused(bool paused) => isPaused = paused;

        public float GetGrowthProgress()
        {
            if (totalGrowDuration <= 0f || currentPlantData == null) return 0f;
            return Mathf.Clamp01(plantTime / totalGrowDuration);
        }

        public float GetRemainingTime()
        {
            if (totalGrowDuration <= 0f || currentPlantData == null) return 0f;
            return Mathf.Max(0f, totalGrowDuration - plantTime);
        }

        /// <summary>고사까지 남은 시간 (비율 0~1)</summary>
        public float GetWitherProgress()
        {
            if (currentPlantData == null) return 0f;
            float witherLimit = currentPlantData.witherTimeSeconds;
            if (currentPhase == GrowthPhase.Withered)
                return Mathf.Clamp01((timeSinceLastWater - witherLimit) / GameConstants.WITHER_GRACE_SECONDS);
            return Mathf.Clamp01(timeSinceLastWater / witherLimit);
        }

        // 물준 직후 5초간 물방울 아이콘 유지
        public bool IsWatered() => timeSinceLastWater < 5f;
    }
}

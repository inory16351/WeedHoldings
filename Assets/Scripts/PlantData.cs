using System;
using UnityEngine;

namespace WeedHoldings
{
    [Serializable]
    [CreateAssetMenu(fileName = "NewPlant", menuName = "WeedHoldings/식물 데이터")]
    public class PlantData : ScriptableObject
    {
        [Header("식물 기본 정보")]
        public int plantID;
        public string plantName;
        [TextArea(2, 4)]
        public string description;

        [Header("재배 정보")]
        public float growTimeSeconds = 60f;
        public int requiredHarvestLevel = 1;
        public int requiredGoldToUnlock = 100;
        public float witherTimeSeconds = 30f;

        [Header("수확 정보")]
        public int harvestMinCount = 1;
        public int harvestMaxCount = 3;
        public int sellGoldValue = 10;
        public int harvestGroupID = 1001;

        [Header("이미지 리소스 (추후 적용)")]
        public Sprite iconSprite;
        public Sprite babySprite;
        public Sprite fullGrowSprite;

        [Header("성장 단계별 시간 비율")]
        public float babyPhaseRatio = 0.3f;
        public float growingPhaseRatio = 0.4f;
        public float maturePhaseRatio = 0.3f;

        public float BabyPhaseDuration => growTimeSeconds * babyPhaseRatio;
        public float GrowingPhaseDuration => growTimeSeconds * growingPhaseRatio;
        public float MaturePhaseDuration => growTimeSeconds * maturePhaseRatio;
    }
}

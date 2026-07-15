using System;
using UnityEngine;

namespace WeedHoldings
{
    /// <summary>
    /// 제조테이블.xlsx의 "보약" 시트 한 행. 에셋 파일이 아니라 DataManager가 엑셀에서 직접 읽어
    /// 런타임에 채우는 순수 데이터 클래스다 (PlantData/LabUpgradeData처럼 별도 .asset을 만들지 않음).
    /// </summary>
    [Serializable]
    public class PotionData
    {
        public int potionID;
        public string potionName;
        public float potionTimeSeconds;
        public float failProbBase;
        public int reqPlantGroup;
        public int requiredUnlockLevel;
        public int requiredUnlockGold;
        public int sellGold;
        public Sprite potionIcon;
    }
}

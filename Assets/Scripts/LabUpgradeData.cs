using System;
using UnityEngine;

namespace WeedHoldings
{
    [Serializable]
    [CreateAssetMenu(fileName = "NewLabUpgrade", menuName = "WeedHoldings/시설 업그레이드 데이터")]
    public class LabUpgradeData : ScriptableObject
    {
        [Header("업그레이드 정보")]
        public int upgradeID;
        public int upgradeLevel;
        public int growBonusPercent;
        public int requiredGold;
        public int unlockFieldRow;
        public int harvestGroupID;
    }
}

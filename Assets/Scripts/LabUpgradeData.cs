using System;
using UnityEngine;

namespace WeedHoldings
{
    /// <summary>
    /// 연구소 업그레이드 1레벨 분량의 데이터
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "LabUpgrade", menuName = "WeedHoldings/연구소 업그레이드 데이터")]
    public class LabUpgradeData : ScriptableObject
    {
        [Header("업그레이드 기본 정보")]
        public int upgradeID;           // 20001~20020
        public int upgradeLevel;        // 1~20
        public float growBonusPercent;  // 성장 속도 보너스 %
        public int requiredGold;        // 업그레이드 비용

        [Header("밭 확장 (0 = 없음)")]
        public int unlockFieldRow;      // 해금되는 밭 행 번호 (0 = 없음)

        [Header("한번에 심기/수확 그룹 (0 = 없음)")]
        public int harvestGroupID;      // 활성화되는 그룹 ID (0 = 없음)
    }
}

using System;

namespace WeedHoldings
{
    /// <summary>
    /// 제조테이블.xlsx의 "제조 시설 업그레이드" 시트 한 행.
    /// </summary>
    [Serializable]
    public class FactoryUpgradeData
    {
        public int upgradeLevel;
        public float failProbBonus;    // 누적값이 아니라 "이 레벨의" 보정치 (LabUpgradeData와 동일 규칙)
        public float potionTimeBonus;  // 제조 시간 단축 보너스(%)
        public int requiredGold;       // 이 레벨로 업그레이드하는 데 필요한 골드
        public int unlockTrack;        // 0이면 이 레벨에서 새로 해금되는 트랙 없음
    }
}

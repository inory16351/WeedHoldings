using System;

namespace WeedHoldings
{
    /// <summary>
    /// 무역소 테이블.xlsx의 "시설 업그레이드" 시트 한 행.
    /// </summary>
    [Serializable]
    public class SellUpgradeData
    {
        public int upgradeLevel;
        public float goldBonusPercent;  // 이 레벨의 전역 판매가 보너스(%). 누적값이 아니라 "이 레벨의" 값
        public int requiredGold;
        // 테이블 컬럼명은 "해금 지역 그룹아이디"이지만 실제 값은 "지역" 시트의 Region_ID를 직접 가리킨다.
        public int unlockRegionID;      // 이 레벨에서 새로 판매 가능해지는 지역. 0이면 해당 없음
        public int cargoCapacity;       // 이 레벨의 적재량(총 화물 개수 상한)
        public int unlockShipTrack;     // 이 레벨에서 새로 해금되는 선박 트랙 인덱스(1-based). 0이면 해당 없음
    }
}

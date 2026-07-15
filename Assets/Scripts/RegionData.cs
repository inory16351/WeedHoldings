using System;

namespace WeedHoldings
{
    /// <summary>
    /// 무역소 테이블.xlsx의 "지역" 시트 한 행. 포션 데이터처럼 .asset을 만들지 않고
    /// 실행할 때마다 DataManager가 엑셀에서 직접 읽어 채운다.
    /// </summary>
    [Serializable]
    public class RegionData
    {
        public int regionID;
        public string regionName;
        public float sellTimeSeconds;
        public int regionBonusGroupID;
    }
}

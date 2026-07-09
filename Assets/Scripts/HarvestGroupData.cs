using System.Collections.Generic;
using UnityEngine;

namespace WeedHoldings
{
    /// <summary>
    /// 한번에 심기/수확 그룹: 그룹 ID와 포함된 식물 ID 목록
    /// </summary>
    [CreateAssetMenu(fileName = "HarvestGroup", menuName = "WeedHoldings/한번에심기수확 그룹")]
    public class HarvestGroupData : ScriptableObject
    {
        public int groupID;
        public int requiredLabLevel;        // 이 그룹이 활성화되는 연구소 레벨
        public List<int> plantIDs = new List<int>();
    }
}

using System;
using UnityEngine;

namespace WeedHoldings
{
    /// <summary>
    /// 캐릭터테이블.xlsx의 "캐릭터테이블" 시트 한 행. PotionData/RegionData처럼 별도 .asset을 만들지 않고
    /// DataManager가 엑셀에서 직접 읽어 런타임에 채우는 순수 데이터 클래스다.
    /// </summary>
    [Serializable]
    public class CharacterData
    {
        public int characterID;
        public string characterName;
        public string grade; // R / SR / SSR
        public string explain;
        public string resourceName; // Resources/Characters 안의 스프라이트 파일명 (예: Character_01)
        public float drawChance; // 뽑기 가중치. 전체 캐릭터 합이 100이 되도록 설계됨

        public float farmEffectAllNum;
        public int farmEffectTarget;
        public int farmEffectNum;
        public float farmEffectChance;

        public float factoryEffectAllNum;
        public int factoryEffectTarget;
        public int factoryEffectNum;
        public float factoryEffectChance;

        public float sellEffectAllNum;
        public int sellEffectTarget;
        public int sellEffectNum;
        public float sellEffectChance;

        public Sprite characterIcon;
    }
}

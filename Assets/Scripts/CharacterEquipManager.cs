using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WeedHoldings
{
    public enum EquipCategory { Farm, Factory, Sell }

    /// <summary>
    /// 농장/공장/무역소 각 3칸(총 9칸)에 어떤 캐릭터가 장착돼 있는지 보관하고,
    /// 장착 보너스 설명 텍스트를 만들어준다. 캐릭터 카드 프레임(Resources/Cards/{grade}_card2,
    /// 투명 배경 크롭 버전 - 뽑기 결과 카드와는 다른 파일)도 여기서 캐싱해서 공용으로 로드한다.
    /// </summary>
    public class CharacterEquipManager : MonoBehaviour
    {
        public static CharacterEquipManager Instance { get; private set; }

        const int SlotsPerCategory = 3;

        readonly int[] farmSlots = new int[SlotsPerCategory];
        readonly int[] factorySlots = new int[SlotsPerCategory];
        readonly int[] sellSlots = new int[SlotsPerCategory];

        static readonly Dictionary<string, Sprite> frameCache = new Dictionary<string, Sprite>();

        void Awake()
        {
            Instance = this;
        }

        int[] GetSlots(EquipCategory category)
        {
            return category switch
            {
                EquipCategory.Farm => farmSlots,
                EquipCategory.Factory => factorySlots,
                _ => sellSlots,
            };
        }

        public int GetEquippedCharacterID(EquipCategory category, int slotIndex)
        {
            var slots = GetSlots(category);
            if (slotIndex < 0 || slotIndex >= slots.Length) return 0;
            return slots[slotIndex];
        }

        static readonly EquipCategory[] AllCategories = { EquipCategory.Farm, EquipCategory.Factory, EquipCategory.Sell };

        public void Equip(EquipCategory category, int slotIndex, int characterID)
        {
            var slots = GetSlots(category);
            if (slotIndex < 0 || slotIndex >= slots.Length) return;

            // 캐릭터 한 명은 항상 한 슬롯에만 장착 가능 - 다른 칸에 이미 있었다면 그 칸은 비운다.
            foreach (var cat in AllCategories)
            {
                var otherSlots = GetSlots(cat);
                for (int i = 0; i < otherSlots.Length; i++)
                {
                    if (cat == category && i == slotIndex) continue;
                    if (otherSlots[i] == characterID) otherSlots[i] = 0;
                }
            }

            slots[slotIndex] = characterID;
        }

        /// <summary>주어진 슬롯을 제외하고, 이 캐릭터가 다른 어딘가에 이미 장착돼 있는지 확인한다.</summary>
        public bool IsCharacterEquippedElsewhere(int characterID, EquipCategory excludeCategory, int excludeSlotIndex)
        {
            if (characterID == 0) return false;

            foreach (var cat in AllCategories)
            {
                var slots = GetSlots(cat);
                for (int i = 0; i < slots.Length; i++)
                {
                    if (cat == excludeCategory && i == excludeSlotIndex) continue;
                    if (slots[i] == characterID) return true;
                }
            }
            return false;
        }

        /// <summary>카드 프레임(장착 팝업/인벤토리 슬롯 배경용, 투명 배경 크롭된 버전)을 등급별로 캐싱해 반환한다.</summary>
        public static Sprite GetFrame(string grade)
        {
            if (string.IsNullOrEmpty(grade)) return null;

            string key = grade.ToLowerInvariant();
            if (frameCache.TryGetValue(key, out var cached)) return cached;

            var sprite = Resources.Load<Sprite>($"Cards/{key}_card2");
            frameCache[key] = sprite;
            return sprite;
        }

        static readonly Dictionary<string, Sprite> badgeCache = new Dictionary<string, Sprite>();

        /// <summary>
        /// 등급 배지 이미지(Rating (1)/Rating 자리용). 아직 전용 이미지 에셋이 없어서 항상 null을 반환하지만,
        /// Resources/Badges/{grade} 로 에셋이 추가되면 코드 변경 없이 바로 붙도록 조회 로직만 미리 만들어둔다.
        /// </summary>
        public static Sprite GetGradeBadge(string grade)
        {
            if (string.IsNullOrEmpty(grade)) return null;

            string key = grade.ToLowerInvariant();
            if (badgeCache.TryGetValue(key, out var cached)) return cached;

            var sprite = Resources.Load<Sprite>($"Badges/{key}");
            badgeCache[key] = sprite;
            return sprite;
        }

        static Sprite emptySlotIconCache;

        /// <summary>빈 슬롯 배경 아이콘(Resources/Cards/slot, 투명 배경 크롭본).</summary>
        public static Sprite GetEmptySlotIcon()
        {
            if (emptySlotIconCache == null)
                emptySlotIconCache = Resources.Load<Sprite>("Cards/slot");
            return emptySlotIconCache;
        }

        /// <summary>
        /// "SSR\n모든 식물 성장 속도  10%증가\n[대상] 성장 속도  25%증가" 형태의 장착 보너스 설명을 만든다.
        /// All_Num은 해당 분야 전체(농장/공장/무역소)에 적용되는 보너스, Target/Chance는 특정 대상
        /// (식물/보약/지역)에 적용되는 보너스다.
        /// </summary>
        public static string BuildEffectDescription(CharacterData character, EquipCategory category)
        {
            if (character == null) return "";

            GetEffectFields(character, category, out float allNum, out int targetId, out float targetNum);

            string allLabel = category switch
            {
                EquipCategory.Farm => "모든 식물 성장 속도",
                EquipCategory.Factory => "모든 보약 제조 속도",
                _ => "모든 화물 판매 속도",
            };
            string targetActionLabel = category switch
            {
                EquipCategory.Farm => "성장 속도",
                EquipCategory.Factory => "제조 속도",
                _ => "판매 속도",
            };

            string targetName = ResolveTargetName(targetId);
            return $"{allLabel}  {allNum:0.#}%증가\n{targetName} {targetActionLabel}  {targetNum:0.#}%증가";
        }

        /// <summary>
        /// 캐릭터테이블의 분야별 효과 컬럼(All_Num/Target/Chance)을 꺼내온다. Chance 컬럼은 이름과 달리
        /// 실제로는 확률이 아니라 Target 식물 하나에만 추가로 붙는 속도 보너스(%)로 쓰인다.
        /// </summary>
        static void GetEffectFields(CharacterData character, EquipCategory category, out float allNum, out int targetId, out float targetNum)
        {
            switch (category)
            {
                case EquipCategory.Farm:
                    allNum = character.farmEffectAllNum;
                    targetId = character.farmEffectTarget;
                    targetNum = character.farmEffectChance;
                    break;
                case EquipCategory.Factory:
                    allNum = character.factoryEffectAllNum;
                    targetId = character.factoryEffectTarget;
                    targetNum = character.factoryEffectChance;
                    break;
                default:
                    allNum = character.sellEffectAllNum;
                    targetId = character.sellEffectTarget;
                    targetNum = character.sellEffectChance;
                    break;
            }
        }

        /// <summary>
        /// 해당 분야(농장/공장/무역소)에 장착된 캐릭터 최대 3명의 속도 보너스 합계(%). 전체효과(allNum)는
        /// 장착된 캐릭터마다 항상 더해지고, 대상 한정 보너스(targetNum)는 그 캐릭터의 대상 식물 ID가
        /// targetPlantId와 일치할 때만 추가로 더해진다. 연구소 업그레이드 보너스와 합산해서 쓰라고
        /// 퍼센트 값 자체를 반환한다(예: 20 = 20%).
        /// </summary>
        public float GetSpeedBonusPercent(EquipCategory category, int targetPlantId)
        {
            return SumSpeedBonus(category, targetId => targetId == targetPlantId);
        }

        /// <summary>
        /// 무역소처럼 한 번에 여러 보약(서로 다른 주 재료 식물)을 함께 실어 나르는 경우용 오버로드.
        /// 대상 한정 보너스는 캐릭터의 대상 식물이 실린 화물 중 하나와 일치하기만 해도 더해진다.
        /// </summary>
        public float GetSpeedBonusPercent(EquipCategory category, ICollection<int> targetPlantIds)
        {
            return SumSpeedBonus(category, targetId => targetPlantIds != null && targetPlantIds.Contains(targetId));
        }

        float SumSpeedBonus(EquipCategory category, System.Func<int, bool> targetMatches)
        {
            if (DataManager.Instance == null) return 0f;

            float total = 0f;
            foreach (var characterId in GetSlots(category))
            {
                if (characterId == 0) continue;
                var character = DataManager.Instance.GetCharacterByID(characterId);
                if (character == null) continue;

                GetEffectFields(character, category, out float allNum, out int targetId, out float targetNum);
                total += allNum;
                if (targetMatches(targetId)) total += targetNum;
            }
            return total;
        }

        /// <summary>
        /// 캐릭터테이블의 Farm/Factory/Sell_Effect_Target 컬럼은 세 분야 전부 항상 "식물 ID"를
        /// 담고 있다(포션 ID나 지역 ID가 아니다 - 어느 식물 관련 활동을 밀어주는 캐릭터인지가 기준).
        /// 이전에 분야별로 GetPotionByID/GetRegionByID를 섞어 쓰면서 항상 조회에 실패해
        /// "#10001" 같은 숫자 ID가 그대로 노출되던 버그가 있었다.
        /// </summary>
        static string ResolveTargetName(int targetId)
        {
            if (DataManager.Instance == null) return $"#{targetId}";
            return DataManager.Instance.GetPlantByID(targetId)?.plantName ?? $"#{targetId}";
        }
    }
}

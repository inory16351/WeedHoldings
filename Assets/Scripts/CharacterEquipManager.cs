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

        public void Equip(EquipCategory category, int slotIndex, int characterID)
        {
            var slots = GetSlots(category);
            if (slotIndex < 0 || slotIndex >= slots.Length) return;
            slots[slotIndex] = characterID;
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

            string allLabel;
            string targetActionLabel;
            float allNum;
            int targetId;
            float targetNum;

            switch (category)
            {
                case EquipCategory.Farm:
                    allLabel = "모든 식물 성장 속도";
                    targetActionLabel = "성장 속도";
                    allNum = character.farmEffectAllNum;
                    targetId = character.farmEffectTarget;
                    targetNum = character.farmEffectChance;
                    break;
                case EquipCategory.Factory:
                    allLabel = "모든 보약 제조 속도";
                    targetActionLabel = "제조 속도";
                    allNum = character.factoryEffectAllNum;
                    targetId = character.factoryEffectTarget;
                    targetNum = character.factoryEffectChance;
                    break;
                default:
                    allLabel = "모든 화물 판매 속도";
                    targetActionLabel = "판매 속도";
                    allNum = character.sellEffectAllNum;
                    targetId = character.sellEffectTarget;
                    targetNum = character.sellEffectChance;
                    break;
            }

            string targetName = ResolveTargetName(category, targetId);
            return $"{allLabel}  {allNum:0.#}%증가\n{targetName} {targetActionLabel}  {targetNum:0.#}%증가";
        }

        static string ResolveTargetName(EquipCategory category, int targetId)
        {
            if (DataManager.Instance == null) return $"#{targetId}";

            return category switch
            {
                EquipCategory.Farm => DataManager.Instance.GetPlantByID(targetId)?.plantName ?? $"#{targetId}",
                EquipCategory.Factory => DataManager.Instance.GetPotionByID(targetId)?.potionName ?? $"#{targetId}",
                _ => DataManager.Instance.GetRegionByID(targetId)?.regionName ?? $"#{targetId}",
            };
        }
    }
}

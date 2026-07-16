using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WeedHoldings
{
    public class GachaManager : MonoBehaviour
    {
        public static GachaManager Instance { get; private set; }

        public struct GachaResult
        {
            public CharacterData character;
            public bool isDuplicate;
        }

        static readonly Dictionary<string, Sprite> shipSpriteCache = new Dictionary<string, Sprite>();

        void Awake()
        {
            Instance = this;
        }

        public static int GradeRank(string grade)
        {
            return grade switch
            {
                "SSR" => 3,
                "SR" => 2,
                "R" => 1,
                _ => 0,
            };
        }

        /// <summary>결과 중 가장 높은 등급의 배 이미지(Resources/Cards/{grade}_ship, 투명 배경 크롭본)를 반환한다.</summary>
        public static Sprite GetShipSpriteForBestGrade(List<GachaResult> results)
        {
            if (results == null || results.Count == 0) return null;

            string bestGrade = results
                .Select(r => r.character?.grade)
                .Where(g => !string.IsNullOrEmpty(g))
                .OrderByDescending(GradeRank)
                .FirstOrDefault();

            return GetShipSprite(bestGrade);
        }

        static Sprite GetShipSprite(string grade)
        {
            if (string.IsNullOrEmpty(grade)) return null;

            string key = grade.ToLowerInvariant();
            if (shipSpriteCache.TryGetValue(key, out var cached)) return cached;

            var sprite = Resources.Load<Sprite>($"Cards/{key}_ship");
            shipSpriteCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// count번 뽑기를 한 번에 확정한다. 같은 배치 안에서 먼저 뽑힌 캐릭터를 뒤 슬롯이 다시 뽑으면
        /// 그 슬롯도 중복으로 처리된다(이미 이번 결과로 보유하게 되었으므로).
        /// </summary>
        public List<GachaResult> RollMultiple(int count)
        {
            var results = new List<GachaResult>();
            if (DataManager.Instance == null) return results;

            var pool = DataManager.Instance.GetAllCharacters();
            if (pool.Count == 0) return results;

            for (int i = 0; i < count; i++)
            {
                var picked = WeightedPick(pool);
                bool isDuplicate = DataManager.Instance.IsCharacterOwned(picked.characterID);
                if (!isDuplicate)
                    DataManager.Instance.AddCharacterToInventory(picked.characterID);

                results.Add(new GachaResult { character = picked, isDuplicate = isDuplicate });
            }

            return results;
        }

        CharacterData WeightedPick(List<CharacterData> pool)
        {
            float total = pool.Sum(c => Mathf.Max(0f, c.drawChance));
            if (total <= 0f) return pool[Random.Range(0, pool.Count)];

            float roll = Random.value * total;
            float cumulative = 0f;
            foreach (var c in pool)
            {
                cumulative += Mathf.Max(0f, c.drawChance);
                if (roll <= cumulative) return c;
            }
            return pool[pool.Count - 1];
        }
    }
}

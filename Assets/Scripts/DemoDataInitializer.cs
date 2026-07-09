using System.Collections.Generic;
using UnityEngine;

namespace WeedHoldings
{
    public class DemoDataInitializer : MonoBehaviour
    {
        void Start()
        {
            PlantDatabase db = PlantDatabase.Instance;
            if (db == null)
            {
                Debug.LogError("[DemoDataInitializer] PlantDatabase.Instance is null!");
                return;
            }

            // Resources/Plants/ 에 에셋이 있으면 우선 사용
            PlantData[] loaded = Resources.LoadAll<PlantData>("Plants");
            if (loaded != null && loaded.Length > 0)
            {
                db.allPlants.Clear();
                db.allPlants.AddRange(loaded);
                db.InitializeDatabase();
                Debug.Log($"[DemoDataInitializer] Resources/Plants/ 에서 {loaded.Length}개 로드");
                InitLabFallback();
            }
            else
            {
                InitializePlants(db);
                InitLabFallback();
            }

            // UI 갱신 (Awake/Start 순서 보장)
            FindFirstObjectByType<UIPlantSelectPanel>()?.RefreshPlantList();
            FindFirstObjectByType<UILabPanel>()?.Refresh();
        }

        // ────────────────────────────────────────────────
        // 식물 데이터 (엑셀: 슬기로운재배생활_재배테이블.xlsx 기준)
        //  ID | 이름       | 성장(초) | 필요레벨 | 해금골드 | 고사(초) | 그룹ID
        // ────────────────────────────────────────────────
        void InitializePlants(PlantDatabase db)
        {
            if (db.allPlants.Count > 0)
            {
                Debug.Log("[DemoDataInitializer] PlantDatabase에 이미 데이터가 있습니다. 건너뜀.");
                return;
            }

            db.allPlants.Clear();

            // sellGoldValue: 해금골드 / 10
            db.allPlants.Add(Make(10001, "몽실낮잠꽃",     30f,  1,  100,  15f, 1001, 1, 3, 10));
            db.allPlants.Add(Make(10002, "찌릿클로버",     45f,  3,  150,  22.5f, 1001, 1, 3, 15));
            db.allPlants.Add(Make(10003, "말랑환상버섯",   60f,  7,  200,  30f, 1001, 1, 3, 20));
            db.allPlants.Add(Make(10004, "헤롱헤롱꽃",     90f,  9,  350,  45f, 1002, 1, 2, 35));
            db.allPlants.Add(Make(10005, "아찔핑글덩굴",  120f, 11,  500,  60f, 1002, 1, 2, 50));
            db.allPlants.Add(Make(10006, "몽환비틀이끼",  150f, 12,  700,  75f, 1002, 1, 2, 70));
            db.allPlants.Add(Make(10007, "불끈으라차뿌리", 180f, 15,  900,  90f, 1002, 1, 2, 90));
            db.allPlants.Add(Make(10008, "가물가물방울꽃", 240f, 17, 1200, 120f, 1003, 1, 1, 120));
            db.allPlants.Add(Make(10009, "찌르르가시풀",   300f, 19, 2000, 150f, 1003, 1, 1, 200));
            db.allPlants.Add(Make(10010, "멍하니방울꽃",   360f, 20, 3000, 180f, 1003, 1, 1, 300));

            db.InitializeDatabase();
            Debug.Log("[DemoDataInitializer] 식물 데이터 10종 초기화 완료 (엑셀 기준)");
        }

        PlantData Make(int id, string pName, float growTime, int reqLevel,
                       int reqGold, float witherTime, int groupID,
                       int minH, int maxH, int sellVal)
        {
            PlantData p = ScriptableObject.CreateInstance<PlantData>();
            p.plantID              = id;
            p.plantName            = pName;
            p.description          = $"{pName}. 밭에 심어 재배할 수 있는 원료 식물입니다.";
            p.growTimeSeconds      = growTime;
            p.requiredHarvestLevel = reqLevel;
            p.requiredGoldToUnlock = reqGold;
            p.witherTimeSeconds    = witherTime;
            p.harvestGroupID       = groupID;
            p.harvestMinCount      = minH;
            p.harvestMaxCount      = maxH;
            p.sellGoldValue        = sellVal;
            p.babyPhaseRatio       = 0.3f;
            p.growingPhaseRatio    = 0.4f;
            p.maturePhaseRatio     = 0.3f;
            p.name                 = $"Plant_{id}_{pName}";
            return p;
        }

        // ────────────────────────────────────────────────
        // 연구소 업그레이드 데이터 (엑셀 Sheet2 기준)
        // ────────────────────────────────────────────────
        void InitLabFallback()
        {
            LabManager lab = LabManager.Instance;
            if (lab == null) return;
            if (lab.upgradeTable.Count > 0) return;

            // (ID, level, bonus%, cost, unlockRow, groupID)
            var table = new (int id, int lv, float bonus, int cost, int row, int grp)[]
            {
                (20001,  1,  5, 200,  0,    0),
                (20002,  2, 10, 700,  0,    0),
                (20003,  3, 15,1200,  0,    0),
                (20004,  4, 20,1700,  0,    0),
                (20005,  5, 25,2200,  3, 1001),  // 행 해금 + 그룹1001 활성
                (20006,  6, 30,2700,  0, 1001),
                (20007,  7, 35,3200,  0, 1001),
                (20008,  8, 40,3700,  0, 1001),
                (20009,  9, 45,4200,  0, 1001),
                (20010, 10, 50,4700,  4, 1002),  // 행 해금 + 그룹1002 활성
                (20011, 11, 55,5200,  0, 1002),
                (20012, 12, 60,5700,  0, 1002),
                (20013, 13, 65,6200,  0, 1002),
                (20014, 14, 70,6700,  0, 1002),
                (20015, 15, 75,7200,  0, 1003),  // 그룹1003 활성
                (20016, 16, 80,7700,  0, 1003),
                (20017, 17, 85,8200,  0, 1003),
                (20018, 18, 90,8700,  0, 1003),
                (20019, 19, 95,9200,  0, 1003),
                (20020, 20,100,9700,  0,    0),
            };

            foreach (var t in table)
            {
                var d = ScriptableObject.CreateInstance<LabUpgradeData>();
                d.upgradeID         = t.id;
                d.upgradeLevel      = t.lv;
                d.growBonusPercent  = t.bonus;
                d.requiredGold      = t.cost;
                d.unlockFieldRow    = t.row;
                d.harvestGroupID    = t.grp;
                d.name              = $"LabUpgrade_{t.lv:D2}";
                lab.upgradeTable.Add(d);
            }
            Debug.Log("[DemoDataInitializer] 연구소 업그레이드 테이블 20단계 초기화 완료");
        }
    }
}

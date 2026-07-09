using UnityEngine;

namespace WeedHoldings
{
    public static class GameConstants
    {
        // 밭 기본 설정 (가로 3열, 세로 최대 8행)
        public const int FARM_GRID_COLS = 3;
        public const int FARM_GRID_ROWS = 8;
        public const int MAX_FARM_PLOTS = FARM_GRID_COLS * FARM_GRID_ROWS;

        // 기본 해금 행 수 (첫 3행 × 3열 = 9칸 기본 해금)
        public const int DEFAULT_UNLOCKED_ROWS = 3;

        // 물 주기
        public const float WATER_BOOST_MULTIPLIER = 2.0f;
        public const float WATER_BOOST_DURATION = 10f;

        // 고사 후 완전 소멸까지 유예 시간 (초)
        public const float WITHER_GRACE_SECONDS = 10f;

        // 재화
        public const string GOLD_SAVE_KEY = "PlayerGold";
        public const int STARTING_GOLD = 99999;

        // 연구소 레벨 저장 키
        public const string LAB_LEVEL_SAVE_KEY = "LabLevel";

        // 밭 해금 기본 가격 (칸당)
        public const int PLOT_UNLOCK_BASE_PRICE = 300;

        // 색상
        public static readonly Color32 CULTIVATION_GREEN  = new Color32(127, 174,  74, 255);
        public static readonly Color32 CULTIVATION_BEIGE  = new Color32(217, 194, 139, 255);
        public static readonly Color32 WATER_BLUE         = new Color32(100, 180, 255, 255);
        public static readonly Color32 LOCKED_GRAY        = new Color32(150, 150, 150, 255);
        public static readonly Color32 WITHER_RED         = new Color32(220,  60,  60, 255);
        public static readonly Color32 HARVEST_GOLD       = new Color32(255, 200,  50, 255);

        // UI 레이아웃
        public static readonly Vector2Int GRID_ORIGIN = new Vector2Int(0, 0);
    }
}

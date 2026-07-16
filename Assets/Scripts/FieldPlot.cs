using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace WeedHoldings
{
    /// <summary>
    /// 성장 상태 (성장 과정)
    /// </summary>
    public enum GrowthState
    {
        Empty,
        Planted,
        Growing,
        ReadyToHarvest
    }

    /// <summary>
    /// 시들음 상태 (고사 관리) - 별도 관리
    /// </summary>
    public enum WitherState
    {
        Normal,
        Withering,
        Withered
    }

    public class FieldPlot : MonoBehaviour
    {
        [Header("플롯 정보")]
        public int PlotIndex => plotIndex;
        public GrowthState GrowthState => growthState;
        public WitherState WitherState => witherState;
        public bool IsGrowing => isGrowing;
        public float GrowTimeRemaining => growTimeRemaining;
        public float WitherTimeRemaining => witherTimeRemaining;

        [Header("데이터")]
        [SerializeField] private int plotIndex = 0;
        public GrowthState growthState = GrowthState.Empty;
        public WitherState witherState = WitherState.Normal;
        public PlantData plantedPlant { get; private set; }

        // 밭 구간 해금: 좌상단 3x3(9칸)은 항상 무료로 해금되어 있다.
        // 그 이후 칸은 재배 시설 레벨(FieldManager.RefreshUnlockStates가 계산하는 상한선) 안에서
        // 이미 해금된 칸과 인접해야만 골드를 내고 구매할 수 있다.
        private bool isPurchased = false;
        private bool isAvailableToPurchase = false;
        public bool IsPurchased => isPurchased;
        public bool IsAvailableToPurchase => isAvailableToPurchase;

        // 격자가 6열이라 plotIndex만으로는 3x3 좌상단 블록을 판별할 수 없어 row/col로 계산한다.
        int Row => plotIndex / FieldManager.GridColumns;
        int Col => plotIndex % FieldManager.GridColumns;
        public bool IsUnlocked => (Row < FieldManager.FreeBlockSize && Col < FieldManager.FreeBlockSize) || isPurchased;

        [Header("UI 참조")]
        private Button button;
        private Image bgImage;
        private Image plantImage;
        private TMP_Text timeText;

        [Header("시간 텍스트 폰트 설정")]
        public TMP_FontAsset timeTextFont;

        private float growTimeRemaining = 0f;
        private float witherTimeRemaining = 0f;
        private float witheredGraceTimeRemaining = 0f;
        private bool isGrowing = false;
        private const float WITHERED_GRACE_TIME = 10f;

        public System.Action<FieldPlot, GrowthState> OnGrowthStateChanged;
        public System.Action<FieldPlot, WitherState> OnWitherStateChanged;

        Color growingColor = new Color(1f, 1f, 1f, 1f);

        private Coroutine blinkCoroutine;
        private Coroutine witheringBlinkCoroutine;

        void Awake()
        {
            button = GetComponent<Button>();
            bgImage = GetComponent<Image>();

            plantImage = FindChildImage("Plant_Image") ?? FindChildImage("PlantImage");

            timeText = FindChildText("Grow_Time") ?? FindChildText("TimeText");

            if (bgImage != null) bgImage.raycastTarget = true;
            if (button != null) button.image = bgImage;

            var buttonText = button?.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
                buttonText.text = "";

            if (plantImage != null)
            {
                Color c = plantImage.color;
                c.a = 0f;
                plantImage.color = c;
                plantImage.enabled = true;
                plantImage.preserveAspect = true;
            }

            if (timeText != null)
            {
                timeText.text = "";
                timeText.enabled = false;
            }

            // 주의: 여기서는 CultivationManager에 등록하지 않는다.
            // plotIndex는 FieldManager.InitializePlots()가 이 직후에 호출하는 Initialize(i)에서
            // 확정되므로, 지금 시점(Awake)에 등록을 시도하면 아직 기본값(0)인 잘못된 인덱스로
            // 등록되어버린다. 도메인 리로드 없이 플레이 모드를 반복하는 등 CultivationManager.Instance가
            // 이 시점에 이미 존재하는 경우, 24개 밭이 전부 인덱스 0으로 등록을 시도하면서
            // 첫 번째(Field_00)만 성공하고 나머지는 영구히 등록에 실패하는 버그가 있었다.
            // 실제 등록은 plotIndex가 확정된 뒤인 Start()에서 수행한다.
        }

        void OnDestroy()
        {
            if (CultivationManager.Instance != null)
            {
                CultivationManager.Instance.UnregisterPlot(this);
            }
        }

        /// <summary>
        /// 자식 오브젝트에서 Image 컴포넌트 찾기
        /// </summary>
        Image FindChildImage(string childName)
        {
            var t = transform.Find(childName);
            return t != null ? t.GetComponent<Image>() : null;
        }

        /// <summary>
        /// 자식 오브젝트에서 TMP_Text 컴포넌트 찾기
        /// </summary>
        TMP_Text FindChildText(string childName)
        {
            var t = transform.Find(childName);
            return t != null ? t.GetComponent<TMP_Text>() : null;
        }

        void Start()
        {
            if (button != null)
            {
                button.onClick.AddListener(OnPlotClicked);
                button.transition = Selectable.Transition.ColorTint;
            }

            if (timeText != null)
            {
                if (timeTextFont != null)
                    timeText.font = timeTextFont;
                timeText.color = Color.white;
                timeText.alignment = TextAlignmentOptions.Center;
                timeText.enableAutoSizing = true;
                // 밭 칸 크기가 작아서(150x145) 기본 오토사이징 범위로는 텍스트가 밭 밖으로 넘칠 수 있어 상한선을 낮춘다.
                timeText.fontSizeMin = 7f;
                timeText.fontSizeMax = 14f;
                timeText.text = "";
                timeText.enabled = false;
            }

            UpdateVisual();

            // Awake() 시점엔 plotIndex가 아직 Initialize()로 확정되기 전이라 CultivationManager 등록을
            // 시도해도 잘못된(기본값 0) 인덱스로 등록될 위험이 있다. Start() 시점에는 FieldManager.Awake()가
            // 이미 끝나 Initialize(i)가 호출된 뒤이므로 plotIndex가 확정된 상태에서 다시 등록을 시도한다.
            // RegisterPlot은 중복 등록을 막아주므로 Awake()의 시도와 겹쳐도 안전하다.
            if (CultivationManager.Instance != null)
            {
                CultivationManager.Instance.RegisterPlot(this);
            }
        }

        public void SetupTimeText(TMP_FontAsset font, Color color, int fontSize)
        {
            if (timeText != null)
            {
                timeText.font = font;
                timeText.color = color;
                timeText.fontSize = fontSize;
                timeText.alignment = TextAlignmentOptions.Center;
                timeText.enableAutoSizing = true;
            }
        }

        public void Initialize(int index)
        {
            plotIndex = index;
            name = $"Field_{index:D2}";
        }

        /// <summary>FieldManager.RefreshUnlockStates()가 매번 호출해 구매 가능 여부를 다시 계산해준다.</summary>
        public void ApplyUnlockState(bool availableToPurchase)
        {
            isAvailableToPurchase = !IsUnlocked && availableToPurchase;
            if (button != null) button.interactable = IsUnlocked || isAvailableToPurchase;
            UpdateVisual();
        }

        /// <summary>구매 확인 팝업에서 '예'를 눌렀을 때 실제로 골드를 지불하고 밭을 해금한다.</summary>
        public void ConfirmPurchase()
        {
            if (IsUnlocked || FieldManager.Instance == null) return;

            int cost = FieldManager.Instance.GetFieldUnlockCost();
            if (GoldManager.Instance == null || !GoldManager.Instance.SpendGold(cost))
            {
                Debug.Log($"Field_{plotIndex} 밭 구매 실패: 골드 부족 ({cost}G 필요)");
                return;
            }

            isPurchased = true;
            Debug.Log($"Field_{plotIndex} 밭 칸 구매 완료! ({cost}G 소모)");

            // 이 칸이 새로 해금되면 그 이웃 칸들도 새로 구매 가능해질 수 있으므로 전체를 다시 계산한다.
            FieldManager.Instance.RefreshUnlockStates();
        }

        public void OnPlotClicked()
        {
            if (!IsUnlocked)
            {
                if (isAvailableToPurchase && FieldManager.Instance != null && UIFieldPurchasePopup.Instance != null)
                {
                    UIFieldPurchasePopup.Instance.Show(this, FieldManager.Instance.GetFieldUnlockCost());
                }
                return;
            }

            if (growthState == GrowthState.Empty &&
                PlantSelectionManager.Instance != null &&
                PlantSelectionManager.Instance.HasSelection)
            {
                PlantData selected = PlantSelectionManager.Instance.selectedPlant;
                PlantPlant(selected);
                return;
            }

            if (growthState == GrowthState.ReadyToHarvest && plantedPlant != null)
            {
                Harvest();
                return;
            }

            if (witherState == WitherState.Withered)
            {
                ClearWithered();
                return;
            }

            if (witherState == WitherState.Withering)
            {
                Water();
                return;
            }

            Debug.Log($"Field_{plotIndex} clicked | Growth: {growthState}, Wither: {witherState}");
        }

        public void PlantPlant(PlantData plant)
        {
            plantedPlant = plant;
            growthState = GrowthState.Planted;
            witherState = WitherState.Normal;

            // 연구소 성장 속도 보너스는 심는 순간 성장시간에 직접 반영해서 확정한다.
            // (매 프레임 배율을 곱하는 방식 대신, 여기서 한 번만 나눠서 growTimeRemaining 자체를 줄인다.
            //  고사 시간은 기획서상 성장 속도 보너스의 영향을 받지 않으므로 원본 값을 그대로 쓴다.)
            float speedMultiplier = CultivationManager.Instance != null
                ? CultivationManager.Instance.GetCurrentGrowthSpeedMultiplier(plant.plantID)
                : 1f;
            growTimeRemaining = plant.growTimeSeconds / speedMultiplier;
            witherTimeRemaining = plant.witherTimeSeconds;
            isGrowing = true;

            UpdateVisual();
            OnGrowthStateChanged?.Invoke(this, growthState);
            OnWitherStateChanged?.Invoke(this, witherState);
            Debug.Log($"Field_{plotIndex} planted with {plant.plantName} " +
                $"(Grow: {growTimeRemaining:F1}s [base {plant.growTimeSeconds}s / {speedMultiplier:F2}x], Wither: {witherTimeRemaining}s)");
        }

        public void Harvest()
        {
            if (plantedPlant == null) return;

            // 식물 객체 하나를 수확하면 재료도 정확히 1개만 수급된다.
            int count = 1;
            if (DataManager.Instance != null)
            {
                DataManager.Instance.AddPlantToInventory(plantedPlant.plantID, count);
                var panel = Object.FindFirstObjectByType<UIAvPlantListPanel>();
                if (panel != null) panel.RefreshAll();
            }
            Debug.Log($"Field_{plotIndex} harvested! Got {count}x {plantedPlant.plantName}");

            plantedPlant = null;
            growthState = GrowthState.Empty;
            witherState = WitherState.Normal;
            growTimeRemaining = 0f;
            witherTimeRemaining = 0f;
            isGrowing = false;
            UpdateVisual();
            OnGrowthStateChanged?.Invoke(this, growthState);
            OnWitherStateChanged?.Invoke(this, witherState);
        }

        public void ClearWithered()
        {
            if (witherState != WitherState.Withered) return;

            plantedPlant = null;
            growthState = GrowthState.Empty;
            witherState = WitherState.Normal;
            growTimeRemaining = 0f;
            witherTimeRemaining = 0f;
            isGrowing = false;
            UpdateVisual();
            OnGrowthStateChanged?.Invoke(this, growthState);
            OnWitherStateChanged?.Invoke(this, witherState);
            Debug.Log($"Field_{plotIndex} withered plant cleared");
        }

        /// <summary>
        /// 고사 시 자동으로 밭 비우기 (식물 삭제 후 빈 밭 상태로)
        /// </summary>
        private void ClearWitheredAutomatic()
        {
            plantedPlant = null;
            growthState = GrowthState.Empty;
            witherState = WitherState.Normal;
            growTimeRemaining = 0f;
            witherTimeRemaining = 0f;
            isGrowing = false;
            UpdateVisual();
            OnGrowthStateChanged?.Invoke(this, growthState);
            OnWitherStateChanged?.Invoke(this, witherState);
            Debug.Log($"Field_{plotIndex} 자동 비워짐 (고사로 인한 삭제)");
        }

        public void ResetPlot()
        {
            plantedPlant = null;
            growthState = GrowthState.Empty;
            witherState = WitherState.Normal;
            growTimeRemaining = 0f;
            witherTimeRemaining = 0f;
            isGrowing = false;
            UpdateVisual();
            OnGrowthStateChanged?.Invoke(this, growthState);
            OnWitherStateChanged?.Invoke(this, witherState);
        }

        public void UpdateGrowthTime(float deltaTime)
        {
            if (!isGrowing || plantedPlant == null) return;

            growTimeRemaining -= deltaTime;
            witherTimeRemaining -= deltaTime;

            if (growTimeRemaining <= 0f)
            {
                growTimeRemaining = 0f;

                // 성장 시간이 다 지나도 지금 시들음(Withering) 상태라면 바로 수확 가능으로 바꾸지 않는다.
                // 물을 줘서 시들음이 해제되는 순간(Water() 참고) 그때 수확 가능 상태로 전환한다.
                // (시들음 유예 시간이 계속 흐르도록 isGrowing은 그대로 true로 둔 채 아래 switch로 넘어간다)
                if (witherState != WitherState.Withering)
                {
                    growthState = GrowthState.ReadyToHarvest;
                    witherState = WitherState.Normal;
                    isGrowing = false;
                    UpdateVisual();
                    UpdateTimeDisplay();
                    OnGrowthStateChanged?.Invoke(this, growthState);
                    OnWitherStateChanged?.Invoke(this, witherState);
                    Debug.Log($"Field_{plotIndex} ready to harvest!");
                    return;
                }
            }

            switch (witherState)
            {
                case WitherState.Normal:
                    if (witherTimeRemaining <= 0f)
                    {
                        witherState = WitherState.Withering;
                        witheredGraceTimeRemaining = WITHERED_GRACE_TIME;
                        Debug.Log($"Field_{plotIndex} withering started! Grace period: {WITHERED_GRACE_TIME}s");
                        OnWitherStateChanged?.Invoke(this, witherState);
                        UpdateVisual();
                    }
                    break;

                case WitherState.Withering:
                    witheredGraceTimeRemaining -= deltaTime;
                    if (witheredGraceTimeRemaining <= 0f)
                    {
                        witherState = WitherState.Withered;
                        isGrowing = false;
                        Debug.Log($"Field_{plotIndex} withered completely!");
                        OnWitherStateChanged?.Invoke(this, witherState);
                        UpdateVisual();
                        // 고사 시 자동으로 밭 비우기
                        ClearWitheredAutomatic();
                    }
                    break;

                case WitherState.Withered:
                    break;
            }

            if (isGrowing || growthState == GrowthState.ReadyToHarvest)
            {
                UpdateTimeDisplay();
            }
        }

        void UpdateTimeDisplay()
        {
            if (timeText == null) return;

            timeText.enabled = true;

            if (growthState == GrowthState.ReadyToHarvest)
            {
                timeText.text = "수확 가능!";
                timeText.color = Color.yellow;
                return;
            }

            if (witherState == WitherState.Withering)
            {
                int seconds = Mathf.FloorToInt(witheredGraceTimeRemaining);
                timeText.text = $"시들음: {seconds}s";
                timeText.color = Color.red;
            }
            else
            {
                int minutes = Mathf.FloorToInt(growTimeRemaining / 60f);
                int seconds = Mathf.FloorToInt(growTimeRemaining % 60f);
                timeText.text = $"{minutes:D2}:{seconds:D2}";
                timeText.color = Color.white;
            }
        }

        public void Water()
        {
            if (plantedPlant == null) return;

            // 시든 상태(Withering)에서 물주기 -> Normal 복구
            if (witherState == WitherState.Withering)
            {
                witherState = WitherState.Normal;

                if (witheringBlinkCoroutine != null)
                {
                    StopCoroutine(witheringBlinkCoroutine);
                    witheringBlinkCoroutine = null;
                }

                if (blinkCoroutine != null)
                {
                    StopCoroutine(blinkCoroutine);
                    blinkCoroutine = null;
                }

                if (bgImage != null) bgImage.color = growingColor;

                // 시들음 상태인 동안 성장 시간이 이미 다 지났었다면, 시들음이 해제되는
                // 지금 이 순간 수확 가능 상태로 전환한다.
                if (growTimeRemaining <= 0f)
                {
                    growthState = GrowthState.ReadyToHarvest;
                    isGrowing = false;
                    OnGrowthStateChanged?.Invoke(this, growthState);
                    Debug.Log($"Field_{plotIndex} 시들음 해제 - 수확 가능 상태로 전환");
                }
                else
                {
                    isGrowing = true;
                }
            }
            // 고사 상태(Withered)에서도 물주기로 부활 가능
            else if (witherState == WitherState.Withered)
            {
                witherState = WitherState.Normal;
                isGrowing = true;
                growthState = GrowthState.Growing;

                if (blinkCoroutine != null)
                {
                    StopCoroutine(blinkCoroutine);
                    blinkCoroutine = null;
                }

                if (bgImage != null) bgImage.color = growingColor;
            }
            else if (!isGrowing)
            {
                // 수확 가능 상태 등은 물주기 불가
                return;
            }

            // 두 타이머 모두 즉시 초기화
            witherTimeRemaining = plantedPlant.witherTimeSeconds;
            witheredGraceTimeRemaining = 0f;

            Debug.Log($"Field_{plotIndex} watered! Wither timer reset to {witherTimeRemaining}s");

            UpdateVisual();
            UpdateTimeDisplay();
            OnWitherStateChanged?.Invoke(this, witherState);
        }

        void UpdateVisual()
        {
            if (bgImage == null) return;

            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }
            if (witheringBlinkCoroutine != null)
            {
                StopCoroutine(witheringBlinkCoroutine);
                witheringBlinkCoroutine = null;
            }

            if (!IsUnlocked)
            {
                if (plantImage != null)
                {
                    Color c = plantImage.color;
                    c.a = 0f;
                    plantImage.color = c;
                }

                if (isAvailableToPurchase)
                {
                    // 구매 가능: 흐릿하지만 골드빛이 도는 톤으로 "살 수 있음"을 구분해서 알려준다.
                    bgImage.color = new Color(0.55f, 0.48f, 0.3f, 0.85f);
                    if (timeText != null)
                    {
                        timeText.enabled = true;
                        timeText.text = "구매 가능";
                        timeText.color = new Color(1f, 0.85f, 0.35f);
                    }
                }
                else
                {
                    // 아직 레벨/인접 조건이 안 된 밭: 흐리게 표시(명도만 낮춤, 완전히 검게 만들지 않는다).
                    bgImage.color = new Color(0.4f, 0.4f, 0.4f, 0.75f);
                    if (timeText != null)
                    {
                        timeText.enabled = true;
                        timeText.text = "잠김";
                        timeText.color = new Color(0.75f, 0.75f, 0.75f);
                    }
                }
                return;
            }

            UpdatePlantImage();
            UpdateTimeTextVisibility();

            switch (growthState)
            {
                case GrowthState.Empty:
                    bgImage.color = Color.white;
                    break;

                case GrowthState.Planted:
                case GrowthState.Growing:
                    bgImage.color = Color.white;
                    UpdatePlantImageForGrowth();
                    break;

                case GrowthState.ReadyToHarvest:
                    bgImage.color = Color.white;
                    UpdatePlantImageForHarvest();
                    break;
            }

            switch (witherState)
            {
                case WitherState.Normal:
                    break;

                case WitherState.Withering:
                    bgImage.color = new Color(1f, 0.5f, 0f, 0.85f);
                    // FarmPanel이 닫혀 있는 동안(GameObject 비활성)에도 CultivationManager는 백그라운드로
                    // 계속 시간을 흘려보내는데, 이때는 코루틴을 시작할 수 없다. 상태/타이머는 정상 갱신되고
                    // 점멸 애니메이션만 패널을 다시 열어 UpdateVisual()이 재호출될 때 시작된다.
                    if (gameObject.activeInHierarchy)
                        witheringBlinkCoroutine = StartCoroutine(BlinkWithering());
                    break;

                case WitherState.Withered:
                    bgImage.color = new Color(0.5f, 0.1f, 0.1f, 0.85f);
                    if (gameObject.activeInHierarchy)
                        blinkCoroutine = StartCoroutine(BlinkWithered());
                    break;
            }
        }

        /// <summary>
        /// Plant_Image 알파값 제어: Empty면 투명, 심겨있으면 불투명
        /// </summary>
        void UpdatePlantImage()
        {
            if (plantImage == null) return;

            bool hasPlant = (plantedPlant != null && growthState != GrowthState.Empty);
            Color c = plantImage.color;
            c.a = hasPlant ? 1f : 0f;
            plantImage.color = c;

            if (!hasPlant)
            {
                plantImage.sprite = null;
            }
        }

        /// <summary>
        /// TimeText 표시 제어
        /// </summary>
        void UpdateTimeTextVisibility()
        {
            if (timeText == null) return;
            bool shouldShow = isGrowing || growthState == GrowthState.ReadyToHarvest;
            timeText.enabled = shouldShow;
            if (!shouldShow) timeText.text = "";
        }

        /// <summary>
        /// 성장 중 이미지: Plant_Baby_00 (새싹) 표시
        /// </summary>
        void UpdatePlantImageForGrowth()
        {
            if (plantImage == null || plantedPlant == null) return;

            Sprite babySprite = Resources.Load<Sprite>("Plants/Plant_Baby_00");
            if (babySprite != null)
            {
                plantImage.sprite = babySprite;
            }
            else
            {
                string iconPath = $"Plants/Plant_Icon_{plantedPlant.plantID:D2}";
                Sprite iconSprite = Resources.Load<Sprite>(iconPath);
                if (iconSprite != null) plantImage.sprite = iconSprite;
            }
        }

        /// <summary>
        /// 수확 가능 이미지: Plant_Full_Grow_{plantIndex} 표시
        /// </summary>
        void UpdatePlantImageForHarvest()
        {
            if (plantImage == null || plantedPlant == null) return;

            int plantIndex = (plantedPlant.plantID - 10000);
            string fullGrowPath = $"Plants/Plant_Full_Grow_{plantIndex:D2}";
            Sprite fullGrownSprite = Resources.Load<Sprite>(fullGrowPath);
            if (fullGrownSprite != null)
            {
                plantImage.sprite = fullGrownSprite;
            }
            else
            {
                string iconPath = $"Plants/Plant_Icon_{plantedPlant.plantID:D2}";
                Sprite iconSprite = Resources.Load<Sprite>(iconPath);
                if (iconSprite != null) plantImage.sprite = iconSprite;
            }
        }

        IEnumerator BlinkWithering()
        {
            bool isWither = true;
            while (witherState == WitherState.Withering)
            {
                if (bgImage != null)
                {
                    bgImage.color = isWither ? new Color(1f, 0.5f, 0f, 0.85f) : Color.white;
                }
                isWither = !isWither;
                yield return new WaitForSeconds(0.5f);
            }
            if (bgImage != null && witherState == WitherState.Normal && growthState != GrowthState.ReadyToHarvest)
            {
                bgImage.color = Color.white;
            }
        }

        IEnumerator BlinkWithered()
        {
            bool isRed = true;
            while (witherState == WitherState.Withered)
            {
                if (bgImage != null)
                {
                    bgImage.color = isRed ? new Color(0.5f, 0.1f, 0.1f, 0.85f) : Color.white;
                }
                isRed = !isRed;
                yield return new WaitForSeconds(1f);
            }
            if (bgImage != null && witherState == WitherState.Normal && growthState != GrowthState.ReadyToHarvest)
            {
                bgImage.color = Color.white;
            }
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WeedHoldings
{
    /// <summary>
    /// Canvas UI 기반 밭 셀 1칸.
    /// FarmPlot 데이터를 시각적으로 표현합니다.
    /// </summary>
    public class UIFarmPlotCell : MonoBehaviour
    {
        [Header("참조")]
        public FarmPlot farmPlot;

        [Header("UI 요소")]
        public Image        bgImage;
        public Image        plantImage;
        public Image        progressBarFill;
        public Image        witherBarFill;
        public Image        waterIcon;
        public TextMeshProUGUI plantNameText;
        public TextMeshProUGUI timerText;
        public Image        lockOverlay;
        public TextMeshProUGUI lockCostText;
        public Image        harvestReadyGlow;

        // 색상 팔레트
        static readonly Color COL_EMPTY    = Color.white;
        static readonly Color COL_GROWING  = Color.white;
        static readonly Color COL_HARVEST  = new Color(1.00f, 0.95f, 0.85f);
        static readonly Color COL_WITHER   = new Color(0.75f, 0.50f, 0.50f);
        static readonly Color COL_LOCKED   = new Color(0.40f, 0.40f, 0.40f);

        bool blinkState;
        float blinkTimer;

        void Awake()
        {
            if (farmPlot != null)
                SubscribeEvents();

            if (bgImage != null && bgImage.sprite == null)
            {
                bgImage.sprite = Resources.Load<Sprite>("Field");
                bgImage.color  = Color.white;
            }
        }

        public void Setup(FarmPlot plot)
        {
            if (farmPlot != null) UnsubscribeEvents();
            farmPlot = plot;
            SubscribeEvents();
            Refresh();
        }

        void SubscribeEvents()
        {
            if (farmPlot == null) return;
            farmPlot.OnPlotStateChanged += _ => Refresh();
            if (farmPlot.growthManager != null)
                farmPlot.growthManager.OnGrowthProgressUpdated += _ => UpdateTimerText();
        }

        void UnsubscribeEvents()
        {
            if (farmPlot == null) return;
            farmPlot.OnPlotStateChanged -= _ => Refresh();
        }

        void Update()
        {
            if (farmPlot == null || farmPlot.growthManager == null) return;

            if (farmPlot.growthManager.IsWarningActive)
            {
                blinkTimer += Time.deltaTime;
                if (blinkTimer >= 0.5f)
                {
                    blinkTimer = 0f;
                    blinkState = !blinkState;
                    if (bgImage != null)
                        bgImage.color = blinkState ? COL_WITHER : COL_GROWING;
                }
            }
            else
            {
                // Reset background color to correct state color when warning is not active
                if (bgImage != null)
                {
                    switch (farmPlot.plotState)
                    {
                        case PlotState.Empty:
                            bgImage.color = COL_EMPTY;
                            break;
                        case PlotState.Growing:
                            bgImage.color = COL_GROWING;
                            break;
                        case PlotState.ReadyToHarvest:
                            bgImage.color = COL_HARVEST;
                            break;
                        case PlotState.Locked:
                            bgImage.color = COL_LOCKED;
                            break;
                        case PlotState.Withered:
                            bgImage.color = COL_WITHER;
                            break;
                    }
                }
            }
        }

        public void Refresh()
        {
            if (farmPlot == null) return;
            blinkTimer = 0f;
            blinkState = false;

            switch (farmPlot.plotState)
            {
                case PlotState.Locked:
                    SetBg(COL_LOCKED);
                    ShowLock(true);
                    HidePlantInfo();
                    SetProgressBars(0, 0);
                    break;

                case PlotState.Empty:
                    SetBg(COL_EMPTY);
                    ShowLock(false);
                    HidePlantInfo();
                    SetProgressBars(0, 0);
                    break;

                case PlotState.Growing:
                    SetBg(COL_GROWING);
                    ShowLock(false);
                    ShowPlantInfo();
                    UpdateTimerText();
                    UpdateProgressBars();
                    break;

                case PlotState.ReadyToHarvest:
                    SetBg(COL_HARVEST);
                    ShowLock(false);
                    ShowPlantInfo();
                    if (plantNameText != null)
                    {
                        string pn = farmPlot.growthManager?.currentPlantData?.plantName ?? "";
                        plantNameText.text = pn;
                    }
                    if (timerText != null) timerText.text = "수확 가능!";
                    SetProgressBars(1, 0);
                    break;

                case PlotState.Withered:
                    SetBg(COL_WITHER);
                    ShowLock(false);
                    ShowPlantInfo();
                    if (plantNameText != null) plantNameText.text = "시들음";
                    if (timerText != null) timerText.text = "클릭하여 제거";
                    UpdateProgressBars();
                    break;
            }

            if (harvestReadyGlow != null)
                harvestReadyGlow.gameObject.SetActive(farmPlot.plotState == PlotState.ReadyToHarvest);
        }

        void SetBg(Color c)
        {
            if (bgImage != null) bgImage.color = c;
        }

        void ShowLock(bool show)
        {
            if (lockOverlay != null) lockOverlay.gameObject.SetActive(show);
            if (lockCostText != null)
            {
                lockCostText.gameObject.SetActive(show);
                if (show)
                {
                    int cost = FarmGridManager.Instance != null ? FarmGridManager.Instance.GetUnlockCostForNextPlot() : 500;
                    lockCostText.text = $"🔒 {cost}G";
                }
            }
        }

        void ShowPlantInfo()
        {
            var pgm = farmPlot.growthManager;
            if (pgm == null || pgm.currentPlantData == null) return;

            string pName = pgm.currentPlantData.plantName;
            if (plantNameText != null)
            {
                plantNameText.gameObject.SetActive(true);
                plantNameText.text = pName;
            }
            if (timerText != null) timerText.gameObject.SetActive(true);

            if (waterIcon != null)
                waterIcon.gameObject.SetActive(pgm.IsWatered());

            // 식물 이미지 렌더링 추가
            if (plantImage != null)
            {
                plantImage.gameObject.SetActive(true);
                
                Sprite babySp = pgm.currentPlantData.babySprite;
                if (babySp == null)
                    babySp = Resources.Load<Sprite>("Plants/Plant_Baby_00");

                Sprite fullSp = pgm.currentPlantData.fullGrowSprite;
                if (fullSp == null)
                {
                    int num = pgm.currentPlantData.plantID - 10000;
                    if (num > 0 && num <= 99)
                        fullSp = Resources.Load<Sprite>($"Plants/Plant_Full_Grow_{num:D2}");
                }

                if (farmPlot.plotState == PlotState.ReadyToHarvest)
                {
                    plantImage.sprite = fullSp;
                    plantImage.color = Color.white;
                }
                else if (farmPlot.plotState == PlotState.Withered)
                {
                    plantImage.sprite = fullSp != null ? fullSp : babySp;
                    plantImage.color = new Color(0.4f, 0.3f, 0.3f, 0.8f);
                }
                else // Growing
                {
                    plantImage.sprite = babySp;
                    plantImage.color = Color.white;
                }
            }
        }

        void HidePlantInfo()
        {
            if (plantNameText != null) plantNameText.gameObject.SetActive(false);
            if (timerText != null) timerText.gameObject.SetActive(false);
            if (waterIcon != null) waterIcon.gameObject.SetActive(false);

            if (plantImage != null)
            {
                plantImage.gameObject.SetActive(false);
                plantImage.sprite = null;
            }
        }

        void UpdateTimerText()
        {
            if (timerText == null) return;
            var pgm = farmPlot?.growthManager;
            if (pgm == null) return;

            float remaining = pgm.GetRemainingTime();
            int min = Mathf.FloorToInt(remaining / 60f);
            int sec = Mathf.FloorToInt(remaining % 60f);
            timerText.text = $"{min:D2}분 {sec:D2}초";
        }

        void SetProgressBars(float grow, float wither)
        {
            if (progressBarFill != null) progressBarFill.fillAmount = grow;
            if (witherBarFill != null)   witherBarFill.fillAmount   = wither;
        }

        void UpdateProgressBars()
        {
            var pgm = farmPlot?.growthManager;
            if (pgm == null) return;
            float grow   = pgm.GetGrowthProgress();
            float wither = pgm.GetWitherProgress();
            SetProgressBars(grow, wither);
        }

        public void OnCellClicked()
        {
            FarmGridManager.Instance?.OnPlotCellClicked(farmPlot.plotIndex);
        }
    }
}

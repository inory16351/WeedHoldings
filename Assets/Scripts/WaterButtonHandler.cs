using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace WeedHoldings
{
    /// <summary>
    /// 물주기 버튼 핸들러 - Water_Button에 부착하여 사용
    /// </summary>
    public class WaterButtonHandler : MonoBehaviour
    {
        [Header("설정")]
        [Tooltip("쿨타임 (초)")]
        public float cooldownTime = 20f;

        private Button waterButton;
        private float lastWaterTime = -999f;
        private Image buttonImage;

        void Awake()
        {
            waterButton = GetComponent<Button>();
            buttonImage = GetComponent<Image>();
            if (waterButton != null)
            {
                waterButton.onClick.AddListener(OnWaterButtonClicked);
            }
            UpdateButtonInteractable();
        }

        void Update()
        {
            UpdateButtonInteractable();
        }

        void UpdateButtonInteractable()
        {
            if (waterButton == null || buttonImage == null) return;

            float timeSinceLastWater = Time.time - lastWaterTime;
            bool onCooldown = timeSinceLastWater < cooldownTime;

            waterButton.interactable = !onCooldown;

            // 쿨타임 시각화 (버튼 이미지 알파 조절)
            Color c = buttonImage.color;
            c.a = onCooldown ? 0.5f : 1f;
            buttonImage.color = c;
        }

        void OnWaterButtonClicked()
        {
            // CultivationManager가 없으면 자동 생성 시도
            if (CultivationManager.Instance == null)
            {
                var existingManager = Object.FindFirstObjectByType<CultivationManager>();
                if (existingManager == null)
                {
                    // CultivationManager GameObject 자동 생성
                    var managerGO = new GameObject("CultivationManager");
                    managerGO.AddComponent<CultivationManager>();
                    Debug.Log("[WaterButton] CultivationManager 자동 생성됨");
                }
            }

            if (CultivationManager.Instance == null)
            {
                Debug.LogError("[WaterButton] CultivationManager 생성 실패! 씬에 CultivationManager GameObject가 있는지 확인하세요.");
                return;
            }

            // 쿨타임 체크
            if (Time.time - lastWaterTime < cooldownTime)
            {
                Debug.Log($"[WaterButton] 쿨타임 중... {cooldownTime - (Time.time - lastWaterTime):F1}초 남음");
                return;
            }

            lastWaterTime = Time.time;

            int wateredCount = 0;

            // 모든 성장 중인 밭 + 시든 밭 + 고사된 밭에 물주기
            var manager = CultivationManager.Instance;
            var plots = new List<FieldPlot>(manager.GetAllPlots());
            
            // FieldManager도 사용 중일 수 있으니 병합
            if (FieldManager.Instance != null)
            {
                foreach (var plot in FieldManager.Instance.plots)
                {
                    if (plot != null && !plots.Contains(plot))
                        plots.Add(plot);
                }
            }

            foreach (var plot in plots)
            {
                if (plot != null && (plot.IsGrowing || plot.WitherState == WitherState.Withering || plot.WitherState == WitherState.Withered))
                {
                    plot.Water();
                    wateredCount++;
                }
            }

            Debug.Log($"[WaterButton] {wateredCount}개 밭에 물주기 완료 (쿨타임: {cooldownTime}초)");
        }

        void OnDestroy()
        {
            if (waterButton != null)
            {
                waterButton.onClick.RemoveListener(OnWaterButtonClicked);
            }
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.UI;

namespace WeedHoldings
{
    public class UIAvPlantListPanel : MonoBehaviour
    {
        private RectTransform contentRoot;
        private ScrollRect scrollRect;
        private List<UIAvPlantListSlot> slotList = new List<UIAvPlantListSlot>();
        int selectedIndex = -1;

        float slotSpacing = 8f;

        void Awake()
        {
            CreateScrollView();
            FindAndBindSlots();
        }

        void CreateScrollView()
        {
            scrollRect = UIScrollListFactory.Create(transform, out contentRoot, slotSpacing);

            // 기존 패널들을 Content 아래로 이동
            MoveExistingPanelsToContent();
        }

        void MoveExistingPanelsToContent()
        {
            // Find Av_Plant_Panel_01 ~ Av_Plant_Panel_10 by name pattern
            for (int i = 1; i <= 10; i++)
            {
                string panelName = $"Av_Plant_Panel_{i:D2}";
                var panelTransform = transform.Find(panelName);
                if (panelTransform != null && panelTransform.parent != contentRoot)
                {
                    UIScrollListFactory.MoveIntoContent(panelTransform, contentRoot);

                    // Ensure panel has UIAvPlantListSlot component
                    var slot = panelTransform.GetComponent<UIAvPlantListSlot>();
                    if (slot == null)
                        slot = panelTransform.gameObject.AddComponent<UIAvPlantListSlot>();
                }
            }

            // Also move any existing UIAvPlantListSlot components that weren't caught above
            var slots = GetComponentsInChildren<UIAvPlantListSlot>(true);
            foreach (var slot in slots)
            {
                if (slot.transform.parent != contentRoot)
                    UIScrollListFactory.MoveIntoContent(slot.transform, contentRoot);
            }
        }

        void FindAndBindSlots()
        {
            slotList.Clear();
            
            // Find all slots in contentRoot and sort by name (Av_Plant_Panel_01 ~ 10)
            var allSlots = contentRoot.GetComponentsInChildren<UIAvPlantListSlot>(true);
            var sortedSlots = allSlots
                .OrderBy(s => {
                    // Extract number from name (e.g., "Av_Plant_Panel_01" -> 1)
                    var name = s.gameObject.name;
                    var match = System.Text.RegularExpressions.Regex.Match(name, @"(\d+)$");
                    return match.Success ? int.Parse(match.Value) : 0;
                })
                .ToArray();

            for (int i = 0; i < sortedSlots.Length; i++)
            {
                var slot = sortedSlots[i];
                int capturedIndex = i;
                
                var btn = slot.selectButton;
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnSlotSelected(capturedIndex));
                }
                slotList.Add(slot);
            }
        }

        void OnSlotSelected(int index)
        {
            if (index < 0 || index >= slotList.Count) return;
            var slot = slotList[index];
            if (slot == null || !slot.gameObject.activeSelf) return;

            // 이미 선택된 슬롯을 다시 누르면 선택을 해제한다.
            if (selectedIndex == index)
            {
                if (PlantSelectionManager.Instance != null)
                    PlantSelectionManager.Instance.ClearSelection();
                else
                {
                    selectedIndex = -1;
                    ApplySelection();
                }
                return;
            }

            PlantData plant = DataManager.Instance?.GetPlantByID(GetPlantIDForIndex(index));
            if (plant == null) return;

            // 식물 해금은 연구소의 언락 리스트에서만 가능. 잠긴 슬롯은 실루엣으로만 표시되고
            // selectButton.interactable=false이므로 여기까지 도달하지 않지만 방어적으로 재확인.
            if (DataManager.Instance == null || !DataManager.Instance.IsPlantUnlocked(plant.plantID))
                return;

            if (PlantSelectionManager.Instance != null)
            {
                PlantSelectionManager.Instance.SelectPlant(plant);
                selectedIndex = index;
                ApplySelection();
                // 전역 버튼 사운드 훅이 리스트가 새로고침되며 자식 오브젝트가 새로 만들어지는 타이밍을
                // 놓칠 수 있어, 식물 선택은 여기서 직접 확실하게 재생한다.
                SfxManager.Play("Select_Sound");
            }
        }

        void ApplySelection()
        {
            for (int i = 0; i < slotList.Count; i++)
            {
                if (slotList[i] != null)
                    slotList[i].SetHighlight(i == selectedIndex);
            }
            // 주의: VerticalLayoutGroup 안에서 SetAsLastSibling()은 "맨 위로"가 아니라
            // 리스트의 "맨 아래로" 이동시키는 효과라 선택한 식물이 리스트 아래로 밀려나는 버그가 있었다.
            // 하이라이트(SetHighlight)만으로 선택 표시가 충분하므로 형제 순서는 건드리지 않는다.
        }

        public void RefreshAll()
        {
            if (DataManager.Instance == null) 
            {
                Debug.LogWarning("[UIAvPlantListPanel] DataManager.Instance is null!");
                return;
            }

            var allPlants = DataManager.Instance.GetAllPlants();
            Debug.Log($"[UIAvPlantListPanel] RefreshAll: found {allPlants.Count} plants, slotList.Count={slotList.Count}");
            
            for (int i = 0; i < slotList.Count; i++)
            {
                if (slotList[i] == null) 
                {
                    Debug.LogWarning($"[UIAvPlantListPanel] slotList[{i}] is null");
                    continue;
                }
                
                int plantId = GetPlantIDForIndex(i);
                bool unlocked = DataManager.Instance.IsPlantUnlocked(plantId);
                var plant = DataManager.Instance.GetPlantByID(plantId);

                // 잠긴 식물도 숨기지 않고 실루엣 상태로 리스트에 노출한다.
                slotList[i].gameObject.SetActive(plant != null);
                if (plant != null)
                    slotList[i].Setup(plant, !unlocked);
            }
            ApplySelection();
            UpdateContentSize();
        }

        void UpdateContentSize()
        {
            if (contentRoot == null) return;

            // Content의 실제 높이는 각 패널의 LayoutElement.preferredHeight를 기반으로
            // VerticalLayoutGroup + ContentSizeFitter가 계산한다(UIScrollListFactory.MoveIntoContent 참고).
            // 과거에 있던 "slotHeight 고정값으로 수동 재계산" 로직은 실제 높이와 어긋나
            // 스크롤 한계가 실제 콘텐츠 크기와 안 맞는 원인이었으므로 제거했다.
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);

            // 런타임에 슬롯을 옮긴 직후 RectMask2D의 클립 영역이 그 프레임에 반영 안 될 수 있어 강제 갱신
            Canvas.ForceUpdateCanvases();
        }

        int GetPlantIDForIndex(int index)
        {
            if (DataManager.Instance == null) return 0;
            var all = DataManager.Instance.GetAllPlants();
            if (index >= 0 && index < all.Count)
                return all[index].plantID;
            return 0;
        }

        void OnGlobalSelectionChanged()
        {
            if (PlantSelectionManager.Instance == null || !PlantSelectionManager.Instance.HasSelection)
            {
                selectedIndex = -1;
                ApplySelection();
            }
        }

        void OnEnable()
        {
            if (PlantSelectionManager.Instance != null)
                PlantSelectionManager.Instance.OnSelectionChanged += OnGlobalSelectionChanged;
            RefreshAll();
        }

        void OnDisable()
        {
            if (PlantSelectionManager.Instance != null)
                PlantSelectionManager.Instance.OnSelectionChanged -= OnGlobalSelectionChanged;
        }
    }
}
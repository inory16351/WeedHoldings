using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WeedHoldings
{
    /// <summary>
    /// Char_equip 팝업 담당. Charactor_XX 슬롯의 Char_Icon을 누르면 Open()이 호출되어 Inventory에
    /// 보유 캐릭터 전부를 나열하고(등급 높은 순 -> 같은 등급이면 이름 가나다순), 슬롯을 고르면 오른쪽
    /// Char_Info에 상세 설명이 뜬다. Equip 버튼을 누르면 팝업을 연 슬롯(category/slotIndex)에
    /// 선택된 캐릭터를 장착하고 팝업을 닫는다.
    /// </summary>
    public class UICharacterEquipPopup : MonoBehaviour
    {
        static readonly Vector2 FallbackCellSize = new Vector2(160f, 180f);

        Transform inventoryTransform;
        Transform inventoryContent;
        RectTransform charSlotTemplate;
        Vector2 slotCellSize;
        bool gridInitialized;
        Transform backButtonObject;
        readonly List<CanvasGroup> dimmedColumns = new List<CanvasGroup>();

        Image infoIcon;
        Image infoBg;
        Image infoRating;
        TextMeshProUGUI infoName;
        TextMeshProUGUI infoExplain;
        TextMeshProUGUI infoEffect;
        Button equipButton;

        readonly List<UICharacterInventorySlot> spawnedSlots = new List<UICharacterInventorySlot>();

        EquipCategory pendingCategory;
        int pendingSlotIndex;
        CharacterData selectedCharacter;

        System.Action onEquipped;

        void Awake()
        {
            // Char_equip -> CharactorPanel -> (Top_Panel과 공유하는 최상위 루트)
            var sharedRoot = transform.parent != null ? transform.parent.parent : null;
            // Back 버튼은 Char_equip의 자식으로 있을 수도, 최상위 루트의 형제로 있을 수도 있어 둘 다 찾아본다.
            backButtonObject = transform.Find("Back");
            if (backButtonObject == null && sharedRoot != null) backButtonObject = sharedRoot.Find("Back");
            Debug.Log($"[CharEquip] Back 버튼 탐색 결과: {(backButtonObject != null ? backButtonObject.name + " (경로 정상)" : "못 찾음")}");

            // 팝업 자신도 형제 순서 대신 전용 Canvas로 항상 최상단에 렌더링/입력받도록 고정한다.
            var popupCanvas = GetComponent<Canvas>();
            if (popupCanvas == null) popupCanvas = gameObject.AddComponent<Canvas>();
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = 32000;
            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            // CharactorPanel의 농장/공장/무역소 열 + Top_Panel을 어둡게 + 클릭 막기용 CanvasGroup 준비.
            // (완전히 숨기지는 않고 어둡게 + 상호작용만 차단)
            var characterPanel = transform.parent;
            if (characterPanel != null)
            {
                foreach (var columnName in new[] { "Farm", "Factory", "Sell" })
                {
                    var column = characterPanel.Find(columnName);
                    if (column == null) continue;

                    var group = column.GetComponent<CanvasGroup>();
                    if (group == null) group = column.gameObject.AddComponent<CanvasGroup>();
                    dimmedColumns.Add(group);
                }
            }

            var topPanel = sharedRoot != null ? sharedRoot.Find("Top_Panel") : null;
            if (topPanel != null)
            {
                var topGroup = topPanel.GetComponent<CanvasGroup>();
                if (topGroup == null) topGroup = topPanel.gameObject.AddComponent<CanvasGroup>();
                dimmedColumns.Add(topGroup);
            }

            // 뒤에 있는 캐릭터 패널이 반투명하게 비쳐 보이지 않도록 완전 불투명 흰색으로.
            var background = GetComponent<Image>();
            if (background != null) background.color = Color.white;

            var backButton = backButtonObject?.GetComponentInChildren<Button>();
            backButton?.onClick.AddListener(Close);

            if (backButtonObject != null)
            {
                // 형제 순서(SetAsLastSibling)에 기대는 대신, 전용 Canvas로 정렬 순서를 강제 override해서
                // 다른 코드가 나중에 형제 순서를 바꿔도 Back 버튼 클릭이 항상 최상단에서 먹히도록 한다.
                var backCanvas = backButtonObject.GetComponent<Canvas>();
                if (backCanvas == null) backCanvas = backButtonObject.gameObject.AddComponent<Canvas>();
                backCanvas.overrideSorting = true;
                backCanvas.sortingOrder = 32760;

                if (backButtonObject.GetComponent<GraphicRaycaster>() == null)
                    backButtonObject.gameObject.AddComponent<GraphicRaycaster>();
            }

            inventoryTransform = transform.Find("Inventory");
            var charInfo = transform.Find("Char_Info");

            if (charInfo != null)
            {
                var infoIconTransform = charInfo.Find("Char_Icon");
                infoIcon = infoIconTransform?.GetComponent<Image>();
                if (infoIcon != null) infoIcon.preserveAspect = true;
                infoBg = CharacterCardVisuals.MoveBackgroundBehindIcon(infoIconTransform)?.GetComponent<Image>();
                infoName = charInfo.Find("Name")?.GetComponent<TextMeshProUGUI>();
                infoExplain = charInfo.Find("Explain")?.GetComponent<TextMeshProUGUI>();
                infoEffect = charInfo.Find("Effect_Info")?.GetComponent<TextMeshProUGUI>();
                equipButton = charInfo.Find("Equip")?.GetComponent<Button>();

                var effectHeader = charInfo.Find("Effect")?.GetComponent<TextMeshProUGUI>();
                if (effectHeader != null)
                {
                    effectHeader.text = "보너스 효과";
                    effectHeader.overflowMode = TextOverflowModes.Truncate;
                }
                ConfigureAutoSize(effectHeader, 12f, 20f);

                infoRating = charInfo.Find("Rating")?.GetComponent<Image>();

                UIScrollListFactory.ApplyNotoSansFont(charInfo);
                ConfigureAutoSize(infoName, 14f, 26f);
                ConfigureAutoSize(infoExplain, 10f, 16f);
                ConfigureAutoSize(infoEffect, 8f, 14f);
                if (infoEffect != null)
                {
                    // "보너스 효과" 내용은 대상 이름 길이에 따라 한 줄 폭이 들쭉날쭉해서, 줄바꿈을 켜고
                    // 자동 축소 최대값을 낮춰야 박스를 벗어나 잘리지 않는다.
                    infoEffect.enableWordWrapping = true;
                    infoEffect.overflowMode = TextOverflowModes.Truncate;
                    infoEffect.alignment = TextAlignmentOptions.TopLeft;

                    // 박스 높이 자체도 2줄이 넉넉히 들어가도록 세로로 키운다(중심은 유지, 위아래로 확장).
                    if (infoEffect.rectTransform is RectTransform effectRect)
                        effectRect.sizeDelta = new Vector2(effectRect.sizeDelta.x, Mathf.Max(effectRect.sizeDelta.y, 130f));
                }
            }

            equipButton?.onClick.AddListener(OnEquipClicked);

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Inventory의 ScrollRect/Grid를 처음 열릴 때 딱 한 번만 만든다. Awake() 시점엔 CharactorPanel이
        /// 아직 비활성 상태라 중첩된 스트레치 앵커 체인이 제대로 계산 안 돼 Char_Slot 실측 크기가 비정상적으로
        /// 작게 나올 수 있었다 - Open()은 CharactorPanel이 이미 보이는 상태에서만 호출되므로 여기서 재는 게 안전하다.
        /// </summary>
        void EnsureInventoryGrid()
        {
            if (gridInitialized || inventoryTransform == null) return;
            gridInitialized = true;

            var template = inventoryTransform.Find("Char_Slot") as RectTransform;
            if (template == null) return;

            slotCellSize = template.rect.size;
            Debug.Log($"[CharEquip] Char_Slot 템플릿 실측 크기: {slotCellSize}");
            if (slotCellSize.x < 80f || slotCellSize.y < 80f) slotCellSize = FallbackCellSize;

            charSlotTemplate = template;
            template.gameObject.SetActive(false);

            UIScrollListFactory.CreateGrid(inventoryTransform, out var content, slotCellSize, new Vector2(12f, 12f), 5);
            inventoryContent = content;

            // 등급 높은 순으로 정렬된 슬롯들이 가운데 정렬 대신 왼쪽부터 차곡차곡 채워지도록.
            var gridLayout = content.GetComponent<GridLayoutGroup>();
            if (gridLayout != null) gridLayout.childAlignment = TextAnchor.UpperLeft;
        }

        /// <summary>Charactor_XX 슬롯에서 Char_Icon을 눌렀을 때 호출. 팝업을 열고 인벤토리를 채운다.</summary>
        public void Open(EquipCategory category, int slotIndex, System.Action onEquippedCallback)
        {
            pendingCategory = category;
            pendingSlotIndex = slotIndex;
            onEquipped = onEquippedCallback;
            selectedCharacter = null;

            EnsureInventoryGrid();
            PopulateInventory();
            ClearInfo();

            gameObject.SetActive(true);

            if (backButtonObject != null)
                backButtonObject.gameObject.SetActive(true);

            SetColumnsDimmed(true);
        }

        void Close()
        {
            gameObject.SetActive(false);
            if (backButtonObject != null) backButtonObject.gameObject.SetActive(false);
            SetColumnsDimmed(false);
        }

        /// <summary>팝업이 떠 있는 동안 뒤의 농장/공장/무역소 열을 어둡게 만들고 클릭을 막는다.</summary>
        void SetColumnsDimmed(bool dimmed)
        {
            foreach (var group in dimmedColumns)
            {
                if (group == null) continue;
                group.alpha = dimmed ? 0.4f : 1f;
                group.interactable = !dimmed;
                group.blocksRaycasts = !dimmed;
            }
        }

        void PopulateInventory()
        {
            // Destroy()는 이번 프레임 끝까지 파괴를 미루기 때문에, 팝업을 빠르게 다시 열면 이전 슬롯들이
            // 아직 안 지워진 채로 새 슬롯이 또 생겨서 Content 자식 수가 계속 누적됐다(로그로 확인됨).
            // 즉시 지워서 다음 채우기 전에 완전히 비운다.
            foreach (var slot in spawnedSlots)
                if (slot != null) DestroyImmediate(slot.gameObject);
            spawnedSlots.Clear();

            // 위 루프가 놓친 잔여 자식(예: 이전 세션에서 남은 것)까지 확실히 정리.
            if (inventoryContent != null)
            {
                for (int i = inventoryContent.childCount - 1; i >= 0; i--)
                    DestroyImmediate(inventoryContent.GetChild(i).gameObject);
            }

            if (inventoryContent == null || charSlotTemplate == null || DataManager.Instance == null)
            {
                Debug.LogWarning($"[CharEquip] 인벤토리 채우기 실패: inventoryContent={(inventoryContent != null)}, charSlotTemplate={(charSlotTemplate != null)}, DataManager={(DataManager.Instance != null)}");
                return;
            }

            var owned = DataManager.Instance.GetAllCharacters()
                .Where(c => DataManager.Instance.IsCharacterOwned(c.characterID))
                .OrderByDescending(c => GradeRank(c.grade))
                .ThenBy(c => c.characterName, System.StringComparer.Ordinal)
                .ToList();

            Debug.Log($"[CharEquip] 보유 캐릭터 {owned.Count}개(전체 {DataManager.Instance.GetAllCharacters().Count}개 중) 인벤토리에 채우는 중");

            foreach (var character in owned)
            {
                try
                {
                    var clone = Instantiate(charSlotTemplate.gameObject, inventoryContent);
                    clone.name = $"Char_Slot_{character.characterID}";
                    clone.SetActive(true);

                    // Mask는 maskable=false인 그래픽을 무시하고 그대로 그리는데, 에디터에서 만든 오브젝트는
                    // 이 값이 꺼져 있는 경우가 있어(다른 리스트에서도 같은 문제였음) 강제로 켜준다.
                    foreach (var graphic in clone.GetComponentsInChildren<MaskableGraphic>(true))
                        graphic.maskable = true;

                    var slot = clone.GetComponent<UICharacterInventorySlot>();
                    if (slot == null) slot = clone.AddComponent<UICharacterInventorySlot>();
                    slot.Setup(character, OnCharacterSelected);

                    spawnedSlots.Add(slot);
                }
                catch (System.Exception e)
                {
                    // 한 캐릭터에서라도 예외가 나면 foreach 전체가 멈춰서 나머지 슬롯이 전부 빈 채로
                    // 남는 게 실제 원인이었을 수 있다 - 여기서 잡아서 그 캐릭터만 건너뛰고 계속 진행한다.
                    Debug.LogError($"[CharEquip] {character.characterName} 슬롯 생성 중 예외 발생, 이 캐릭터만 건너뜀: {e}");
                }
            }

            Debug.Log($"[CharEquip] 인벤토리 슬롯 {spawnedSlots.Count}개 생성 완료 (Content 자식 수: {inventoryContent.childCount})");
        }

        static int GradeRank(string grade)
        {
            return grade switch
            {
                "SSR" => 3,
                "SR" => 2,
                "R" => 1,
                _ => 0,
            };
        }

        /// <summary>글자 수가 다양한 이름/설명/보너스 텍스트가 칸을 벗어나지 않도록 자동 축소를 켠다.</summary>
        static void ConfigureAutoSize(TMP_Text text, float min, float max)
        {
            if (text == null) return;
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
        }

        void OnCharacterSelected(CharacterData character)
        {
            selectedCharacter = character;

            if (infoIcon != null)
            {
                infoIcon.sprite = character.characterIcon;
                infoIcon.enabled = character.characterIcon != null;
            }
            if (infoBg != null)
            {
                var frame = CharacterEquipManager.GetFrame(character.grade);
                infoBg.enabled = frame != null;
                if (frame != null) infoBg.sprite = frame;
            }
            if (infoRating != null)
            {
                // 등급 배지 이미지는 아직 에셋이 없다 - 있으면 붙이고, 없으면 빈 상태로 둔다(로직만 준비).
                var badge = CharacterEquipManager.GetGradeBadge(character.grade);
                infoRating.enabled = badge != null;
                if (badge != null) infoRating.sprite = badge;
            }
            if (infoName != null) infoName.text = $"[{character.grade}] {character.characterName}";
            if (infoExplain != null) infoExplain.text = character.explain;
            if (infoEffect != null) infoEffect.text = CharacterEquipManager.BuildEffectDescription(character, pendingCategory);
        }

        void ClearInfo()
        {
            selectedCharacter = null;
            if (infoIcon != null) infoIcon.enabled = false;
            // 포션/식물 인벤토리 슬롯과 동일하게: 배경 자리를 아예 끄는 대신 "빈 슬롯" 스프라이트로 바꿔 보여준다.
            if (infoBg != null)
            {
                infoBg.enabled = true;
                infoBg.sprite = CharacterEquipManager.GetEmptySlotIcon();
                infoBg.color = Color.white;
            }
            if (infoRating != null) infoRating.enabled = false;
            if (infoName != null) infoName.text = "";
            if (infoExplain != null) infoExplain.text = "";
            if (infoEffect != null) infoEffect.text = "";
        }

        void OnEquipClicked()
        {
            if (selectedCharacter == null || CharacterEquipManager.Instance == null)
            {
                Close();
                return;
            }

            CharacterEquipManager.Instance.Equip(pendingCategory, pendingSlotIndex, selectedCharacter.characterID);
            onEquipped?.Invoke();
            Close();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WeedHoldings
{
    /// <summary>
    /// 전용 효과음이 이미 있는 버튼(출항하기=Horn02, 가챠 결과 카드/열기 버튼)과 밭 클릭을 뺀 나머지
    /// 모든 선택 버튼에 Select_Sound를 걸어준다. 인벤토리/캐릭터 슬롯처럼 리스트가 새로고침될 때마다
    /// 버튼이 새로 생기는 곳이 많아서, 한 번만 훑고 끝내는 대신 주기적으로 다시 스캔해 새 버튼을 찾는다.
    /// </summary>
    public class GlobalUiSoundHooker : MonoBehaviour
    {
        const float ScanInterval = 0.5f;

        // FieldPlot처럼 밭 패널이 처음 열리기 전까지는(비활성 상태였다가 나중에 활성화되며 Awake로
        // 컴포넌트가 붙는 경우) 처음 스캔 시점에는 아직 제외 대상인지 판단할 수 없는 버튼이 있다.
        // 한 번 훑고 끝내지 않고, 이미 훅을 걸어둔 버튼도 매 스캔마다 제외 조건을 다시 확인해서
        // 뒤늦게 제외 대상으로 판명되면 리스너를 떼어낸다(안 그러면 나중에 심기 사운드와 겹쳐 들렸다).
        readonly Dictionary<Button, bool> hookedState = new Dictionary<Button, bool>(); // value: 현재 리스너 붙어있는지

        void Start()
        {
            StartCoroutine(ScanLoop());
        }

        IEnumerator ScanLoop()
        {
            var wait = new WaitForSeconds(ScanInterval);
            while (true)
            {
                HookNewButtons();
                yield return wait;
            }
        }

        void HookNewButtons()
        {
            var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var button in buttons)
            {
                if (button == null) continue;

                bool shouldHave = !ShouldExclude(button);
                bool alreadyHas = hookedState.TryGetValue(button, out var has) && has;

                if (shouldHave && !alreadyHas)
                {
                    button.onClick.AddListener(PlaySelectSound);
                    hookedState[button] = true;
                }
                else if (!shouldHave && alreadyHas)
                {
                    button.onClick.RemoveListener(PlaySelectSound);
                    hookedState[button] = false;
                }
            }
        }

        static void PlaySelectSound() => SfxManager.Play("Select_Sound");

        static readonly HashSet<string> ExcludedNames = new HashSet<string>
        {
            "ALL_Plant",       // 한번에 심기 - Planting 전용 사운드
            "All_Harvest",     // 한번에 수확 - Planting 전용 사운드
            "Water_Button",    // 한번에 물주기 - Water_Splash02 전용 사운드
            "Craft_Button",    // 보약 제조하기 - 전용 사운드 없이 무음
            "Farm_Upgrade",    // 업그레이드 3종 - Lab 전용 사운드
            "Factory_Upgrade",
            "Sell_Upgrade",
        };

        static bool ShouldExclude(Button button)
        {
            // 밭 클릭: FieldPlot 본인 클릭은 성장/수확 로직이지 "선택"이 아니다.
            if (button.GetComponent<FieldPlot>() != null) return true;

            // 가챠 카드 오픈: Result_Button(연출 화면 클릭 유도)과 개별 결과 카드(Char_Result_XX) 둘 다 제외.
            if (button.name == "Result_Button") return true;
            if (button.GetComponent<UIGachaResultCard>() != null) return true;

            // 출항하기 버튼: 전용 사운드(Horn02)가 이미 있으므로 중복 재생하지 않는다.
            // 로비의 "무역소" 진입 버튼도 이름이 같은 Sell_Button이라 부모 이름(Sell_Panel)까지 확인해 구분한다.
            if (button.name == "Sell_Button" && button.transform.parent != null && button.transform.parent.name == "Sell_Panel")
                return true;

            // 전용 효과음(Planting/Water_Splash02/Lab)이 따로 있거나(한번에 심기/수확/물주기/업그레이드),
            // 아예 무음이어야 하는(보약 제조하기) 버튼들.
            if (ExcludedNames.Contains(button.name)) return true;

            return false;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WeedHoldings
{
    /// <summary>
    /// 상단 탑바의 사운드 ON/OFF 토글 제어.
    /// PDF 기획서 Page 2 (4번 토글 기능) 구현.
    /// </summary>
    public class UISoundToggle : MonoBehaviour
    {
        public Button soundButton;
        public TextMeshProUGUI soundText;

        private bool isMuted = false;

        void Start()
        {
            if (soundButton == null) soundButton = GetComponent<Button>();
            if (soundText == null) soundText = GetComponentInChildren<TextMeshProUGUI>();

            if (soundButton != null)
                soundButton.onClick.AddListener(ToggleSound);

            // 초기 상태 로드
            isMuted = PlayerPrefs.GetInt("SoundMuted", 0) == 1;
            ApplySoundState();
        }

        void ToggleSound()
        {
            isMuted = !isMuted;
            PlayerPrefs.SetInt("SoundMuted", isMuted ? 1 : 0);
            PlayerPrefs.Save();
            ApplySoundState();
        }

        void ApplySoundState()
        {
            AudioListener.pause = isMuted;
            if (soundText != null)
            {
                soundText.text = isMuted ? "사운드 OFF" : "사운드 ON";
            }
        }
    }
}

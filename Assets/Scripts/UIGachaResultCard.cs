using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WeedHoldings
{
    /// <summary>
    /// Result_10_Panel의 Char_Result_01~10 하나를 담당. Result_Button을 누른 시점에 결과가 이미
    /// 확정되어 SetPendingResult로 전달되고, 카드를 클릭하면 그때부터 뒤집기 연출 후 앞면을 공개한다.
    /// 뒷면은 공통 카드 뒷면(Resources/Cards/card_back), 앞면 배경은 등급별 카드 프레임
    /// (Resources/Cards/{grade}_card)을 사용하고 그 위에 캐릭터 아이콘(Char_Icon)이 자식으로 그려진다.
    /// </summary>
    public class UIGachaResultCard : MonoBehaviour
    {
        static readonly Color DuplicateTint = new Color(0.55f, 0.55f, 0.55f, 1f);
        static Sprite cardBackSprite;

        const float FlipHalfDuration = 0.15f;
        const float NameFadeDuration = 0.3f;

        // Char_Result_XX(10연추, 238.838 x 336.179)의 Char_Icon(217.958 x 233.248) 비율을 기준으로 삼아,
        // 카드 크기가 다른 Result_1_Panel(1연추, 더 큰 카드)에서도 아이콘이 카드에 비례해 커지도록 한다.
        const float IconWidthFraction = 217.958f / 238.838f;
        const float IconHeightFraction = 233.248f / 336.179f;

        static readonly System.Collections.Generic.Dictionary<string, Sprite> cardFrameCache =
            new System.Collections.Generic.Dictionary<string, Sprite>();

        Button button;
        Image cardImage;
        Image iconImage;
        TextMeshProUGUI nameText;
        CanvasGroup nameCanvasGroup;

        CharacterData pendingCharacter;
        bool pendingDuplicate;
        bool revealed;
        Coroutine flipRoutine;

        /// <summary>카드 공개(뒤집기 연출까지 포함) 완료 시 호출. 결과 패널의 "전부 확인해야 뒤로가기 활성화" 판정에 쓴다.</summary>
        public System.Action OnRevealed;

        void Awake()
        {
            button = GetComponent<Button>();
            cardImage = GetComponent<Image>();
            if (cardImage != null) cardImage.preserveAspect = true;
            iconImage = transform.Find("Char_Icon")?.GetComponent<Image>();
            if (iconImage != null) iconImage.preserveAspect = true;

            if (cardBackSprite == null)
                cardBackSprite = Resources.Load<Sprite>("Cards/card_back");

            if (iconImage != null && transform is RectTransform cardRect)
            {
                iconImage.rectTransform.sizeDelta = new Vector2(
                    cardRect.rect.width * IconWidthFraction,
                    cardRect.rect.height * IconHeightFraction);
            }

            var nameTransform = transform.Find("Char_Name");
            if (nameTransform != null)
            {
                nameText = nameTransform.GetComponent<TextMeshProUGUI>();
                nameCanvasGroup = nameTransform.GetComponent<CanvasGroup>();
                if (nameCanvasGroup == null)
                    nameCanvasGroup = nameTransform.gameObject.AddComponent<CanvasGroup>();

                // 기본 TMP 폰트(LiberationSans SDF)에는 한글 글리프가 없어 캐릭터 이름이 깨져 보인다.
                // 다른 패널들과 동일하게 노토산스 폰트를 강제 적용한다.
                var notoSans = UIScrollListFactory.ResolveNotoSansFont();
                if (notoSans != null && nameText != null)
                    nameText.font = notoSans;
            }

            if (button != null)
                button.onClick.AddListener(OnClicked);
        }

        static Sprite GetCardFrame(string grade)
        {
            if (string.IsNullOrEmpty(grade)) return null;

            string key = grade.ToLowerInvariant();
            if (cardFrameCache.TryGetValue(key, out var cached))
                return cached;

            var sprite = Resources.Load<Sprite>($"Cards/{key}_card");
            cardFrameCache[key] = sprite;
            return sprite;
        }

        public void SetPendingResult(CharacterData character, bool isDuplicate)
        {
            pendingCharacter = character;
            pendingDuplicate = isDuplicate;
        }

        /// <summary>다음 뽑기를 위해 카드를 뒷면 상태로 되돌린다.</summary>
        public void ResetToBack()
        {
            revealed = false;
            if (flipRoutine != null)
            {
                StopCoroutine(flipRoutine);
                flipRoutine = null;
            }

            transform.localScale = Vector3.one;
            if (cardImage != null)
            {
                cardImage.sprite = cardBackSprite;
                cardImage.color = Color.white;
            }
            if (iconImage != null) iconImage.gameObject.SetActive(false);
            if (nameText != null) nameText.gameObject.SetActive(false);
            if (nameCanvasGroup != null) nameCanvasGroup.alpha = 0f;
        }

        void OnClicked()
        {
            if (revealed || pendingCharacter == null) return;
            revealed = true;
            PlayRevealSound();
            flipRoutine = StartCoroutine(FlipAndReveal());
        }

        /// <summary>R등급이거나 이미 보유한 중복 카드는 낮은 등급 연출음(Sparking_Card04), 처음 얻는
        /// SR은 Sparking_Card05, 처음 얻는 SSR은 가장 화려한 Sparking_Card03을 재생한다.</summary>
        void PlayRevealSound()
        {
            if (pendingDuplicate || pendingCharacter.grade == "R")
            {
                SfxManager.Play("Sparking_Card04");
                return;
            }

            switch (pendingCharacter.grade)
            {
                case "SR":
                    SfxManager.Play("Sparking_Card05");
                    break;
                case "SSR":
                    SfxManager.Play("Sparking_Card03");
                    break;
            }
        }

        IEnumerator FlipAndReveal()
        {
            yield return ScaleX(1f, 0f, FlipHalfDuration);

            Color tint = pendingDuplicate ? DuplicateTint : Color.white;
            if (cardImage != null)
            {
                var frame = GetCardFrame(pendingCharacter.grade);
                if (frame != null) cardImage.sprite = frame;
                cardImage.color = tint;
            }
            if (iconImage != null)
            {
                iconImage.sprite = pendingCharacter.characterIcon;
                iconImage.color = tint;
                iconImage.gameObject.SetActive(true);
            }
            if (nameText != null)
            {
                nameText.text = pendingCharacter.characterName;
                nameText.gameObject.SetActive(true);
            }

            yield return ScaleX(0f, 1f, FlipHalfDuration);

            if (nameCanvasGroup != null)
                yield return FadeIn(nameCanvasGroup, NameFadeDuration);

            flipRoutine = null;
            OnRevealed?.Invoke();
        }

        IEnumerator ScaleX(float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float x = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                transform.localScale = new Vector3(x, 1f, 1f);
                yield return null;
            }
            transform.localScale = new Vector3(to, 1f, 1f);
        }

        IEnumerator FadeIn(CanvasGroup group, float duration)
        {
            float t = 0f;
            group.alpha = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                group.alpha = Mathf.Clamp01(t / duration);
                yield return null;
            }
            group.alpha = 1f;
        }
    }
}

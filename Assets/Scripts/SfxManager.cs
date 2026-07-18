using System.Collections.Generic;
using UnityEngine;

namespace WeedHoldings
{
    /// <summary>
    /// Resources/SFX/*에 있는 효과음을 이름으로 재생하는 전역 매니저. GoldManager 등과 같은
    /// 싱글턴 패턴을 따르며, 씬에 아직 없으면 최초 호출 시 스스로 GameObject를 만든다.
    /// </summary>
    public class SfxManager : MonoBehaviour
    {
        public static SfxManager Instance { get; private set; }

        AudioSource oneShotSource;
        AudioSource loopSource;
        string currentLoopClip;

        static readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            oneShotSource = gameObject.AddComponent<AudioSource>();
            oneShotSource.playOnAwake = false;
            oneShotSource.loop = false;

            loopSource = gameObject.AddComponent<AudioSource>();
            loopSource.playOnAwake = false;
            loopSource.loop = true;
        }

        static void EnsureInstance()
        {
            if (Instance != null) return;
            var go = new GameObject("SfxManager");
            go.AddComponent<SfxManager>();
        }

        static AudioClip Resolve(string clipName)
        {
            if (clipCache.TryGetValue(clipName, out var cached)) return cached;
            var clip = Resources.Load<AudioClip>("SFX/" + clipName);
            if (clip == null)
                Debug.LogWarning($"[SfxManager] Resources/SFX/{clipName}을 찾지 못함");
            clipCache[clipName] = clip;
            return clip;
        }

        /// <summary>확장자를 뺀 파일명(예: "Horn02")으로 한 번 재생한다.</summary>
        public static void Play(string clipName)
        {
            EnsureInstance();
            var clip = Resolve(clipName);
            if (clip != null) Instance.oneShotSource.PlayOneShot(clip);
        }

        /// <summary>사이렌처럼 상태가 지속되는 동안 반복 재생해야 하는 효과음을 켜고 끈다.</summary>
        public static void SetLooping(string clipName, bool playing)
        {
            EnsureInstance();
            if (playing)
            {
                if (Instance.loopSource.isPlaying && Instance.currentLoopClip == clipName) return;
                var clip = Resolve(clipName);
                if (clip == null) return;
                Instance.loopSource.clip = clip;
                Instance.loopSource.Play();
                Instance.currentLoopClip = clipName;
            }
            else if (Instance.currentLoopClip == clipName)
            {
                Instance.loopSource.Stop();
                Instance.currentLoopClip = null;
            }
        }
    }
}

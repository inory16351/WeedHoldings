using System;
using UnityEngine;

namespace WeedHoldings
{
    public enum TrackState { Idle, Crafting, ReadyToCollect, Failed }

    /// <summary>
    /// 6개 제조 트랙(Track_01~06)의 상태를 관리한다. 재료 소모, 제조 타이머, 완료 시
    /// 실패확률 룰렛, 성공 시 수확(인벤토리 추가)까지 담당하는 순수 게임 로직 매니저.
    /// (CultivationManager가 밭을 관리하는 것과 동일한 역할을 공장 트랙에 대해 수행)
    /// </summary>
    public class PotionCraftManager : MonoBehaviour
    {
        public static PotionCraftManager Instance { get; private set; }

        public class TrackSlot
        {
            public int trackIndex;
            public TrackState state = TrackState.Idle;
            public PotionData potion;
            public float totalTime;
            public float remainingTime;
            public bool succeeded;
        }

        public const int TrackCount = 6;
        TrackSlot[] tracks;

        public event Action OnTracksChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            tracks = new TrackSlot[TrackCount];
            for (int i = 0; i < TrackCount; i++)
                tracks[i] = new TrackSlot { trackIndex = i };
        }

        void Update()
        {
            bool changed = false;
            foreach (var track in tracks)
            {
                if (track.state != TrackState.Crafting) continue;

                track.remainingTime -= Time.deltaTime;
                if (track.remainingTime <= 0f)
                {
                    track.remainingTime = 0f;
                    RollResult(track);
                }
                changed = true;
            }
            if (changed) OnTracksChanged?.Invoke();
        }

        void RollResult(TrackSlot track)
        {
            float bonus = FactoryUpgradeManager.Instance != null ? FactoryUpgradeManager.Instance.GetFailProbBonus() : 0f;
            float failProb = Mathf.Clamp(track.potion.failProbBase - bonus, 0f, 100f);
            float roll = UnityEngine.Random.Range(0f, 100f);
            track.succeeded = roll >= failProb;

            if (track.succeeded)
            {
                track.state = TrackState.ReadyToCollect;
                Debug.Log($"[PotionCraftManager] 트랙{track.trackIndex + 1} 제조 성공: {track.potion.potionName} (실패확률 {failProb:F1}%, roll {roll:F1})");
            }
            else
            {
                track.state = TrackState.Failed;
                Debug.Log($"[PotionCraftManager] 트랙{track.trackIndex + 1} 제조 실패: {track.potion.potionName} (실패확률 {failProb:F1}%, roll {roll:F1})");
            }
        }

        void ClearTrack(TrackSlot track)
        {
            track.state = TrackState.Idle;
            track.potion = null;
            track.totalTime = 0f;
            track.remainingTime = 0f;
            track.succeeded = false;
        }

        public TrackSlot GetTrack(int index)
        {
            return (tracks != null && index >= 0 && index < tracks.Length) ? tracks[index] : null;
        }

        /// <summary>재료가 충분하면 해금된 트랙 중 비어있는 곳을 찾아 자동으로 제조를 시작한다.</summary>
        public bool TryStartCraft(PotionData potion)
        {
            if (potion == null || DataManager.Instance == null) return false;
            if (!DataManager.Instance.IsPotionUnlocked(potion.potionID)) return false;

            int trackIndex = FindAvailableTrack();
            if (trackIndex < 0) return false;

            var materials = DataManager.Instance.GetPotionMaterials(potion.reqPlantGroup);
            // 재료 정의가 하나도 없으면 데이터 누락으로 보고 제작을 막는다 (재료 없이 제작되는 것을 방지).
            if (materials.Count == 0) return false;
            foreach (var (plantId, amount) in materials)
            {
                if (!DataManager.Instance.HasPlantInInventory(plantId, amount)) return false;
            }

            foreach (var (plantId, amount) in materials)
            {
                DataManager.Instance.RemovePlantFromInventory(plantId, amount);
            }

            int primaryPlantId = DataManager.Instance.GetPotionPrimaryMaterialPlantID(potion.potionID);
            float timeMultiplier = FactoryUpgradeManager.Instance != null ? FactoryUpgradeManager.Instance.GetPotionTimeMultiplier(primaryPlantId) : 1f;
            float actualTime = potion.potionTimeSeconds / timeMultiplier;

            var track = tracks[trackIndex];
            track.state = TrackState.Crafting;
            track.potion = potion;
            track.totalTime = actualTime;
            track.remainingTime = actualTime;
            track.succeeded = false;

            Debug.Log($"[PotionCraftManager] 트랙{trackIndex + 1}에서 {potion.potionName} 제조 시작 ({actualTime:F1}초)");
            OnTracksChanged?.Invoke();
            return true;
        }

        int FindAvailableTrack()
        {
            for (int i = 0; i < tracks.Length; i++)
            {
                if (FactoryUpgradeManager.Instance != null && !FactoryUpgradeManager.Instance.IsTrackUnlocked(i)) continue;
                if (tracks[i].state == TrackState.Idle) return i;
            }
            return -1;
        }

        /// <summary>
        /// 완성된 트랙의 결과물을 인벤토리에 넣고 트랙을 비운다.
        /// 실패한 트랙에서 호출되면(Crafting 버튼으로 실패 확인) 결과물 없이 트랙만 비운다.
        /// </summary>
        public bool CollectTrack(int index)
        {
            var track = GetTrack(index);
            if (track == null) return false;

            if (track.state == TrackState.ReadyToCollect && track.potion != null)
            {
                DataManager.Instance.AddPotionToInventory(track.potion.potionID, 1);
                Debug.Log($"[PotionCraftManager] {track.potion.potionName} 인벤토리에 추가됨");
                ClearTrack(track);
                OnTracksChanged?.Invoke();
                return true;
            }

            if (track.state == TrackState.Failed)
            {
                ClearTrack(track);
                OnTracksChanged?.Invoke();
                return true;
            }

            return false;
        }
    }
}

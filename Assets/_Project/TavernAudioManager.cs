using UnityEngine;
using UnityEngine.UI;

namespace HeroManagement
{
    // Ambience (fireplace, crowd at the counter) and sound effects (clicks, coins, forge, mission results).
    // Every sound is synthesized by default; assign your own clips to replace any of them.
    public class TavernAudioManager : MonoBehaviour
    {
        [Header("Volumes")]
        [Range(0f, 1f)] public float masterVolume = 0.8f;
        [Range(0f, 1f)] public float ambienceVolume = 0.35f;
        [Range(0f, 1f)] public float sfxVolume = 0.7f;

        [Header("Your own clips (empty = synthesized)")]
        public AudioClip fireLoop;
        public AudioClip chatterLoop;
        public AudioClip hammer;
        public AudioClip click;
        public AudioClip coin;
        public AudioClip success;
        public AudioClip failure;

        private AudioSource fireSource;
        private AudioSource chatterSource;
        private AudioSource sfxSource;

        private int lastGold = 0;
        private bool goldInitialized = false;

        void Awake()
        {
            if (fireLoop == null) fireLoop = ProceduralSfx.FireLoop();
            if (chatterLoop == null) chatterLoop = ProceduralSfx.ChatterLoop();
            if (hammer == null) hammer = ProceduralSfx.Hammer();
            if (click == null) click = ProceduralSfx.Click();
            if (coin == null) coin = ProceduralSfx.Coin();
            if (success == null) success = ProceduralSfx.Success();
            if (failure == null) failure = ProceduralSfx.Failure();

            fireSource = MakeSource("FireAmbience", fireLoop, true);
            chatterSource = MakeSource("CrowdAmbience", chatterLoop, true);
            sfxSource = MakeSource("Sfx", null, false);
        }

        private AudioSource MakeSource(string name, AudioClip clip, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);

            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0f; // plain 2D sound
            return source;
        }

        void OnEnable()
        {
            GameManager.OnGoldChanged += HandleGold;
            ForgeManager.OnForgeChanged += PlayHammer;
            TavernFacilitiesManager.OnFacilitiesChanged += PlayHammer;
            RecruitmentManager.OnVisitorsChanged += UpdateCrowd;
            MissionDispatcher.OnMissionReport += HandleReport;
            GameFlowManager.OnGameEnded += HandleGameEnded;
        }

        void OnDisable()
        {
            GameManager.OnGoldChanged -= HandleGold;
            ForgeManager.OnForgeChanged -= PlayHammer;
            TavernFacilitiesManager.OnFacilitiesChanged -= PlayHammer;
            RecruitmentManager.OnVisitorsChanged -= UpdateCrowd;
            MissionDispatcher.OnMissionReport -= HandleReport;
            GameFlowManager.OnGameEnded -= HandleGameEnded;
        }

        void Start()
        {
            // A click sound on every button of the scene, including the ones in hidden panels
            foreach (var button in Resources.FindObjectsOfTypeAll<Button>())
            {
                if (button != null && button.gameObject.scene.IsValid()) button.onClick.AddListener(PlayClick);
            }

            ApplyVolumes();
            fireSource.Play();
            chatterSource.Play();
            UpdateCrowd();
        }

        void Update() { ApplyVolumes(); }

        private void ApplyVolumes()
        {
            if (fireSource != null) fireSource.volume = masterVolume * ambienceVolume;
            if (sfxSource != null) sfxSource.volume = masterVolume * sfxVolume;
        }

        // The crowd gets louder when mercenaries wait at the counter
        private void UpdateCrowd()
        {
            if (chatterSource == null) return;

            int visitors = RecruitmentManager.Instance != null ? RecruitmentManager.Instance.currentTavernVisitors.Count : 0;
            chatterSource.volume = masterVolume * ambienceVolume * (0.25f + 0.25f * Mathf.Min(visitors, 3));
        }

        private void Play(AudioClip clip)
        {
            if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip);
        }

        public void PlayClick() { Play(click); }
        public void PlayHammer() { Play(hammer); }

        private void HandleGold(int gold)
        {
            if (goldInitialized && gold > lastGold) Play(coin);
            lastGold = gold;
            goldInitialized = true;
        }

        private void HandleReport(MissionReport report)
        {
            if (report != null) Play(report.success ? success : failure);
        }

        private void HandleGameEnded(bool won, string reason)
        {
            Play(won ? success : failure);
        }
    }
}

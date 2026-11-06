using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SynthOfRage.Scripts.Gameplay.Core
{
    /// <summary>
    /// DSP-based musical clock.
    ///
    /// Supports two audio modes:
    /// - FixedMusic: One main music AudioClip.
    /// - StemSystem: An unlimited number of synchronized audio stems.
    ///
    /// All gameplay beat positions are relative to the first musical beat.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    [RequireComponent(typeof(AudioSource))]
    public class RhythmManager : MonoBehaviour
    {
        public static RhythmManager Instance { get; private set; }

        // ============================================================
        // AUDIO MODE
        // ============================================================

        public enum AudioMode
        {
            FixedMusic,
            StemSystem
        }

        [Header("Audio Mode")]
        [SerializeField] private AudioMode _audioMode = AudioMode.FixedMusic;

        public AudioMode CurrentAudioMode => _audioMode;

        // ============================================================
        // FIXED MUSIC
        // ============================================================

        [Header("Fixed Music")]
        [SerializeField] private AudioClip _music;
        [SerializeField] private bool _playOnStart = true;
        [SerializeField] private bool _loop = true;

        // ============================================================
        // TEMPO
        // ============================================================

        [Header("Tempo")]
        [SerializeField, Min(1f)] private float _bpm = 87f;
        [SerializeField, Min(1)] private int _beatsPerBar = 4;

        [Tooltip("Seconds between the start of the audio file and its first musical beat.")]
        [SerializeField, Min(0f)] private float _firstBeatOffsetSeconds;

        [Tooltip("Delay before scheduling audio playback.")]
        [SerializeField, Min(0.1f)] private float _startDelay = 0.5f;

        // ============================================================
        // AUDIO STEMS
        // ============================================================

        public enum StemTransition
        {
            Immediate,
            NextBeat,
            NextBar
        }

        [Serializable]
        public class StemDefinition
        {
            [Tooltip("Unique identifier used by scripts to control this stem.")]
            public string id = "New Stem";

            [Tooltip("Audio file for this stem.")]
            public AudioClip clip;

            [Tooltip("Reference BPM of this stem. Must match the global BPM for synchronization.")]
            [Min(1f)]
            public float bpm = 87f;

            [Range(0f, 1f)]
            public float volume = 1f;

            [Tooltip("Start this stem automatically when music starts.")]
            public bool enabledOnPlay;

            [Tooltip("Loop this stem.")]
            public bool loop = true;

            [Tooltip("Quantization used when enabling or disabling this stem.")]
            public StemTransition transition = StemTransition.NextBar;

            [Header("Fade In")]

            [Tooltip("Enable a fade-in when this stem starts.")]
            public bool fadeIn;

            [Tooltip("Fade-in duration, measured in beats.")]
            [Min(0f)]
            public float fadeInBeats = 1f;

            [Header("Fade Out")]

            [Tooltip("Enable a fade-out when this stem stops.")]
            public bool fadeOut;

            [Tooltip("Fade-out duration, measured in beats.")]
            [Min(0f)]
            public float fadeOutBeats = 1f;
        }

        [Header("Audio Stems")]
        [Tooltip("Add or remove as many stems as needed.")]
        [SerializeField]
        private List<StemDefinition> _stems = new List<StemDefinition>();

        [Tooltip("Automatically create and manage AudioSources for each stem.")]
        [SerializeField]
        private bool _createStemAudioSources = true;

        [Tooltip("Log stem activation and deactivation.")]
        [SerializeField]
        private bool _debugStems;

        private class StemRuntime
        {
            public StemDefinition definition;
            public AudioSource source;

            // Effective state: global request OR at least one owner request.
            public bool desiredState;

            // Request made through the legacy/global SetStem API.
            public bool globalDesiredState;

            // Owners currently requesting this stem.
            public readonly HashSet<EntityId> ownerRequests = new HashSet<EntityId>();

            public bool isPlaying;

            // DSP fade state.
            public bool isFading;
            public bool stopAfterFade;

            public double fadeStartDsp;
            public double fadeEndDsp;

            public float fadeStartVolume;
            public float fadeTargetVolume;
        }

        private readonly List<StemRuntime> _stemRuntime =
            new List<StemRuntime>();

        // Instance ID -> Unity object.
        // Keeping the object reference lets us detect destroyed owners.
        private readonly Dictionary<EntityId, UnityEngine.Object> _stemOwners = new Dictionary<EntityId, UnityEngine.Object>();

        private readonly List<EntityId> _ownersToRemove = new List<EntityId>();

        // ============================================================
        // UI - BEAT INDICATORS
        // ============================================================

        [Header("UI - 4 Beat Indicators")]
        [Tooltip("Images, ordered from the first to the fourth beat.")]
        [SerializeField] private Image[] _beatImages = new Image[4];

        [SerializeField] private Color _upcomingBeatColor =
            new Color(0.47f, 0.49f, 0.53f);

        [SerializeField] private Color _currentBeatColor =
            new Color(0.18f, 0.91f, 0.39f);

        [SerializeField] private Color _passedBeatColor =
            new Color(0.06f, 0.31f, 0.17f);

        [SerializeField] private Color _completedBarColor =
            new Color(0.13f, 0.52f, 0.95f);

        [Tooltip("Duration of the blue flash at the end of each complete bar, in seconds.")]
        [SerializeField, Min(0f)]
        private float _completedBarFlashSeconds = 0.12f;

        [Tooltip("How many beats EARLY the UI changes color. 0.15 beats is about 103 ms at 87 BPM. Does not affect gameplay timing.")]
        [SerializeField, Min(0f)]
        private float _beatIndicatorAnticipationBeats = 0.15f;

        [Tooltip("Update the indicator every frame. Keeps the flash synchronized to DSP time.")]
        [SerializeField]
        private bool _showBeatIndicator = true;

        // ============================================================
        // DEBUG
        // ============================================================

        [Header("Debug")]
        [SerializeField] private bool _debugBeats;

        // ============================================================
        // INTERNAL CLOCK
        // ============================================================

        private AudioSource _source;

        private double _songStartDsp;
        private double _firstBeatDsp;

        private float _activeBpm;

        private long _lastBeat = -1;

        private bool _scheduled;

        private int _lastIndicatorState = int.MinValue;

        // ============================================================
        // EVENTS
        // ============================================================

        public event Action<long> OnBeat;
        public event Action<long> OnBar;

        // ============================================================
        // PUBLIC PROPERTIES
        // ============================================================

        public float BPM =>
            _scheduled ? _activeBpm : Mathf.Max(1f, _bpm);

        public double SecondsPerBeat => 60.0 / BPM;

        public bool IsReady =>
            _scheduled &&
            AudioSettings.dspTime >= _firstBeatDsp &&
            (_audioMode == AudioMode.StemSystem ||
             (_source && _source.isPlaying));

        public double CurrentBeat =>
            !_scheduled
                ? 0.0
                : Math.Max(0.0,
                    (AudioSettings.dspTime - _firstBeatDsp) / SecondsPerBeat);

        public long BeatIndex => (long)Math.Floor(CurrentBeat);

        public double BeatPhase => CurrentBeat - Math.Floor(CurrentBeat);

        public bool IsMusicPlaying => _scheduled;

        public double SongStartDsp => _songStartDsp;

        public int StemCount =>
            _stems != null ? _stems.Count : 0;

        // ============================================================
        // UNITY LIFECYCLE
        // ============================================================

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = _loop;

            InitializeStems();

            ResetBeatIndicator();
        }

        private void Start()
        {
            if (_playOnStart)
                PlayMusic();
        }

        private void Update()
        {
            UpdateStemFades();
            PruneDestroyedStemOwners();

            if (!_scheduled)
            {
                UpdateBeatIndicator();
                return;
            }

            // Only Fixed Music has a master clip whose end can stop the clock.
            if (_audioMode == AudioMode.FixedMusic &&
                AudioSettings.dspTime >= _songStartDsp &&
                !_source.isPlaying)
            {
                StopMusic();
                return;
            }

            UpdateBeatIndicator();

            if (!IsReady)
                return;

            long index = BeatIndex;

            // Avoid thousands of callbacks after an editor pause or an OS stall.
            if (index - _lastBeat > 32)
                _lastBeat = index - 32;

            while (_lastBeat < index)
            {
                ++_lastBeat;

                OnBeat?.Invoke(_lastBeat);

                if (_lastBeat % _beatsPerBar == 0)
                    OnBar?.Invoke(_lastBeat / _beatsPerBar);

                if (_debugBeats)
                {
                    UnityEngine.Debug.Log(
                        $"[RhythmManager] Beat {_lastBeat} / BPM {BPM}",
                        this);
                }
            }
        }

        // ============================================================
        // STEM INITIALIZATION
        // ============================================================

        private void InitializeStems()
        {
            _stemRuntime.Clear();
            _stemOwners.Clear();

            if (_stems == null)
                return;

            for (int i = 0; i < _stems.Count; i++)
            {
                StemDefinition definition = _stems[i];

                if (definition == null)
                    continue;

                if (string.IsNullOrWhiteSpace(definition.id))
                {
                    UnityEngine.Debug.LogWarning(
                        $"[RhythmManager] Stem at index {i} has no ID.",
                        this);
                    continue;
                }

                if (!definition.clip)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[RhythmManager] Stem '{definition.id}' has no AudioClip.",
                        this);
                    continue;
                }

                if (FindStemRuntime(definition.id) != null)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[RhythmManager] Duplicate stem ID '{definition.id}'.",
                        this);
                    continue;
                }

                if (!_createStemAudioSources)
                {
                    UnityEngine.Debug.LogError(
                        "[RhythmManager] Automatic AudioSource creation is disabled. " +
                        "Enable it to use the stem system.",
                        this);
                    continue;
                }

                GameObject stemObject =
                    new GameObject($"Stem - {definition.id}");

                stemObject.transform.SetParent(transform, false);

                AudioSource stemSource =
                    stemObject.AddComponent<AudioSource>();

                stemSource.playOnAwake = false;
                stemSource.clip = definition.clip;
                stemSource.loop = definition.loop;
                stemSource.volume = definition.volume;
                stemSource.spatialBlend = 0f;
                stemSource.priority = _source.priority;
                stemSource.pitch = 1f;

                StemRuntime runtime = new StemRuntime
                {
                    definition = definition,
                    source = stemSource,
                    globalDesiredState = definition.enabledOnPlay,
                    desiredState = definition.enabledOnPlay,
                    isPlaying = false,
                    isFading = false,
                    stopAfterFade = false
                };

                _stemRuntime.Add(runtime);

                if (Mathf.Abs(definition.bpm - _bpm) > 0.01f)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[RhythmManager] Stem '{definition.id}' uses " +
                        $"{definition.bpm} BPM, but the global BPM is {_bpm}. " +
                        "The stem will not be time-stretched.",
                        this);
                }
            }
        }

        private StemRuntime FindStemRuntime(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            for (int i = 0; i < _stemRuntime.Count; i++)
            {
                if (string.Equals(
                    _stemRuntime[i].definition.id,
                    id,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return _stemRuntime[i];
                }
            }

            return null;
        }

        // ============================================================
        // STEM OWNERSHIP
        // ============================================================

        /// <summary>
        /// Requests a stem for a specific owner.
        ///
        /// A stem remains active while at least one owner requests it,
        /// or while the global API requests it.
        ///
        /// Releasing one owner never releases another owner's request.
        /// </summary>
        public void SetStemForOwner(
            UnityEngine.Object owner,
            string id,
            bool enable)
        {
            if (!owner)
                return;

            if (_audioMode != AudioMode.StemSystem)
                return;

            StemRuntime runtime = FindStemRuntime(id);

            if (runtime == null)
            {
                UnityEngine.Debug.LogWarning(
                    $"[RhythmManager] Stem '{id}' was not found.",
                    this);
                return;
            }

            EntityId ownerId = owner.GetEntityId();

            _stemOwners[ownerId] = owner;

            bool changed = enable
                ? runtime.ownerRequests.Add(ownerId)
                : runtime.ownerRequests.Remove(ownerId);

            if (!changed)
                return;

            RecalculateStemDesiredState(runtime);
        }

        /// <summary>
        /// Releases every stem request belonging to this owner.
        /// Other owners and global requests are unaffected.
        /// </summary>
        public void ReleaseAllStemsForOwner(UnityEngine.Object owner)
        {
            if (!owner)
                return;

            ReleaseAllStemsForOwnerId(owner.GetEntityId());
        }

        private void ReleaseAllStemsForOwnerId(EntityId ownerId)
        {
            bool removedFromAnyStem = false;

            foreach (StemRuntime runtime in _stemRuntime)
            {
                if (runtime == null)
                    continue;

                if (runtime.ownerRequests.Remove(ownerId))
                {
                    removedFromAnyStem = true;
                    RecalculateStemDesiredState(runtime);
                }
            }

            _stemOwners.Remove(ownerId);

            if (_debugStems && removedFromAnyStem)
            {
                UnityEngine.Debug.Log(
                    $"[RhythmManager] Released all stem requests for owner {ownerId}.",
                    this);
            }
        }

        private void PruneDestroyedStemOwners()
        {
            if (_stemOwners.Count == 0)
                return;

            _ownersToRemove.Clear();

            foreach (KeyValuePair<EntityId, UnityEngine.Object> pair in _stemOwners)
            {
                if (!pair.Value)
                    _ownersToRemove.Add(pair.Key);
            }

            for (int i = 0; i < _ownersToRemove.Count; i++)
            {
                ReleaseAllStemsForOwnerId(_ownersToRemove[i]);
            }

            _ownersToRemove.Clear();
        }

        /// <summary>
        /// Computes the effective state from all active requests.
        /// A transition is scheduled only when the effective state changes.
        /// </summary>
        private void RecalculateStemDesiredState(StemRuntime runtime)
        {
            if (runtime == null)
                return;

            bool effectiveState =
                runtime.globalDesiredState ||
                runtime.ownerRequests.Count > 0;

            if (runtime.desiredState == effectiveState)
                return;

            runtime.desiredState = effectiveState;

            if (_debugStems)
            {
                UnityEngine.Debug.Log(
                    $"[RhythmManager] Stem '{runtime.definition.id}' effective state: {effectiveState} " +
                    $"(Global: {runtime.globalDesiredState}, Owners: {runtime.ownerRequests.Count}).",
                    this);
            }

            // Remember the state if the musical clock isn't running.
            if (!_scheduled)
                return;

            ScheduleStemTransition(runtime);
        }

        // ============================================================
        // STEM PUBLIC API - GLOBAL / LEGACY
        // ============================================================

        public void EnableStem(string id)
        {
            SetStem(id, true);
        }

        public void DisableStem(string id)
        {
            SetStem(id, false);
        }

        /// <summary>
        /// Global stem request.
        ///
        /// This API is intentionally independent from owner requests.
        /// Calling SetStem(id, false) does not cancel requests made by players.
        /// </summary>
        public void SetStem(string id, bool enable)
        {
            if (_audioMode != AudioMode.StemSystem)
            {
                if (_debugStems)
                {
                    UnityEngine.Debug.Log(
                        $"[RhythmManager] Ignored stem command '{id}': Fixed Music mode is active.",
                        this);
                }

                return;
            }

            StemRuntime runtime = FindStemRuntime(id);

            if (runtime == null)
            {
                UnityEngine.Debug.LogWarning(
                    $"[RhythmManager] Stem '{id}' was not found.",
                    this);
                return;
            }

            if (!runtime.source)
                return;

            if (runtime.globalDesiredState == enable)
                return;

            runtime.globalDesiredState = enable;

            RecalculateStemDesiredState(runtime);
        }

        public void SetStems(params (string id, bool enabled)[] stems)
        {
            if (stems == null)
                return;

            foreach (var stem in stems)
                SetStem(stem.id, stem.enabled);
        }

        public void EnableAllStems()
        {
            if (_audioMode != AudioMode.StemSystem)
                return;

            foreach (StemRuntime runtime in _stemRuntime)
            {
                if (runtime == null)
                    continue;

                runtime.globalDesiredState = true;
                RecalculateStemDesiredState(runtime);
            }
        }

        public void DisableAllStems()
        {
            if (_audioMode != AudioMode.StemSystem)
                return;

            foreach (StemRuntime runtime in _stemRuntime)
            {
                if (runtime == null)
                    continue;

                runtime.globalDesiredState = false;
                RecalculateStemDesiredState(runtime);
            }
        }

        public bool IsStemEnabled(string id)
        {
            StemRuntime runtime = FindStemRuntime(id);

            return runtime != null && runtime.desiredState;
        }

        public bool IsStemAudible(string id)
        {
            StemRuntime runtime = FindStemRuntime(id);

            return runtime != null &&
                   runtime.source &&
                   runtime.source.isPlaying;
        }

        public void SetStemVolume(string id, float volume)
        {
            StemRuntime runtime = FindStemRuntime(id);

            if (runtime == null || !runtime.source)
                return;

            volume = Mathf.Clamp01(volume);

            runtime.definition.volume = volume;

            if (!runtime.isFading)
                runtime.source.volume = volume;
            else
                runtime.fadeTargetVolume = volume;
        }

        // ============================================================
        // STEM SCHEDULING
        // ============================================================

        private void ScheduleStemTransition(StemRuntime runtime)
        {
            if (runtime == null || !runtime.source)
                return;

            double targetDsp;

            switch (runtime.definition.transition)
            {
                case StemTransition.Immediate:
                    targetDsp = AudioSettings.dspTime + 0.05;
                    break;

                case StemTransition.NextBeat:
                    targetDsp = GetNextGridDspTime(1.0);
                    break;

                case StemTransition.NextBar:
                default:
                    targetDsp = GetNextGridDspTime(_beatsPerBar);
                    break;
            }

            if (runtime.desiredState)
                StartStem(runtime, targetDsp);
            else
                StopStem(runtime, targetDsp);
        }

        private double GetNextGridDspTime(double gridBeats)
        {
            double currentBeat = CurrentBeat;

            double targetBeat =
                QuantizeBeat(currentBeat + 0.00001, gridBeats);

            return _firstBeatDsp + targetBeat * SecondsPerBeat;
        }

        // ============================================================
        // STEM START / STOP
        // ============================================================

        private void StartStem(StemRuntime runtime, double targetDsp)
        {
            if (!runtime.source || !runtime.definition.clip)
                return;

            AudioSource stemSource = runtime.source;
            AudioClip clip = runtime.definition.clip;

            CancelStemFade(runtime);

            // Cancel previous scheduled playback or ending.
            stemSource.Stop();

            stemSource.clip = clip;
            stemSource.loop = runtime.definition.loop;
            stemSource.pitch = 1f;

            bool useFadeIn =
                runtime.definition.fadeIn &&
                runtime.definition.fadeInBeats > 0f;

            stemSource.volume = useFadeIn
                ? 0f
                : runtime.definition.volume;

            // Join the musical arrangement at the correct position.
            double elapsedSeconds = targetDsp - _songStartDsp;

            if (runtime.definition.loop && clip.length > 0f)
                elapsedSeconds %= clip.length;

            elapsedSeconds = Math.Max(0.0, elapsedSeconds);

            int samplePosition = Mathf.Clamp(
                Mathf.FloorToInt(
                    (float)(elapsedSeconds * clip.frequency)),
                0,
                Mathf.Max(0, clip.samples - 1));

            stemSource.timeSamples = samplePosition;

            stemSource.PlayScheduled(targetDsp);

            runtime.isPlaying = true;

            if (useFadeIn)
            {
                double fadeDuration =
                    BeatsToSeconds(runtime.definition.fadeInBeats);

                StartStemFade(
                    runtime,
                    targetDsp,
                    targetDsp + fadeDuration,
                    0f,
                    runtime.definition.volume,
                    false);
            }

            if (_debugStems)
            {
                UnityEngine.Debug.Log(
                    $"[RhythmManager] Stem ON: {runtime.definition.id} " +
                    $"at beat {CurrentBeat:F2} (DSP: {targetDsp:F3})" +
                    (useFadeIn
                        ? $" | Fade In: {runtime.definition.fadeInBeats:F2} beats"
                        : ""),
                    this);
            }
        }

        private void StopStem(StemRuntime runtime, double targetDsp)
        {
            if (!runtime.source)
                return;

            CancelStemFade(runtime);

            bool useFadeOut =
                runtime.definition.fadeOut &&
                runtime.definition.fadeOutBeats > 0f &&
                runtime.isPlaying;

            if (useFadeOut)
            {
                double fadeDuration =
                    BeatsToSeconds(runtime.definition.fadeOutBeats);

                float currentVolume = runtime.source.volume;

                StartStemFade(
                    runtime,
                    targetDsp,
                    targetDsp + fadeDuration,
                    currentVolume,
                    0f,
                    true);

                if (_debugStems)
                {
                    UnityEngine.Debug.Log(
                        $"[RhythmManager] Stem FADE OUT: {runtime.definition.id} " +
                        $"at beat {CurrentBeat:F2} | " +
                        $"Duration: {runtime.definition.fadeOutBeats:F2} beats",
                        this);
                }
            }
            else
            {
                if (runtime.isPlaying)
                {
                    runtime.source.SetScheduledEndTime(targetDsp);
                }
                else
                {
                    runtime.source.Stop();
                }

                runtime.isPlaying = false;

                if (_debugStems)
                {
                    UnityEngine.Debug.Log(
                        $"[RhythmManager] Stem OFF: {runtime.definition.id} " +
                        $"at beat {CurrentBeat:F2} (DSP: {targetDsp:F3})",
                        this);
                }
            }
        }

        // ============================================================
        // STEM FADE SYSTEM
        // ============================================================

        private void StartStemFade(
            StemRuntime runtime,
            double startDsp,
            double endDsp,
            float startVolume,
            float targetVolume,
            bool stopAfterFade)
        {
            if (runtime == null || !runtime.source)
                return;

            runtime.isFading = true;
            runtime.stopAfterFade = stopAfterFade;

            runtime.fadeStartDsp = startDsp;
            runtime.fadeEndDsp = endDsp;

            runtime.fadeStartVolume = startVolume;
            runtime.fadeTargetVolume = targetVolume;
        }

        private void CancelStemFade(StemRuntime runtime)
        {
            if (runtime == null)
                return;

            runtime.isFading = false;
            runtime.stopAfterFade = false;
        }

        private void UpdateStemFades()
        {
            if (_stemRuntime.Count == 0)
                return;

            double currentDsp = AudioSettings.dspTime;

            foreach (StemRuntime runtime in _stemRuntime)
            {
                if (runtime == null ||
                    !runtime.source ||
                    !runtime.isFading)
                    continue;

                if (currentDsp < runtime.fadeStartDsp)
                    continue;

                double duration =
                    runtime.fadeEndDsp - runtime.fadeStartDsp;

                float progress = duration <= 0.0
                    ? 1f
                    : Mathf.Clamp01(
                        (float)((currentDsp - runtime.fadeStartDsp) / duration));

                runtime.source.volume = Mathf.Lerp(
                    runtime.fadeStartVolume,
                    runtime.fadeTargetVolume,
                    progress);

                if (progress < 1f)
                    continue;

                runtime.isFading = false;

                if (runtime.stopAfterFade)
                {
                    runtime.source.Stop();
                    runtime.source.volume = 0f;

                    runtime.isPlaying = false;
                    runtime.stopAfterFade = false;

                    if (_debugStems)
                    {
                        UnityEngine.Debug.Log(
                            $"[RhythmManager] Stem fade-out completed: {runtime.definition.id}",
                            this);
                    }
                }
                else
                {
                    runtime.source.volume = runtime.definition.volume;

                    if (_debugStems)
                    {
                        UnityEngine.Debug.Log(
                            $"[RhythmManager] Stem fade-in completed: {runtime.definition.id}",
                            this);
                    }
                }
            }
        }

        private void StopAllStemSources()
        {
            foreach (StemRuntime runtime in _stemRuntime)
            {
                if (!runtime.source)
                    continue;

                CancelStemFade(runtime);

                runtime.source.Stop();
                runtime.source.volume = runtime.definition.volume;

                runtime.isPlaying = false;
            }
        }

        // ============================================================
        // UI - BEAT INDICATOR
        // ============================================================

        /// <summary>
        /// Draws the four beats of the current bar. At the end of every bar, all four
        /// images flash blue, then the next bar begins at the first image.
        /// The visual display can anticipate the DSP beat without moving the musical clock.
        /// </summary>
        private void UpdateBeatIndicator()
        {
            if (!_showBeatIndicator || !_scheduled || !IsReady)
            {
                SetIndicatorState(-1);
                return;
            }

            double visualBeat =
                (AudioSettings.dspTime - _firstBeatDsp) / SecondsPerBeat
                + _beatIndicatorAnticipationBeats;

            if (visualBeat < 0.0)
            {
                SetIndicatorState(-1);
                return;
            }

            int beatsInCycle = Mathf.Max(1, _beatsPerBar);

            long beat = (long)Math.Floor(visualBeat);

            int beatInBar = (int)(beat % beatsInCycle);

            int slot = Mathf.Clamp(
                beatInBar * 4 / beatsInCycle,
                0,
                3);

            double visualBeatPhase =
                visualBeat - Math.Floor(visualBeat);

            double secondsFromBarStart =
                visualBeatPhase * SecondsPerBeat;

            bool completedBar =
                beat > 0 &&
                beatInBar == 0 &&
                secondsFromBarStart < _completedBarFlashSeconds;

            SetIndicatorState(completedBar ? 4 : slot);
        }

        private void SetIndicatorState(int state)
        {
            if (_lastIndicatorState == state)
                return;

            _lastIndicatorState = state;

            if (_beatImages == null)
                return;

            for (int i = 0; i < _beatImages.Length; i++)
            {
                Image image = _beatImages[i];

                if (!image)
                    continue;

                if (state == 4)
                    image.color = _completedBarColor;
                else if (state < 0 || i > state)
                    image.color = _upcomingBeatColor;
                else if (i == state)
                    image.color = _currentBeatColor;
                else
                    image.color = _passedBeatColor;
            }
        }

        private void ResetBeatIndicator()
        {
            _lastIndicatorState = int.MinValue;
            SetIndicatorState(-1);
        }

        // ============================================================
        // PLAYBACK
        // ============================================================

        public void PlayMusic()
        {
            if (_audioMode == AudioMode.FixedMusic && !_music)
            {
                UnityEngine.Debug.LogWarning(
                    "[RhythmManager] Fixed Music mode requires a Music Clip.",
                    this);
                return;
            }

            if (_audioMode == AudioMode.StemSystem &&
                _stemRuntime.Count == 0)
            {
                UnityEngine.Debug.LogWarning(
                    "[RhythmManager] Stem System mode has no valid stems configured. " +
                    "The musical clock will still run.",
                    this);
            }

            _source.Stop();
            StopAllStemSources();

            _activeBpm = Mathf.Max(1f, _bpm);

            _songStartDsp = AudioSettings.dspTime + _startDelay;

            _firstBeatDsp =
                _songStartDsp + _firstBeatOffsetSeconds;

            _lastBeat = -1;
            _scheduled = true;

            ResetBeatIndicator();

            // FIXED MUSIC MODE
            if (_audioMode == AudioMode.FixedMusic)
            {
                _source.clip = _music;
                _source.loop = _loop;
                _source.volume = 1f;

                _source.PlayScheduled(_songStartDsp);

                if (_loop)
                {
                    double beatCount =
                        _music.length / SecondsPerBeat;

                    if (Math.Abs(beatCount - Math.Round(beatCount)) > 0.04)
                    {
                        UnityEngine.Debug.LogWarning(
                            $"[RhythmManager] Clip length is {beatCount:F2} beats. " +
                            "The loop may drift against the beat grid.",
                            this);
                    }
                }
            }

            // STEM SYSTEM MODE
            else
            {
                _source.Stop();

                foreach (StemRuntime runtime in _stemRuntime)
                {
                    if (runtime == null)
                        continue;

                    // enabledOnPlay controls the global request.
                    // Owner requests remain independent.
                    runtime.globalDesiredState =
                        runtime.definition.enabledOnPlay;

                    runtime.desiredState =
                        runtime.globalDesiredState ||
                        runtime.ownerRequests.Count > 0;

                    runtime.isPlaying = false;

                    if (runtime.desiredState)
                        StartStem(runtime, _songStartDsp);
                }
            }

            if (_debugBeats)
            {
                UnityEngine.Debug.Log(
                    $"[RhythmManager] Playback started. " +
                    $"Mode: {_audioMode} | BPM: {_activeBpm}",
                    this);
            }
        }

        public void StopMusic()
        {
            _scheduled = false;
            _lastBeat = -1;

            if (_source)
                _source.Stop();

            StopAllStemSources();

            ResetBeatIndicator();
        }

        // ============================================================
        // MUSICAL TIMING UTILITIES
        // ============================================================

        public double BeatsToSeconds(double beats) =>
            beats * SecondsPerBeat;

        public double SecondsToBeats(double seconds) =>
            seconds / SecondsPerBeat;

        /// <summary>
        /// Returns the next grid position at or after the requested beat.
        /// </summary>
        public static double QuantizeBeat(double beat, double everyBeats)
        {
            everyBeats = Math.Max(0.001, everyBeats);

            return Math.Ceiling(
                (beat - 1e-7) / everyBeats) * everyBeats;
        }

        public double NextGridBeat(double everyBeats = 1.0) =>
            QuantizeBeat(CurrentBeat, everyBeats);

        /// <summary>
        /// Hybrid snap: delay only when the next grid point is at most maxDelaySeconds away.
        /// Otherwise the input executes at the earliest allowed beat.
        /// </summary>
        public double HybridSnapBeat(
            double earliestBeat,
            double everyBeats,
            double maxDelaySeconds,
            double hitOffsetBeats = 0.0)
        {
            double grid =
                QuantizeBeat(
                    earliestBeat + hitOffsetBeats,
                    everyBeats)
                - hitOffsetBeats;

            return (grid - earliestBeat) * SecondsPerBeat <=
                   Math.Max(0.0, maxDelaySeconds) + 1e-7
                ? grid
                : earliestBeat;
        }

        public bool IsOnGrid(
            double everyBeats = 1.0,
            double toleranceSeconds = 0.09)
        {
            if (!IsReady)
                return false;

            double step = Math.Max(0.001, everyBeats);

            double phase = CurrentBeat % step;

            return Math.Min(phase, step - phase) * SecondsPerBeat <=
                   Math.Max(0, toleranceSeconds);
        }

        // ============================================================
        // VALIDATION
        // ============================================================

        private void OnValidate()
        {
            _bpm = Mathf.Max(1f, _bpm);

            _beatsPerBar = Mathf.Max(1, _beatsPerBar);

            _startDelay = Mathf.Max(0.1f, _startDelay);

            _completedBarFlashSeconds =
                Mathf.Max(0f, _completedBarFlashSeconds);

            _beatIndicatorAnticipationBeats =
                Mathf.Max(0f, _beatIndicatorAnticipationBeats);

            _lastIndicatorState = int.MinValue;

            if (_stems == null)
                return;

            foreach (StemDefinition stem in _stems)
            {
                if (stem == null)
                    continue;

                stem.bpm = Mathf.Max(1f, stem.bpm);

                stem.volume = Mathf.Clamp01(stem.volume);

                stem.fadeInBeats = Mathf.Max(0f, stem.fadeInBeats);

                stem.fadeOutBeats = Mathf.Max(0f, stem.fadeOutBeats);
            }
        }

        // ============================================================
        // CLEANUP
        // ============================================================

        private void OnDestroy()
        {
            StopAllStemSources();

            _stemOwners.Clear();

            if (Instance == this)
                Instance = null;
        }
    }
}
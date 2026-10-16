using System;
using UnityEngine;
using UnityEngine.UI;

namespace SynthOfRage.Scripts.Gameplay.Core
{
    /// <summary>
    /// DSP-based musical clock. The music's actual tempo must match BPM.
    /// All gameplay beat positions are relative to the first musical beat.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    [RequireComponent(typeof(AudioSource))]
    public class RhythmManager : MonoBehaviour
    {
        public static RhythmManager Instance { get; private set; }

        [Header("Music")]
        [SerializeField] private AudioClip _music;
        [SerializeField] private bool _playOnStart = true;
        [SerializeField] private bool _loop = true;
        [SerializeField, Min(0.1f)] private float _startDelay = 0.5f;
        [Tooltip("Seconds between the start of the audio file and its first musical beat.")]
        [SerializeField, Min(0f)] private float _firstBeatOffsetSeconds;

        [Header("Tempo")]
        [SerializeField, Min(1f)] private float _bpm = 87f;
        [SerializeField, Min(1)] private int _beatsPerBar = 4;

        [Header("UI - 4 Beat Indicators")]
        [Tooltip("Images, ordered from the first to the fourth beat.")]
        [SerializeField] private Image[] _beatImages = new Image[4];
        [SerializeField] private Color _upcomingBeatColor = new Color(0.47f, 0.49f, 0.53f);
        [SerializeField] private Color _currentBeatColor = new Color(0.18f, 0.91f, 0.39f);
        [SerializeField] private Color _passedBeatColor = new Color(0.06f, 0.31f, 0.17f);
        [SerializeField] private Color _completedBarColor = new Color(0.13f, 0.52f, 0.95f);
        [Tooltip("Duration of the blue flash at the end of each complete bar, in seconds.")]
        [SerializeField, Min(0f)] private float _completedBarFlashSeconds = 0.12f;
        [Tooltip("How many beats EARLY the UI changes color. 0.15 beats is about 103 ms at 87 BPM. Does not affect gameplay timing.")]
        [SerializeField, Min(0f)] private float _beatIndicatorAnticipationBeats = 0.15f;
        [Tooltip("Update the indicator every frame. Keeps the flash synchronized to DSP time.")]
        [SerializeField] private bool _showBeatIndicator = true;

        [Header("Debug")]
        [SerializeField] private bool _debugBeats;

        private AudioSource _source;
        private double _songStartDsp;
        private double _firstBeatDsp;
        private float _activeBpm;
        private long _lastBeat = -1;
        private bool _scheduled;
        private int _lastIndicatorState = int.MinValue;

        public event Action<long> OnBeat;
        public event Action<long> OnBar;

        public float BPM => _scheduled ? _activeBpm : Mathf.Max(1f, _bpm);
        public double SecondsPerBeat => 60.0 / BPM;
        public bool IsReady => _scheduled && AudioSettings.dspTime >= _firstBeatDsp && _source && _source.isPlaying;
        public double CurrentBeat => !_scheduled ? 0.0 : Math.Max(0.0, (AudioSettings.dspTime - _firstBeatDsp) / SecondsPerBeat);
        public long BeatIndex => (long)Math.Floor(CurrentBeat);
        public double BeatPhase => CurrentBeat - Math.Floor(CurrentBeat);

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
            ResetBeatIndicator();
        }

        private void Start()
        {
            if (_playOnStart) PlayMusic();
        }

        private void Update()
        {
            if (!_scheduled)
            {
                UpdateBeatIndicator();
                return;
            }

            if (AudioSettings.dspTime >= _songStartDsp && !_source.isPlaying)
            {
                // An unlooped song has finished (or playback was stopped externally).
                StopMusic();
                return;
            }

            UpdateBeatIndicator();
            if (!IsReady) return;

            long index = BeatIndex;
            // Avoid thousands of callbacks after an editor pause or an OS stall.
            if (index - _lastBeat > 32) _lastBeat = index - 32;

            while (_lastBeat < index)
            {
                ++_lastBeat;
                OnBeat?.Invoke(_lastBeat);
                if (_lastBeat % _beatsPerBar == 0)
                    OnBar?.Invoke(_lastBeat / _beatsPerBar);

                if (_debugBeats)
                    UnityEngine.Debug.Log($"[RhythmManager] Beat {_lastBeat} / BPM {BPM}", this);
            }
        }

        /// <summary>
        /// Draws the four beats of the current bar. At the end of every bar, all four
        /// images flash blue, then the next bar begins at the first image.
        /// The visual display can anticipate the DSP beat without moving the musical clock.
        /// </summary>
        private void UpdateBeatIndicator()
        {
            if (!_showBeatIndicator || !_scheduled || !_source || !_source.isPlaying)
            {
                SetIndicatorState(-1);
                return;
            }

            // UI-only musical clock. Unlike CurrentBeat, it can become positive
            // shortly BEFORE the first true beat. Gameplay uses the unmodified DSP clock.
            double visualBeat = (AudioSettings.dspTime - _firstBeatDsp) / SecondsPerBeat
                                + _beatIndicatorAnticipationBeats;
            if (visualBeat < 0.0)
            {
                SetIndicatorState(-1);
                return;
            }

            // Four images display four beats. The musical bar can have any length,
            // but when using the indicator the intended configuration is 4 beats/bar.
            int beatsInCycle = Mathf.Max(1, _beatsPerBar);
            long beat = (long)Math.Floor(visualBeat);
            int beatInBar = (int)(beat % beatsInCycle);
            int slot = Mathf.Clamp(beatInBar * 4 / beatsInCycle, 0, 3);

            // Flash on the visually anticipated first beat AFTER a completed bar
            // (not at song start). All four indicators turn blue together.
            double visualBeatPhase = visualBeat - Math.Floor(visualBeat);
            double secondsFromBarStart = visualBeatPhase * SecondsPerBeat;
            bool completedBar = beat > 0 && beatInBar == 0 &&
                                secondsFromBarStart < _completedBarFlashSeconds;

            SetIndicatorState(completedBar ? 4 : slot);
        }

        private void SetIndicatorState(int state)
        {
            if (_lastIndicatorState == state) return;
            _lastIndicatorState = state;

            if (_beatImages == null) return;
            for (int i = 0; i < _beatImages.Length; i++)
            {
                Image image = _beatImages[i];
                if (!image) continue;

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

        public void PlayMusic()
        {
            if (!_music)
            {
                UnityEngine.Debug.LogWarning("[RhythmManager] No music clip assigned.", this);
                return;
            }

            _source.Stop();
            _source.clip = _music;
            _source.loop = _loop;
            _activeBpm = Mathf.Max(1f, _bpm);
            _songStartDsp = AudioSettings.dspTime + _startDelay;
            _firstBeatDsp = _songStartDsp + _firstBeatOffsetSeconds;
            _lastBeat = -1;
            _scheduled = true;
            ResetBeatIndicator();
            _source.PlayScheduled(_songStartDsp);

            if (_loop)
            {
                double beatCount = _music.length / SecondsPerBeat;
                if (Math.Abs(beatCount - Math.Round(beatCount)) > 0.04)
                    UnityEngine.Debug.LogWarning($"[RhythmManager] Clip length is {beatCount:F2} beats. The loop may drift against the beat grid; use a musically seamless loop.", this);
            }
        }

        public void StopMusic()
        {
            _scheduled = false;
            _lastBeat = -1;
            if (_source) _source.Stop();
            ResetBeatIndicator();
        }

        public double BeatsToSeconds(double beats) => beats * SecondsPerBeat;
        public double SecondsToBeats(double seconds) => seconds / SecondsPerBeat;

        /// <summary>Returns the next grid position at or after the requested beat.</summary>
        public static double QuantizeBeat(double beat, double everyBeats)
        {
            everyBeats = Math.Max(0.001, everyBeats);
            return Math.Ceiling((beat - 1e-7) / everyBeats) * everyBeats;
        }

        public double NextGridBeat(double everyBeats = 1.0) => QuantizeBeat(CurrentBeat, everyBeats);

        /// <summary>
        /// Hybrid snap: delay only when the next grid point is at most maxDelaySeconds away.
        /// Otherwise the input executes at the earliest allowed beat.
        /// </summary>
        public double HybridSnapBeat(double earliestBeat, double everyBeats, double maxDelaySeconds,
            double hitOffsetBeats = 0.0)
        {
            double grid = QuantizeBeat(earliestBeat + hitOffsetBeats, everyBeats) - hitOffsetBeats;
            return (grid - earliestBeat) * SecondsPerBeat <= Math.Max(0.0, maxDelaySeconds) + 1e-7
                ? grid : earliestBeat;
        }

        public bool IsOnGrid(double everyBeats = 1.0, double toleranceSeconds = 0.09)
        {
            if (!IsReady) return false;
            double step = Math.Max(0.001, everyBeats);
            double phase = CurrentBeat % step;
            return Math.Min(phase, step - phase) * SecondsPerBeat <= Math.Max(0, toleranceSeconds);
        }

        private void OnValidate()
        {
            _bpm = Mathf.Max(1f, _bpm);
            _beatsPerBar = Mathf.Max(1, _beatsPerBar);
            _startDelay = Mathf.Max(0.1f, _startDelay);
            _completedBarFlashSeconds = Mathf.Max(0f, _completedBarFlashSeconds);
            _beatIndicatorAnticipationBeats = Mathf.Max(0f, _beatIndicatorAnticipationBeats);
            _lastIndicatorState = int.MinValue;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}

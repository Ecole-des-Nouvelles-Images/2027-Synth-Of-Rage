
using System.Collections;
using UnityEngine;
using SynthOfRage.Scripts.Gameplay.Core;

public class AudioSyncScale : MonoBehaviour
{
    [Header("Scale Multipliers")]
    [Tooltip("Scale multiplier applied on each beat.")]
    public Vector3 beatScale = Vector3.one * 1.2f;

    [Tooltip("Scale multiplier when resting.")]
    public Vector3 restScale = Vector3.one;

    [Header("Beat Transition")]
    [Tooltip("Duration of the scale transition, expressed in beats.")]
    [SerializeField, Min(0.01f)]
    private float _transitionBeats = 0.5f;

    [Tooltip("Controls the interpolation progression.")]
    [SerializeField]
    private AnimationCurve _scaleCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("If enabled, the object returns to restScale between beats.")]
    [SerializeField]
    private bool _returnToRest = true;

    private Coroutine _scaleCoroutine;
    private RhythmManager _rhythmManager;

    private Vector3 _baseScale;

    private void Awake()
    {
        // Store the original scale before applying any multiplier.
        _baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        _rhythmManager = RhythmManager.Instance;

        if (_rhythmManager != null)
            _rhythmManager.OnBeat += HandleBeat;
    }

    private void OnDisable()
    {
        if (_rhythmManager != null)
            _rhythmManager.OnBeat -= HandleBeat;

        if (_scaleCoroutine != null)
        {
            StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = null;
        }
    }

    private void HandleBeat(long beatIndex)
    {
        if (_scaleCoroutine != null)
            StopCoroutine(_scaleCoroutine);

        _scaleCoroutine = StartCoroutine(
            MoveToScale(Vector3.Scale(_baseScale, beatScale))
        );
    }

    private IEnumerator MoveToScale(Vector3 target)
    {
        Vector3 initialScale = transform.localScale;

        float elapsedBeats = 0f;
        float duration = Mathf.Max(0.01f, _transitionBeats);

        while (elapsedBeats < duration)
        {
            if (_rhythmManager == null || !_rhythmManager.IsReady)
            {
                _scaleCoroutine = null;
                yield break;
            }

            elapsedBeats += Time.deltaTime /
                            (float)_rhythmManager.SecondsPerBeat;

            float t = Mathf.Clamp01(elapsedBeats / duration);
            float curveValue = _scaleCurve.Evaluate(t);

            transform.localScale = Vector3.LerpUnclamped(
                initialScale,
                target,
                curveValue
            );

            yield return null;
        }

        transform.localScale = target;
        _scaleCoroutine = null;
    }

    private void Update()
    {
        if (!_returnToRest)
            return;

        if (_rhythmManager == null || !_rhythmManager.IsReady)
            return;

        if (_scaleCoroutine != null)
            return;

        Vector3 targetRestScale = Vector3.Scale(_baseScale, restScale);

        float returnDuration = Mathf.Max(
            0.01f,
            _transitionBeats * (float)_rhythmManager.SecondsPerBeat
        );

        float t = Mathf.Clamp01(Time.deltaTime / returnDuration);
        float curveValue = _scaleCurve.Evaluate(t);

        transform.localScale = Vector3.LerpUnclamped(
            transform.localScale,
            targetRestScale,
            curveValue
        );
    }
}

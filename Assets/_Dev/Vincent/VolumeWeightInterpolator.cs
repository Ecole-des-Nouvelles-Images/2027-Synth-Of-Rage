
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace SynthOfRage.Scripts.Rendering
{
    public class VolumeWeightInterpolator : MonoBehaviour
    {
        [Header("Volumes")]
        [SerializeField] private Volume _volumeA;
        [SerializeField] private Volume _volumeB;

        [Header("Transition")]
        [SerializeField, Min(0f)] private float _duration = 2f;
        [SerializeField] private AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Initial State")]
        [SerializeField, Range(0f, 1f)] private float _initialWeightA = 1f;
        [SerializeField, Range(0f, 1f)] private float _initialWeightB = 0f;

        private Coroutine _transitionCoroutine;

        private void Awake()
        {
            ApplyWeights(_initialWeightA, _initialWeightB);
        }
        
        // ============================================================
        // CONTEXT MENU - TESTING
        // ============================================================

        [ContextMenu("Test Transition/Transition to Volume A")]
        private void TestTransitionToA()
        {
            TransitionToA();
        }

        [ContextMenu("Test Transition/Transition to Volume B")]
        private void TestTransitionToB()
        {
            TransitionToB();
        }


        /// <summary>
        /// Transitions from Volume A to Volume B.
        /// </summary>
        public void TransitionToB()
        {
            TransitionTo(0f, 1f);
        }

        /// <summary>
        /// Transitions from Volume B to Volume A.
        /// </summary>
        public void TransitionToA()
        {
            TransitionTo(1f, 0f);
        }
        
        /// <summary>
        /// Interpolates both volume weights toward the specified values.
        /// </summary>
        public void TransitionTo(float targetWeightA, float targetWeightB)
        {
            targetWeightA = Mathf.Clamp01(targetWeightA);
            targetWeightB = Mathf.Clamp01(targetWeightB);

            if (_transitionCoroutine != null)
                StopCoroutine(_transitionCoroutine);

            _transitionCoroutine = StartCoroutine(
                InterpolateWeights(targetWeightA, targetWeightB)
            );
        }

        private IEnumerator InterpolateWeights(float targetWeightA, float targetWeightB)
        {
            float startWeightA = _volumeA.weight;
            float startWeightB = _volumeB.weight;

            if (_duration <= 0f)
            {
                ApplyWeights(targetWeightA, targetWeightB);
                _transitionCoroutine = null;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < _duration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / _duration);
                float curveT = _transitionCurve.Evaluate(t);

                float weightA = Mathf.Lerp(startWeightA, targetWeightA, curveT);
                float weightB = Mathf.Lerp(startWeightB, targetWeightB, curveT);

                ApplyWeights(weightA, weightB);

                yield return null;
            }

            ApplyWeights(targetWeightA, targetWeightB);

            _transitionCoroutine = null;
        }

        private void ApplyWeights(float weightA, float weightB)
        {
            if (_volumeA != null)
                _volumeA.weight = weightA;

            if (_volumeB != null)
                _volumeB.weight = weightB;
        }

        public void SetDuration(float duration)
        {
            _duration = Mathf.Max(0f, duration);
        }

        public void CancelTransition()
        {
            if (_transitionCoroutine == null)
                return;

            StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = null;
        }
    }
}

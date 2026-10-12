using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SynthOfRage.Scripts.Gameplay.Unit.Player.Movement.Utils
{
    public class MovementSpace : MonoBehaviour
    {
        public enum Boundary { DepthMin, DepthMax, SideLeft, SideRight }

        public struct BlendResult
        {
            public bool IsBlending;
            public MovementSpace Source;
            public MovementSpace Target;
            public float T;
            public Quaternion Rotation;
            public Boundary SourceBoundary;
            public Boundary TargetBoundary;
            public Vector3 WorldStart;
            public Vector3 WorldEnd;
        }

        [Header("Depth Limits")]
        [SerializeField] private bool useDepthMin = true;
        [SerializeField] private bool useDepthMax = true;
        [SerializeField] private float minDepth = -3.5f;
        [SerializeField] private float maxDepth = 3.5f;

        [Header("Side Limits")]
        [SerializeField] private bool useSideLeft;
        [SerializeField] private bool useSideRight;
        [SerializeField] private float minSide = -5f;
        [SerializeField] private float maxSide = 5f;

        [Header("Connected Movement Spaces")]
        [SerializeField] private MovementSpaceConnection depthMinConnection;
        [SerializeField] private MovementSpaceConnection depthMaxConnection;
        [SerializeField] private MovementSpaceConnection sideLeftConnection;
        [SerializeField] private MovementSpaceConnection sideRightConnection;

        [Header("Visual Box")]
        [SerializeField] private bool showVisualBox = true;
        [SerializeField] private Vector3 visualBoxSize = new(10f, 2f, 7f);
        [SerializeField] private Vector3 visualBoxOffset = new(0f, 1f, 0f);
        [SerializeField] private bool showBoxWire = true;
        [SerializeField] private bool showBoxSolid;

        [Header("Visual Colors")]
        [SerializeField] private Color limitColor = Color.red;
        [SerializeField] private Color passableColor = Color.green;
        [SerializeField] private Color boxColor = new(1f, 1f, 1f, 0.15f);

        [Header("Visual")]
        [SerializeField] private bool showLabels = true;

        [Header("Blend Debug")]
        [SerializeField] private bool showBlendDebug = true;
        [SerializeField] private Color blendDebugColor = Color.yellow;
        [SerializeField, Min(0.001f)] private float blendDebugPointRadius = 0.08f;

        public bool UseDepthMin => useDepthMin;
        public bool UseDepthMax => useDepthMax;
        public float MinDepth => minDepth;
        public float MaxDepth => maxDepth;

        public bool UseSideLeft => useSideLeft;
        public bool UseSideRight => useSideRight;
        public float MinSide => minSide;
        public float MaxSide => maxSide;

        public MovementSpace DepthMinConnection => depthMinConnection?.Target;
        public MovementSpace DepthMaxConnection => depthMaxConnection?.Target;
        public MovementSpace SideLeftConnection => sideLeftConnection?.Target;
        public MovementSpace SideRightConnection => sideRightConnection?.Target;

        public MovementSpaceConnection DepthMinConnectionSettings => depthMinConnection;
        public MovementSpaceConnection DepthMaxConnectionSettings => depthMaxConnection;
        public MovementSpaceConnection SideLeftConnectionSettings => sideLeftConnection;
        public MovementSpaceConnection SideRightConnectionSettings => sideRightConnection;

        private void OnValidate()
        {
            if (minDepth > maxDepth) (minDepth, maxDepth) = (maxDepth, minDepth);
            if (minSide > maxSide) (minSide, maxSide) = (maxSide, minSide);

            visualBoxSize.x = Mathf.Max(0.01f, visualBoxSize.x);
            visualBoxSize.y = Mathf.Max(0.01f, visualBoxSize.y);
            visualBoxSize.z = Mathf.Max(0.01f, visualBoxSize.z);
            blendDebugPointRadius = Mathf.Max(0.001f, blendDebugPointRadius);

            depthMinConnection?.Validate();
            depthMaxConnection?.Validate();
            sideLeftConnection?.Validate();
            sideRightConnection?.Validate();
        }

        // =========================================================
        // BLEND
        // =========================================================

        public bool TryGetBlend(Boundary sourceBoundary, Vector3 worldPosition, out BlendResult result)
        {
            result = default;

            MovementSpaceConnection connection = GetConnection(sourceBoundary);
            if (connection == null || !connection.BlendEnabled || connection.Target == null)
                return false;

            MovementSpace target = connection.Target;

            float sourceBlendDistance = GetMovementLength(sourceBoundary) * Mathf.Abs(connection.BlendNormalizedFromSource);
            float targetBlendDistance = target.GetMovementLength(connection.TargetEntryBoundary) * connection.BlendNormalizedIntoTarget;

            if (sourceBlendDistance + targetBlendDistance <= Mathf.Epsilon)
                return false;

            Vector3 sourceBoundaryPoint = GetBoundaryWorldPoint(sourceBoundary);
            Vector3 blendStart = sourceBoundaryPoint + GetInsideDirection(sourceBoundary) * sourceBlendDistance;

            Vector3 targetBoundaryPoint = target.GetBoundaryWorldPoint(connection.TargetEntryBoundary);
            Vector3 blendEnd = targetBoundaryPoint + target.GetInsideDirection(connection.TargetEntryBoundary) * targetBlendDistance;

            Vector3 startXZ = FlattenXZ(blendStart);
            Vector3 endXZ = FlattenXZ(blendEnd);
            Vector3 positionXZ = FlattenXZ(worldPosition);
            Vector3 blendVector = endXZ - startXZ;

            float blendLengthSqr = blendVector.sqrMagnitude;
            if (blendLengthSqr <= Mathf.Epsilon)
                return false;

            Vector3 sourceLocal = transform.InverseTransformPoint(worldPosition);

            if (!IsPastSourceBoundary(sourceLocal, sourceBoundary))
            {
                float distanceToBoundary = GetDistanceToBoundary(sourceLocal, sourceBoundary);

                if (distanceToBoundary > sourceBlendDistance)
                    return false;
            }

            float geometricProgress = Vector3.Dot(positionXZ - startXZ, blendVector) / blendLengthSqr;

            if (geometricProgress < 0f || geometricProgress > 1f)
                return false;

            geometricProgress = Mathf.Clamp01(geometricProgress);

            float curvedProgress = connection.BlendCurve != null ? connection.BlendCurve.Evaluate(geometricProgress) : geometricProgress;
            curvedProgress = Mathf.Clamp01(curvedProgress);

            result = new BlendResult
            {
                IsBlending = true,
                Source = this,
                Target = target,
                T = curvedProgress,
                Rotation = Quaternion.Slerp(transform.rotation, target.transform.rotation, curvedProgress),
                SourceBoundary = sourceBoundary,
                TargetBoundary = connection.TargetEntryBoundary,
                WorldStart = blendStart,
                WorldEnd = blendEnd
            };

            return true;
        }

        public bool TryGetAnyBlend(Vector3 worldPosition, out BlendResult result)
        {
            result = default;

            if (!useDepthMin && depthMinConnection?.Target != null && TryGetBlend(Boundary.DepthMin, worldPosition, out result))
                return true;

            if (!useDepthMax && depthMaxConnection?.Target != null && TryGetBlend(Boundary.DepthMax, worldPosition, out result))
                return true;

            if (!useSideLeft && sideLeftConnection?.Target != null && TryGetBlend(Boundary.SideLeft, worldPosition, out result))
                return true;

            if (!useSideRight && sideRightConnection?.Target != null && TryGetBlend(Boundary.SideRight, worldPosition, out result))
                return true;

            return false;
        }

        private static Vector3 FlattenXZ(Vector3 position)
        {
            position.y = 0f;
            return position;
        }

        // =========================================================
        // CONNECTION / BOUNDARIES
        // =========================================================

        public MovementSpaceConnection GetConnection(Boundary boundary)
        {
            return boundary switch
            {
                Boundary.DepthMin => depthMinConnection,
                Boundary.DepthMax => depthMaxConnection,
                Boundary.SideLeft => sideLeftConnection,
                Boundary.SideRight => sideRightConnection,
                _ => null
            };
        }

        public float GetMovementLength(Boundary boundary)
        {
            return boundary switch
            {
                Boundary.DepthMin or Boundary.DepthMax => Mathf.Abs(maxDepth - minDepth),
                Boundary.SideLeft or Boundary.SideRight => Mathf.Abs(maxSide - minSide),
                _ => 0f
            };
        }

        private float GetDistanceToBoundary(Vector3 localPosition, Boundary boundary)
        {
            return boundary switch
            {
                Boundary.DepthMin => localPosition.z - minDepth,
                Boundary.DepthMax => maxDepth - localPosition.z,
                Boundary.SideLeft => localPosition.x - minSide,
                Boundary.SideRight => maxSide - localPosition.x,
                _ => 0f
            };
        }

        private bool IsPastSourceBoundary(Vector3 localPosition, Boundary boundary)
        {
            return boundary switch
            {
                Boundary.DepthMin => localPosition.z < minDepth,
                Boundary.DepthMax => localPosition.z > maxDepth,
                Boundary.SideLeft => localPosition.x < minSide,
                Boundary.SideRight => localPosition.x > maxSide,
                _ => false
            };
        }

        public float GetDistanceInsideFromBoundary(Vector3 worldPosition, Boundary boundary)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);

            return boundary switch
            {
                Boundary.DepthMin => local.z - minDepth,
                Boundary.DepthMax => maxDepth - local.z,
                Boundary.SideLeft => local.x - minSide,
                Boundary.SideRight => maxSide - local.x,
                _ => 0f
            };
        }

        public float GetBoundaryValue(Boundary boundary)
        {
            return boundary switch
            {
                Boundary.DepthMin => minDepth,
                Boundary.DepthMax => maxDepth,
                Boundary.SideLeft => minSide,
                Boundary.SideRight => maxSide,
                _ => 0f
            };
        }

        public bool IsDepthBoundary(Boundary boundary) => boundary is Boundary.DepthMin or Boundary.DepthMax;

        // =========================================================
        // GIZMOS
        // =========================================================

        private void OnDrawGizmos()
        {
            if (showVisualBox)
                DrawVisualBox();

            if (showBlendDebug)
                DrawAllBlendDebug();
        }

        private void DrawVisualBox()
        {
            Matrix4x4 previousMatrix = Gizmos.matrix;

            Gizmos.matrix = transform.localToWorldMatrix;

            if (showBoxSolid)
            {
                Gizmos.color = boxColor;
                Gizmos.DrawCube(visualBoxOffset, visualBoxSize);
            }

            if (showBoxWire)
            {
                Gizmos.color = boxColor;
                Gizmos.DrawWireCube(visualBoxOffset, visualBoxSize);
            }

            Gizmos.matrix = previousMatrix;

            DrawDepthLimits();
            DrawSideLimits();
        }

        private void DrawDepthLimits()
        {
            float halfWidth = visualBoxSize.x * 0.5f;
            float halfHeight = visualBoxSize.y * 0.5f;

            Vector3 minCenter = new(visualBoxOffset.x, visualBoxOffset.y, minDepth);
            Vector3 maxCenter = new(visualBoxOffset.x, visualBoxOffset.y, maxDepth);

            DrawLimitPlane(minCenter, halfWidth, halfHeight, useDepthMin ? limitColor : passableColor, false);
            DrawLimitPlane(maxCenter, halfWidth, halfHeight, useDepthMax ? limitColor : passableColor, false);

#if UNITY_EDITOR
            if (!showLabels)
                return;

            Handles.Label(transform.TransformPoint(minCenter), useDepthMin ? $"Depth Min : {minDepth:F2} [LIMIT]" : $"Depth Min : {minDepth:F2} [PASSABLE]");
            Handles.Label(transform.TransformPoint(maxCenter), useDepthMax ? $"Depth Max : {maxDepth:F2} [LIMIT]" : $"Depth Max : {maxDepth:F2} [PASSABLE]");
#endif
        }

        private void DrawSideLimits()
        {
            float halfDepth = visualBoxSize.z * 0.5f;
            float halfHeight = visualBoxSize.y * 0.5f;

            Vector3 leftCenter = new(minSide, visualBoxOffset.y, visualBoxOffset.z);
            Vector3 rightCenter = new(maxSide, visualBoxOffset.y, visualBoxOffset.z);

            DrawLimitPlane(leftCenter, halfDepth, halfHeight, useSideLeft ? limitColor : passableColor, true);
            DrawLimitPlane(rightCenter, halfDepth, halfHeight, useSideRight ? limitColor : passableColor, true);

#if UNITY_EDITOR
            if (!showLabels)
                return;

            Handles.Label(transform.TransformPoint(leftCenter), useSideLeft ? $"Side Left : {minSide:F2} [LIMIT]" : $"Side Left : {minSide:F2} [PASSABLE]");
            Handles.Label(transform.TransformPoint(rightCenter), useSideRight ? $"Side Right : {maxSide:F2} [LIMIT]" : $"Side Right : {maxSide:F2} [PASSABLE]");
#endif
        }

        private void DrawLimitPlane(Vector3 center, float halfHorizontal, float halfVertical, Color color, bool horizontalIsDepth)
        {
            Vector3 horizontalAxis = horizontalIsDepth ? transform.forward : transform.right;
            Vector3 verticalAxis = transform.up;
            Vector3 worldCenter = transform.TransformPoint(center);

            Vector3 bottomLeft = worldCenter - horizontalAxis * halfHorizontal - verticalAxis * halfVertical;
            Vector3 bottomRight = worldCenter + horizontalAxis * halfHorizontal - verticalAxis * halfVertical;
            Vector3 topLeft = worldCenter - horizontalAxis * halfHorizontal + verticalAxis * halfVertical;
            Vector3 topRight = worldCenter + horizontalAxis * halfHorizontal + verticalAxis * halfVertical;

            Gizmos.color = color;
            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }

        private void DrawAllBlendDebug()
        {
            DrawBlendDebug(Boundary.DepthMin);
            DrawBlendDebug(Boundary.DepthMax);
            DrawBlendDebug(Boundary.SideLeft);
            DrawBlendDebug(Boundary.SideRight);
        }

        private void DrawBlendDebug(Boundary sourceBoundary)
        {
            MovementSpaceConnection connection = GetConnection(sourceBoundary);

            if (connection == null || !connection.BlendEnabled || connection.Target == null)
                return;

            MovementSpace target = connection.Target;

            float sourceBlendDistance = GetMovementLength(sourceBoundary) * Mathf.Abs(connection.BlendNormalizedFromSource);
            float targetBlendDistance = target.GetMovementLength(connection.TargetEntryBoundary) * connection.BlendNormalizedIntoTarget;

            Vector3 sourceBoundaryPoint = GetBoundaryWorldPoint(sourceBoundary);
            Vector3 blendStart = sourceBoundaryPoint + GetInsideDirection(sourceBoundary) * sourceBlendDistance;

            Vector3 targetBoundaryPoint = target.GetBoundaryWorldPoint(connection.TargetEntryBoundary);
            Vector3 blendEnd = targetBoundaryPoint + target.GetInsideDirection(connection.TargetEntryBoundary) * targetBlendDistance;

            Gizmos.color = blendDebugColor;
            Gizmos.DrawLine(blendStart, blendEnd);
            Gizmos.DrawSphere(blendStart, blendDebugPointRadius);
            Gizmos.DrawSphere(blendEnd, blendDebugPointRadius);
        }

        private Vector3 GetBoundaryWorldPoint(Boundary boundary)
        {
            Vector3 localPoint = visualBoxOffset;

            switch (boundary)
            {
                case Boundary.DepthMin: localPoint.z = minDepth; break;
                case Boundary.DepthMax: localPoint.z = maxDepth; break;
                case Boundary.SideLeft: localPoint.x = minSide; break;
                case Boundary.SideRight: localPoint.x = maxSide; break;
            }

            return transform.TransformPoint(localPoint);
        }

        private Vector3 GetInsideDirection(Boundary boundary)
        {
            return boundary switch
            {
                Boundary.DepthMin => transform.forward,
                Boundary.DepthMax => -transform.forward,
                Boundary.SideLeft => transform.right,
                Boundary.SideRight => -transform.right,
                _ => Vector3.zero
            };
        }
    }

    [Serializable]
    public class MovementSpaceConnection
    {
        [SerializeField] private MovementSpace target;

        [Header("Blend")]
        [SerializeField] private bool blendEnabled = true;
        [SerializeField, Range(-1f, 0f)] private float blendNormalizedFromSource = -0.25f;
        [SerializeField, Range(0f, 1f)] private float blendNormalizedIntoTarget = 0.25f;
        [SerializeField] private AnimationCurve blendCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Target Entry")]
        [SerializeField] private MovementSpace.Boundary targetEntryBoundary = MovementSpace.Boundary.DepthMin;

        public MovementSpace Target => target;
        public bool BlendEnabled => blendEnabled;
        public float BlendNormalizedFromSource => blendNormalizedFromSource;
        public float BlendNormalizedIntoTarget => blendNormalizedIntoTarget;
        public AnimationCurve BlendCurve => blendCurve;
        public MovementSpace.Boundary TargetEntryBoundary => targetEntryBoundary;

        public void Validate()
        {
            blendNormalizedFromSource = Mathf.Clamp(blendNormalizedFromSource, -1f, 0f);
            blendNormalizedIntoTarget = Mathf.Clamp01(blendNormalizedIntoTarget);
            blendCurve ??= AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }
    }
}
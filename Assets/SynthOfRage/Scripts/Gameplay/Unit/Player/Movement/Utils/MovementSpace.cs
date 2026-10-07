using System;
using UnityEditor;
using UnityEngine;

namespace SynthOfRage.Scripts.Gameplay.Unit.Player.Movement.Utils
{
    public class MovementSpace : MonoBehaviour
    {
        // =========================================================
        // ENUM
        // =========================================================

        public enum Boundary
        {
            DepthMin,
            DepthMax,
            SideLeft,
            SideRight
        }

        // =========================================================
        // BLEND RESULT
        // =========================================================

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

        // =========================================================
        // DEPTH LIMITS
        // =========================================================

        [Header("Depth Limits")]
        [SerializeField] private bool useDepthMin = true;
        [SerializeField] private bool useDepthMax = true;
        [SerializeField] private float minDepth = -3.5f;
        [SerializeField] private float maxDepth = 3.5f;

        // =========================================================
        // SIDE LIMITS
        // =========================================================

        [Header("Side Limits")]
        [SerializeField] private bool useSideLeft = false;
        [SerializeField] private bool useSideRight = false;
        [SerializeField] private float minSide = -5f;
        [SerializeField] private float maxSide = 5f;

        // =========================================================
        // CONNECTIONS
        // =========================================================

        [Header("Connected Movement Spaces")]
        [SerializeField] private MovementSpaceConnection depthMinConnection;
        [SerializeField] private MovementSpaceConnection depthMaxConnection;
        [SerializeField] private MovementSpaceConnection sideLeftConnection;
        [SerializeField] private MovementSpaceConnection sideRightConnection;

        // =========================================================
        // VISUAL BOX
        // =========================================================

        [Header("Visual Box")]
        [SerializeField] private bool showVisualBox = true;
        [SerializeField] private Vector3 visualBoxSize = new Vector3(10f, 2f, 7f);
        [SerializeField] private Vector3 visualBoxOffset = new Vector3(0f, 1f, 0f);
        [SerializeField] private bool showBoxWire = true;
        [SerializeField] private bool showBoxSolid = false;

        // =========================================================
        // VISUAL COLORS
        // =========================================================

        [Header("Visual Colors")]
        [SerializeField] private Color limitColor = new Color(1f, 0f, 0f, 1f);
        [SerializeField] private Color passableColor = new Color(0f, 1f, 0f, 1f);
        [SerializeField] private Color boxColor = new Color(1f, 1f, 1f, 0.15f);

        // =========================================================
        // VISUAL LINES
        // =========================================================

        [Header("Visual Lines")]
        [SerializeField]
        [Min(0.001f)]
        private float lineWidth = 0.05f;

        [SerializeField]
        [Min(0f)]
        private float lineExtension = 0.15f;

        [SerializeField] private bool showLabels = true;

        // =========================================================
        // BLEND DEBUG
        // =========================================================

        [Header("Blend Debug")]
        [SerializeField] private bool showBlendDebug = true;
        [SerializeField] private Color blendDebugColor = Color.yellow;

        [SerializeField]
        [Min(0.001f)]
        private float blendDebugPointRadius = 0.08f;

        // =========================================================
        // PUBLIC API
        // =========================================================

        public bool UseDepthMin => useDepthMin;
        public bool UseDepthMax => useDepthMax;
        public float MinDepth => minDepth;
        public float MaxDepth => maxDepth;

        public bool UseSideLeft => useSideLeft;
        public bool UseSideRight => useSideRight;
        public float MinSide => minSide;
        public float MaxSide => maxSide;

        // ---------------------------------------------------------
        // CONNECTION TARGETS
        // ---------------------------------------------------------

        public MovementSpace DepthMinConnection => depthMinConnection != null ? depthMinConnection.Target : null;
        public MovementSpace DepthMaxConnection => depthMaxConnection != null ? depthMaxConnection.Target : null;
        public MovementSpace SideLeftConnection => sideLeftConnection != null ? sideLeftConnection.Target : null;
        public MovementSpace SideRightConnection => sideRightConnection != null ? sideRightConnection.Target : null;

        // ---------------------------------------------------------
        // CONNECTION SETTINGS
        // ---------------------------------------------------------

        public MovementSpaceConnection DepthMinConnectionSettings => depthMinConnection;
        public MovementSpaceConnection DepthMaxConnectionSettings => depthMaxConnection;
        public MovementSpaceConnection SideLeftConnectionSettings => sideLeftConnection;
        public MovementSpaceConnection SideRightConnectionSettings => sideRightConnection;

        // =========================================================
        // CONSTRAINT QUERIES
        // =========================================================

        /// <summary>
        /// Constrains a local position to stay within the movement space bounds.
        /// </summary>
        public Vector3 ConstrainPositionToBounds(Vector3 localPosition)
        {
            // Clamp depth (z-axis)
            float clampedZ = localPosition.z;
            if (useDepthMin) clampedZ = Mathf.Max(clampedZ, minDepth);
            if (useDepthMax) clampedZ = Mathf.Min(clampedZ, maxDepth);

            // Clamp side (x-axis)
            float clampedX = localPosition.x;
            if (useSideLeft) clampedX = Mathf.Max(clampedX, minSide);
            if (useSideRight) clampedX = Mathf.Min(clampedX, maxSide);

            return new Vector3(clampedX, localPosition.y, clampedZ);
        }

        /// <summary>
        /// Checks if a target position crosses any boundary and returns the connected space and boundary.
        /// </summary>
        public bool TryGetBoundaryCrossing(Vector3 localTargetPosition, out MovementSpace connectedSpace, out Boundary crossedBoundary)
        {
            connectedSpace = null;
            crossedBoundary = Boundary.DepthMin;

            // Check depth boundaries
            if (useDepthMin && localTargetPosition.z < minDepth)
            {
                connectedSpace = DepthMinConnection;
                crossedBoundary = Boundary.DepthMin;
                return connectedSpace != null;
            }

            if (useDepthMax && localTargetPosition.z > maxDepth)
            {
                connectedSpace = DepthMaxConnection;
                crossedBoundary = Boundary.DepthMax;
                return connectedSpace != null;
            }

            // Check side boundaries
            if (useSideLeft && localTargetPosition.x < minSide)
            {
                connectedSpace = SideLeftConnection;
                crossedBoundary = Boundary.SideLeft;
                return connectedSpace != null;
            }

            if (useSideRight && localTargetPosition.x > maxSide)
            {
                connectedSpace = SideRightConnection;
                crossedBoundary = Boundary.SideRight;
                return connectedSpace != null;
            }

            return false;
        }

        /// <summary>
        /// Gets the effective Y rotation at a world position based on the movement space.
        /// </summary>
        public float GetEffectiveYRotationAtPosition(Vector3 worldPosition)
        {
            return transform.rotation.eulerAngles.y;
        }

        /// <summary>
        /// Tries to get blend information for a world position.
        /// </summary>
        public bool TryGetBlendForPosition(Vector3 worldPosition, out BlendResult result)
        {
            return TryGetAnyBlend(worldPosition, out result);
        }

        /// <summary>
        /// Static method to get effective rotation at a world position, considering blends.
        /// </summary>
        public static Quaternion GetEffectiveRotationAtPosition(Vector3 worldPosition)
        {
            // Find the movement space that contains this position
            var movementSpaces = FindObjectsByType<MovementSpace>();
            foreach (var space in movementSpaces)
            {
                Vector3 localPos = space.transform.InverseTransformPoint(worldPosition);

                // Check if position is within bounds
                bool inDepth = (!space.useDepthMin || localPos.z >= space.minDepth) &&
                              (!space.useDepthMax || localPos.z <= space.maxDepth);
                bool inSide = (!space.useSideLeft || localPos.x >= space.minSide) &&
                             (!space.useSideRight || localPos.x <= space.maxSide);

                if (inDepth && inSide)
                {
                    // Check if there's a blend at this position
                    if (space.TryGetAnyBlend(worldPosition, out var blendResult) && blendResult.IsBlending)
                    {
                        return blendResult.Rotation;
                    }
                    else
                    {
                        return space.transform.rotation;
                    }
                }
            }

            // Default to identity if no space found
            return Quaternion.identity;
        }

        // =========================================================
        // VALIDATION
        // =========================================================

        private void OnValidate()
        {
            if (minDepth > maxDepth)
            {
                float temp = minDepth;
                minDepth = maxDepth;
                maxDepth = temp;
            }

            if (minSide > maxSide)
            {
                float temp = minSide;
                minSide = maxSide;
                maxSide = temp;
            }

            visualBoxSize.x = Mathf.Max(0.01f, visualBoxSize.x);
            visualBoxSize.y = Mathf.Max(0.01f, visualBoxSize.y);
            visualBoxSize.z = Mathf.Max(0.01f, visualBoxSize.z);

            lineWidth = Mathf.Max(0.001f, lineWidth);
            lineExtension = Mathf.Max(0f, lineExtension);
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

            float sourceLength = GetMovementLength(sourceBoundary);
            float targetLength = target.GetMovementLength(connection.TargetEntryBoundary);

            float sourceBlendDistance = sourceLength * Mathf.Abs(connection.BlendNormalizedFromSource);
            float targetBlendDistance = targetLength * connection.BlendNormalizedIntoTarget;

            if (sourceBlendDistance + targetBlendDistance <= Mathf.Epsilon)
                return false;

            Vector3 sourceBoundaryPoint = GetBoundaryWorldPoint(sourceBoundary);
            Vector3 blendStart = sourceBoundaryPoint + GetInsideDirection(sourceBoundary) * sourceBlendDistance;

            Vector3 targetBoundaryPoint = target.GetBoundaryWorldPoint(connection.TargetEntryBoundary);
            Vector3 blendEnd = targetBoundaryPoint + target.GetInsideDirection(connection.TargetEntryBoundary) * targetBlendDistance;

            // XZ ONLY :
            // un saut ne doit pas modifier la progression du blend.
            Vector3 blendStartXZ = new Vector3(blendStart.x, 0f, blendStart.z);
            Vector3 blendEndXZ = new Vector3(blendEnd.x, 0f, blendEnd.z);
            Vector3 worldPositionXZ = new Vector3(worldPosition.x, 0f, worldPosition.z);

            Vector3 blendVector = blendEndXZ - blendStartXZ;
            float blendLengthSqr = blendVector.sqrMagnitude;

            if (blendLengthSqr <= Mathf.Epsilon)
                return false;

            Vector3 sourceLocal = transform.InverseTransformPoint(worldPosition);
            bool pastSourceBoundary = IsPastSourceBoundary(sourceLocal, sourceBoundary);

            if (!pastSourceBoundary)
            {
                float distanceToSourceBoundary = GetDistanceToBoundary(sourceLocal, sourceBoundary);

                if (distanceToSourceBoundary > sourceBlendDistance)
                    return false;
            }

            float geometricProgress = Vector3.Dot(worldPositionXZ - blendStartXZ, blendVector) / blendLengthSqr;

            if (geometricProgress < 0f || geometricProgress > 1f)
                return false;

            geometricProgress = Mathf.Clamp01(geometricProgress);

            float curvedProgress = connection.BlendCurve != null ? connection.BlendCurve.Evaluate(geometricProgress) : geometricProgress;
            curvedProgress = Mathf.Clamp01(curvedProgress);

            Quaternion sourceRotation = transform.rotation;
            Quaternion targetRotation = target.transform.rotation;
            Quaternion blendedRotation = Quaternion.Slerp(sourceRotation, targetRotation, curvedProgress);

            result = new BlendResult
            {
                IsBlending = true,
                Source = this,
                Target = target,
                T = curvedProgress,
                Rotation = blendedRotation,
                SourceBoundary = sourceBoundary,
                TargetBoundary = connection.TargetEntryBoundary,
                WorldStart = blendStart,
                WorldEnd = blendEnd
            };

            return true;
        }

        // =========================================================
        // ANY BLEND
        // =========================================================

        public bool TryGetAnyBlend(Vector3 worldPosition, out BlendResult result)
        {
            result = default;

            if (!useDepthMin && depthMinConnection != null && depthMinConnection.Target != null)
            {
                if (TryGetBlend(Boundary.DepthMin, worldPosition, out result))
                    return true;
            }

            if (!useDepthMax && depthMaxConnection != null && depthMaxConnection.Target != null)
            {
                if (TryGetBlend(Boundary.DepthMax, worldPosition, out result))
                    return true;
            }

            if (!useSideLeft && sideLeftConnection != null && sideLeftConnection.Target != null)
            {
                if (TryGetBlend(Boundary.SideLeft, worldPosition, out result))
                    return true;
            }

            if (!useSideRight && sideRightConnection != null && sideRightConnection.Target != null)
            {
                if (TryGetBlend(Boundary.SideRight, worldPosition, out result))
                    return true;
            }

            return false;
        }

        // =========================================================
        // CONNECTION
        // =========================================================

        public MovementSpaceConnection GetConnection(Boundary boundary)
        {
            switch (boundary)
            {
                case Boundary.DepthMin:
                    return depthMinConnection;

                case Boundary.DepthMax:
                    return depthMaxConnection;

                case Boundary.SideLeft:
                    return sideLeftConnection;

                case Boundary.SideRight:
                    return sideRightConnection;
            }

            return null;
        }

        // =========================================================
        // MOVEMENT LENGTH
        // =========================================================

        public float GetMovementLength(Boundary boundary)
        {
            switch (boundary)
            {
                case Boundary.DepthMin:
                case Boundary.DepthMax:
                    return Mathf.Abs(maxDepth - minDepth);

                case Boundary.SideLeft:
                case Boundary.SideRight:
                    return Mathf.Abs(maxSide - minSide);
            }

            return 0f;
        }

        // =========================================================
        // DISTANCE TO BOUNDARY
        // =========================================================

        private float GetDistanceToBoundary(Vector3 localPosition, Boundary boundary)
        {
            switch (boundary)
            {
                case Boundary.DepthMin:
                    return localPosition.z - minDepth;

                case Boundary.DepthMax:
                    return maxDepth - localPosition.z;

                case Boundary.SideLeft:
                    return localPosition.x - minSide;

                case Boundary.SideRight:
                    return maxSide - localPosition.x;
            }

            return 0f;
        }

        // =========================================================
        // PAST SOURCE BOUNDARY
        // =========================================================

        private bool IsPastSourceBoundary(Vector3 localPosition, Boundary boundary)
        {
            switch (boundary)
            {
                case Boundary.DepthMin:
                    return localPosition.z < minDepth;

                case Boundary.DepthMax:
                    return localPosition.z > maxDepth;

                case Boundary.SideLeft:
                    return localPosition.x < minSide;

                case Boundary.SideRight:
                    return localPosition.x > maxSide;
            }

            return false;
        }

        // =========================================================
        // DISTANCE INSIDE FROM BOUNDARY
        // =========================================================

        public float GetDistanceInsideFromBoundary(Vector3 worldPosition, Boundary boundary)
        {
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);

            switch (boundary)
            {
                case Boundary.DepthMin:
                    return localPosition.z - minDepth;

                case Boundary.DepthMax:
                    return maxDepth - localPosition.z;

                case Boundary.SideLeft:
                    return localPosition.x - minSide;

                case Boundary.SideRight:
                    return maxSide - localPosition.x;
            }

            return 0f;
        }

        // =========================================================
        // BOUNDARY VALUE
        // =========================================================

        public float GetBoundaryValue(Boundary boundary)
        {
            switch (boundary)
            {
                case Boundary.DepthMin:
                    return minDepth;

                case Boundary.DepthMax:
                    return maxDepth;

                case Boundary.SideLeft:
                    return minSide;

                case Boundary.SideRight:
                    return maxSide;
            }

            return 0f;
        }

        // =========================================================
        // IS DEPTH BOUNDARY
        // =========================================================

        public bool IsDepthBoundary(Boundary boundary)
        {
            return boundary == Boundary.DepthMin || boundary == Boundary.DepthMax;
        }

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

        // =========================================================
        // VISUAL BOX
        // =========================================================

        private void DrawVisualBox()
        {
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            Vector3 center = visualBoxOffset;
            Vector3 size = visualBoxSize;

            if (showBoxSolid)
            {
                Gizmos.color = boxColor;
                Gizmos.DrawCube(center, size);
            }

            if (showBoxWire)
            {
                Gizmos.color = boxColor;
                Gizmos.DrawWireCube(center, size);
            }

            Gizmos.matrix = previousMatrix;

            DrawDepthLimits();
            DrawSideLimits();
        }

        // =========================================================
        // DEPTH LIMITS
        // =========================================================

        private void DrawDepthLimits()
        {
            float halfWidth = visualBoxSize.x * 0.5f;
            float halfHeight = visualBoxSize.y * 0.5f;

            Vector3 minCenter = new Vector3(visualBoxOffset.x, visualBoxOffset.y, minDepth);
            DrawLimitPlane(minCenter, halfWidth, halfHeight, useDepthMin ? limitColor : passableColor, false);

            Vector3 maxCenter = new Vector3(visualBoxOffset.x, visualBoxOffset.y, maxDepth);
            DrawLimitPlane(maxCenter, halfWidth, halfHeight, useDepthMax ? limitColor : passableColor, false);

#if UNITY_EDITOR

            if (showLabels)
            {
                Vector3 minWorld = transform.TransformPoint(minCenter);
                Vector3 maxWorld = transform.TransformPoint(maxCenter);

                Handles.Label(minWorld, useDepthMin ? $"Depth Min : {minDepth:F2} [LIMIT]" : $"Depth Min : {minDepth:F2} [PASSABLE]");
                Handles.Label(maxWorld, useDepthMax ? $"Depth Max : {maxDepth:F2} [LIMIT]" : $"Depth Max : {maxDepth:F2} [PASSABLE]");
            }

#endif
        }

        // =========================================================
        // SIDE LIMITS
        // =========================================================

        private void DrawSideLimits()
        {
            float halfDepth = visualBoxSize.z * 0.5f;
            float halfHeight = visualBoxSize.y * 0.5f;

            Vector3 leftCenter = new Vector3(minSide, visualBoxOffset.y, visualBoxOffset.z);
            DrawLimitPlane(leftCenter, halfDepth, halfHeight, useSideLeft ? limitColor : passableColor, true);

            Vector3 rightCenter = new Vector3(maxSide, visualBoxOffset.y, visualBoxOffset.z);
            DrawLimitPlane(rightCenter, halfDepth, halfHeight, useSideRight ? limitColor : passableColor, true);

#if UNITY_EDITOR

            if (showLabels)
            {
                Vector3 minWorld = transform.TransformPoint(leftCenter);
                Vector3 maxWorld = transform.TransformPoint(rightCenter);

                Handles.Label(minWorld, useSideLeft ? $"Side Left : {minSide:F2} [LIMIT]" : $"Side Left : {minSide:F2} [PASSABLE]");
                Handles.Label(maxWorld, useSideRight ? $"Side Right : {maxSide:F2} [LIMIT]" : $"Side Right : {maxSide:F2} [PASSABLE]");
            }

#endif
        }

        // =========================================================
        // LIMIT PLANE
        // =========================================================

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

        // =========================================================
        // BLEND DEBUG
        // =========================================================

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

            if (connection == null)
                return;

            if (!connection.BlendEnabled)
                return;

            MovementSpace target = connection.Target;

            if (target == null)
                return;

            float sourceLength = GetMovementLength(sourceBoundary);
            float sourceBlendDistance = sourceLength * Mathf.Abs(connection.BlendNormalizedFromSource);

            float targetLength = target.GetMovementLength(connection.TargetEntryBoundary);
            float targetBlendDistance = targetLength * connection.BlendNormalizedIntoTarget;

            Vector3 sourceBoundaryPoint = GetBoundaryWorldPoint(sourceBoundary);
            Vector3 blendStart = sourceBoundaryPoint + GetInsideDirection(sourceBoundary) * sourceBlendDistance;

            Vector3 targetBoundaryPoint = target.GetBoundaryWorldPoint(connection.TargetEntryBoundary);
            Vector3 blendEnd = targetBoundaryPoint + target.GetInsideDirection(connection.TargetEntryBoundary) * targetBlendDistance;

            Gizmos.color = blendDebugColor;

            Gizmos.DrawLine(blendStart, blendEnd);
            Gizmos.DrawSphere(blendStart, blendDebugPointRadius);
            Gizmos.DrawSphere(blendEnd, blendDebugPointRadius);
        }

        // =========================================================
        // BOUNDARY WORLD POINT
        // =========================================================

        private Vector3 GetBoundaryWorldPoint(Boundary boundary)
        {
            Vector3 localPoint = visualBoxOffset;

            switch (boundary)
            {
                case Boundary.DepthMin:
                    localPoint.z = minDepth;
                    break;

                case Boundary.DepthMax:
                    localPoint.z = maxDepth;
                    break;

                case Boundary.SideLeft:
                    localPoint.x = minSide;
                    break;

                case Boundary.SideRight:
                    localPoint.x = maxSide;
                    break;
            }

            return transform.TransformPoint(localPoint);
        }

        // =========================================================
        // INSIDE DIRECTION
        // =========================================================

        private Vector3 GetInsideDirection(Boundary boundary)
        {
            switch (boundary)
            {
                case Boundary.DepthMin:
                    return transform.forward;

                case Boundary.DepthMax:
                    return -transform.forward;

                case Boundary.SideLeft:
                    return transform.right;

                case Boundary.SideRight:
                    return -transform.right;
            }

            return Vector3.zero;
        }
    }

    // =============================================================
    // MOVEMENT SPACE CONNECTION
    // =============================================================

    [Serializable]
    public class MovementSpaceConnection
    {
        [SerializeField] private MovementSpace target;

        [Header("Blend")]
        [SerializeField] private bool blendEnabled = true;

        [Range(-1f, 0f)]
        [SerializeField] private float blendNormalizedFromSource = -0.25f;

        [Range(0f, 1f)]
        [SerializeField] private float blendNormalizedIntoTarget = 0.25f;

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

            if (blendCurve == null)
                blendCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }
    }
}

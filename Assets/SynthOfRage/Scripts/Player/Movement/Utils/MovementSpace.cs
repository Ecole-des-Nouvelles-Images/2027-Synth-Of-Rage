using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SynthOfRage.Scripts.Player
{
    public class MovementSpace : MonoBehaviour
    {
        // =========================================================
        // DEPTH LIMITS
        // =========================================================

        [Header("Depth Limits")]

        [Tooltip(
            "Active la limite minimale de profondeur."
        )]
        [SerializeField]
        private bool useDepthMin = true;

        [Tooltip(
            "Active la limite maximale de profondeur."
        )]
        [SerializeField]
        private bool useDepthMax = true;

        [Tooltip(
            "Position minimale sur l'axe local Z."
        )]
        [SerializeField]
        private float minDepth = -3.5f;

        [Tooltip(
            "Position maximale sur l'axe local Z."
        )]
        [SerializeField]
        private float maxDepth = 3.5f;


        // =========================================================
        // SIDE LIMITS
        // =========================================================

        [Header("Side Limits")]

        [Tooltip(
            "Active la limite gauche."
        )]
        [SerializeField]
        private bool useSideLeft = false;

        [Tooltip(
            "Active la limite droite."
        )]
        [SerializeField]
        private bool useSideRight = false;

        [Tooltip(
            "Position de la limite gauche sur l'axe local X."
        )]
        [SerializeField]
        private float minSide = -5f;

        [Tooltip(
            "Position de la limite droite sur l'axe local X."
        )]
        [SerializeField]
        private float maxSide = 5f;


        // =========================================================
        // CONNECTED MOVEMENT SPACES
        // =========================================================

        [Header("Connected Movement Spaces")]

        [Tooltip(
            "Movement Space vers lequel le joueur est transféré " +
            "lorsqu'il franchit la limite Depth Min. " +
            "La limite doit être passable."
        )]
        [SerializeField]
        private MovementSpace depthMinConnection;

        [Tooltip(
            "Movement Space vers lequel le joueur est transféré " +
            "lorsqu'il franchit la limite Depth Max. " +
            "La limite doit être passable."
        )]
        [SerializeField]
        private MovementSpace depthMaxConnection;

        [Tooltip(
            "Movement Space vers lequel le joueur est transféré " +
            "lorsqu'il franchit la limite Side Left. " +
            "La limite doit être passable."
        )]
        [SerializeField]
        private MovementSpace sideLeftConnection;

        [Tooltip(
            "Movement Space vers lequel le joueur est transféré " +
            "lorsqu'il franchit la limite Side Right. " +
            "La limite doit être passable."
        )]
        [SerializeField]
        private MovementSpace sideRightConnection;


        // =========================================================
        // VISUAL BOX
        // =========================================================

        [Header("Visual Box")]

        [Tooltip(
            "Affiche la boîte purement visuelle dans la Scene View."
        )]
        [SerializeField]
        private bool showVisualBox = true;

        [Tooltip(
            "Dimensions X/Y/Z de la boîte visuelle."
        )]
        [SerializeField]
        private Vector3 visualBoxSize =
            new Vector3(10f, 2f, 7f);

        [Tooltip(
            "Décalage local de la boîte par rapport au Movement Space."
        )]
        [SerializeField]
        private Vector3 visualBoxOffset =
            new Vector3(0f, 1f, 0f);

        [Tooltip(
            "Affiche la boîte en filaire."
        )]
        [SerializeField]
        private bool showBoxWire = true;

        [Tooltip(
            "Affiche une surface semi-transparente."
        )]
        [SerializeField]
        private bool showBoxSolid = false;


        // =========================================================
        // VISUAL COLORS
        // =========================================================

        [Header("Visual Colors")]

        [Tooltip(
            "Couleur d'une limite réellement bloquante."
        )]
        [SerializeField]
        private Color limitColor =
            new Color(1f, 0f, 0f, 1f);

        [Tooltip(
            "Couleur d'une limite désactivée."
        )]
        [SerializeField]
        private Color passableColor =
            new Color(0f, 1f, 0f, 1f);

        [Tooltip(
            "Couleur de la boîte visuelle."
        )]
        [SerializeField]
        private Color boxColor =
            new Color(1f, 1f, 1f, 0.15f);


        // =========================================================
        // VISUAL LINES
        // =========================================================

        [Header("Visual Lines")]

        [Tooltip(
            "Épaisseur visuelle des lignes de limites."
        )]
        [Min(0.001f)]
        [SerializeField]
        private float lineWidth = 0.05f;

        [Tooltip(
            "Longueur des traits dépassant de la boîte."
        )]
        [Min(0f)]
        [SerializeField]
        private float lineExtension = 0.15f;

        [Tooltip(
            "Affiche les labels dans la Scene View."
        )]
        [SerializeField]
        private bool showLabels = true;


        // =========================================================
        // PUBLIC API
        // =========================================================

        public bool UseDepthMin =>
            useDepthMin;

        public bool UseDepthMax =>
            useDepthMax;

        public float MinDepth =>
            minDepth;

        public float MaxDepth =>
            maxDepth;


        public bool UseSideLeft =>
            useSideLeft;

        public bool UseSideRight =>
            useSideRight;

        public float MinSide =>
            minSide;

        public float MaxSide =>
            maxSide;


        // ---------------------------------------------------------
        // CONNECTIONS
        // ---------------------------------------------------------

        public MovementSpace DepthMinConnection =>
            depthMinConnection;

        public MovementSpace DepthMaxConnection =>
            depthMaxConnection;

        public MovementSpace SideLeftConnection =>
            sideLeftConnection;

        public MovementSpace SideRightConnection =>
            sideRightConnection;


        // =========================================================
        // VALIDATION
        // =========================================================

        private void OnValidate()
        {
            if (minDepth > maxDepth)
            {
                float temporary =
                    minDepth;

                minDepth =
                    maxDepth;

                maxDepth =
                    temporary;
            }

            if (minSide > maxSide)
            {
                float temporary =
                    minSide;

                minSide =
                    maxSide;

                maxSide =
                    temporary;
            }

            visualBoxSize.x =
                Mathf.Max(
                    0.01f,
                    visualBoxSize.x
                );

            visualBoxSize.y =
                Mathf.Max(
                    0.01f,
                    visualBoxSize.y
                );

            visualBoxSize.z =
                Mathf.Max(
                    0.01f,
                    visualBoxSize.z
                );

            lineWidth =
                Mathf.Max(
                    0.001f,
                    lineWidth
                );

            lineExtension =
                Mathf.Max(
                    0f,
                    lineExtension
                );
        }


        // =========================================================
        // GIZMOS
        // =========================================================

        private void OnDrawGizmos()
        {
            if (!showVisualBox)
                return;

            DrawVisualBox();

            DrawDepthLimits();

            DrawSideLimits();
        }


        // =========================================================
        // VISUAL BOX
        // =========================================================

        private void DrawVisualBox()
        {
            Matrix4x4 previousMatrix =
                Gizmos.matrix;

            Gizmos.matrix =
                Matrix4x4.TRS(
                    transform.TransformPoint(
                        visualBoxOffset
                    ),
                    transform.rotation,
                    Vector3.one
                );

            if (showBoxSolid)
            {
                Color solidColor =
                    boxColor;

                solidColor.a =
                    Mathf.Clamp01(
                        boxColor.a
                    );

                Gizmos.color =
                    solidColor;

                Gizmos.DrawCube(
                    Vector3.zero,
                    visualBoxSize
                );
            }

            if (showBoxWire)
            {
                Color wireColor =
                    boxColor;

                wireColor.a =
                    1f;

                Gizmos.color =
                    wireColor;

                Gizmos.DrawWireCube(
                    Vector3.zero,
                    visualBoxSize
                );
            }

            Gizmos.matrix =
                previousMatrix;
        }


        // =========================================================
        // DEPTH LIMITS
        // =========================================================

        private void DrawDepthLimits()
        {
            float halfWidth =
                visualBoxSize.x * 0.5f;

            float halfHeight =
                visualBoxSize.y * 0.5f;


            // -----------------------------------------------------
            // DEPTH MIN
            // -----------------------------------------------------

            Vector3 minDepthCenter =
                new Vector3(
                    visualBoxOffset.x,
                    visualBoxOffset.y,
                    minDepth
                );

            Color minDepthColor =
                useDepthMin
                    ? limitColor
                    : passableColor;

            DrawLimitPlane(
                minDepthCenter,
                halfWidth,
                halfHeight,
                minDepthColor,
                false
            );


            // -----------------------------------------------------
            // DEPTH MAX
            // -----------------------------------------------------

            Vector3 maxDepthCenter =
                new Vector3(
                    visualBoxOffset.x,
                    visualBoxOffset.y,
                    maxDepth
                );

            Color maxDepthColor =
                useDepthMax
                    ? limitColor
                    : passableColor;

            DrawLimitPlane(
                maxDepthCenter,
                halfWidth,
                halfHeight,
                maxDepthColor,
                false
            );


#if UNITY_EDITOR

            if (showLabels)
            {
                Vector3 minWorld =
                    transform.TransformPoint(
                        minDepthCenter
                    );

                Vector3 maxWorld =
                    transform.TransformPoint(
                        maxDepthCenter
                    );

                Handles.color =
                    minDepthColor;

                Handles.Label(
                    minWorld,
                    useDepthMin
                        ? $"Depth Min : {minDepth:F2} [LIMIT]"
                        : $"Depth Min : {minDepth:F2} [PASSABLE]"
                );

                Handles.color =
                    maxDepthColor;

                Handles.Label(
                    maxWorld,
                    useDepthMax
                        ? $"Depth Max : {maxDepth:F2} [LIMIT]"
                        : $"Depth Max : {maxDepth:F2} [PASSABLE]"
                );
            }

#endif
        }


        // =========================================================
        // SIDE LIMITS
        // =========================================================

        private void DrawSideLimits()
        {
            float halfDepth =
                visualBoxSize.z * 0.5f;

            float halfHeight =
                visualBoxSize.y * 0.5f;


            // -----------------------------------------------------
            // SIDE LEFT
            // -----------------------------------------------------

            Vector3 minSideCenter =
                new Vector3(
                    minSide,
                    visualBoxOffset.y,
                    visualBoxOffset.z
                );

            Color leftColor =
                useSideLeft
                    ? limitColor
                    : passableColor;

            DrawLimitPlane(
                minSideCenter,
                halfDepth,
                halfHeight,
                leftColor,
                true
            );


            // -----------------------------------------------------
            // SIDE RIGHT
            // -----------------------------------------------------

            Vector3 maxSideCenter =
                new Vector3(
                    maxSide,
                    visualBoxOffset.y,
                    visualBoxOffset.z
                );

            Color rightColor =
                useSideRight
                    ? limitColor
                    : passableColor;

            DrawLimitPlane(
                maxSideCenter,
                halfDepth,
                halfHeight,
                rightColor,
                true
            );


#if UNITY_EDITOR

            if (showLabels)
            {
                Vector3 minWorld =
                    transform.TransformPoint(
                        minSideCenter
                    );

                Vector3 maxWorld =
                    transform.TransformPoint(
                        maxSideCenter
                    );

                Handles.color =
                    leftColor;

                Handles.Label(
                    minWorld,
                    useSideLeft
                        ? $"Side Left : {minSide:F2} [LIMIT]"
                        : $"Side Left : {minSide:F2} [PASSABLE]"
                );

                Handles.color =
                    rightColor;

                Handles.Label(
                    maxWorld,
                    useSideRight
                        ? $"Side Right : {maxSide:F2} [LIMIT]"
                        : $"Side Right : {maxSide:F2} [PASSABLE]"
                );
            }

#endif
        }


        // =========================================================
        // LIMIT PLANE
        // =========================================================

        private void DrawLimitPlane(
            Vector3 center,
            float halfHorizontal,
            float halfVertical,
            Color color,
            bool horizontalIsDepth
        )
        {
            Vector3 horizontalAxis =
                horizontalIsDepth
                    ? transform.forward
                    : transform.right;

            Vector3 verticalAxis =
                transform.up;

            Vector3 worldCenter =
                transform.TransformPoint(
                    center
                );

            Vector3 bottomLeft =
                worldCenter
                - horizontalAxis * halfHorizontal
                - verticalAxis * halfVertical;

            Vector3 bottomRight =
                worldCenter
                + horizontalAxis * halfHorizontal
                - verticalAxis * halfVertical;

            Vector3 topLeft =
                worldCenter
                - horizontalAxis * halfHorizontal
                + verticalAxis * halfVertical;

            Vector3 topRight =
                worldCenter
                + horizontalAxis * halfHorizontal
                + verticalAxis * halfVertical;

            Gizmos.color =
                color;

            Gizmos.DrawLine(
                bottomLeft,
                bottomRight
            );

            Gizmos.DrawLine(
                bottomRight,
                topRight
            );

            Gizmos.DrawLine(
                topRight,
                topLeft
            );

            Gizmos.DrawLine(
                topLeft,
                bottomLeft
            );
        }
    }
}
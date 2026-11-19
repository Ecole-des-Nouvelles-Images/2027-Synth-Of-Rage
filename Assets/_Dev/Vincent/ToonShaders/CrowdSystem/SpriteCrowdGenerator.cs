using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _Dev.Vincent.ToonShaders
{
    /// <summary>
    /// Surface-sampled Sprite3D crowd generator.
    /// Instances are grouped by sprite variant and spatial cell. Each cell is
    /// one combined mesh, allowing renderer/frustum culling at chunk granularity.
    /// Breathing and billboard vertices are updated only for visible chunks.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    public sealed class SpriteCrowdGenerator : MonoBehaviour
    {
        [Serializable]
        public class SpriteVariant
        {
            public string name = "Sprite";
            public Sprite sprite;
            [Tooltip("Material using Custom/SOR/Sprite3D.")]
            public Material material;
            [Min(0f)] public float weight = 1f;
        }

        [Serializable]
        public class ScaleVariation
        {
            public string name = "Default";
            [Min(0.001f)] public float min = 1f;
            [Min(0.001f)] public float max = 1f;
        }

        [Serializable]
        public class ColorVariation
        {
            public string name = "White";
            public Color color = Color.white;
            [Min(0f)] public float weight = 1f;
        }

        private struct SurfaceTriangle
        {
            public Vector3 a;
            public Vector3 b;
            public Vector3 c;
            public Vector3 normal;
            public float cumulativeArea;
        }

        private struct SpriteInstance
        {
            public Vector3 basePosition;
            public Vector2 halfSize;
            public Color tint;
            public float phase;
        }

        private struct ChunkKey : IEquatable<ChunkKey>
        {
            public int variant;
            public Vector3Int cell;

            public bool Equals(ChunkKey other)
            {
                return variant == other.variant && cell.Equals(other.cell);
            }

            public override bool Equals(object obj)
            {
                return obj is ChunkKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = variant;
                    hash = (hash * 397) ^ cell.x;
                    hash = (hash * 397) ^ cell.y;
                    hash = (hash * 397) ^ cell.z;
                    return hash;
                }
            }
        }

        private sealed class CrowdChunk
        {
            public int variantIndex;
            public List<SpriteInstance> instances = new List<SpriteInstance>(64);
            public Mesh mesh;
            public MeshRenderer renderer;
            public Vector3[] vertices;
            public Bounds localBounds;
            public bool wasVisible;
        }

        [Header("Distribution")]
        [SerializeField, Min(0)] private int instanceCount = 1000;
        [SerializeField] private int seed = 12345;
        [SerializeField, Min(0.25f)] private float chunkSize = 8f;
        [SerializeField, Min(0f)] private float surfaceOffset = 0.005f;
        [SerializeField] private bool regenerateOnEnable = true;

        [Header("Sprite Library")]
        [SerializeField] private List<SpriteVariant> sprites = new List<SpriteVariant>();

        [Header("Scale Variations")]
        [SerializeField] private List<ScaleVariation> scales = new List<ScaleVariation>
        {
            new ScaleVariation { name = "Small", min = 0.75f, max = 0.9f },
            new ScaleVariation { name = "Medium", min = 0.9f, max = 1.1f },
            new ScaleVariation { name = "Large", min = 1.1f, max = 1.3f }
        };

        [Header("Color Variations")]
        [SerializeField] private List<ColorVariation> colors = new List<ColorVariation>
        {
            new ColorVariation { name = "Natural", color = Color.white, weight = 1f },
            new ColorVariation { name = "Light", color = new Color(0.9f, 1f, 0.88f, 1f), weight = 1f },
            new ColorVariation { name = "Warm", color = new Color(1f, 0.9f, 0.75f, 1f), weight = 1f }
        };

        [Header("Breathing")]
        [SerializeField] private bool breathingEnabled = true;
        [SerializeField, Range(0f, 0.2f)] private float breathingIntensity = 0.035f;
        [SerializeField, Range(0f, 10f)] private float breathingSpeed = 1.8f;
        [SerializeField] private Vector2 breathingDirection = new Vector2(0.15f, 1f);
        [SerializeField] private bool randomizeBreathingPhase = true;

        [Header("Billboard")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool billboard = true;
        [SerializeField] private bool useCameraRoll = true;
        [SerializeField] private bool useWorldUpWhenNoCameraRoll = true;

        [Header("Culling")]
        [SerializeField] private bool frustumCulling = true;
        [SerializeField, Min(0f)] private float maxVisibleDistance = 0f;
        [Tooltip("Additional local-space padding around each chunk's bounds.")]
        [SerializeField, Min(0f)] private float cullingBoundsPadding = 0.25f;

        [Header("Sprite3D Outline")]
        [SerializeField] private bool crowdOutlineEnabled = true;
        [SerializeField] private bool crowdAffectOtherOutlines = true;

        [Header("Debug - Chunk Gizmos")]
        [SerializeField] private bool drawChunkBounds = true;
        [SerializeField] private bool drawChunkBoundsOnlyWhenSelected = true;
        [SerializeField] private Color visibleChunkColor = new Color(0.1f, 1f, 0.2f, 0.9f);
        [SerializeField] private Color hiddenChunkColor = new Color(1f, 0.35f, 0.1f, 0.9f);
        [SerializeField] private Color chunkCenterColor = Color.white;

        [Header("Generated Objects")]
        [SerializeField] private string generatedRootName = "__GeneratedSpriteCrowd";

        private readonly List<CrowdChunk> chunks = new List<CrowdChunk>(32);
        private readonly Dictionary<ChunkKey, CrowdChunk> chunkLookup =
            new Dictionary<ChunkKey, CrowdChunk>(32);
        private readonly Plane[] frustumPlanes = new Plane[6];

        private Transform generatedRoot;
        private SurfaceTriangle[] surfaceTriangles;
        private float totalSurfaceArea;
        private bool isGenerating;

#if UNITY_EDITOR
        private void OnEnableEditorCleanup()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private void OnDisableEditorCleanup()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        }

        private void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                ClearGeneratedImmediate();
        }
#endif

        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BreathingEnabledId = Shader.PropertyToID("_BreathingEnabled");

        private void OnEnable()
        {
#if UNITY_EDITOR
            OnEnableEditorCleanup();
#endif
            if (regenerateOnEnable)
                Regenerate();
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            OnDisableEditorCleanup();
#endif
            // OnDisable can run while Unity is still transitioning out of Play Mode.
            // Always clear generated objects; use immediate destruction in the Editor.
            ClearGeneratedImmediate();
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            OnDisableEditorCleanup();
#endif
            ClearGeneratedImmediate();
        }

        private void LateUpdate()
        {
            if (isGenerating || chunks.Count == 0)
                return;

            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            Vector3 cameraPosition = cam != null ? cam.transform.position : transform.position;

            // Edit Mode is a preview mode: never hide generated chunks there.
            // Frustum and distance culling are runtime-only optimizations.
            bool runtimeCulling = Application.isPlaying;
            bool canFrustumCull = runtimeCulling && frustumCulling && cam != null;
            if (canFrustumCull)
                GeometryUtility.CalculateFrustumPlanes(cam, frustumPlanes);

            Vector3 rightWorld;
            Vector3 upWorld;
            GetBillboardAxes(cam, out rightWorld, out upWorld);

            Vector3 rightLocal = transform.InverseTransformVector(rightWorld);
            Vector3 upLocal = transform.InverseTransformVector(upWorld);
            float time = Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartup;

            for (int i = 0; i < chunks.Count; i++)
            {
                CrowdChunk chunk = chunks[i];
                Bounds worldBounds = TransformBounds(transform.localToWorldMatrix, chunk.localBounds);

                bool visible = true;

                if (canFrustumCull)
                    visible = GeometryUtility.TestPlanesAABB(frustumPlanes, worldBounds);

                if (runtimeCulling && visible && maxVisibleDistance > 0f)
                {
                    float distanceSqr = (worldBounds.ClosestPoint(cameraPosition) - cameraPosition).sqrMagnitude;
                    visible = distanceSqr <= maxVisibleDistance * maxVisibleDistance;
                }

                // Keep every generated renderer enabled in Edit Mode so the
                // complete crowd remains visible while arranging the scene.
                bool shouldEnableRenderer = !runtimeCulling || visible;
                if (chunk.renderer != null && chunk.renderer.enabled != shouldEnableRenderer)
                    chunk.renderer.enabled = shouldEnableRenderer;

                if (!visible)
                {
                    chunk.wasVisible = false;
                    continue;
                }

                // Billboard orientation changes only when required. Breathing
                // still animates visible chunks, never off-screen chunks.
                UpdateChunkVertices(chunk, rightLocal, upLocal, time);
                chunk.wasVisible = true;
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawChunkBounds || drawChunkBoundsOnlyWhenSelected)
                return;

            DrawChunkGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawChunkBounds || !drawChunkBoundsOnlyWhenSelected)
                return;

            DrawChunkGizmos();
        }

        private void DrawChunkGizmos()
        {
            if (chunks == null || chunks.Count == 0)
                return;

            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            Gizmos.matrix = Matrix4x4.identity;

            for (int i = 0; i < chunks.Count; i++)
            {
                CrowdChunk chunk = chunks[i];
                if (chunk == null)
                    continue;

                Bounds worldBounds = TransformBounds(transform.localToWorldMatrix, chunk.localBounds);
                bool visible = !Application.isPlaying || chunk.wasVisible;
                Gizmos.color = visible ? visibleChunkColor : hiddenChunkColor;
                Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);

                Gizmos.color = chunkCenterColor;
                Gizmos.DrawSphere(worldBounds.center, 0.06f);
            }

            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }

        /// <summary>
        /// Controls the Sprite3D outline on every generated chunk.
        /// All sprites inside a combined chunk share the same outline state.
        /// </summary>
        public void SetCrowdOutlineEnabled(bool enabled)
        {
            crowdOutlineEnabled = enabled;
            for (int i = 0; i < chunks.Count; i++)
            {
                CrowdChunk chunk = chunks[i];
                if (chunk == null || chunk.renderer == null)
                    continue;

                Sprite3DOutlineToggle toggle = chunk.renderer.GetComponent<Sprite3DOutlineToggle>();
                if (toggle != null)
                    toggle.SetOutlineEnabled(enabled);
            }
        }

        public void EnableCrowdOutline() => SetCrowdOutlineEnabled(true);
        public void DisableCrowdOutline() => SetCrowdOutlineEnabled(false);

        public void SetCrowdAffectOtherOutlines(bool enabled)
        {
            crowdAffectOtherOutlines = enabled;
            for (int i = 0; i < chunks.Count; i++)
            {
                CrowdChunk chunk = chunks[i];
                if (chunk == null || chunk.renderer == null)
                    continue;

                Sprite3DOutlineToggle toggle = chunk.renderer.GetComponent<Sprite3DOutlineToggle>();
                if (toggle != null)
                    toggle.SetAffectOtherOutlines(enabled);
            }
        }

        private void GetBillboardAxes(Camera cam, out Vector3 right, out Vector3 up)
        {
            if (!billboard || cam == null)
            {
                right = transform.right;
                up = transform.up;
                return;
            }

            if (useCameraRoll)
            {
                right = cam.transform.right;
                up = cam.transform.up;
                return;
            }

            Vector3 forward = cam.transform.forward;
            Vector3 referenceUp = useWorldUpWhenNoCameraRoll ? Vector3.up : transform.up;

            right = Vector3.Cross(referenceUp, forward);
            if (right.sqrMagnitude < 0.000001f)
                right = cam.transform.right;
            else
                right.Normalize();

            up = Vector3.Cross(forward, right).normalized;
        }

        private void UpdateChunkVertices(
            CrowdChunk chunk,
            Vector3 right,
            Vector3 up,
            float time)
        {
            int vertex = 0;

            for (int i = 0; i < chunk.instances.Count; i++)
            {
                SpriteInstance instance = chunk.instances[i];

                float pulse = breathingEnabled
                    ? Mathf.Sin(time * breathingSpeed + instance.phase) * breathingIntensity
                    : 0f;

                float widthScale = Mathf.Max(0.01f, 1f + pulse * breathingDirection.x);
                float heightScale = Mathf.Max(0.01f, 1f + pulse * breathingDirection.y);

                float halfWidth = instance.halfSize.x * widthScale;
                float height = instance.halfSize.y * heightScale;

                Vector3 left = instance.basePosition - right * halfWidth;
                Vector3 rightBase = instance.basePosition + right * halfWidth;

                chunk.vertices[vertex++] = left;
                chunk.vertices[vertex++] = left + up * height;
                chunk.vertices[vertex++] = rightBase + up * height;
                chunk.vertices[vertex++] = rightBase;
            }

            // Reuse the preallocated array; no per-frame mesh or vertex arrays.
            chunk.mesh.SetVertices(chunk.vertices);
        }

        [ContextMenu("Regenerate Crowd")]
        public void Regenerate()
        {
            if (isGenerating)
                return;

            isGenerating = true;
            try
            {
                ClearGeneratedInternal();

                Mesh surface = GetComponent<MeshFilter>().sharedMesh;
                if (surface == null || instanceCount <= 0 || !HasValidVariant())
                    return;

                if (!BuildSurfaceTriangleCache(surface))
                {
                    Debug.LogWarning(
                        "[SpriteCrowdGenerator] The source mesh contains no valid triangle surface.",
                        this);
                    return;
                }

                generatedRoot = new GameObject(generatedRootName).transform;
                generatedRoot.gameObject.hideFlags = HideFlags.DontSaveInEditor;
                generatedRoot.SetParent(transform, false);
                generatedRoot.localPosition = Vector3.zero;
                generatedRoot.localRotation = Quaternion.identity;
                generatedRoot.localScale = Vector3.one;
                generatedRoot.gameObject.layer = gameObject.layer;

                System.Random random = new System.Random(seed);

                for (int i = 0; i < instanceCount; i++)
                {
                    Vector3 localPosition;
                    Vector3 worldNormal;
                    SampleSurface(random, out localPosition, out worldNormal);

                    int variantIndex = PickSpriteIndex(random);
                    if (variantIndex < 0)
                        continue;

                    SpriteVariant variant = sprites[variantIndex];
                    float scale = PickScale(random);
                    float pixelsPerUnit = Mathf.Max(0.0001f, variant.sprite.pixelsPerUnit);
                    float width = variant.sprite.rect.width / pixelsPerUnit * scale;
                    float height = variant.sprite.rect.height / pixelsPerUnit * scale;

                    Vector3 worldPosition = transform.TransformPoint(localPosition);
                    worldPosition += worldNormal * surfaceOffset;
                    localPosition = transform.InverseTransformPoint(worldPosition);

                    Vector3Int cell = new Vector3Int(
                        Mathf.FloorToInt(localPosition.x / chunkSize),
                        Mathf.FloorToInt(localPosition.y / chunkSize),
                        Mathf.FloorToInt(localPosition.z / chunkSize)
                    );

                    ChunkKey key = new ChunkKey { variant = variantIndex, cell = cell };
                    if (!chunkLookup.TryGetValue(key, out CrowdChunk chunk))
                    {
                        chunk = CreateChunk(variantIndex);
                        chunkLookup.Add(key, chunk);
                        chunks.Add(chunk);
                    }

                    chunk.instances.Add(new SpriteInstance
                    {
                        basePosition = localPosition,
                        halfSize = new Vector2(width * 0.5f, height),
                        tint = PickColor(random),
                        phase = randomizeBreathingPhase
                            ? (float)random.NextDouble() * Mathf.PI * 2f
                            : 0f
                    });
                }

                for (int i = 0; i < chunks.Count; i++)
                    BuildChunkMesh(chunks[i]);
            }
            finally
            {
                surfaceTriangles = null;
                totalSurfaceArea = 0f;
                isGenerating = false;
            }
        }

        private bool BuildSurfaceTriangleCache(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            List<SurfaceTriangle> triangles = new List<SurfaceTriangle>(mesh.triangles.Length / 3);
            totalSurfaceArea = 0f;

            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                if (mesh.GetTopology(subMesh) != MeshTopology.Triangles)
                    continue;

                int[] indices = mesh.GetIndices(subMesh);
                for (int i = 0; i + 2 < indices.Length; i += 3)
                {
                    int ia = indices[i];
                    int ib = indices[i + 1];
                    int ic = indices[i + 2];

                    if (ia >= vertices.Length || ib >= vertices.Length || ic >= vertices.Length)
                        continue;

                    Vector3 a = vertices[ia];
                    Vector3 b = vertices[ib];
                    Vector3 c = vertices[ic];

                    Vector3 worldA = transform.TransformPoint(a);
                    Vector3 worldB = transform.TransformPoint(b);
                    Vector3 worldC = transform.TransformPoint(c);
                    Vector3 cross = Vector3.Cross(worldB - worldA, worldC - worldA);
                    float area = cross.magnitude * 0.5f;

                    if (area <= 0.000001f)
                        continue;

                    totalSurfaceArea += area;
                    triangles.Add(new SurfaceTriangle
                    {
                        a = a,
                        b = b,
                        c = c,
                        // Store a local-space normal; SampleSurface converts it
                        // with the inverse-transpose for non-uniform transforms.
                        normal = Vector3.Cross(b - a, c - a).normalized,
                        cumulativeArea = totalSurfaceArea
                    });
                }
            }

            surfaceTriangles = triangles.ToArray();
            return surfaceTriangles.Length > 0 && totalSurfaceArea > 0f;
        }

        private void SampleSurface(
            System.Random random,
            out Vector3 localPosition,
            out Vector3 worldNormal)
        {
            float sample = (float)random.NextDouble() * totalSurfaceArea;

            int low = 0;
            int high = surfaceTriangles.Length - 1;
            while (low < high)
            {
                int middle = (low + high) >> 1;
                if (sample <= surfaceTriangles[middle].cumulativeArea)
                    high = middle;
                else
                    low = middle + 1;
            }

            SurfaceTriangle triangle = surfaceTriangles[low];

            // Square-root barycentric sampling is uniform over triangle area.
            float root = Mathf.Sqrt((float)random.NextDouble());
            float v = (float)random.NextDouble();
            float wa = 1f - root;
            float wb = root * (1f - v);
            float wc = root * v;

            localPosition =
                triangle.a * wa +
                triangle.b * wb +
                triangle.c * wc;

            // Convert the triangle normal correctly when the source transform
            // contains non-uniform scale.
            worldNormal = transform.localToWorldMatrix.inverse.transpose
                .MultiplyVector(triangle.normal).normalized;
        }

        private CrowdChunk CreateChunk(int variantIndex)
        {
            SpriteVariant variant = sprites[variantIndex];

            GameObject go = new GameObject(
                "Crowd_" + variant.name + "_" + chunks.Count.ToString("D3"));
            go.layer = gameObject.layer;
            go.hideFlags = HideFlags.DontSaveInEditor;
            go.transform.SetParent(generatedRoot, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            MeshFilter filter = go.AddComponent<MeshFilter>();
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = variant.material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetTexture(MainTexId, variant.sprite.texture);
            block.SetColor(ColorId, Color.white);
            block.SetFloat(BreathingEnabledId, 0f);
            renderer.SetPropertyBlock(block);

            // One combined renderer per chunk: this toggle affects every sprite in it.
            Sprite3DOutlineToggle outlineToggle = go.AddComponent<Sprite3DOutlineToggle>();
            outlineToggle.SetAffectOtherOutlines(crowdAffectOtherOutlines);
            outlineToggle.SetOutlineEnabled(crowdOutlineEnabled);

            return new CrowdChunk
            {
                variantIndex = variantIndex,
                mesh = new Mesh
                {
                    name = go.name + "_Mesh",
                    indexFormat = IndexFormat.UInt32,
                    hideFlags = HideFlags.DontSaveInEditor
                },
                renderer = renderer
            };
        }

        private void BuildChunkMesh(CrowdChunk chunk)
        {
            int count = chunk.instances.Count;
            int vertexCount = count * 4;
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            Color[] vertexColors = new Color[vertexCount];
            int[] indices = new int[count * 6];

            Sprite sprite = sprites[chunk.variantIndex].sprite;
            Rect rect = sprite.textureRect;
            Texture texture = sprite.texture;

            float u0 = rect.xMin / texture.width;
            float u1 = rect.xMax / texture.width;
            float v0 = rect.yMin / texture.height;
            float v1 = rect.yMax / texture.height;

            Bounds localBounds = new Bounds(chunk.instances[0].basePosition, Vector3.zero);

            for (int i = 0; i < count; i++)
            {
                int v = i * 4;
                int t = i * 6;
                SpriteInstance instance = chunk.instances[i];

                // Seed the quad; LateUpdate will orient it to the active camera.
                vertices[v] = instance.basePosition;
                vertices[v + 1] = instance.basePosition;
                vertices[v + 2] = instance.basePosition;
                vertices[v + 3] = instance.basePosition;

                uvs[v] = new Vector2(u0, v0);
                uvs[v + 1] = new Vector2(u0, v1);
                uvs[v + 2] = new Vector2(u1, v1);
                uvs[v + 3] = new Vector2(u1, v0);

                vertexColors[v] = instance.tint;
                vertexColors[v + 1] = instance.tint;
                vertexColors[v + 2] = instance.tint;
                vertexColors[v + 3] = instance.tint;

                indices[t] = v;
                indices[t + 1] = v + 1;
                indices[t + 2] = v + 2;
                indices[t + 3] = v;
                indices[t + 4] = v + 2;
                indices[t + 5] = v + 3;

                // A sphere around the planting point is conservative for every
                // billboard orientation, including camera roll and side views.
                float radius = Mathf.Sqrt(
                    instance.halfSize.x * instance.halfSize.x +
                    instance.halfSize.y * instance.halfSize.y
                ) + cullingBoundsPadding;

                Bounds spriteBounds = new Bounds(
                    instance.basePosition,
                    Vector3.one * (radius * 2f)
                );
                localBounds.Encapsulate(spriteBounds);
            }

            localBounds.Expand(cullingBoundsPadding * 2f);
            chunk.localBounds = localBounds;
            chunk.vertices = vertices;

            chunk.mesh.Clear();
            chunk.mesh.indexFormat = IndexFormat.UInt32;
            chunk.mesh.vertices = vertices;
            chunk.mesh.uv = uvs;
            chunk.mesh.colors = vertexColors;
            chunk.mesh.SetIndices(indices, MeshTopology.Triangles, 0, false);
            chunk.mesh.bounds = localBounds;
            chunk.mesh.MarkDynamic();

            chunk.renderer.GetComponent<MeshFilter>().sharedMesh = chunk.mesh;
            chunk.renderer.localBounds = localBounds;
        }

        private static Bounds TransformBounds(Matrix4x4 matrix, Bounds localBounds)
        {
            Vector3 center = matrix.MultiplyPoint3x4(localBounds.center);
            Vector3 extents = localBounds.extents;

            Vector3 axisX = matrix.MultiplyVector(new Vector3(extents.x, 0f, 0f));
            Vector3 axisY = matrix.MultiplyVector(new Vector3(0f, extents.y, 0f));
            Vector3 axisZ = matrix.MultiplyVector(new Vector3(0f, 0f, extents.z));

            Vector3 worldExtents = new Vector3(
                Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
                Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
                Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z)
            );

            return new Bounds(center, worldExtents * 2f);
        }

        private bool HasValidVariant()
        {
            for (int i = 0; i < sprites.Count; i++)
            {
                SpriteVariant entry = sprites[i];
                if (entry != null && entry.sprite != null &&
                    entry.material != null && entry.weight > 0f)
                    return true;
            }

            return false;
        }

        private int PickSpriteIndex(System.Random random)
        {
            float total = 0f;

            for (int i = 0; i < sprites.Count; i++)
            {
                SpriteVariant entry = sprites[i];
                if (entry != null && entry.sprite != null && entry.material != null)
                    total += Mathf.Max(0f, entry.weight);
            }

            if (total <= 0f)
                return -1;

            float value = (float)random.NextDouble() * total;

            for (int i = 0; i < sprites.Count; i++)
            {
                SpriteVariant entry = sprites[i];
                if (entry == null || entry.sprite == null || entry.material == null)
                    continue;

                value -= Mathf.Max(0f, entry.weight);
                if (value <= 0f)
                    return i;
            }

            return -1;
        }

        private float PickScale(System.Random random)
        {
            if (scales == null || scales.Count == 0)
                return 1f;

            ScaleVariation entry = scales[random.Next(scales.Count)];
            if (entry == null)
                return 1f;

            float min = Mathf.Max(0.001f, Mathf.Min(entry.min, entry.max));
            float max = Mathf.Max(min, Mathf.Max(entry.min, entry.max));
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }

        private Color PickColor(System.Random random)
        {
            if (colors == null || colors.Count == 0)
                return Color.white;

            float total = 0f;
            for (int i = 0; i < colors.Count; i++)
                if (colors[i] != null)
                    total += Mathf.Max(0f, colors[i].weight);

            if (total <= 0f)
                return Color.white;

            float value = (float)random.NextDouble() * total;
            for (int i = 0; i < colors.Count; i++)
            {
                ColorVariation entry = colors[i];
                if (entry == null)
                    continue;

                value -= Mathf.Max(0f, entry.weight);
                if (value <= 0f)
                    return entry.color;
            }

            return Color.white;
        }

        [ContextMenu("Clear Generated Crowd")]
        public void ClearGenerated()
        {
            if (isGenerating)
                return;

            ClearGeneratedInternal();
        }

        private void ClearGeneratedInternal()
        {
            ClearGeneratedObjects(Application.isPlaying);
        }

        private void ClearGeneratedImmediate()
        {
            ClearGeneratedObjects(true);
        }

        private void ClearGeneratedObjects(bool immediate)
        {
            // Recover the root after a domain reload or if the cached Transform was lost.
            if (generatedRoot == null)
            {
                Transform existing = transform.Find(generatedRootName);
                if (existing != null)
                    generatedRoot = existing;
            }

            for (int i = 0; i < chunks.Count; i++)
            {
                CrowdChunk chunk = chunks[i];
                if (chunk != null && chunk.mesh != null)
                {
                    if (immediate)
                        DestroyImmediate(chunk.mesh);
                    else
                        Destroy(chunk.mesh);
                }
            }

            chunks.Clear();
            chunkLookup.Clear();

            if (generatedRoot != null)
            {
                if (immediate)
                    DestroyImmediate(generatedRoot.gameObject);
                else
                    Destroy(generatedRoot.gameObject);
            }

            generatedRoot = null;
        }
    }
}
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteOutlineProxy : MonoBehaviour
{
    private const string ProxyChildName = "_SpriteOutlineProxy";
    private const string MaterialAssetPath = "Assets/Materials/SpriteOutlineProxy.mat";
    private const string UnlitShaderName = "Universal Render Pipeline/Unlit";

    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int CutoffId = Shader.PropertyToID("_Cutoff");
    private static readonly int AlphaClipId = Shader.PropertyToID("_AlphaClip");
    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
    private static readonly int CullId = Shader.PropertyToID("_Cull");

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField, Min(0f)] private float alphaCutoff = 0.1f;
    [SerializeField, Min(0f)] private float outlineThickness = 0.06f;
    [SerializeField] private Color outlineColor = Color.black;
    [SerializeField] private Vector3 localOffset = new(0f, 0f, 0.01f);
    [SerializeField] private bool outlineEnabled;

    private Transform proxyRoot;
    private MeshFilter proxyFilter;
    private MeshRenderer proxyRenderer;
    private Mesh proxyMesh;
    private Material runtimeMaterial;
    private MaterialPropertyBlock propertyBlock;
    private Sprite lastSprite;

    public Renderer ProxyRenderer => MeshRenderer;
    public bool OutlineEnabled => outlineEnabled;
    public float OutlineThickness => outlineThickness;
    public Color OutlineColor => outlineColor;

    private SpriteRenderer Renderer => this.ResolveComponent(ref spriteRenderer);
    private MeshFilter MeshFilter => proxyFilter ? proxyFilter : proxyFilter = ProxyRoot.GetOrAddComponent<MeshFilter>();
    private MeshRenderer MeshRenderer => proxyRenderer ? proxyRenderer : proxyRenderer = ProxyRoot.GetOrAddComponent<MeshRenderer>();
    private Transform ProxyRoot => transform.EnsureChild(ref proxyRoot, ProxyChildName);
    private MaterialPropertyBlock PropertyBlock => propertyBlock ??= new MaterialPropertyBlock();

    private void Reset()
    {
        this.ResolveComponent(ref spriteRenderer);
        SyncProxy();
    }

    private void Awake()
    {
        SyncProxy();
    }

    private void OnEnable()
    {
        SyncProxy();
    }

    private void OnValidate()
    {
        alphaCutoff = Mathf.Clamp01(alphaCutoff);
        outlineThickness = Mathf.Max(0f, outlineThickness);
        if (!spriteRenderer)
        {
            this.ResolveComponent(ref spriteRenderer);
        }

        SyncProxy();
    }

    private void LateUpdate()
    {
        SyncProxy();
    }

    private void OnDestroy()
    {
        if (proxyMesh)
        {
            if (Application.isPlaying)
            {
                Destroy(proxyMesh);
            }
            else
            {
                DestroyImmediate(proxyMesh);
            }
        }

        if (runtimeMaterial)
        {
            if (Application.isPlaying)
            {
                Destroy(runtimeMaterial);
            }
            else
            {
                DestroyImmediate(runtimeMaterial);
            }
        }
    }

    public void SetOutline(bool enabled, Color color, float thickness)
    {
        outlineEnabled = enabled;
        outlineColor = color;
        outlineThickness = Mathf.Max(0f, thickness);
        SyncProxy();
    }

    private void SyncProxy()
    {
        if (!Renderer || !Renderer.sprite)
        {
            if (proxyRenderer)
            {
                proxyRenderer.enabled = false;
            }

            return;
        }

        EnsureProxyObjects();
        SyncTransform();
        ApplyMaterial();
        RebuildMeshIfNeeded(Renderer.sprite);
        MeshRenderer.enabled = outlineEnabled && Renderer.enabled && outlineColor.a > 0.001f && outlineThickness > 0f;
    }

    private void EnsureProxyObjects()
    {
        ProxyRoot.gameObject.layer = gameObject.layer;
        MeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        MeshRenderer.receiveShadows = false;
        MeshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        MeshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        MeshRenderer.allowOcclusionWhenDynamic = false;
        MeshRenderer.sortingLayerID = Renderer.sortingLayerID;
        MeshRenderer.sortingOrder = Renderer.sortingOrder - 1;
        if (MeshFilter.sharedMesh != EnsureMesh())
        {
            MeshFilter.sharedMesh = EnsureMesh();
        }
    }

    private void SyncTransform()
    {
        ProxyRoot.localPosition = localOffset;
        ProxyRoot.localRotation = Quaternion.identity;
        Vector2 expansionScale = GetExpansionScale();
        ProxyRoot.localScale = new Vector3(
            (Renderer.flipX ? -1f : 1f) * expansionScale.x,
            (Renderer.flipY ? -1f : 1f) * expansionScale.y,
            1f);
    }

    private Vector2 GetExpansionScale()
    {
        if (!Renderer || !Renderer.sprite)
        {
            return Vector2.one;
        }

        Bounds spriteBounds = Renderer.sprite.bounds;
        float width = Mathf.Max(0.001f, spriteBounds.size.x);
        float height = Mathf.Max(0.001f, spriteBounds.size.y);
        return new Vector2(1f + outlineThickness / width, 1f + outlineThickness / height);
    }

    private void RebuildMeshIfNeeded(Sprite sprite)
    {
        Mesh mesh = EnsureMesh();
        if (!NeedsMeshRebuild(sprite, mesh))
        {
            return;
        }

        mesh.Clear();
        Vector2[] spriteVertices = sprite.vertices;
        Vector2[] spriteUvs = sprite.uv;
        ushort[] spriteTriangles = sprite.triangles;
        Vector3[] vertices = new Vector3[spriteVertices.Length];
        Vector3[] normals = new Vector3[spriteVertices.Length];
        Vector4[] tangents = new Vector4[spriteVertices.Length];
        int[] triangles = new int[spriteTriangles.Length];

        for (int i = 0; i < spriteVertices.Length; i++)
        {
            vertices[i] = spriteVertices[i];
            normals[i] = Vector3.back;
            tangents[i] = new Vector4(1f, 0f, 0f, -1f);
        }

        for (int i = 0; i < spriteTriangles.Length; i++)
        {
            triangles[i] = spriteTriangles[i];
        }

        mesh.vertices = vertices;
        mesh.uv = spriteUvs;
        mesh.normals = normals;
        mesh.tangents = tangents;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        MeshFilter.sharedMesh = mesh;
        lastSprite = sprite;
    }

    private void ApplyMaterial()
    {
        Material material = GetProxyMaterial();
        if (!material)
        {
            return;
        }

        if (MeshRenderer.sharedMaterial != material)
        {
            MeshRenderer.sharedMaterial = material;
        }

        PropertyBlock.Clear();
        PropertyBlock.SetTexture(BaseMapId, Renderer.sprite.texture);
        PropertyBlock.SetColor(BaseColorId, outlineColor);
        PropertyBlock.SetFloat(CutoffId, alphaCutoff);
        PropertyBlock.SetFloat(AlphaClipId, 1f);
        MeshRenderer.SetPropertyBlock(PropertyBlock);
    }

    private Material GetProxyMaterial()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Material assetMaterial = GetOrCreateSharedMaterialAsset();
            if (assetMaterial)
            {
                return assetMaterial;
            }
        }
#endif

        if (runtimeMaterial)
        {
            return runtimeMaterial;
        }

        Shader shader = Shader.Find(UnlitShaderName);
        if (!shader)
        {
            return null;
        }

        runtimeMaterial = new Material(shader) { name = "SpriteOutlineProxy Runtime" };
        ConfigureMaterial(runtimeMaterial);
        return runtimeMaterial;
    }

    private Mesh EnsureMesh()
    {
        if (proxyMesh)
        {
            return proxyMesh;
        }

        proxyMesh = new Mesh { name = $"{name}_SpriteOutlineProxy" };
        proxyMesh.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        return proxyMesh;
    }

    private bool NeedsMeshRebuild(Sprite sprite, Mesh mesh)
    {
        if (!sprite || !mesh)
        {
            return false;
        }

        if (lastSprite != sprite || MeshFilter.sharedMesh != mesh)
        {
            return true;
        }

        return mesh.vertexCount != sprite.vertices.Length || mesh.GetIndexCount(0) != sprite.triangles.Length;
    }

    private static void ConfigureMaterial(Material material)
    {
        material.enableInstancing = true;
        material.SetFloat(SurfaceId, 1f);
        material.SetFloat(AlphaClipId, 1f);
        material.SetFloat(CutoffId, 0.1f);
        material.SetFloat(CullId, 0f);
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
    }

#if UNITY_EDITOR
    private static Material GetOrCreateSharedMaterialAsset()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialAssetPath);
        if (material)
        {
            ConfigureMaterial(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        Shader shader = Shader.Find(UnlitShaderName);
        if (!shader)
        {
            return null;
        }

        EnsureFolder("Assets/Materials");
        material = new Material(shader) { name = "SpriteOutlineProxy" };
        ConfigureMaterial(material);
        AssetDatabase.CreateAsset(material, MaterialAssetPath);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
        if (!string.IsNullOrWhiteSpace(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent ?? "Assets", System.IO.Path.GetFileName(path));
    }
#endif
}

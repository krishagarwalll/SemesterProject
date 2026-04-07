using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteShadowCaster : MonoBehaviour
{
    private const string ShadowChildName = "_SpriteShadow";
    private const string MaterialAssetPath = "Assets/Materials/SpriteShadowCaster.mat";
    private const string LitShaderName = "Universal Render Pipeline/Lit";

    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int CutoffId = Shader.PropertyToID("_Cutoff");
    private static readonly int AlphaClipId = Shader.PropertyToID("_AlphaClip");
    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
    private static readonly int CullId = Shader.PropertyToID("_Cull");

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool shadowsOnly = true;
    [SerializeField, Min(0f)] private float alphaCutoff = 0.1f;
    [SerializeField] private Vector3 localOffset = Vector3.zero;

    private Transform shadowRoot;
    private MeshFilter shadowFilter;
    private MeshRenderer shadowRenderer;
    private Mesh shadowMesh;
    private Material runtimeMaterial;
    private MaterialPropertyBlock propertyBlock;
    private Sprite lastSprite;

    private SpriteRenderer Renderer => this.ResolveComponent(ref spriteRenderer);
    private MeshFilter ShadowFilter => shadowFilter ? shadowFilter : shadowFilter = ShadowRoot.GetOrAddComponent<MeshFilter>();
    private MeshRenderer ShadowRenderer => shadowRenderer ? shadowRenderer : shadowRenderer = ShadowRoot.GetOrAddComponent<MeshRenderer>();
    private Transform ShadowRoot => transform.EnsureChild(ref shadowRoot, ShadowChildName);
    private MaterialPropertyBlock PropertyBlock => propertyBlock ??= new MaterialPropertyBlock();

    private void Reset()
    {
        this.ResolveComponent(ref spriteRenderer);
        SyncShadow();
    }

    private void Awake()
    {
        SyncShadow();
    }

    private void OnEnable()
    {
        SyncShadow();
    }

    private void OnDisable()
    {
        if (!shadowRenderer)
        {
            return;
        }

        shadowRenderer.SetPropertyBlock(null);
    }

    private void OnDestroy()
    {
        DestroyShadowMesh();
        if (!runtimeMaterial)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(runtimeMaterial);
            return;
        }

        DestroyImmediate(runtimeMaterial);
    }

    private void LateUpdate()
    {
        SyncShadow();
    }

    private void OnValidate()
    {
        alphaCutoff = Mathf.Clamp01(alphaCutoff);
        if (!spriteRenderer)
        {
            this.ResolveComponent(ref spriteRenderer);
        }

        SyncShadow();
    }

    private void SyncShadow()
    {
        if (!Renderer)
        {
            return;
        }

        Sprite sprite = Renderer.sprite;
        if (!sprite)
        {
            SetShadowEnabled(false);
            lastSprite = null;
            return;
        }

        EnsureShadowObjects();
        SyncTransform();
        ApplyMaterial();
        RebuildMeshIfNeeded(sprite);
        SetShadowEnabled(true);
    }

    private void SyncTransform()
    {
        if (!ShadowRoot || !Renderer)
        {
            return;
        }

        if (ShadowRoot.name != ShadowChildName)
        {
            ShadowRoot.name = ShadowChildName;
        }

        ShadowRoot.localPosition = localOffset;
        ShadowRoot.localRotation = Quaternion.identity;
        ShadowRoot.localScale = new Vector3(Renderer.flipX ? -1f : 1f, Renderer.flipY ? -1f : 1f, 1f);
    }

    private void RebuildMeshIfNeeded(Sprite sprite)
    {
        if (!ShadowFilter)
        {
            return;
        }

        Mesh mesh = EnsureShadowMesh();
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

        ShadowFilter.sharedMesh = mesh;
        lastSprite = sprite;
    }

    private void ApplyMaterial()
    {
        if (!ShadowRenderer || !Renderer || !Renderer.sprite)
        {
            return;
        }

        Material material = GetShadowMaterial();
        if (!material)
        {
            return;
        }

        if (ShadowRenderer.sharedMaterial != material)
        {
            ShadowRenderer.sharedMaterial = material;
        }

        PropertyBlock.Clear();
        PropertyBlock.SetTexture(BaseMapId, Renderer.sprite.texture);
        PropertyBlock.SetColor(BaseColorId, Color.white);
        PropertyBlock.SetFloat(CutoffId, alphaCutoff);
        PropertyBlock.SetFloat(AlphaClipId, 1f);
        ShadowRenderer.SetPropertyBlock(PropertyBlock);
    }

    private Material GetShadowMaterial()
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

        Shader shader = Shader.Find(LitShaderName);
        if (!shader)
        {
            Debug.LogWarning($"SpriteShadowCaster could not find '{LitShaderName}'.", this);
            return null;
        }

        runtimeMaterial = new Material(shader) { name = "SpriteShadowCaster Runtime" };
        ConfigureMaterial(runtimeMaterial);
        return runtimeMaterial;
    }

    private void EnsureShadowObjects()
    {
        Transform root = ShadowRoot;
        if (!root)
        {
            return;
        }

        MeshFilter filter = ShadowFilter;
        MeshRenderer renderer = ShadowRenderer;
        if (!filter || !renderer)
        {
            return;
        }

        root.gameObject.layer = gameObject.layer;
        if (ShadowFilter.sharedMesh != EnsureShadowMesh())
        {
            ShadowFilter.sharedMesh = EnsureShadowMesh();
        }

        renderer.shadowCastingMode = shadowsOnly ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        renderer.allowOcclusionWhenDynamic = true;
    }

    private void SetShadowEnabled(bool value)
    {
        value &= Renderer && Renderer.enabled;
        if (ShadowRenderer)
        {
            ShadowRenderer.enabled = value;
        }

        if (ShadowFilter)
        {
            ShadowFilter.gameObject.SetActive(value);
        }
    }

    private Mesh EnsureShadowMesh()
    {
        if (shadowMesh)
        {
            return shadowMesh;
        }

        shadowMesh = new Mesh { name = $"{name}_SpriteShadow" };
        shadowMesh.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        return shadowMesh;
    }

    private bool NeedsMeshRebuild(Sprite sprite, Mesh mesh)
    {
        if (!mesh || !sprite)
        {
            return false;
        }

        if (ShadowFilter.sharedMesh != mesh || sprite != lastSprite)
        {
            return true;
        }

        if (mesh.vertexCount != sprite.vertices.Length || mesh.subMeshCount == 0 || mesh.GetIndexCount(0) != sprite.triangles.Length)
        {
            return true;
        }

        Bounds spriteBounds = sprite.bounds;
        Bounds meshBounds = mesh.bounds;
        return !Mathf.Approximately(meshBounds.size.x, spriteBounds.size.x)
            || !Mathf.Approximately(meshBounds.size.y, spriteBounds.size.y);
    }

    private void DestroyShadowMesh()
    {
        if (!shadowMesh)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(shadowMesh);
        }
        else
        {
            DestroyImmediate(shadowMesh);
        }

        shadowMesh = null;
    }

    private static void ConfigureMaterial(Material material)
    {
        if (!material)
        {
            return;
        }

        material.enableInstancing = true;
        material.doubleSidedGI = true;
        material.SetFloat(SurfaceId, 0f);
        material.SetFloat(AlphaClipId, 1f);
        material.SetFloat(CutoffId, 0.1f);
        material.SetFloat(CullId, 0f);
        material.SetColor(BaseColorId, Color.white);
        material.renderQueue = (int)RenderQueue.AlphaTest;
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

        Shader shader = Shader.Find(LitShaderName);
        if (!shader)
        {
            return null;
        }

        EnsureFolder("Assets/Materials");

        material = new Material(shader) { name = "SpriteShadowCaster" };
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

        string folderName = System.IO.Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent ?? "Assets", folderName);
    }
#endif
}

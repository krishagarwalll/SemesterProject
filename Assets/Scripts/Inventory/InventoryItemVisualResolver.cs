using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public static class InventoryItemVisualResolver
{
    private const int PreviewLayer = 2;
    private const int PreviewTextureSize = 256;
    private static readonly Dictionary<int, Sprite> Cache = new();

    private static Camera previewCamera;
    private static Light previewLight;
    private static Transform previewRoot;
    private static RenderTexture previewTexture;

    public static Sprite GetSprite(InventoryItemDefinition definition)
    {
        if (!definition)
        {
            return null;
        }

        if (definition.Icon)
        {
            return definition.Icon;
        }

        int cacheKey = definition.GetInstanceID();
        if (Cache.TryGetValue(cacheKey, out Sprite cachedSprite) && cachedSprite)
        {
            return cachedSprite;
        }

        Sprite sprite = TryGetPrefabSprite(definition.WorldPrefab);
        if (sprite)
        {
            Cache[cacheKey] = sprite;
            return sprite;
        }

        if (!Application.isPlaying || !definition.WorldPrefab)
        {
            return null;
        }

        Sprite renderedSprite = RenderPrefabThumbnail(definition.WorldPrefab);
        if (renderedSprite)
        {
            Cache[cacheKey] = renderedSprite;
        }

        return renderedSprite;
    }

    private static Sprite TryGetPrefabSprite(GameObject prefab)
    {
        if (!prefab)
        {
            return null;
        }

        SpriteRenderer spriteRenderer = prefab.GetComponentInChildren<SpriteRenderer>(true);
        return spriteRenderer ? spriteRenderer.sprite : null;
    }

    private static Sprite RenderPrefabThumbnail(GameObject prefab)
    {
        EnsurePreviewRig();
        if (!previewRoot || !previewCamera)
        {
            return null;
        }

        GameObject instance = Object.Instantiate(prefab, previewRoot);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        PreparePreviewInstance(instance);

        if (!TryGetRenderableBounds(instance, out Bounds bounds))
        {
            DestroyPreviewInstance(instance);
            return null;
        }

        const float verticalFov = 25f;
        Vector3 lookDirection = new(-0.55f, 0.3f, -1f);
        lookDirection.Normalize();
        float radius = Mathf.Max(0.1f, bounds.extents.magnitude);
        float distance = radius / Mathf.Sin(verticalFov * Mathf.Deg2Rad * 0.5f);

        previewCamera.transform.position = bounds.center - lookDirection * distance;
        previewCamera.transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        previewCamera.fieldOfView = verticalFov;
        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = Mathf.Max(10f, distance * 4f);

        previewLight.transform.position = previewCamera.transform.position - previewCamera.transform.forward * 0.5f;
        previewLight.transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);

        RenderTexture currentActive = RenderTexture.active;
        RenderTexture currentTarget = previewCamera.targetTexture;
        previewCamera.targetTexture = previewTexture;
        previewCamera.Render();
        RenderTexture.active = previewTexture;

        Texture2D texture = new(PreviewTextureSize, PreviewTextureSize, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0f, 0f, PreviewTextureSize, PreviewTextureSize), 0, 0);
        texture.Apply(false, false);
        texture.name = $"{prefab.name}_InventoryPreview";

        RenderTexture.active = currentActive;
        previewCamera.targetTexture = currentTarget;
        DestroyPreviewInstance(instance);

        Rect rect = new(0f, 0f, texture.width, texture.height);
        return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f);
    }

    private static void EnsurePreviewRig()
    {
        if (previewRoot && previewCamera && previewLight && previewTexture)
        {
            return;
        }

        GameObject root = new("InventoryItemPreviewRig");
        root.hideFlags = HideFlags.HideAndDontSave;
        root.transform.position = new Vector3(0f, -10000f, 0f);
        previewRoot = root.transform;

        GameObject cameraObject = new("PreviewCamera");
        cameraObject.hideFlags = HideFlags.HideAndDontSave;
        cameraObject.transform.SetParent(previewRoot, false);
        previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        previewCamera.orthographic = false;
        previewCamera.enabled = false;
        previewCamera.cullingMask = 1 << PreviewLayer;

        GameObject lightObject = new("PreviewLight");
        lightObject.hideFlags = HideFlags.HideAndDontSave;
        lightObject.transform.SetParent(previewRoot, false);
        previewLight = lightObject.AddComponent<Light>();
        previewLight.type = LightType.Directional;
        previewLight.intensity = 1.15f;
        previewLight.shadows = LightShadows.None;

        previewTexture = new RenderTexture(PreviewTextureSize, PreviewTextureSize, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 1,
            useMipMap = false,
            autoGenerateMips = false,
            hideFlags = HideFlags.HideAndDontSave
        };
    }

    private static void PreparePreviewInstance(GameObject instance)
    {
        foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = PreviewLayer;
        }

        foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        foreach (Collider2D collider in instance.GetComponentsInChildren<Collider2D>(true))
        {
            collider.enabled = false;
        }

        foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>(true))
        {
            body.isKinematic = true;
            body.detectCollisions = false;
        }

        foreach (Rigidbody2D body in instance.GetComponentsInChildren<Rigidbody2D>(true))
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.simulated = false;
        }

        foreach (NavMeshObstacle obstacle in instance.GetComponentsInChildren<NavMeshObstacle>(true))
        {
            obstacle.enabled = false;
        }
    }

    private static bool TryGetRenderableBounds(GameObject instance, out Bounds bounds)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        bounds = default;
        bool initialized = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (!renderer || !renderer.enabled)
            {
                continue;
            }

            if (!initialized)
            {
                bounds = renderer.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds.min);
                bounds.Encapsulate(renderer.bounds.max);
            }
        }

        return initialized;
    }

    private static void DestroyPreviewInstance(GameObject instance)
    {
        if (!instance)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(instance);
        }
        else
        {
            Object.DestroyImmediate(instance);
        }
    }
}

using System;
using UnityEngine;

public static class WorldDragUtility
{
    private const float MinimumExtent = 0.01f;

    public static Bounds GetWorldBounds(Renderer[] renderers, Collider[] colliders, Vector3 currentPosition, Vector3 candidatePosition)
    {
        Bounds bounds = new(candidatePosition, Vector3.one * 0.1f);
        Vector3 offset = candidatePosition - currentPosition;
        bool initialized = EncapsulateBounds(renderers, offset, ref bounds);
        if (!initialized)
        {
            EncapsulateBounds(colliders, offset, ref bounds);
        }

        return bounds;
    }

    public static bool IsBlocked(
        Bounds bounds,
        Quaternion rotation,
        LayerMask blockingLayers,
        Collider[] ownColliders,
        float supportSurfaceTolerance,
        Func<Collider, bool> shouldIgnore = null)
    {
        Collider[] hits = Physics.OverlapBox(bounds.center, GetQueryExtents(bounds.extents), rotation, blockingLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (!hit || ContainsCollider(ownColliders, hit) || shouldIgnore != null && shouldIgnore(hit))
            {
                continue;
            }

            if (IsSupportSurface(bounds, hit, supportSurfaceTolerance))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    public static Vector3 ResolveRestingPosition(
        Bounds liftedBounds,
        Vector3 liftedPosition,
        Quaternion rotation,
        LayerMask blockingLayers,
        Collider[] ownColliders,
        float supportSurfaceTolerance,
        float fallbackY,
        float maxDropDistance,
        Func<Collider, bool> shouldIgnore = null)
    {
        if (!TryGetSupportTop(liftedBounds, rotation, blockingLayers, ownColliders, supportSurfaceTolerance, maxDropDistance, shouldIgnore, out float supportTop))
        {
            liftedPosition.y = fallbackY;
            return liftedPosition;
        }

        float bottomOffset = liftedPosition.y - liftedBounds.min.y;
        liftedPosition.y = supportTop + bottomOffset;
        return liftedPosition;
    }

    public static bool IsSupportSurface(Bounds itemBounds, Collider hit, float tolerance)
    {
        return hit.bounds.max.y <= itemBounds.min.y + tolerance;
    }

    private static bool TryGetSupportTop(
        Bounds liftedBounds,
        Quaternion rotation,
        LayerMask blockingLayers,
        Collider[] ownColliders,
        float supportSurfaceTolerance,
        float maxDropDistance,
        Func<Collider, bool> shouldIgnore,
        out float supportTop)
    {
        supportTop = float.NegativeInfinity;
        float castDistance = Mathf.Max(maxDropDistance, supportSurfaceTolerance + 0.01f);
        Vector3 halfExtents = GetCastExtents(liftedBounds.extents);
        RaycastHit[] hits = Physics.BoxCastAll(
            liftedBounds.center,
            halfExtents,
            Vector3.down,
            rotation,
            castDistance,
            blockingLayers,
            QueryTriggerInteraction.Ignore);

        bool found = false;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            Collider collider = hit.collider;
            if (!collider || ContainsCollider(ownColliders, collider) || shouldIgnore != null && shouldIgnore(collider))
            {
                continue;
            }

            if (hit.normal.y <= 0.05f)
            {
                continue;
            }

            float top = collider.bounds.max.y;
            if (top > liftedBounds.min.y + supportSurfaceTolerance)
            {
                continue;
            }

            if (!found || top > supportTop)
            {
                supportTop = top;
                found = true;
            }
        }

        return found;
    }

    private static bool EncapsulateBounds(Renderer[] renderers, Vector3 offset, ref Bounds bounds)
    {
        bool initialized = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (!renderer)
            {
                continue;
            }

            Bounds rendererBounds = renderer.bounds;
            rendererBounds.center += offset;
            if (!initialized)
            {
                bounds = rendererBounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(rendererBounds.min);
                bounds.Encapsulate(rendererBounds.max);
            }
        }

        return initialized;
    }

    private static bool EncapsulateBounds(Collider[] colliders, Vector3 offset, ref Bounds bounds)
    {
        bool initialized = false;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (!TryGetColliderBounds(collider, out Bounds colliderBounds))
            {
                continue;
            }

            colliderBounds.center += offset;
            if (!initialized)
            {
                bounds = colliderBounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(colliderBounds.min);
                bounds.Encapsulate(colliderBounds.max);
            }
        }

        return initialized;
    }

    private static bool TryGetColliderBounds(Collider collider, out Bounds bounds)
    {
        bounds = default;
        if (!collider)
        {
            return false;
        }

        if (collider.enabled)
        {
            bounds = collider.bounds;
            return true;
        }

        switch (collider)
        {
            case BoxCollider boxCollider:
                bounds = TransformBounds(boxCollider.transform.localToWorldMatrix, new Bounds(boxCollider.center, boxCollider.size));
                return true;

            case SphereCollider sphereCollider:
            {
                float radius = sphereCollider.radius * GetMaxAbsScale(sphereCollider.transform.lossyScale);
                bounds = new Bounds(sphereCollider.transform.TransformPoint(sphereCollider.center), Vector3.one * radius * 2f);
                return true;
            }

            case CapsuleCollider capsuleCollider:
            {
                Vector3 size = Vector3.one * capsuleCollider.radius * 2f;
                size[capsuleCollider.direction] = Mathf.Max(size[capsuleCollider.direction], capsuleCollider.height);
                bounds = TransformBounds(capsuleCollider.transform.localToWorldMatrix, new Bounds(capsuleCollider.center, size));
                return true;
            }

            case MeshCollider meshCollider when meshCollider.sharedMesh:
                bounds = TransformBounds(meshCollider.transform.localToWorldMatrix, meshCollider.sharedMesh.bounds);
                return true;
        }

        return false;
    }

    private static bool ContainsCollider(Collider[] ownColliders, Collider candidate)
    {
        for (int i = 0; i < ownColliders.Length; i++)
        {
            if (ownColliders[i] == candidate)
            {
                return true;
            }
        }

        return false;
    }

    private static Vector3 GetQueryExtents(Vector3 extents)
    {
        return new Vector3(
            Mathf.Max(MinimumExtent, extents.x),
            Mathf.Max(MinimumExtent, extents.y),
            Mathf.Max(MinimumExtent, extents.z));
    }

    private static Vector3 GetCastExtents(Vector3 extents)
    {
        return GetQueryExtents(extents * 0.95f);
    }

    private static Bounds TransformBounds(Matrix4x4 matrix, Bounds localBounds)
    {
        Vector3 center = matrix.MultiplyPoint3x4(localBounds.center);
        Vector3 extents = localBounds.extents;
        Vector3 axisX = matrix.MultiplyVector(new Vector3(extents.x, 0f, 0f));
        Vector3 axisY = matrix.MultiplyVector(new Vector3(0f, extents.y, 0f));
        Vector3 axisZ = matrix.MultiplyVector(new Vector3(0f, 0f, extents.z));
        Vector3 worldExtents = new(
            Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
            Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
            Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z));
        return new Bounds(center, worldExtents * 2f);
    }

    private static float GetMaxAbsScale(Vector3 scale)
    {
        return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
    }
}

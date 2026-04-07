using System;
using UnityEngine;

[DisallowMultipleComponent]
public class SpriteFlipper : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer[] spriteRenderers = Array.Empty<SpriteRenderer>();
    [SerializeField] private bool includeInactiveChildren = true;

    [SerializeField] private bool snapToCardinal = true;
    [SerializeField] private bool flipX = true;
    [SerializeField] private bool flipY;
    [SerializeField, Min(0f)] private float deadzone = 0.01f;

    [SerializeField] private bool mirrorVisualRoot = true;
    [SerializeField] private bool useManualOffset;
    [SerializeField] private Vector2 mirroredOffset = Vector2.zero;

    private Vector2 facing = Vector2.right;
    private Vector3 baseLocalPosition;

    private SpriteRenderer[] Renderers => spriteRenderers is { Length: > 0 }
        ? spriteRenderers
        : spriteRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactiveChildren);
    private Transform VisualRoot => visualRoot ? visualRoot : visualRoot = transform;

    private void Reset()
    {
        visualRoot = transform;
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactiveChildren);
        CacheBaseState();
    }

    private void Awake()
    {
        CacheBaseState();
        ApplyFacing();
    }

    private void OnValidate()
    {
        deadzone = Mathf.Max(0f, deadzone);
        if (!visualRoot)
        {
            visualRoot = transform;
        }
        CacheBaseState();
        ApplyFacing();
    }

    public void SetFacing(Vector2 direction)
    {
        if (direction.sqrMagnitude <= deadzone * deadzone)
        {
            return;
        }

        facing = snapToCardinal ? ToCardinal(direction) : direction.normalized;
        if (facing == Vector2.zero)
        {
            return;
        }

        ApplyFacing();
    }

    private void CacheBaseState()
    {
        baseLocalPosition = VisualRoot.localPosition;
    }

    private void ApplyFacing()
    {
        bool mirrorX = flipX && facing.x < 0f;
        bool mirrorY = flipY && facing.y < 0f;

        for (int i = 0; i < Renderers.Length; i++)
        {
            if (!Renderers[i])
            {
                continue;
            }

            Renderers[i].flipX = mirrorX;
            Renderers[i].flipY = mirrorY;
        }

        if (!mirrorVisualRoot || VisualRoot == transform)
        {
            return;
        }

        Vector3 position = baseLocalPosition;
        if (mirrorX)
        {
            position.x = -position.x;
        }

        if (mirrorY)
        {
            position.y = -position.y;
        }

        if (useManualOffset)
        {
            position += new Vector3(mirrorX ? mirroredOffset.x : 0f, mirrorY ? mirroredOffset.y : 0f, 0f);
        }

        VisualRoot.localPosition = position;
    }

    private static Vector2 ToCardinal(Vector2 direction)
    {
        if (direction == Vector2.zero)
        {
            return Vector2.zero;
        }

        return Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)
            ? new Vector2(Mathf.Sign(direction.x), 0f)
            : new Vector2(0f, Mathf.Sign(direction.y));
    }
}

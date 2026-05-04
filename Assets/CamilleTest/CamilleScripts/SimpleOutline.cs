using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SimpleOutline : MonoBehaviour
{
    public Color outlineColor = Color.white;
    public float thickness = 0.05f;

    private SpriteRenderer source;
    private List<SpriteRenderer> outlines = new List<SpriteRenderer>();

    private Vector2[] directions = new Vector2[]
    {
        Vector2.up,
        Vector2.down,
        Vector2.left,
        Vector2.right,
        new Vector2(1,1),
        new Vector2(-1,1),
        new Vector2(1,-1),
        new Vector2(-1,-1),
    };

    void Awake()
    {
        source = GetComponent<SpriteRenderer>();

        foreach (var dir in directions)
        {
            GameObject obj = new GameObject("Outline");
            obj.transform.SetParent(transform);
            obj.transform.localPosition = Vector3.zero;

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = source.sprite;
            sr.sortingLayerID = source.sortingLayerID;
            sr.sortingOrder = source.sortingOrder - 1;
            sr.color = outlineColor;

            outlines.Add(sr);
        }

        SetOutline(false);
    }

    public void SetOutline(bool enabled)
    {
        foreach (var sr in outlines)
        {
            sr.enabled = enabled;
        }
    }

    void LateUpdate()
{
    for (int i = 0; i < outlines.Count; i++)
    {
        outlines[i].sprite = source.sprite;
        outlines[i].flipX = source.flipX;
        outlines[i].flipY = source.flipY;

        // 🚫 DO NOT SCALE
        outlines[i].transform.localScale = Vector3.one;

        // ✅ ONLY OFFSET POSITION
        outlines[i].transform.localPosition =
            (Vector3)(directions[i] * thickness);
    }
}
}
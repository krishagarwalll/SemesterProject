using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "InputGlyphLibrary", menuName = "How To Get To Heaven/Input Glyph Library")]
public class InputGlyphLibrary : ScriptableObject
{
    [Serializable]
    public struct GlyphEntry
    {
        public string glyphId;
        public string fallbackText;
        public Sprite keyboardMouseSprite;
        public Sprite gamepadSprite;
    }

    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private List<GlyphEntry> entries = new();

    public TMP_FontAsset FontAsset => fontAsset ? fontAsset : TMP_Settings.defaultFontAsset;

    public bool TryResolve(string glyphId, out Sprite sprite, out string fallbackText)
    {
        sprite = null;
        fallbackText = glyphId;
        if (string.IsNullOrWhiteSpace(glyphId))
        {
            return false;
        }

        PlayerInput input = playerInput ? playerInput : (PlayerInput.all.Count > 0 ? PlayerInput.all[0] : null);
        bool useGamepad = input && string.Equals(input.currentControlScheme, "Gamepad", StringComparison.OrdinalIgnoreCase);
        for (int i = 0; i < entries.Count; i++)
        {
            if (!string.Equals(entries[i].glyphId, glyphId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            sprite = useGamepad ? entries[i].gamepadSprite : entries[i].keyboardMouseSprite;
            fallbackText = string.IsNullOrWhiteSpace(entries[i].fallbackText) ? glyphId : entries[i].fallbackText;
            return sprite || !string.IsNullOrWhiteSpace(fallbackText);
        }

        return false;
    }
}

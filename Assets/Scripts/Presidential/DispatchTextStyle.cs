using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Rendering only: keep the authored font, metrics and UI geometry.
public static class DispatchTextStyle
{
    private static readonly Dictionary<TMP_FontAsset, Material[]> materials = new Dictionary<TMP_FontAsset, Material[]>();

    public static void Apply(TMP_Text text, bool heading = false)
    {
        if (!text || !text.font) return;
        if (!materials.TryGetValue(text.font, out var pair))
        {
            pair = new[] { CreateMaterial(text.font, false), CreateMaterial(text.font, true) };
            foreach (var material in pair) material.hideFlags = HideFlags.HideAndDontSave;
            materials.Add(text.font, pair);
        }
        text.fontSharedMaterial = pair[heading ? 1 : 0];
        text.extraPadding = true;
    }

    public static Material CreateMaterial(TMP_FontAsset font, bool heading)
    {
        var material = new Material(font.material) { name = font.name + (heading ? " Dispatch Heading" : " Dispatch Body") };
        // Small SDF face expansion strengthens strokes without outlines or blur.
        if (material.HasProperty("_FaceDilate")) material.SetFloat("_FaceDilate", FaceExpansion(font, heading));
        if (material.HasProperty("_Sharpness")) material.SetFloat("_Sharpness", .15f);
        return material;
    }

    public static float FaceExpansion(TMP_FontAsset font, bool heading)
        => font.name.StartsWith("Liberation") ? (heading ? .07f : .045f) : (heading ? .04f : .022f);
}

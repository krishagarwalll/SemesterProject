// EXPERIMENTAL EDGE DETECTION VARIANT STRIPPER

// #if UNITY_EDITOR
// using System.Collections.Generic;
// using System.Linq;
// using Linework.EdgeDetection;
// using UnityEditor;
// using UnityEditor.Build;
// using UnityEditor.Rendering;
// using UnityEngine;
// using UnityEngine.Rendering;
//
//
// // TODO: TRY THIS
// // csharp// In an editor script, generate this asset from your settings
// // var collection = new ShaderVariantCollection();
// // collection.Add(new ShaderVariantCollection.ShaderVariant(
// //     outlineShader,
// //     PassType.SceneSelectionPass,
// //     requiredKeywords.ToArray()
// // ));
// // AssetDatabase.CreateAsset(collection, "Assets/EdgeDetectionVariants.shadervariants");
// //     ```
//
// namespace Linework.Editor.EdgeDetection
// {
//     public class EdgeDetectionVariantStripper : IPreprocessShaders
//     {
//         public int callbackOrder => 0;
//
//         // Load settings once, not per-shader-variant
//         private readonly EdgeDetectionSettings settings;
//
//         public EdgeDetectionVariantStripper()
//         {
//             settings = AssetDatabase.FindAssets("t:EdgeDetectionSettings")
//                 .Select(guid => AssetDatabase.LoadAssetAtPath<EdgeDetectionSettings>(
//                     AssetDatabase.GUIDToAssetPath(guid)))
//                 .FirstOrDefault();
//
//             if (settings == null)
//                 Debug.LogWarning("[EdgeDetection] No EdgeDetectionSettings asset found. No variants will be stripped.");
//         }
//
//         public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
//         {
//             if (shader.name !=  "Hidden/Outlines/Edge Detection/Outline") return;
//             if (settings == null) return;
//
//             // Build the set of keywords your current settings require
//             var requiredKeywords = GetRequiredKeywords(settings);
//
//             for (int i = data.Count - 1; i >= 0; i--)
//             {
//                 if (!VariantMatchesSettings(data[i], requiredKeywords))
//                     data.RemoveAt(i);
//             }
//
//             Debug.Log($"[EdgeDetection] Outline shader: kept {data.Count} variant(s).");
//         }
//
//         private static bool VariantMatchesSettings(ShaderCompilerData variant, HashSet<string> requiredKeywords)
//         {
//             var set = variant.shaderKeywordSet;
//
//             // Every required keyword must be enabled in this variant
//             foreach (var keyword in requiredKeywords)
//             {
//                 if (!set.IsEnabled(new ShaderKeyword(keyword)))
//                     return false;
//             }
//
//             // No unexpected keywords should be enabled that we didn't ask for
//             // (prevents keeping e.g. DEPTH+FILL when only DEPTH is needed)
//             var allTrackedKeywords = new[]
//             {
//                 "DEPTH", "NORMALS", "LUMINANCE", "SECTIONS",
//                 "DEPTH_MASK", "NORMALS_MASK", "LUMINANCE_MASK",
//                 "OPERATOR_CROSS", "OPERATOR_SOBEL", "OPERATOR_CIRCULAR",
//                 "OVERRIDE_SHADOW", "SCALE_WITH_DISTANCE", "SCALE_WITH_RESOLUTION",
//                 "FILL", "FADE_BY_DISTANCE", "FADE_BY_HEIGHT", "DISTORTION"
//             };
//
//             foreach (var keyword in allTrackedKeywords)
//             {
//                 bool isRequired = requiredKeywords.Contains(keyword);
//                 bool isEnabled = set.IsEnabled(new ShaderKeyword(keyword));
//
//                 if (isEnabled != isRequired)
//                     return false;
//             }
//
//             return true;
//         }
//
//         private static HashSet<string> GetRequiredKeywords(EdgeDetectionSettings s)
//         {
//             var keywords = new HashSet<string>();
//
//             // Discontinuity inputs
//             if (s.discontinuityInput.HasFlag(DiscontinuityInput.Depth))     keywords.Add("DEPTH");
//             if (s.discontinuityInput.HasFlag(DiscontinuityInput.Normals))   keywords.Add("NORMALS");
//             if (s.discontinuityInput.HasFlag(DiscontinuityInput.Luminance)) keywords.Add("LUMINANCE");
//             if (s.discontinuityInput.HasFlag(DiscontinuityInput.Sections))  keywords.Add("SECTIONS");
//
//             // Masks
//             if (s.maskInfluence.HasFlag(MaskInfluence.Depth))     keywords.Add("DEPTH_MASK");
//             if (s.maskInfluence.HasFlag(MaskInfluence.Normals))   keywords.Add("NORMALS_MASK");
//             if (s.maskInfluence.HasFlag(MaskInfluence.Luminance)) keywords.Add("LUMINANCE_MASK");
//
//             // Kernel — exactly one of these must always be present
//             keywords.Add(s.kernel switch
//             {
//                 Kernel.RobertsCross => "OPERATOR_CROSS",
//                 Kernel.Sobel        => "OPERATOR_SOBEL",
//                 Kernel.Circular     => "OPERATOR_CIRCULAR",
//                 _                   => "OPERATOR_CROSS"
//             });
//
//             // Feature toggles
//             if (s.overrideColorInShadow) keywords.Add("OVERRIDE_SHADOW");
//             if (s.scaleWithDistance)     keywords.Add("SCALE_WITH_DISTANCE");
//             if (s.scaleWithResolution)   keywords.Add("SCALE_WITH_RESOLUTION");
//             if (s.fill)                  keywords.Add("FILL");
//             if (s.fadeByDistance)        keywords.Add("FADE_BY_DISTANCE");
//             if (s.fadeByHeight)          keywords.Add("FADE_BY_HEIGHT");
//             if (s.distortEdges)          keywords.Add("DISTORTION");
//
//             return keywords;
//         }
//     }
// }
// #endif
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class GenerateTilemapShadowCasters
{
    [MenuItem("Tools/2D Shadows/Generate From Selected CompositeCollider2D")]
    private static void Generate()
    {
        var selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("2D Shadows", "Select a GameObject with CompositeCollider2D.", "OK");
            return;
        }

        var composite = selected.GetComponent<CompositeCollider2D>();
        if (composite == null)
        {
            EditorUtility.DisplayDialog("2D Shadows", "Selected GameObject does not have CompositeCollider2D.", "OK");
            return;
        }
        
        if (composite.geometryType != CompositeCollider2D.GeometryType.Outlines)
        {
            if (EditorUtility.DisplayDialog("2D Shadows", "CompositeCollider2D Geometry Type is not set to 'Outlines'. This is recommended for Tilemap shadows. Change it now?", "Yes", "No"))
            {
                composite.geometryType = CompositeCollider2D.GeometryType.Outlines;
            }
        }

        var existing = selected.transform.Find("ShadowCasters");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        var parent = new GameObject("ShadowCasters");
        parent.transform.SetParent(selected.transform, false);
        parent.layer = selected.layer; 
        
        // ShadowCasterGroup2D is abstract in some URP versions, skipping to avoid errors
        // If you need to group shadows, you can add a script that inherits from it manually if needed

        for (int pathIndex = 0; pathIndex < composite.pathCount; pathIndex++)
        {
            var pointCount = composite.GetPathPointCount(pathIndex);
            if (pointCount < 3)
            {
                continue;
            }

            var points = new Vector2[pointCount];
            composite.GetPath(pathIndex, points);

            var localPoints = new List<Vector3>(pointCount);
            for (int i = 0; i < pointCount; i++)
            {
                var world = composite.transform.TransformPoint(points[i]);
                localPoints.Add(parent.transform.InverseTransformPoint(world));
            }

            var simplified = Simplify(localPoints, 0.0005f, 0.001f);
            if (simplified.Count < 3)
            {
                continue;
            }

            var child = new GameObject($"ShadowPath_{pathIndex}");
            child.transform.SetParent(parent.transform, false);

            var caster = child.AddComponent<ShadowCaster2D>();
            caster.selfShadows = false; // Usually better for walls
            
            // Critical: Disable silhouette renderer source so it uses our custom path points
            TrySetUseRendererSilhouette(caster, false);
            
            TrySetShapePath(caster, simplified.ToArray());
            TryEnableCustomPath(caster, true);
            
            // Set some common properties that might be needed
            TrySetShadowIntensity(caster, 1.0f);
            TrySetHasNoShadows(caster, false);
            TrySetApplyToAllSortingLayers(caster, true);
            
            TryRebuild(caster);
            
            if (simplified.Count > 0)
            {
                var worldFirst = caster.transform.TransformPoint(simplified[0]);
                Debug.Log($"[2D Shadows] Generated {child.name} with {simplified.Count} points. First point world pos: {worldFirst}");
            }
        }

        Selection.activeGameObject = parent;
        EditorUtility.DisplayDialog("2D Shadows", "ShadowCasters generated from CompositeCollider2D.\n\nIMPORTANT: Ensure your Light2D has 'Shadows' enabled and its 'Shadow Filter' includes the layer of this Tilemap.", "OK");
    }

    private static void TrySetShadowIntensity(ShadowCaster2D caster, float intensity)
    {
        var field = caster.GetType().GetField("m_ShadowIntensity", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null) field.SetValue(caster, intensity);
    }

    private static void TrySetUseRendererSilhouette(ShadowCaster2D caster, bool useSilhouette)
    {
        var type = caster.GetType();
        
        // Try public property first (common in newer URP)
        var prop = type.GetProperty("useRendererSilhouette", BindingFlags.Public | BindingFlags.Instance);
        if (prop != null)
        {
            prop.SetValue(caster, useSilhouette);
            return;
        }

        // Try common field names as fallback
        var names = new[] { "m_UseRendererSilhouette", "m_UseRenderer", "m_UseCustomPath" };
        foreach (var name in names)
        {
            var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(caster, useSilhouette);
                break;
            }
        }
    }

    private static void TrySetHasNoShadows(ShadowCaster2D caster, bool hasNoShadows)
    {
        // Some versions of URP have this to disable shadows
        var field = caster.GetType().GetField("m_HasNoShadows", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null) field.SetValue(caster, hasNoShadows);
    }

    private static void TrySetApplyToAllSortingLayers(ShadowCaster2D caster, bool applyToAll)
    {
        var field = caster.GetType().GetField("m_ApplyToSortingLayers", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            // This is usually a sorting layer array, but in some versions it's a bool or handled differently
            // Actually, usually it's better to just ensure the ShadowCaster2D is enabled and rebuilt.
        }
    }

    private static List<Vector3> Simplify(List<Vector3> points, float angleEpsilon, float distanceEpsilon)
    {
        var output = new List<Vector3>();

        for (int i = 0; i < points.Count; i++)
        {
            var prev = points[(i - 1 + points.Count) % points.Count];
            var curr = points[i];
            var next = points[(i + 1) % points.Count];

            var v1 = (curr - prev).normalized;
            var v2 = (next - curr).normalized;

            var dot = Vector3.Dot(v1, v2);
            var nearlyCollinear = 1f - Mathf.Abs(dot) < angleEpsilon;
            var shortEdge = (next - curr).sqrMagnitude < distanceEpsilon * distanceEpsilon;

            if (!nearlyCollinear && !shortEdge)
            {
                output.Add(curr);
            }
        }

        if (output.Count == 0)
        {
            output.AddRange(points);
        }

        return output;
    }

    private static void TrySetShapePath(ShadowCaster2D caster, Vector3[] path)
    {
        var field = caster.GetType().GetField("m_ShapePath", BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(caster, path);
    }

    private static void TryEnableCustomPath(ShadowCaster2D caster, bool enabled)
    {
        var type = caster.GetType();
        // Check for common field names in different URP versions
        var names = new[] { "m_UseCustomPath", "m_UseCustomSpriteSilhouette", "m_UseCustom" };
        foreach (var name in names)
        {
            var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(caster, enabled);
                break;
            }
        }
    }

    private static void TryRebuild(ShadowCaster2D caster)
    {
        var method = caster.GetType().GetMethod("GenerateShadowMesh", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method != null)
        {
            method.Invoke(caster, null);
        }
        else
        {
            caster.enabled = false;
            caster.enabled = true;
        }

        EditorUtility.SetDirty(caster);
    }
}
#endif

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class GenerateTilemapShadowCasters
{
    [MenuItem("Tools/2D Shadows/DEBUG: Generate ALL Paths (No Inversion)")]
    private static void GenerateDebug()
    {
        GenerateWithOptions(InversionMode.None, false, true);
    }

    [MenuItem("Tools/2D Shadows/Generate For Enclosed Cave (Invert Holes)")]
    private static void GenerateForCave()
    {
        GenerateWithOptions(InversionMode.InvertHoles, true, false);
    }

    [MenuItem("Tools/2D Shadows/Generate For Enclosed Cave (Invert NON-Holes)")]
    private static void GenerateForCaveInvertNonHoles()
    {
        GenerateWithOptions(InversionMode.InvertNonHoles, true, false);
    }

    [MenuItem("Tools/2D Shadows/Generate For Enclosed Cave (Invert LARGEST Only)")]
    private static void GenerateForCaveInvertLargestOnly()
    {
        GenerateWithOptions(InversionMode.InvertLargestOnly, false, false);
    }

    [MenuItem("Tools/2D Shadows/⭐ YOUR CASE: Ignore Largest, Invert 2nd Largest")]
    private static void GenerateForYourCase()
    {
        GenerateWithOptions(InversionMode.InvertSecondLargestOnly, true, false);
    }

    [MenuItem("Tools/2D Shadows/⭐ YOUR CASE 2: Keep Largest, Invert Holes")]
    private static void GenerateForYourCase2()
    {
        GenerateWithOptions(InversionMode.InvertHoles, false, false);
    }

    [MenuItem("Tools/2D Shadows/Generate For Enclosed Cave (Invert All)")]
    private static void GenerateForCaveInvertAll()
    {
        GenerateWithOptions(InversionMode.InvertAll, true, false);
    }

    [MenuItem("Tools/2D Shadows/Generate For Islands/Platforms")]
    private static void GenerateForIslands()
    {
        GenerateWithOptions(InversionMode.None, true, false);
    }

    private enum InversionMode
    {
        None,
        InvertAll,
        InvertHoles,
        InvertNonHoles,
        InvertLargestOnly,
        InvertSecondLargestOnly
    }

    private static void GenerateWithOptions(InversionMode inversionMode, bool ignoreLargestPath, bool debugMode)
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
        
        // Fix: Ensure the parent renderer doesn't receive its own shadows
        var wallRenderer = selected.GetComponent<Renderer>();
        if (wallRenderer != null && wallRenderer.receiveShadows)
        {
            wallRenderer.receiveShadows = false;
            EditorUtility.SetDirty(wallRenderer);
            Debug.Log($"[2D Shadows] Disabled 'Receive Shadows' on {selected.name} to prevent tiles from turning black.");
        }

        // Find the largest and second largest paths
        int largestPathIndex = -1;
        int secondLargestPathIndex = -1;
        float maxArea = -1f;
        
        // First pass: collect all areas
        var pathAreas = new List<(int index, float area, int pointCount)>();
        for (int i = 0; i < composite.pathCount; i++)
        {
            var pointCount = composite.GetPathPointCount(i);
            if (pointCount < 3) continue;
            
            var points = new Vector2[pointCount];
            composite.GetPath(i, points);
            
            float area = CalculatePolygonArea(points);
            pathAreas.Add((i, area, pointCount));
        }
        
        // Sort by area descending
        pathAreas.Sort((a, b) => b.area.CompareTo(a.area));
        
        if (pathAreas.Count > 0)
        {
            largestPathIndex = pathAreas[0].index;
            maxArea = pathAreas[0].area;
            
            if (ignoreLargestPath)
            {
                Debug.Log($"[2D Shadows] Largest path (bounding box) at index {largestPathIndex}, area {maxArea}, points {pathAreas[0].pointCount} - will be ignored.");
            }
            else
            {
                Debug.Log($"[2D Shadows] Largest path at index {largestPathIndex}, area {maxArea}, points {pathAreas[0].pointCount} - will be included (DEBUG mode).");
            }
            
            if (pathAreas.Count > 1)
            {
                secondLargestPathIndex = pathAreas[1].index;
                Debug.Log($"[2D Shadows] Second largest path at index {secondLargestPathIndex}, area {pathAreas[1].area}, points {pathAreas[1].pointCount}");
            }
            
            for (int i = 1; i < pathAreas.Count; i++)
            {
                Debug.Log($"[2D Shadows] Path {pathAreas[i].index}: area {pathAreas[i].area}, points {pathAreas[i].pointCount}");
            }
        }

        for (int pathIndex = 0; pathIndex < composite.pathCount; pathIndex++)
        {
            // Skip the largest path only if not in debug mode
            if (ignoreLargestPath && pathIndex == largestPathIndex)
            {
                continue;
            }
            
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

            // Calculate area for this path
            float area = CalculatePolygonArea(points);
            float signedArea = GetSignedArea(localPoints);
            bool isHole = signedArea > 0;
            
            string pathLabel = $"Path_{pathIndex}_Area{area:F0}_Points{pointCount}";
            if (pathIndex == largestPathIndex) pathLabel += "_LARGEST";
            if (isHole) pathLabel += "_HOLE";
            
            // Invert if needed
            bool shouldInvert = false;
            if (inversionMode == InversionMode.InvertAll)
            {
                shouldInvert = true;
            }
            else if (inversionMode == InversionMode.InvertHoles && isHole)
            {
                shouldInvert = true;
            }
            else if (inversionMode == InversionMode.InvertNonHoles && !isHole)
            {
                shouldInvert = true;
            }
            else if (inversionMode == InversionMode.InvertLargestOnly && pathIndex == largestPathIndex)
            {
                shouldInvert = true;
            }
            else if (inversionMode == InversionMode.InvertSecondLargestOnly && pathIndex == secondLargestPathIndex)
            {
                shouldInvert = true;
            }
            
            if (shouldInvert)
            {
                localPoints.Reverse();
                Debug.Log($"[2D Shadows] Inverted {pathLabel}");
            }

            var simplified = Simplify(localPoints, 0.0005f, 0.001f);
            if (simplified.Count < 3)
            {
                continue;
            }

            var child = new GameObject(pathLabel);
            child.transform.SetParent(parent.transform, false);
            child.layer = parent.layer; // Critical: Ensure it's on the same layer as the Tilemap

            var caster = child.AddComponent<ShadowCaster2D>();
            caster.selfShadows = false; 
            
            // Critical: Disable silhouette renderer source so it uses our custom path points
            TrySetUseRendererSilhouette(caster, false);
            TrySetHasRenderer(caster, false);
            
            TrySetShapePath(caster, simplified.ToArray());
            TryEnableCustomPath(caster, true);
            
            // Set some common properties that might be needed
            TrySetShadowIntensity(caster, 1.0f);
            TrySetHasNoShadows(caster, false);
            
            // Force it to affect all sorting layers
            TrySetApplyToAllSortingLayers(caster, true);
            
            // For holes in enclosed caves, sometimes unchecking "Is Closed Path" 
            // is better, but reversing winding is usually enough.
            // TrySetIsClosedPath(caster, !isHole); 
            
            TryRebuild(caster);
            
            if (simplified.Count > 0)
            {
                var worldFirst = caster.transform.TransformPoint(simplified[0]);
                Debug.Log($"[2D Shadows] Generated {child.name} with {simplified.Count} points. First point world pos: {worldFirst}");
            }
        }

        Selection.activeGameObject = parent;
        
        string modeText = "Unknown mode";
        if (debugMode)
        {
            modeText = "DEBUG MODE (all paths)";
        }
        else if (inversionMode == InversionMode.InvertAll)
        {
            modeText = "Enclosed Cave (Invert ALL)";
        }
        else if (inversionMode == InversionMode.InvertHoles)
        {
            modeText = "Enclosed Cave (Invert HOLES only)";
        }
        else if (inversionMode == InversionMode.InvertNonHoles)
        {
            modeText = "Enclosed Cave (Invert NON-HOLES only)";
        }
        else if (inversionMode == InversionMode.InvertLargestOnly)
        {
            modeText = "Enclosed Cave (Invert LARGEST path only)";
        }
        else if (inversionMode == InversionMode.InvertSecondLargestOnly)
        {
            modeText = "YOUR CASE: Ignore Largest, Invert 2nd Largest";
        }
        else
        {
            modeText = "Islands/Platforms mode";
        }
        EditorUtility.DisplayDialog("2D Shadows", $"ShadowCasters generated in {modeText}.\n\nIMPORTANT: Ensure your Light2D has 'Shadows' enabled and its 'Shadow Filter' includes the layer of this Tilemap.", "OK");
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

    private static void TrySetHasRenderer(ShadowCaster2D caster, bool hasRenderer)
    {
        var field = caster.GetType().GetField("m_HasRenderer", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null) field.SetValue(caster, hasRenderer);
    }

    private static void TrySetApplyToAllSortingLayers(ShadowCaster2D caster, bool applyToAll)
    {
        var type = caster.GetType();
        var field = type.GetField("m_ApplyToSortingLayers", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            if (field.FieldType == typeof(bool))
            {
                field.SetValue(caster, applyToAll);
            }
            else if (field.FieldType == typeof(int[]))
            {
                var layers = SortingLayer.layers;
                var ids = new int[layers.Length];
                for (int i = 0; i < layers.Length; i++) ids[i] = layers[i].id;
                field.SetValue(caster, ids);
            }
            else
            {
                field.SetValue(caster, -1);
            }
        }
    }

    private static float GetSignedArea(List<Vector3> points)
    {
        float area = 0;
        for (int i = 0; i < points.Count; i++)
        {
            var p1 = points[i];
            var p2 = points[(i + 1) % points.Count];
            area += (p2.x - p1.x) * (p2.y + p1.y);
        }
        return area;
    }

    private static float CalculatePolygonArea(Vector2[] points)
    {
        float area = 0f;
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 p1 = points[i];
            Vector2 p2 = points[(i + 1) % points.Length];
            area += (p1.x * p2.y - p2.x * p1.y);
        }
        return Mathf.Abs(area * 0.5f);
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
        var type = caster.GetType();
        var field = type.GetField("m_ShapePath", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(caster, path);
            
            // Force a hash update if possible
            var hashField = type.GetField("m_ShapePathHash", BindingFlags.NonPublic | BindingFlags.Instance);
            if (hashField != null)
            {
                int hash = 0;
                foreach (var p in path) hash ^= p.GetHashCode();
                hashField.SetValue(caster, hash);
            }
        }
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

    private static void TrySetIsClosedPath(ShadowCaster2D caster, bool isClosed)
    {
        var type = caster.GetType();
        var names = new[] { "m_IsClosedPath", "m_ClosedPath", "m_IsClosed" };
        foreach (var name in names)
        {
            var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(caster, isClosed);
                break;
            }
        }
    }

    private static void TryRebuild(ShadowCaster2D caster)
    {
        var type = caster.GetType();
        
        // Try calling the internal Rebuild method if it exists
        var methods = new[] { "OnEnable", "GenerateShadowMesh", "Rebuild" };
        foreach (var methodName in methods)
        {
            var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if (method != null)
            {
                method.Invoke(caster, null);
            }
        }
        
        EditorUtility.SetDirty(caster);
    }
}
#endif

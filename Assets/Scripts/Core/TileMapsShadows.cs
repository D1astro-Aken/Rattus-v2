using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

#if UNITY_EDITOR

public class ShadowCaster2DCreator : MonoBehaviour
{
	public enum ShadowSource
	{
		CompositeCollider,
		Tilemap
	}

	[Header("Settings")]
	[SerializeField]
	private ShadowSource source = ShadowSource.Tilemap;

	[SerializeField]
	private bool selfShadows = true;

	[SerializeField]
	[Tooltip("Offset the shadow caster on the Z axis.")]
	private float zOffset = 0f;

	[SerializeField]
	[Range(0.01f, 1.5f)]
	[Tooltip("Scales the shadow shape. < 1.0 shrinks it (good for clipping issues).")]
	private float scaleModifier = 0.95f;

	[Header("Composite Collider Settings")]
	[SerializeField]
	[Tooltip("If true, ignores the path with the largest area (usually the bounding box in Outlines mode).")]
	private bool ignoreLargestPath = true;

	private CompositeCollider2D tilemapCollider;
	private Tilemap tilemap;

	static readonly FieldInfo meshField = typeof(ShadowCaster2D).GetField("m_Mesh", BindingFlags.NonPublic | BindingFlags.Instance);
	static readonly FieldInfo shapePathField = typeof(ShadowCaster2D).GetField("m_ShapePath", BindingFlags.NonPublic | BindingFlags.Instance);
	static readonly FieldInfo shapePathHashField = typeof(ShadowCaster2D).GetField("m_ShapePathHash", BindingFlags.NonPublic | BindingFlags.Instance);
	static readonly MethodInfo generateShadowMeshMethod = typeof(ShadowCaster2D)
									.Assembly
									.GetType("UnityEngine.Rendering.Universal.ShadowUtility")
									.GetMethod("GenerateShadowMesh", BindingFlags.Public | BindingFlags.Static);

	public void Create()
	{
		DestroyOldShadowCasters();

		if (source == ShadowSource.CompositeCollider)
		{
			CreateFromCompositeCollider();
		}
		else if (source == ShadowSource.Tilemap)
		{
			CreateFromTilemap();
		}
	}

	private void CreateFromCompositeCollider()
	{
		tilemapCollider = GetComponent<CompositeCollider2D>();
		if (tilemapCollider == null)
		{
			Debug.LogError("CompositeCollider2D missing!");
			return;
		}

		int pathCount = tilemapCollider.pathCount;
		int largestPathIndex = -1;
		float maxArea = -1f;

		// Find largest path if needed
		if (ignoreLargestPath)
		{
			for (int i = 0; i < pathCount; i++)
			{
				Vector2[] pts = new Vector2[tilemapCollider.GetPathPointCount(i)];
				tilemapCollider.GetPath(i, pts);
				float area = CalculatePolygonArea(pts);
				if (area > maxArea)
				{
					maxArea = area;
					largestPathIndex = i;
				}
			}
		}

		for (int i = 0; i < pathCount; i++)
		{
			if (ignoreLargestPath && i == largestPathIndex) continue;

			Vector2[] pathVertices = new Vector2[tilemapCollider.GetPathPointCount(i)];
			tilemapCollider.GetPath(i, pathVertices);
			
			CreateShadowCaster(pathVertices, "shadow_caster_collider_" + i);
		}
	}

	private void CreateFromTilemap()
	{
		tilemap = GetComponent<Tilemap>();
		if (tilemap == null)
		{
			Debug.LogError("Tilemap component missing!");
			return;
		}

		// Simple Horizontal Strip Merging
		BoundsInt bounds = tilemap.cellBounds;
		TileBase[] allTiles = tilemap.GetTilesBlock(bounds);
		
		// Use a visited set or just iterate rows
		// iterating rows is easier for strips
		
		int width = bounds.size.x;
		int height = bounds.size.y;

		for (int y = 0; y < height; y++)
		{
			int startX = -1;
			for (int x = 0; x < width; x++)
			{
				TileBase tile = allTiles[x + y * width];
				bool hasTile = tile != null; // Simple check. Can add collider type check if needed.

				if (hasTile)
				{
					if (startX == -1) startX = x;
				}
				else
				{
					if (startX != -1)
					{
						// End of strip
						CreateStrip(startX, x - 1, y, bounds);
						startX = -1;
					}
				}
			}
			if (startX != -1)
			{
				CreateStrip(startX, width - 1, y, bounds);
			}
		}
	}

	private void CreateStrip(int startX, int endX, int y, BoundsInt bounds)
	{
		// Convert grid coords to local space vertices
		// Cell bounds min is the offset
		int gridX = bounds.xMin + startX;
		int gridY = bounds.yMin + y;
		int gridEndX = bounds.xMin + endX;

		// We assume standard 1x1 cell size for simplicity, or use CellToLocal
		// But CellToLocal returns center.
		// Let's assume the grid is rectangular.
		
		Vector3 cellSize = tilemap.layoutGrid.cellSize;
		Vector3 centerStart = tilemap.CellToLocal(new Vector3Int(gridX, gridY, 0));
		Vector3 centerEnd = tilemap.CellToLocal(new Vector3Int(gridEndX, gridY, 0));
		
		Vector3 bl = centerStart - cellSize * 0.5f;
		Vector3 tl = centerStart + new Vector3(-cellSize.x, cellSize.y, 0) * 0.5f;
		Vector3 tr = centerEnd + cellSize * 0.5f;
		Vector3 br = centerEnd + new Vector3(cellSize.x, -cellSize.y, 0) * 0.5f;

		// Correct winding to Counter-Clockwise (CCW) for proper rendering
		// Order: BL -> BR -> TR -> TL
		Vector2[] path = new Vector2[] { bl, br, tr, tl };
		
		CreateShadowCaster(path, $"shadow_caster_tile_{gridX}_{gridY}");
	}

	private void CreateShadowCaster(Vector2[] pathVertices, string name)
	{
		GameObject shadowCaster = new GameObject(name);
		shadowCaster.transform.parent = gameObject.transform;
		shadowCaster.transform.localPosition = new Vector3(0, 0, zOffset);
		shadowCaster.transform.localScale = Vector3.one;

		ShadowCaster2D shadowCasterComponent = shadowCaster.AddComponent<ShadowCaster2D>();
		shadowCasterComponent.selfShadows = this.selfShadows;

		Vector3[] testPath = new Vector3[pathVertices.Length];
		
		// Calculate Centroid for scaling
		Vector2 center = Vector2.zero;
		foreach(var p in pathVertices) center += p;
		center /= pathVertices.Length;

		for (int j = 0; j < pathVertices.Length; j++)
		{
			Vector2 dir = pathVertices[j] - center;
			testPath[j] = center + dir * scaleModifier;
		}

		// Try using public SetPath API first (Unity 2021+)
		bool apiSuccess = false;
		try 
		{
			var setPathMethod = typeof(ShadowCaster2D).GetMethod("SetPath", BindingFlags.Public | BindingFlags.Instance);
			var setPathHashMethod = typeof(ShadowCaster2D).GetMethod("SetPathHash", BindingFlags.Public | BindingFlags.Instance);
			
			if (setPathMethod != null && setPathHashMethod != null)
			{
				setPathMethod.Invoke(shadowCasterComponent, new object[] { testPath.ToArray() });
				setPathHashMethod.Invoke(shadowCasterComponent, new object[] { Random.Range(int.MinValue, int.MaxValue) });
				apiSuccess = true;
			}
		}
		catch { /* Ignore and fallback to reflection */ }

		if (!apiSuccess)
		{
			if (shapePathField != null) shapePathField.SetValue(shadowCasterComponent, testPath);
			if (shapePathHashField != null) shapePathHashField.SetValue(shadowCasterComponent, Random.Range(int.MinValue, int.MaxValue));
			if (meshField != null) meshField.SetValue(shadowCasterComponent, new Mesh());
			
			if (generateShadowMeshMethod != null)
			{
				generateShadowMeshMethod.Invoke(shadowCasterComponent,
				new object[] { meshField.GetValue(shadowCasterComponent), shapePathField.GetValue(shadowCasterComponent) });
			}
			else
			{
				Debug.LogError("Could not find GenerateShadowMesh method. Shadows may not appear.");
			}
		}
	}

	private float CalculatePolygonArea(Vector2[] points)
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

	public void DestroyOldShadowCasters()
	{
		var tempList = transform.Cast<Transform>().ToList();
		foreach (var child in tempList)
		{
			if (child.name.StartsWith("shadow_caster_"))
			{
				DestroyImmediate(child.gameObject);
			}
		}
	}
}

[CustomEditor(typeof(ShadowCaster2DCreator))]
public class ShadowCaster2DTileMapEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Create"))
		{
			var creator = (ShadowCaster2DCreator)target;
			creator.Create();
		}

		if (GUILayout.Button("Remove Shadows"))
		{
			var creator = (ShadowCaster2DCreator)target;
			creator.DestroyOldShadowCasters();
		}
		EditorGUILayout.EndHorizontal();
	}

}

#endif

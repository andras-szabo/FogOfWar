using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicHeightmapAdjuster : MonoBehaviour
{
	public Transform minBounds;
	public Transform maxBounds;

	public Color gizmoColor = Color.white;
	public SerializedRelativeHM serializedHeightMap;

	private RelativeHeightMap relativeHeightMap;
	private bool isPlacedOnMap;

	public Vector3 Size
	{
		get
		{
			Vector3 currentAngles = transform.rotation.eulerAngles;
			if (!Mathf.Approximately(currentAngles.x, 0f) ||
				!Mathf.Approximately(currentAngles.y, 0f) ||
				!Mathf.Approximately(currentAngles.z, 0f))
			{
				Quaternion actualRotation = transform.rotation;
				transform.rotation = Quaternion.identity;
				Vector3 unrotatedSize = maxBounds.position - minBounds.position;
				transform.rotation = actualRotation;
				return unrotatedSize;
			}

			return maxBounds.position - minBounds.position;
		}
	}

	private void Awake()
	{
		relativeHeightMap = new RelativeHeightMap(serializedHeightMap);
	}

	private void OnEnable()
	{
		if (!isPlacedOnMap)
		{
			StartCoroutine(PlaceOnMapRoutine());
		}
	}

	private IEnumerator PlaceOnMapRoutine()
	{
		while (relativeHeightMap == null || VM20.DiscoveryMap == null)
		{
			yield return null;
		}

		PlaceOnMap(VM20.DiscoveryMap);
	}

	private void OnDisable()
	{
		if (isPlacedOnMap && VM20.DiscoveryMap != null)
		{
			RemoveFromMap(VM20.DiscoveryMap);
		}
	}

	public void PlaceOnMap(DiscoveryMap map)
	{
		relativeHeightMap.CalculateActualHeight(Size.y, map.terrainSize.y);
		AdjustHeightMap(map, add: true);
		isPlacedOnMap = true;
	}

	public void RemoveFromMap(DiscoveryMap map)
	{
		AdjustHeightMap(map, add: false);
		isPlacedOnMap = false;
	}

	private void AdjustHeightMap(DiscoveryMap map, bool add)
	{
		Vector2 mapCentre = map.CalculateMapPosition(transform.position);
		Vector2 mapSpaceSize = map.CalculateMapSpaceDimensions(Size);

		float orientation = Mathf.Deg2Rad * transform.eulerAngles.y;
		float cos = Mathf.Cos(orientation);
		float sin = Mathf.Sin(orientation);

		Vector2 right = new Vector2(cos, sin);
		Vector2 up = new Vector2(-sin, cos);

		Vector2 mapStartPos = mapCentre - (right * mapSpaceSize.x / 2f) - (up * mapSpaceSize.y / 2f);
		IntVector2 mapStartIndex = new IntVector2(Mathf.RoundToInt(mapStartPos.x), Mathf.RoundToInt(mapStartPos.y));

		var actualHeights = relativeHeightMap.actualHeights;

		Vector2 pos = mapStartPos;
		float sign = add ? 1f : -1f;

		for (int y = 0; y < mapSpaceSize.y; ++y)
		{
			for (int x = 0; x < mapSpaceSize.x; ++x)
			{
				IntVector2 customHeightMapIndex = new IntVector2(x * relativeHeightMap.sizeX / mapSpaceSize.x,
																 y * relativeHeightMap.sizeY / mapSpaceSize.y);

				IntVector2 nextCustomHeightMapIndex = new IntVector2((x + 1) * relativeHeightMap.sizeX / mapSpaceSize.x,
																	 (y + 1) * relativeHeightMap.sizeY / mapSpaceSize.y);

				int affectedHeightmapPixelCount = 0;
				float heightAdjustmentValue = 0f;
				bool paintedAtLeastOnePixel = false;

				for (int cy = customHeightMapIndex.y; !paintedAtLeastOnePixel || cy < nextCustomHeightMapIndex.y; ++cy)
				{
					for (int cx = customHeightMapIndex.x; !paintedAtLeastOnePixel || cx < nextCustomHeightMapIndex.x; ++cx)
					{
						paintedAtLeastOnePixel = true;

						if (cx >= 0 && cx < relativeHeightMap.sizeX &&
							cy >= 0 && cy < relativeHeightMap.sizeY)
						{
							float heightDelta = actualHeights[cy, cx] * sign;

							affectedHeightmapPixelCount++;
							heightAdjustmentValue += heightDelta;
						}
					}
				}

				if (affectedHeightmapPixelCount > 0)
				{
					Vector2 nextMapPos = mapStartPos + right * x + up * y;

					int mapIndexX = Mathf.RoundToInt(nextMapPos.x);
					int mapIndexY = Mathf.RoundToInt(nextMapPos.y);

					map.heightMap[mapIndexY, mapIndexX] += (heightAdjustmentValue / affectedHeightmapPixelCount);
				}
			}
		}
	}

	#region Gizmos
	private Vector3[] gizmoEdges;

	private void OnDrawGizmos()
	{
		if (minBounds && maxBounds)
		{
			DrawBoundingCube(minBounds.position, maxBounds.position, gizmoColor);
		}
	}

	private void DrawBoundingCube(Vector3 bottomLeft, Vector3 topRight, Color color)
	{
		if (transform.hasChanged || gizmoEdges == null)
		{
			gizmoEdges = new Vector3[8];

			Vector3 size = Size;

			Vector3 fwrd = transform.forward * size.z;
			Vector3 right = transform.right * size.x;
			Vector3 up = transform.up * size.y;

			gizmoEdges[0] = bottomLeft;
			gizmoEdges[1] = bottomLeft + up;
			gizmoEdges[2] = bottomLeft + up + right;
			gizmoEdges[3] = bottomLeft + right;

			gizmoEdges[4] = bottomLeft + fwrd;
			gizmoEdges[5] = bottomLeft + fwrd + up;
			gizmoEdges[6] = topRight;
			gizmoEdges[7] = bottomLeft + fwrd + right;
		}

		Gizmos.color = color;

		for (int i = 0; i < 3; ++i)
		{
			Gizmos.DrawLine(gizmoEdges[i], gizmoEdges[i + 1]);
			Gizmos.DrawLine(gizmoEdges[i + 4], gizmoEdges[i + 5]);
			Gizmos.DrawLine(gizmoEdges[i], gizmoEdges[i + 4]);
		}

		Gizmos.DrawLine(gizmoEdges[3], gizmoEdges[0]);
		Gizmos.DrawLine(gizmoEdges[7], gizmoEdges[4]);
	}
	#endregion
}


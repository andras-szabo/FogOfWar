using System.Collections.Generic;
using UnityEngine;

// As in: VisibilityManager 2.0
[RequireComponent(typeof(Terrain))]
public class VM20 : MonoBehaviour
{
	// Working arrays, so we only allocate them once
	private static int[] wDeltaX = new int[8];
	private static int[] wDeltaY = new int[8];
	private static float[] wBlockerHeight = new float[8];
	private static Color32 discovered = new Color32(255, 255, 255, 255);

	public static VM20 Instance { get; private set; }

	public static DiscoveryMap DiscoveryMap
	{
		get
		{
			return Instance?.discoveryMap;
		}
	}

	public static void UpdateDiscoveryStatus(DiscoveryMap discoveryMap, List<Observer> activeObservers, int updateSpread, int updateCycle)
	{
		bool didUpdateAnything = false;
		bool isLastUpdateInCycle = updateSpread - 1 == updateCycle;

		for (int i = updateCycle; i < activeObservers.Count; i += updateSpread)
		{
			var observer = activeObservers[i];
			observer.HasUpdatedEver = true;

			int startX = Mathf.RoundToInt(observer.mapPositionX);
			int startY = Mathf.RoundToInt(observer.mapPositionY);

			// Draw 1/8th of a circle
			int r = Mathf.RoundToInt(observer.ViewRadiusMapUnits);
			int deltaX = r;
			int deltaY = 0;

			do
			{
				if (r <= deltaY * deltaY)
				{
					deltaX -= 1;
					r += 2 * deltaX + 1;
				}

				for (int j = 0; j < 1; ++j)
				{
					bool didUpdateAnytingInArc = DiscoverLOS(discoveryMap, deltaX, deltaY, startX, startY, observer.height);
					if (didUpdateAnytingInArc)
					{
						didUpdateAnything = true;
					}
				}

				deltaY++;
			} while (deltaY < deltaX);
		}

		if (didUpdateAnything)
		{
			discoveryMap.texture.SetPixels32(discoveryMap.asPixelBlock);
		}

		discoveryMap.currentVisibilityMap.SetPixels32(discoveryMap.currentVisibilityPixelBlock);

		if (isLastUpdateInCycle)
		{
			discoveryMap.texture.Apply();
			discoveryMap.currentVisibilityMap.Apply();

			discoveryMap.ClearCurrentVisibilityBlock();	
		}
	}

	private static bool DiscoverLOS(DiscoveryMap map, int deltaX, int deltaY, int startX, int startY, float obsHeight)
	{
		var pixelBlock = map.asPixelBlock;
		var currentVisibilityBlock = map.currentVisibilityPixelBlock;

		var heightArray = map.heightMap;

		int mapPixelWidth = map.pixelWidth;
		int mapPixelHeight = map.pixelHeight;

		pixelBlock[startY * mapPixelWidth + startX] = discovered;

		// This is essentially a line-drawing routine that has some strong assumptions:
		// namely that deltaX > deltaY. Then, after the calculation of each point in the line,
		// it mirrors it 7 times, to replicate a filled circle.

		int counter = deltaX / 2;
		int currentX = startX;
		int currentY = startY;

		bool didUpdate = false;
		float startHeight = heightArray[startY, startX] + obsHeight;

		for (int i = 0; i < 8; ++i)
		{
			// Negative height means no blocking height found.
			wBlockerHeight[i] = -startHeight;
		}

		int maxLen = pixelBlock.Length;

		for (int i = 0; i < deltaX; ++i)
		{
			counter += deltaY;
			if (counter > deltaX)
			{
				counter -= deltaX;
				currentY += 1;
			}

			currentX += 1;

			if (currentX < 0 || currentX >= mapPixelWidth || currentY < 0 || currentY >= mapPixelHeight)
			{
				continue;
			}

			int dx = currentX - startX;
			int dy = currentY - startY;

			wDeltaX[0] = dx; wDeltaY[0] = dy;
			wDeltaX[1] = -dx; wDeltaY[1] = dy;
			wDeltaX[2] = dx; wDeltaY[2] = -dy;
			wDeltaX[3] = -dx; wDeltaY[3] = -dy;

			wDeltaX[4] = dy; wDeltaY[4] = dx;
			wDeltaX[5] = -dy; wDeltaY[5] = dx;
			wDeltaX[6] = dy; wDeltaY[6] = -dx;
			wDeltaX[7] = -dy; wDeltaY[7] = -dx;

			for (int j = 0; j < 8; ++j)
			{
				int y = startY + wDeltaY[j];
				int x = startX + wDeltaX[j];

				if (x < 0 || x >= mapPixelWidth || y < 0 || y >= mapPixelHeight)
				{
					continue;
				}

				float currentHeight = heightArray[startY + wDeltaY[j], startX + wDeltaX[j]];

				// Found a blocker if the blocker height is > 0f
				if (wBlockerHeight[j] < 0f)
				{
					if (currentHeight > startHeight)
					{
						wBlockerHeight[j] = currentHeight;
					}
				}

				int index = ((startY + wDeltaY[j]) * mapPixelWidth) + startX + wDeltaX[j];

				bool hasNotFoundBlockerYet = wBlockerHeight[j] < 0f;
				bool currentHeightHigherThanLastBlocker = currentHeight >= wBlockerHeight[j];

				if (hasNotFoundBlockerYet || currentHeightHigherThanLastBlocker)
				{
					if (currentHeight > wBlockerHeight[j] && !hasNotFoundBlockerYet)
					{
						wBlockerHeight[j] = currentHeight;
					}

					if (index < maxLen)
					{
						didUpdate = didUpdate || pixelBlock[i].a <= 0;

						pixelBlock[index] = discovered;
						currentVisibilityBlock[index] = discovered;

						// Paint an extra pixel to cover accidental holes

						if (index + mapPixelWidth < maxLen)
						{
							pixelBlock[index + mapPixelWidth] = discovered;
							currentVisibilityBlock[index + mapPixelWidth] = discovered;
						}
					}
				}
			}
		}

		return didUpdate;
	}

	public Material discoveryMapMaterial;
	public Material stencilMarkerMaterial;
	public float updateIntervalSeconds = 0.1f;

	public int updateSpread = 4;
	private int currentUpdateCycle = 0;

	private DiscoveryMap discoveryMap;
	private Terrain terrain;
	private Camera mainCamera;
	private Transform mainCamTransform;
	private Vector3[] mainCamFrustumPos = new Vector3[4];

	private void Awake()
	{
		Instance = this;
		terrain = GetComponent<Terrain>();
	}

	private void Start()
	{
		discoveryMap = new DiscoveryMap(terrain);

		discoveryMapMaterial.SetTexture("_Mask", discoveryMap.texture);
		stencilMarkerMaterial.SetTexture("_CurrentVisibilityMap", discoveryMap.currentVisibilityMap);
		discoveryMapMaterial.SetVector("_TerrainSize", discoveryMap.terrainSize);
		stencilMarkerMaterial.SetVector("_TerrainSize", discoveryMap.terrainSize);

		mainCamera = Camera.main;
		mainCamTransform = mainCamera.transform;
	}

	private void Update()
	{
		UpdateCamera();

		UpdateDiscoveryStatus(discoveryMap, EntityManager.GetActiveObservers(), updateSpread, currentUpdateCycle);
		currentUpdateCycle = (currentUpdateCycle + 1) % updateSpread;
	}

	private void OnDestroy()
	{
		discoveryMap?.Clear();
	}

	private void UpdateCamera()
	{
		mainCamera.CalculateFrustumCorners(mainCamera.rect, mainCamera.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, mainCamFrustumPos);

		// The shader expects directions to the view frustum
		// corners in world space.

		discoveryMapMaterial.SetVector("_CamBottomLeft", mainCamTransform.TransformVector(mainCamFrustumPos[0]));
		discoveryMapMaterial.SetVector("_CamTopLeft", mainCamTransform.TransformVector(mainCamFrustumPos[1]));
		discoveryMapMaterial.SetVector("_CamTopRight", mainCamTransform.TransformVector(mainCamFrustumPos[2]));
		discoveryMapMaterial.SetVector("_CamBottomRight", mainCamTransform.TransformVector(mainCamFrustumPos[3]));
	}

}

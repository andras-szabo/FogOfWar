using System.Collections.Generic;
using System.Threading.Tasks;
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

	private static List<Observer.Info> observerInfo;
	private static Color32[] pixelBlock;
	private static Color32[] currentVisibilityBlock;
	private static float[,] heightArray;
	private static int mapPixelWidth;
	private static int mapPixelHeight;

	public static void UpdateDiscoveryStatusOnSideThread(DiscoveryMap discoveryMap, List<Observer> activeObservers)
	{
		if (observerInfo == null)
		{
			observerInfo = new List<Observer.Info>(activeObservers.Count);
		}
		else
		{
			observerInfo.Clear();
		}

		for (int i = 0; i < activeObservers.Count; ++i)
		{
			observerInfo.Add(new Observer.Info(activeObservers[i]));
		}

		discoveryMap.ClearCurrentVisibilityBlock();

		pixelBlock = discoveryMap.asPixelBlock;
		currentVisibilityBlock = discoveryMap.currentVisibilityPixelBlock;
		heightArray = discoveryMap.heightMap;
		mapPixelWidth = discoveryMap.pixelWidth;
		mapPixelHeight = discoveryMap.pixelHeight;

		for (int i = 0; i < activeObservers.Count; ++i)
		{
			var observer = observerInfo[i];

			int startX = Mathf.RoundToInt(observer.mapPositionX);
			int startY = Mathf.RoundToInt(observer.mapPositionY);

			// Draw 1/8th of a circle
			int r = Mathf.RoundToInt(observer.viewRadiusMapUnits);
			int deltaX = r;
			int deltaY = 0;

			do
			{
				if (r <= deltaY * deltaY)
				{
					deltaX -= 1;
					r += 2 * deltaX + 1;
				}

				DiscoverLOS(discoveryMap, deltaX, deltaY, startX, startY, observer.height);
				deltaY++;
			} while (deltaY < deltaX);
		}
	}

	private static bool DiscoverLOS(DiscoveryMap map, int deltaX, int deltaY, int startX, int startY, float obsHeight)
	{
		pixelBlock[startY * mapPixelWidth + startX] = discovered;

		// This is essentially a line-drawing routine that has some strong assumptions:
		// namely that deltaX > deltaY. Then, after the calculation of each point in the line,
		// it mirrors it 7 times, to replicate a filled circle.

		bool didUpdate = false;
		float startHeight = heightArray[startY, startX] + obsHeight;

		for (int i = 0; i < 8; ++i)
		{
			// Negative height means no blocking height found.
			wBlockerHeight[i] = -startHeight;
		}

		int maxLen = pixelBlock.Length;

		for (int circleSegment = 0; circleSegment < 8; ++circleSegment)
		{
			int counter = deltaX / 2;
			int currentX = startX;
			int currentY = startY;

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

				// Depending on the segment of the circle we're in, we have to mirror
				// the original coordinate pair of (dx, dy), in all combinations of (+/-dx and +/-dy),
				// e.g. in the 1st segment, the pair turns into (-dx, dy), in the next one into (dx, -dy) etc.

				int wdx = circleSegment < 4 ? dx : dy;
				int wdy = circleSegment < 4 ? (circleSegment < 2 ? dy : -dy) : (circleSegment < 6 ? dx : -dx);
				if (circleSegment % 2 != 0) { wdx *= -1; }

				int y = startY + wdy;
				int x = startX + wdx;

				if (x < 0 || x >= mapPixelWidth || y < 0 || y >= mapPixelHeight)
				{
					continue;
				}

				float currentHeight = heightArray[y, x];

				// Found a blocker if the blocker height is > 0f
				if (wBlockerHeight[circleSegment] < 0f)
				{
					if (currentHeight > startHeight)
					{
						wBlockerHeight[circleSegment] = currentHeight;
					}
				}

				int index = (y * mapPixelWidth) + x;

				bool hasNotFoundBlockerYet = wBlockerHeight[circleSegment] < 0f;
				bool currentHeightHigherThanLastBlocker = currentHeight >= wBlockerHeight[circleSegment];

				if (hasNotFoundBlockerYet || currentHeightHigherThanLastBlocker)
				{
					if (currentHeight > wBlockerHeight[circleSegment] && !hasNotFoundBlockerYet)
					{
						wBlockerHeight[circleSegment] = currentHeight;
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
	public float updateIntervalSeconds = 0.1f;

	public int updateSpread = 4;
	private int currentUpdateCycle = 0;

	private DiscoveryMap discoveryMap;
	private Terrain terrain;
	private Camera mainCamera;
	private Transform mainCamTransform;
	private Vector3[] mainCamFrustumPos = new Vector3[4];

	private Task updateTask;

	public bool IsVisible(Vector3 worldPosition)
	{
		return discoveryMap != null && discoveryMap.IsWorldPositionVisible(worldPosition);
	}

	private void Awake()
	{
		Instance = this;
		terrain = GetComponent<Terrain>();
	}

	private void Start()
	{
		discoveryMap = new DiscoveryMap(terrain);

		discoveryMapMaterial.SetTexture("_Mask", discoveryMap.texture);
		discoveryMapMaterial.SetTexture("_CurrentVisibility", discoveryMap.currentVisibilityMap);
		discoveryMapMaterial.SetVector("_TerrainSize", discoveryMap.terrainSize);

		mainCamera = Camera.main;
		mainCamTransform = mainCamera.transform;
	}

	private void Update()
	{
		UpdateCamera();

		if (updateTask == null || updateTask.IsCompleted)
		{
			if (updateTask != null)
			{
				discoveryMap.texture.SetPixels32(discoveryMap.asPixelBlock);
				discoveryMap.currentVisibilityMap.SetPixels32(discoveryMap.currentVisibilityPixelBlock);
				discoveryMap.texture.Apply(false);
				discoveryMap.currentVisibilityMap.Apply(false);
			}
			updateTask = UpdateOnSideThread();
		}
	}

	private async Task UpdateOnSideThread()
	{
		await Task.Run(() => UpdateDiscoveryStatusOnSideThread(discoveryMap, EntityManager<Observer>.GetActiveEntities()))
				  .ConfigureAwait(false);
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

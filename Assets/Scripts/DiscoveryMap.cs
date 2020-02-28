using UnityEngine;

public class DiscoveryMap
{
	public DiscoveryMap(Terrain terrain)
	{
		this.terrain = terrain;

		pixelWidth = terrain.terrainData.heightmapWidth;
		pixelHeight = terrain.terrainData.heightmapHeight;
		blockLength = pixelWidth * pixelHeight;

		texture = new Texture2D(pixelWidth, pixelHeight, TextureFormat.RGBA32, false);
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.filterMode = FilterMode.Bilinear;
		texture.SetPixels32(new Color32[pixelWidth * pixelHeight]);

		currentVisibilityMap = new Texture2D(pixelWidth, pixelHeight, TextureFormat.RGBA32, false);
		currentVisibilityMap.SetPixels32(new Color32[pixelWidth * pixelHeight]);

		terrainOffset = terrain.GetPosition();
		terrainSize = terrain.terrainData.size;
		mapSize = new Vector2(pixelWidth, pixelHeight);

		asPixelBlock = new Color32[pixelWidth * pixelHeight];
		currentVisibilityPixelBlock = new Color32[pixelWidth * pixelHeight];
		emptyVisibilityBlock = new Color32[pixelWidth * pixelHeight];

		heightMap = terrain.terrainData.GetHeights(0, 0, pixelWidth, pixelHeight);
	}

	public bool IsWorldPositionVisible(Vector3 position)
	{
		float mapPositionX = (position.x - terrainOffset.x) / terrainSize.x * mapSize.x;
		float mapPositionY = (position.z - terrainOffset.z) / terrainSize.z * mapSize.y;

		int x = Mathf.RoundToInt(mapPositionX);
		int z = Mathf.RoundToInt(mapPositionY);

		int index = z * pixelWidth + x;

		return index >= 0 && index < blockLength && currentVisibilityMap.GetPixel(x, z).a > 0f;
	}

	public void ClearCurrentVisibilityBlock()
	{
		emptyVisibilityBlock.CopyTo(currentVisibilityPixelBlock, 0);
	}

	public void Clear()
	{
		Object.Destroy(currentVisibilityMap);
		Object.Destroy(texture);
	}

	public Vector2 CalculateMapPosition(Vector3 worldPosition)
	{
		float x = (worldPosition.x - terrainOffset.x) / terrainSize.x * mapSize.x;
		float y = (worldPosition.z - terrainOffset.z) / terrainSize.z * mapSize.y;

		return new Vector2(x, y);
	}

	public Vector2 CalculateMapSpaceDimensions(Vector3 worldSpaceSize)
	{
		return new Vector2(Mathf.Abs(worldSpaceSize.x / terrainSize.x * mapSize.x),
						   Mathf.Abs(worldSpaceSize.z / terrainSize.z * mapSize.y));
	}

	public readonly Terrain terrain;

	public readonly Texture2D texture;
	public readonly Texture2D currentVisibilityMap;

	public readonly Vector3 terrainOffset;
	public readonly Vector3 terrainSize;
	public readonly Vector2 mapSize;

	public readonly Color32[] asPixelBlock;
	public readonly Color32[] currentVisibilityPixelBlock;
	public readonly Color32[] emptyVisibilityBlock;

	public readonly int blockLength;

	public readonly int pixelWidth;
	public readonly int pixelHeight;

	public readonly float[,] heightMap;
}

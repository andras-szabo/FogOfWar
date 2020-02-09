using UnityEngine;

public class DiscoveryMap
{
	public DiscoveryMap(Terrain terrain)
	{
		this.terrain = terrain;

		pixelWidth = terrain.terrainData.heightmapWidth;
		pixelHeight = terrain.terrainData.heightmapHeight;

		texture = new Texture2D(pixelWidth, pixelHeight, TextureFormat.RGBA32, false);
		texture.SetPixels(new Color[pixelWidth * pixelHeight]);

		terrainOffset = terrain.GetPosition();
		terrainSize = terrain.terrainData.size;
		mapSize = new Vector2(pixelWidth, pixelHeight);

		asPixelBlock = new Color32[pixelWidth * pixelHeight];
		heightMap = terrain.terrainData.GetHeights(0, 0, pixelWidth, pixelHeight);
	}

	public void Clear()
	{
		Object.Destroy(texture);
	}

	public readonly Terrain terrain;
	public readonly Texture2D texture;
	public readonly Vector3 terrainOffset;
	public readonly Vector3 terrainSize;
	public readonly Vector2 mapSize;

	public readonly Color32[] asPixelBlock;
	public readonly int pixelWidth;
	public readonly int pixelHeight;

	public readonly float[,] heightMap;
}

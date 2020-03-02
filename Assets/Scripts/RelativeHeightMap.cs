public class RelativeHeightMap
{
	public readonly int sizeX, sizeY;
	public readonly float[,] relativeHeights;
	public float[,] actualHeights;

	public RelativeHeightMap(SerializedRelativeHM hm)
	{
		sizeX = hm.sizeX;
		sizeY = hm.sizeY;
		relativeHeights = new float[sizeY, sizeX];
		actualHeights = new float[sizeY, sizeX];
		int i = 0;
		for (int y = 0; y < sizeY; ++y)
		{
			for (int x = 0; x < sizeX; ++x)
			{
				relativeHeights[y, x] = hm.heights[i];
				i++;
			}
		}
	}

	public RelativeHeightMap(int sizeX, int sizeY, float[,] relativeHeights)
	{
		this.sizeX = sizeX;
		this.sizeY = sizeY;
		this.relativeHeights = relativeHeights;
		actualHeights = new float[sizeY, sizeX];
	}

	public void CalculateActualHeight(float worldUnitMaxHeight, float heightMapSizeY)
	{
		float heightFactor = worldUnitMaxHeight / heightMapSizeY;
		for (int y = 0; y < sizeY; ++y)
		{
			for (int x = 0; x < sizeX; ++x)
			{
				actualHeights[y, x] = relativeHeights[y, x] * heightFactor;
			}
		}
	}
}

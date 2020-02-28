using UnityEngine;

public struct IntVector2 
{
	public int x, y;

	public IntVector2(int x, int y)
	{
		this.x = x;
		this.y = y;
	}

	public IntVector2(float x, float y)
	{
		this.x = Mathf.RoundToInt(x);
		this.y = Mathf.RoundToInt(y);
	}
}

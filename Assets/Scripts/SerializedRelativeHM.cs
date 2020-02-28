using UnityEngine;

[CreateAssetMenu()]
public class SerializedRelativeHM : ScriptableObject
{
	public int sizeX;
	public int sizeY;

	[Tooltip("Starting at bottom left, column first")]	
	public float[] heights;
}

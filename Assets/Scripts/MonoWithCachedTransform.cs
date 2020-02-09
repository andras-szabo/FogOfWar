using UnityEngine;

public class MonoWithCachedTransform : MonoBehaviour
{
	private Transform cachedTransform;
	public Transform CachedTransform
	{
		get
		{
			return cachedTransform ?? (cachedTransform = transform);
		}
	}
}

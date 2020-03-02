using System.Collections;
using UnityEngine;

public class Observer : MonoWithCachedTransform
{
	public struct Info
	{
		public float mapPositionX, mapPositionY, height;
		public float viewRadiusMapUnits;

		public Info(Observer o)
		{
			mapPositionX = o.mapPositionX;
			mapPositionY = o.mapPositionY;
			height = o.height;
			viewRadiusMapUnits = o.ViewRadiusMapUnits;
		}
	}

	[HideInInspector] public float mapPositionX, mapPositionY, height;
	[Range(1f, 50f)]  public float viewRadiusWorldUnits;

	public float ViewRadiusMapUnits { get; private set; }
	public bool HasMovedSinceLastUpdate { get; private set; }

	private int entityID;
	private Vector3 terrainOffset;
	private Vector3 terrainSize;
	private Vector2 discoveryMapSize;

	private IEnumerator Start()
	{
		while (EntityManager.Instance == null || VM20.DiscoveryMap == null)
		{
			yield return null;	
		}

		entityID = EntityManager.Instance.RegisterAndGetID(this);
		Setup(viewRadiusWorldUnits, VM20.DiscoveryMap);
	}

	private void Update()
	{
		HasMovedSinceLastUpdate |= UpdatePosition(CachedTransform.position);	
	}

	private void OnDestroy()
	{
		EntityManager.TryUnregister(this, entityID);	
	}

	public void Setup(float worldRadius, DiscoveryMap map)
	{
		terrainOffset = map.terrainOffset;
		terrainSize = map.terrainSize;
		discoveryMapSize = map.mapSize;

		UpdatePosition(CachedTransform.position);
		UpdateRadius(worldRadius);
	}

	public bool UpdatePosition(Vector3 worldPosition)
	{
		float newMapPositionX = (worldPosition.x - terrainOffset.x) / terrainSize.x * discoveryMapSize.x;
		float newMapPositionY = (worldPosition.z - terrainOffset.z) / terrainSize.z * discoveryMapSize.y;

		float newHeight = (worldPosition.y - terrainOffset.y) / terrainSize.y;

		if (!Mathf.Approximately(newMapPositionX, mapPositionX) ||
			!Mathf.Approximately(newMapPositionY, mapPositionY) ||
			!Mathf.Approximately(newHeight, height))
		{
			mapPositionX = newMapPositionX;
			mapPositionY = newMapPositionY;
			height = newHeight;
			return true;
		}

		return false;
	}

	public void UpdateRadius(float worldRadius)
	{
		ViewRadiusMapUnits = worldRadius * discoveryMapSize.x / terrainSize.x;
	}
}

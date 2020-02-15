using System;
using UnityEngine;

public class EnemyUnitController : MonoWithCachedTransform
{
	public const string TAG_PLAYER = "Player";

	public event Action<bool> OnVisibilityStateChanged;

	public float visibilityCheckPeriodSeconds = 0.25f;

	private bool isVisible = false;
	private int observerCount;
	private float remainingSecondsTillCheck = 0f;

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag(TAG_PLAYER))
		{
			observerCount++;
			remainingSecondsTillCheck = 0f;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag(TAG_PLAYER))
		{
			observerCount--;
			Debug.Assert(observerCount >= 0, "EnemyUnitController: Negative observer count reached.");

			if (observerCount == 0)
			{
				if (isVisible)
				{
					isVisible = false;
					OnVisibilityStateChanged?.Invoke(isVisible);
				}
			}
		}
	}

	private void Update()
	{
		if (observerCount > 0)
		{
			remainingSecondsTillCheck -= Time.deltaTime;
			if (remainingSecondsTillCheck <= 0f)
			{
				remainingSecondsTillCheck += visibilityCheckPeriodSeconds;
				bool isVisibleNow = VM20.Instance.IsVisible(CachedTransform.position);
				if (isVisibleNow != isVisible)
				{
					isVisible = isVisibleNow;
					OnVisibilityStateChanged?.Invoke(isVisible);
				}
			}
		}
	}
}

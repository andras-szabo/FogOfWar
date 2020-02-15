using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyUnitScalerView : MonoWithCachedTransform
{
	public EnemyUnitController controller;

	private Vector3 originalScale;
	private Vector3 currentScale;
	private float elapsedSinceScaleStart;
	private const float SCALE_DURATION_SECS = 5f;
	private const float SCALE_FACTOR_TARGET = 10f;

	private bool isVisible;

	private void Start()
	{
		if (controller)
		{
			controller.OnVisibilityStateChanged += HandleVisibilityChanged;
		}

		originalScale = CachedTransform.localScale;
		StartCoroutine(ScaleRoutine(originalScale, 0f));
	}

	private void OnDestroy()
	{
		if (controller)
		{
			controller.OnVisibilityStateChanged -= HandleVisibilityChanged;
		}
	}

	private void HandleVisibilityChanged(bool isVisible)
	{
		this.isVisible = isVisible;
	}

	private void OnDrawGizmos()
	{
		if (Application.isPlaying)
		{
			if (!isVisible)
			{
				Gizmos.color = Color.cyan;
				Gizmos.DrawWireSphere(CachedTransform.position, currentScale.x * 0.5f);
			}
		}
	}

	private IEnumerator ScaleRoutine(Vector3 startingScale, float elapsedSinceScaleStart)
	{
		float expansionDuration = SCALE_DURATION_SECS / 3f;
		float compressionDuration = SCALE_DURATION_SECS - expansionDuration;

		float elapsed = elapsedSinceScaleStart;

		Vector3 targetScale = originalScale * SCALE_FACTOR_TARGET;

		currentScale = startingScale;
		CachedTransform.localScale = currentScale;

		while (true)
		{
			while (elapsed < expansionDuration)
			{
				elapsed += Time.deltaTime;	
				yield return null;
				currentScale = Vector3.Lerp(originalScale, targetScale, elapsed / expansionDuration);

				if (isVisible)
				{
					CachedTransform.localScale = currentScale;
				}
			}

			if (isVisible)
			{
				CachedTransform.localScale = targetScale;
			}

			while (elapsed < SCALE_DURATION_SECS)
			{
				elapsed += Time.deltaTime;
				yield return null;
				currentScale = Vector3.Lerp(targetScale, originalScale, (elapsed - expansionDuration) / compressionDuration);

				if (isVisible)
				{
					CachedTransform.localScale = currentScale;
				}
			}

			if (isVisible)
			{
				CachedTransform.localScale = originalScale;
			}

			elapsed = 0f;
		}
	}
}

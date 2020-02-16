using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraInputHandler : MonoWithCachedTransform
{
	public const float GROUND_LEVEL_HEIGHT = 0f;
	public const float NOT_SET = -1f;

	[Range(10f, 360f)] public float orbitDegreesPerSecond = 90f;
	[Range(1f, 50f)] public float moveUnitsPerSecond = 32f;

	private float _distanceToOrbitCentre = NOT_SET;

	private Vector3 _orbitTarget;
	private Vector3 _orbitTargetOnGround;
	private Vector3 _fromOrbitTargetToCameraFlat;

	private bool _isPitchDirty;

#if UNITY_EDITOR
	public bool setPitchDirty;
#endif

	public float GetDistanceToOrbitCentre()
	{
		if (_isPitchDirty || _distanceToOrbitCentre < 0f)
		{
			float height = CachedTransform.position.y - GROUND_LEVEL_HEIGHT;
			float angleToGround = 90f - CachedTransform.rotation.eulerAngles.x;
			_distanceToOrbitCentre = Mathf.Tan(Mathf.Deg2Rad * angleToGround) * height;
		}

		return _distanceToOrbitCentre;
	}

	public void CalculateOrbitCentre()
	{
		float distanceToOrbitCentre = GetDistanceToOrbitCentre();

		Vector3 xzOrientation = new Vector3(CachedTransform.forward.x, 0f, CachedTransform.forward.z).normalized;

		_orbitTarget = xzOrientation * distanceToOrbitCentre + CachedTransform.position;
		_fromOrbitTargetToCameraFlat = -xzOrientation * distanceToOrbitCentre;

		_orbitTargetOnGround = _orbitTarget;
		_orbitTargetOnGround.y = GROUND_LEVEL_HEIGHT;
	}

	private void Update()
    {
		float orbitChange = Input.GetAxis("Horizontal") * orbitDegreesPerSecond;
		float speedChange = Input.GetAxis("Vertical") * moveUnitsPerSecond;

		if (!Mathf.Approximately(orbitChange, 0f))
		{
			CalculateOrbitCentre();
			HandleRotation(orbitChange);
		}

		if (!Mathf.Approximately(speedChange, 0f))
		{
			HandleMove(speedChange);
		}

#if UNITY_EDITOR
		if (setPitchDirty)
		{
			_isPitchDirty = true;
			setPitchDirty = false;
		}
#endif
	}

	private void HandleMove(float speedChangeUnitsPerSecond)
	{
		float delta = speedChangeUnitsPerSecond * Time.deltaTime;
		Vector3 flatForward = new Vector3(CachedTransform.forward.x, 0f, CachedTransform.forward.z).normalized;

		Vector3 newPosition = CachedTransform.position + flatForward * delta;
		CachedTransform.SetPositionAndRotation(newPosition, CachedTransform.rotation);
	}

	private void HandleRotation(float orbitChangeAnglesPerSecond)
	{
		float angles = orbitChangeAnglesPerSecond * Time.deltaTime;

		Quaternion rotation = Quaternion.AngleAxis(angles, Vector3.up);
		Vector3 camPos = (rotation * _fromOrbitTargetToCameraFlat) + _orbitTarget;
		Quaternion camRot = Quaternion.Euler(new Vector3(0f, angles, 0f)) * CachedTransform.rotation;

		CachedTransform.SetPositionAndRotation(camPos, camRot);
	}

	private void OnDrawGizmos()
	{
		if (Application.isPlaying)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(_orbitTargetOnGround, 2f);
		}
	}
}

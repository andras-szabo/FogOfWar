using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CarSteeringWithoutNavmeshAgent : MonoWithCachedTransform
{
	private const float MAX_DISTANCE = 5f;

	private class PathRequest
	{
		public PathRequest(Vector3 destination, NavMeshPath path)
		{
			isPending = false;
			this.destination = destination;
			this.path = path;
		}

		public NavMeshPath path;
		public Vector3 destination;
		public bool isPending;
	}

	[Range(0, 8)] public int agentTypeID;
	[Range(-5f, 5f)] public float verticalOffset = 1f;

	private float lastTargetTimestamp;
	private NavMeshPath path;
	private PathRequest pathRequest;
	private NavMeshQueryFilter navMeshQueryFilter;

	private void RequestPath(PathRequest request)
	{
		if (TryFindNavMeshPosition(request.destination, out Vector3 targetPosition))
		{
			NavMesh.CalculatePath(CachedTransform.position, targetPosition, navMeshQueryFilter, request.path);
			request.isPending = false;
		}
		else
		{
			request.path = null;
			request.isPending = false;
		}
	}

	private bool TryFindNavMeshPosition(Vector3 target, out Vector3 targetOnNavMesh)
	{
		if (NavMesh.SamplePosition(target, out NavMeshHit hit, MAX_DISTANCE, navMeshQueryFilter))
		{
			targetOnNavMesh = hit.position;
			return true;
		}

		targetOnNavMesh = target;
		return false;
	}

	private void ForceOnNavMesh()
	{
		if (NavMesh.SamplePosition(CachedTransform.position, out NavMeshHit hit, MAX_DISTANCE, navMeshQueryFilter))
		{
			CachedTransform.position = hit.position + new Vector3(0f, verticalOffset, 0f);
			currentNavMeshPosition = hit.position;
		}
	}

	private void Awake()
	{
		navMeshQueryFilter = new NavMeshQueryFilter();
		navMeshQueryFilter.agentTypeID = agentTypeID;
		navMeshQueryFilter.areaMask = NavMesh.AllAreas;

		maxRadiansDelta = angularSpeedPerSec * Mathf.Deg2Rad * Time.fixedDeltaTime;
	}

	public bool IsMoving { get; private set; }

	public float maxSlowdownDistance = 10f;
	public float accelerationPerSec = 5f;
	public float angularSpeedPerSec = 180f;
	public float maxSpeedPerSec = 8f;
	public float stoppingDistance = 0.25f;

	public float maxTurnAngle = 52f;    // When speed is lowest
	public float minTurnAngle = 28f;    // When speed is highest

	private const float MIN_SLOWDOWN_DISTANCE = 1f;

	private int steeringTargetID;
	private int lastSteeringTargetID;
	private bool isSlowingDown = false;
	private bool shouldSlowDown = false;
	private float currentSpeed = 0f;
	private float speedAtSlowdownStart = 0f;
	private float totalSlowdownDistance = 0f;
	private float targetSpeed = 0f;

	private Vector3 currentNavMeshPosition;
	private Vector3 steeringTarget;
	private float maxRadiansDelta;

	private void Update()
	{
		ForceOnNavMesh();

		if (ControlByMouse.LastTargetSetTimestamp > lastTargetTimestamp)
		{
			path = new NavMeshPath();
			lastTargetTimestamp = ControlByMouse.LastTargetSetTimestamp;
			pathRequest = new PathRequest(ControlByMouse.TargetPosition, path);
			RequestPath(pathRequest);

			if (path.status != NavMeshPathStatus.PathInvalid)
			{
				path.GetCornersNonAlloc(pathCorners);
				steeringTargetID++;
				steeringTarget = pathCorners[1];
				IsMoving = true;
			}
		}

		float currentMaxTurnHalfRad = 0f;

		if (IsMoving)
		{
			if (steeringTargetID != lastSteeringTargetID)
			{
				lastSteeringTargetID = steeringTargetID;
				isSlowingDown = false;
			}

			float currentMaxTurnHalf = Mathf.Lerp(maxTurnAngle, minTurnAngle, currentSpeed / maxSpeedPerSec);
			currentMaxTurnHalfRad = currentMaxTurnHalf * Mathf.Deg2Rad;
			bool isSteeringTargetInViewCone = IsSteeringTargetInViewCone(steeringTarget, currentSpeed, currentMaxTurnHalfRad, out float degreesToTurn);

			if (!isSteeringTargetInViewCone)
			{

			}

			float currentSlowdownDistance = Mathf.Lerp(MIN_SLOWDOWN_DISTANCE, maxSlowdownDistance, currentSpeed / maxSpeedPerSec);
			float remainingDistance = Vector3.Magnitude(currentNavMeshPosition - steeringTarget);

			if (remainingDistance < currentSlowdownDistance)
			{
				// This is where we should slow down for the next corner to; but for now, it will
				// simply be 1 destination, that's it.
				shouldSlowDown = true;
			}
			else
			{
				shouldSlowDown = false;
			}

			if (!isSlowingDown && !shouldSlowDown)
			{
				if (currentSpeed < maxSpeedPerSec)
				{
					int accelerateChangeSign = 1;

					if (currentSpeed > 0f)
					{
						float remainingTime = remainingDistance / currentSpeed;
						float turnableAngles = remainingTime * currentMaxTurnHalf * 2f;
						accelerateChangeSign = (int)Mathf.Sign(turnableAngles - degreesToTurn);
					}

					currentSpeed += (accelerationPerSec * accelerateChangeSign * Time.fixedDeltaTime);
				}
			}
			else
			{
				if (!isSlowingDown)
				{
					isSlowingDown = true;
					speedAtSlowdownStart = currentSpeed;
					totalSlowdownDistance = currentSlowdownDistance;

					// This is where we could calculate the ideal cornering speed. But for now just
					// set this to 0.
					targetSpeed = 0f;
				}

				currentSpeed = Mathf.Lerp(targetSpeed, speedAtSlowdownStart, remainingDistance / totalSlowdownDistance);
				if (remainingDistance < stoppingDistance)
				{
					currentSpeed = 0f;
					IsMoving = false;
				}
			}
		}

		if (IsMoving)
		{
			//TODO what if we're on a slope!
			CachedTransform.forward = Vector3.RotateTowards(CachedTransform.forward,
															steeringTarget - currentNavMeshPosition,
															currentMaxTurnHalfRad * Time.fixedDeltaTime * 2f, 0f);
		}

		CachedTransform.position += CachedTransform.forward * currentSpeed * Time.fixedDeltaTime;

		// - but on top of a few things.
		//		= if starting (from standstill):
		//			= check if the steering target is in the view cone (max turn angle).
		//			= if it is, jolly good, start accelerating and turning towards it.	
		//			= if it is not, we'll have to do a U-turn or a Y-turn.
		//				= check the direction into which I have to turn _after_ the next corner.	
		//				= if that direction is roughly the same as the current one, prefer a Y turn; otherwise prefer a U-turn.
		//				= do a box cast to see if it's possible to do a U turn (left or right, depending on where to go)
		//				= do a box cast to see if it's possible to do a Y turn right
		//				= do a box cast to see if it's possible to do a Y turn left
		//					= if no cast succeeds, try again in a couple of frames (+ maybe randomly accelerate a little bit FW or back?)
		//				= if we do have options (at least 1), then perform it:
		//					= how to do a u-turn? v1:
		//						= just start turning as much as you possibly can, and accelerate,
		//						  making sure not to overshoot:
		//								= i have left S distance to cover.
		//								= at the current speed, i will cover this in T.
		//								= during T, I can turn a maximum of A angles.
		//								= if A < the angle I need to turn, then I need to decelerate.
		//								= if A == the angle I need to turn, then I should hold speed.
		//								= if A > the angle I need to turn, I can accelerate.
		//								
		//					= how to do a y-turn?
		//						= choose preferred direction: opposite the side we have to go to.
		//						= start reversing towards that, turning as much as possible, until:
		//							= until the steering target is in the view cone. then stop
		//							= ... and start a normal turn.
		//
		//
		//				= maybe make it so that the view cone changes based on velocity - a little bit.
		//				= and maybe make it so that the NEXT point is also considered in overshooting / cornering speed.
		//	
		//		= then next thing: deal with obstacles, uneven terrain, slopes (although that we have, hit.normal, no?).

	}

	private bool IsSteeringTargetInViewCone(Vector3 steeringTarget, float currentSpeed, float currentMaxTurnAngleRad, out float degreesToTurn)
	{
		Vector3 flatForward = CachedTransform.forward;
		flatForward.y = 0f;
		flatForward.Normalize();

		Vector3 toTarget = steeringTarget - CachedTransform.position;
		toTarget.y = 0f;
		toTarget.Normalize();

		float dot = Vector3.Dot(flatForward, toTarget);
		degreesToTurn = Mathf.Acos(dot) * Mathf.Rad2Deg;
		return dot > Mathf.Cos(currentMaxTurnAngleRad);
	}

	private Vector3[] pathCorners = new Vector3[32];
	private void OnDrawGizmosSelected()
	{
		if (Application.isPlaying)
		{
			if (path != null && path.status != NavMeshPathStatus.PathInvalid)
			{
				Gizmos.color = Color.cyan;
				Vector3 prev = pathCorners[0];
				int cornerCount = path.GetCornersNonAlloc(pathCorners);
				for (int i = 1; i < cornerCount; ++i)
				{
					Gizmos.DrawLine(prev, pathCorners[i]);
					prev = pathCorners[i];
				}
			}
		}
	}

}

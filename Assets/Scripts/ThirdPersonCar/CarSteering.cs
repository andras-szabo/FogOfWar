using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CarSteering : MonoWithCachedTransform
{
    public Transform frontLeftWheel;
    public Transform frontRightWheel;

    private NavMeshAgent agent;
    private float lastTargetTimestamp;

    private Vector3 previousSteeringTarget;
    private Quaternion targetWheelRotation;
    private Quaternion startingWheelRotation;
    private float elapsedInWheelRotation;
    private float wheelRotationDuration;
    private float cosMaxTurnAngle;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        cosMaxTurnAngle = Mathf.Cos(45F * Mathf.Deg2Rad);
    }

    private void Update()
    {
        if (ControlByMouse.LastTargetSetTimestamp > lastTargetTimestamp)
        {
            lastTargetTimestamp = ControlByMouse.LastTargetSetTimestamp;
            agent.SetDestination(ControlByMouse.TargetPosition);
        }

        if (agent.velocity.sqrMagnitude > 0f)
        {
            var steeringTarget = agent.steeringTarget;

            if (steeringTarget != previousSteeringTarget)
            {
                steeringTarget.y = frontLeftWheel.position.y;
                startingWheelRotation = frontLeftWheel.rotation;
                Vector3 toSteeringTarget = (steeringTarget - transform.position).normalized;

                if (Vector3.Dot(toSteeringTarget, new Vector3(CachedTransform.forward.x, 0f, CachedTransform.forward.z).normalized) < cosMaxTurnAngle)
                {
                    // Check cross product to see if right or left turn
                    // then apply (sina, cosa) or (-sina, cosa) as toSteeringTarget
                }

                targetWheelRotation = Quaternion.LookRotation(toSteeringTarget, Vector3.up) * Quaternion.Euler(0f, 0f, 90f);
                elapsedInWheelRotation = 0f;
                wheelRotationDuration = Vector3.Angle(frontLeftWheel.forward, toSteeringTarget) / (agent.angularSpeed / 8f);

                previousSteeringTarget = steeringTarget;
            }

            elapsedInWheelRotation += Time.deltaTime;

            Quaternion wheelRotation = Quaternion.Lerp(startingWheelRotation, targetWheelRotation, 
                                                       elapsedInWheelRotation / wheelRotationDuration);

            frontLeftWheel.rotation = wheelRotation;
            frontRightWheel.rotation = wheelRotation;
        }
    }

    private Color GetNavAgentStatusColor()
    {
        if (agent == null)
        {
            return Color.black;
        }

        if (!agent.hasPath)
        {
            return Color.blue;
        }

        switch (agent.pathStatus)
        {
            case NavMeshPathStatus.PathComplete: return Color.green;
            case NavMeshPathStatus.PathInvalid: return Color.red;
            case NavMeshPathStatus.PathPartial: return Color.yellow;
        }

        return Color.white;
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = GetNavAgentStatusColor();
            Gizmos.DrawSphere(CachedTransform.position + Vector3.up * 2f, 0.5f);
        }
    }
}

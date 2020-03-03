using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class MinimapFrustumView : MonoBehaviour
{
    private LineRenderer lr;
    private LineRenderer LineRenderer
    {
        get
        {
            return lr ?? (lr = GetComponent<LineRenderer>());
        }
    }

    private void Start()
    {
        LineRenderer.useWorldSpace = true;
        LineRenderer.generateLightingData = false;
        LineRenderer.positionCount = 4;
        LineRenderer.loop = true;
    }

    public void Setup(Vector3[] camFrustumCornerWorldSpaceVectors, Vector3 camPos, Vector3 terrainOffset, Vector3 terrainSize)
    {
        // Assumption: frustum corner vectors are sent in some logical order, such
        // that we can connect each to the next one, with wraparound, to get the
        // final image.

        float camHeight = camPos.y - terrainOffset.y;
        Vector3 down = new Vector3(0f, -1f, 0f);

        Vector3[] frustumViewWorldPositions = new Vector3[4];

        for (int i = 0; i < 4; ++i)
        {
            Vector3 cornerVector = camFrustumCornerWorldSpaceVectors[i].normalized;

            float cosAngleToVertical = Vector3.Dot(cornerVector, down);

            float frustumCornerToGroundDistance = camHeight / cosAngleToVertical;
            
            Vector3 groundHitPosition = camPos + (frustumCornerToGroundDistance * cornerVector) - terrainOffset;

            frustumViewWorldPositions[i] = groundHitPosition;
        }

        LineRenderer.SetPositions(frustumViewWorldPositions);
     }
}

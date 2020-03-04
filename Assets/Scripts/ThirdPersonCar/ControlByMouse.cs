using UnityEngine;

public class ControlByMouse : MonoBehaviour
{
    private Camera mainCam;

    public static Vector3 TargetPosition { get; private set; }
    public static float LastTargetSetTimestamp { get; private set; }

    public bool keepTargetVisible;
    public float fadeOutDuration = 2f;

    private RaycastHit[] singleHitBuffer = new RaycastHit[1];
    private int terrainLayer;
    private Ray raycastRay;

    private void Start()
    {
        mainCam = Camera.main;
        terrainLayer = LayerMask.GetMask("Terrain");
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SetTargetPositionByMouseCoordinates(Input.mousePosition);
        }
    }

    private void SetTargetPositionByMouseCoordinates(Vector3 mousePositionScreenCoords)
    {
        raycastRay = mainCam.ScreenPointToRay(mousePositionScreenCoords);
        if (Physics.RaycastNonAlloc(raycastRay, singleHitBuffer, 500f, terrainLayer) > 0)
        {
            TargetPosition = singleHitBuffer[0].point;
            LastTargetSetTimestamp = Time.time;
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            float elapsed = Time.time - LastTargetSetTimestamp;

            if (!keepTargetVisible && elapsed > fadeOutDuration)
            {
                return;
            }

            Color opacity = Color.white;
            if (!keepTargetVisible)
            {
                float opacityAlpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                opacity.a = opacityAlpha;
            }

            Gizmos.color = Color.red * opacity;
            Gizmos.DrawSphere(TargetPosition, 0.5f);
        }
    }
}

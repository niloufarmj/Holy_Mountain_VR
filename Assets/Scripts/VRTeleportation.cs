using UnityEngine;

public class VRTeleportation : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public Transform controllerTransform;
    public Transform playerRoot;
    public LayerMask teleportLayers;
    public float maxDistance = 10f;
    public OVRInput.Button teleportButton = OVRInput.Button.SecondaryIndexTrigger;

    void Update()
    {
        bool isAiming = OVRInput.Get(teleportButton);

        if (isAiming)
        {
            Ray ray = new Ray(controllerTransform.position, controllerTransform.forward);
            RaycastHit hit;

            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, controllerTransform.position);

            if (Physics.Raycast(ray, out hit, maxDistance, teleportLayers))
            {
                lineRenderer.SetPosition(1, hit.point);

                if (OVRInput.GetUp(teleportButton))
                {
                    Vector3 newPosition = hit.point;
                    newPosition.y = playerRoot.position.y; // حفظ ارتفاع فعلی بازیکن
                    playerRoot.position = newPosition;
                }
            }
            else
            {
                lineRenderer.SetPosition(1, controllerTransform.position + controllerTransform.forward * maxDistance);
            }
        }
        else
        {
            lineRenderer.enabled = false;
        }
    }
}

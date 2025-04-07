using UnityEngine;

public class VRArcTeleport : MonoBehaviour
{
    [Header("References")]
    public Transform controllerTransform;
    public Transform playerRoot;
    public LineRenderer lineRenderer;
    public GameObject teleportTargetVisual;

    [Header("Settings")]
    public float arcVelocity = 10f;
    public int arcResolution = 30;
    public float maxDistance = 15f;
    public LayerMask teleportLayer;
    public OVRInput.Button teleportButton = OVRInput.Button.SecondaryIndexTrigger;

    private Vector3 hitPoint;
    private bool isValidTeleport;

    void Update()
    {
        bool isAiming = OVRInput.Get(teleportButton);

        if (isAiming)
        {
            Vector3[] arcPoints = new Vector3[arcResolution];
            Vector3 velocity = controllerTransform.forward * arcVelocity;
            Vector3 position = controllerTransform.position;
            isValidTeleport = false;

            lineRenderer.positionCount = arcResolution;

            for (int i = 0; i < arcResolution; i++)
            {
                arcPoints[i] = position;

                if (Physics.Raycast(position, velocity.normalized, out RaycastHit hit, velocity.magnitude * Time.fixedDeltaTime, teleportLayer))
                {
                    hitPoint = hit.point;
                    teleportTargetVisual.SetActive(true);
                    teleportTargetVisual.transform.position = hitPoint + Vector3.up * 0.01f;
                    isValidTeleport = true;

                    // کوتاه‌سازی آرک برای توقف خط در برخورد
                    lineRenderer.positionCount = i + 1;
                    break;
                }

                position += velocity * Time.fixedDeltaTime;
                velocity += Physics.gravity * Time.fixedDeltaTime;
            }

            lineRenderer.enabled = true;
            lineRenderer.SetPositions(arcPoints);
        }
        else
        {
            lineRenderer.enabled = false;
            teleportTargetVisual.SetActive(false);

            if (isValidTeleport && OVRInput.GetUp(teleportButton))
            {
                Vector3 newPosition = hitPoint;

                // گرفتن ارتفاع بدنه (مثلاً نصف ارتفاع کپسول)
                float bodyHeightOffset = 0.35f; // ← مقدار تستی، قابل تنظیم

                newPosition.y += bodyHeightOffset;

                playerRoot.position = newPosition;
            }
        }
    }
}

using UnityEngine;
using System.Collections;

public class SeedCollectible : MonoBehaviour
{
    private bool playerInRange = false;

    [Tooltip("نوع درختی که این بذر متعلق به آن است.")]
    public int prototypeIndex;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[SeedCollectible] OnTriggerEnter: {other.name}");

        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("[SeedCollectible] Player entered trigger zone.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log($"[SeedCollectible] OnTriggerExit: {other.name}");

        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("[SeedCollectible] Player exited trigger zone.");
        }
    }

    void Update()
    {
        if (playerInRange)
        {
            Debug.Log("[SeedCollectible] Player in range. Waiting for input...");

            // Check VR input or keyboard fallback
            bool isCollectPressed = false;

            if (OVRInput.IsControllerConnected(OVRInput.Controller.RTouch))
            {
                isCollectPressed = OVRInput.GetDown(OVRInput.Button.One); // A button on Oculus
            }
            else
            {
                isCollectPressed = Input.GetKeyDown(KeyCode.E); // fallback to E key on keyboard
            }

            if (isCollectPressed)
            {
                Debug.Log("[SeedCollectible] Collect input detected. Collecting...");
                Collect();
            }
        }
    }

    void Collect()
    {
        Debug.Log("[SeedCollectible] Collect() called!");

        GameStats stats = FindObjectOfType<GameStats>();
        if (stats != null)
        {
            stats.AddSeed(prototypeIndex);
            Debug.Log("[SeedCollectible] Seed count increased.");
        }
        else
        {
            Debug.LogWarning("[SeedCollectible] GameStats not found!");
        }

        Destroy(transform.parent.gameObject);
    }
}

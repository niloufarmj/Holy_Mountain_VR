using UnityEngine;
using System.Collections;

public class SeedCollectible : MonoBehaviour
{
    private bool playerInRange = false;

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

            if (OVRInput.GetDown(OVRInput.Button.One)) // دکمه A
            {
                Debug.Log("[SeedCollectible] Button A pressed. Collecting...");
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
            stats.AddSeed();
            Debug.Log("[SeedCollectible] Seed count increased.");
        }
        else
        {
            Debug.LogWarning("[SeedCollectible] GameStats not found!");
        }

        Destroy(transform.parent.gameObject);
    }
}

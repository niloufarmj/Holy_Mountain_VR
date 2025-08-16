using UnityEngine;
using System.Collections;

/// <summary>
/// Represents a seed item that the player can collect.
/// - Detects when the player enters/exits its trigger zone.
/// - Listens for a collect input (VR controller "A" button or keyboard "E").
/// - Adds a seed to <see cref="GameStats"/> when collected.
/// - Destroys its parent GameObject after collection.
/// </summary>
public class SeedCollectible : MonoBehaviour
{
    // Tracks if the player is currently inside the trigger area
    private bool playerInRange = false;

    [Tooltip("Tree prototype index this seed belongs to.")]
    public int prototypeIndex;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[SeedCollectible] OnTriggerEnter: {other.name}");

        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("[SeedCollectible] Player entered trigger zone.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"[SeedCollectible] OnTriggerExit: {other.name}");

        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("[SeedCollectible] Player exited trigger zone.");
        }
    }

    private void Update()
    {
        if (playerInRange)
        {
            Debug.Log("[SeedCollectible] Player in range. Waiting for input...");

            bool isCollectPressed = false;

            // Check VR input first, fallback to keyboard
            if (OVRInput.IsControllerConnected(OVRInput.Controller.RTouch))
            {
                isCollectPressed = OVRInput.GetDown(OVRInput.Button.One); // "A" button on Oculus Touch
            }
            else
            {
                isCollectPressed = Input.GetKeyDown(KeyCode.E); // fallback key
            }

            if (isCollectPressed)
            {
                Debug.Log("[SeedCollectible] Collect input detected. Collecting...");
                Collect();
            }
        }
    }

    /// <summary>
    /// Handles collection of this seed:
    /// - Increases seed count in <see cref="GameStats"/>.
    /// - Destroys the seed's parent object from the scene.
    /// </summary>
    private void Collect()
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

        // Destroy parent (assumes seed model is a child object)
        Destroy(transform.parent.gameObject);
    }
}

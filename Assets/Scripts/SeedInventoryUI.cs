using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays and controls the in-game seed inventory selection panel.
/// - Shows available seed counts per tree prototype.
/// - Lets the player open the panel (B / Key P), navigate with the right thumbstick,
///   and select a tree type to plant (consuming one seed).
/// - On selection, updates the <see cref="TreePlanter"/> and consumes the seed via <see cref="GameStats"/>.
/// </summary>
public class SeedInventoryUI : MonoBehaviour
{
    /// <summary>
    /// UI row definition for a single tree type entry.
    /// </summary>
    [System.Serializable]
    public class UIItem
    {
        [Tooltip("Background image for visual state (available/selected/disabled).")]
        public Image background;

        [Tooltip("Icon/image representing the tree type.")]
        public Image treeImage;

        [Tooltip("Text label that shows the available count (e.g., x3).")]
        public TextMeshProUGUI countText;

        [Tooltip("Tree prototype index represented by this UI entry.")]
        public int prototypeIndex;
    }

    [Header("UI Elements")]
    [Tooltip("List of UI entries representing different seed types.")]
    public UIItem[] items;

    [Tooltip("Root GameObject of the panel that will be toggled on/off.")]
    public GameObject panelRoot;

    [Header("UI Colors")]
    [Tooltip("Background color for the currently selected item.")]
    public Color selectedColor = new Color(0.47f, 1f, 0.47f);    // Green

    [Tooltip("Background color for available (but not selected) items.")]
    public Color availableColor = new Color(0.2f, 0.6f, 1f);     // Blue

    [Tooltip("Background color for items with zero seeds.")]
    public Color disabledColor = new Color(0.67f, 0.67f, 0.67f); // Gray

    // Navigation state
    private int selectedIndex = 0;
    private bool panelOpen = false;
    private float inputCooldown = 0.2f;
    private float lastInputTime;

    // References resolved at runtime
    private GameStats stats;
    private TreePlanter planter;

    private void Start()
    {
        stats = FindObjectOfType<GameStats>();
        planter = FindObjectOfType<TreePlanter>();
        UpdateUI();
        panelRoot.SetActive(false);
    }

    private void Update()
    {
        // Toggle/confirm: B button (OVR secondary) or P key
        if (OVRInput.GetDown(OVRInput.Button.Two) || Input.GetKeyDown(KeyCode.P)) // B
        {
            if (!panelOpen)
                OpenPanel();
            else
                TrySelect();
        }

        if (!panelOpen) return;

        // Horizontal navigation by right thumbstick (Secondary)
        float horizontal = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).x;

        if (Mathf.Abs(horizontal) > 0.5f && Time.time - lastInputTime > inputCooldown)
        {
            int dir = horizontal > 0 ? 1 : -1;
            selectedIndex = Mathf.Clamp(selectedIndex + dir, 0, items.Length - 1);
            UpdateUI();
            lastInputTime = Time.time;
        }
    }

    /// <summary>
    /// Opens the panel and resets selection to the first item.
    /// </summary>
    private void OpenPanel()
    {
        panelOpen = true;
        selectedIndex = 0;
        UpdateUI();
        panelRoot.SetActive(true);
    }

    /// <summary>
    /// Closes the panel.
    /// </summary>
    private void ClosePanel()
    {
        panelOpen = false;
        panelRoot.SetActive(false);
    }

    /// <summary>
    /// Attempts to select the current item:
    /// - Verifies seed availability.
    /// - Sets the planter's selected prototype index.
    /// - Tries to plant and consumes one seed on success.
    /// - Closes the panel afterward.
    /// </summary>
    private void TrySelect()
    {
        int proto = items[selectedIndex].prototypeIndex;
        if (stats.HasSeed(proto))
        {
            planter.selectedPrototypeIndex = proto;
            planter.TryPlantSelectedTree();
            stats.UseSeed(proto);
            ClosePanel();
        }
        else
        {
            Debug.Log("❌ No seeds for this tree type.");
        }
    }

    /// <summary>
    /// Refreshes all UI entries to reflect current counts and selection highlight.
    /// </summary>
    private void UpdateUI()
    {
        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            int count = GetSeedCount(item.prototypeIndex);
            item.countText.text = $"x{count}";

            if (count <= 0)
            {
                item.background.color = disabledColor;
            }
            else if (i == selectedIndex)
            {
                item.background.color = selectedColor;
            }
            else
            {
                item.background.color = availableColor;
            }
        }
    }

    /// <summary>
    /// Helper to retrieve current seed count for a given prototype index from <see cref="GameStats"/>.
    /// </summary>
    private int GetSeedCount(int protoIndex)
    {
        foreach (var entry in stats.seedInventory)
        {
            if (entry.prototypeIndex == protoIndex)
                return entry.count;
        }
        return 0;
    }
}

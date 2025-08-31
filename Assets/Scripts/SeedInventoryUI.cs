using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SeedInventoryUI
///
/// Handles the in-game seed inventory panel:
/// - Displays available seed counts for each tree prototype.
/// - Lets the player open the panel (B on controller / P on keyboard).
/// - Navigation is done with the right thumbstick (left/right).
/// - Selecting consumes one seed and passes the choice to <see cref="TreePlanter"/>.
/// - Seeds are tracked via <see cref="GameStats"/> and automatically update UI on change.
/// - Supports auto-closing after inactivity (configurable).
///
/// ✅ Status: Fully working.
/// </summary>
public class SeedInventoryUI : MonoBehaviour
{
    /// <summary>
    /// Represents one UI row/slot in the seed inventory.
    /// Contains references to UI visuals and the prototype index it represents.
    /// </summary>
    [System.Serializable]
    public class UIItem
    {
        [Tooltip("Background image for state coloring (selected, available, disabled).")]
        public Image background;

        [Tooltip("Icon for the tree type (sprite or image).")]
        public Image treeImage;

        [Tooltip("Text showing available count, e.g., 'x3'.")]
        public TextMeshProUGUI countText;

        [Tooltip("Tree prototype index this slot represents.")]
        public int prototypeIndex;
    }

    [Header("UI Elements")]
    [Tooltip("All the UI entries that represent seed types.")]
    public UIItem[] items;

    [Tooltip("Root panel GameObject that is enabled/disabled when opening/closing.")]
    public GameObject panelRoot;

    [Header("UI Colors")]
    [Tooltip("Color used for the currently selected item.")]
    public Color selectedColor = new Color(0.47f, 1f, 0.47f);    // Green
    [Tooltip("Color used for available (but not selected) items.")]
    public Color availableColor = new Color(0.2f, 0.6f, 1f);     // Blue
    [Tooltip("Color used for disabled items (zero seeds).")]
    public Color disabledColor = new Color(0.67f, 0.67f, 0.67f); // Gray

    // Runtime state
    private int selectedIndex = 0;
    public bool panelOpen = false;
    private float inputCooldown = 0.2f;
    private float lastInputTime;

    // References
    private GameStats stats;
    private TreePlanter planter;

    [Header("Auto-close")]
    [Tooltip("If true, the panel will auto-close after inactivity.")]
    public bool autoClose = true;
    [Tooltip("Seconds of inactivity before auto-closing if seeds are available.")]
    public float autoCloseSeconds = 10f;
    [Tooltip("Seconds of inactivity before auto-closing if inventory is empty.")]
    public float autoCloseWhenEmptySeconds = 5f;

    private float inactivityTimer = 0f;

    private void Start()
    {
        stats = FindObjectOfType<GameStats>();
        planter = FindObjectOfType<TreePlanter>();

        if (stats) stats.OnSeedsChanged += UpdateUI;
        UpdateUI();

        panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (stats) stats.OnSeedsChanged -= UpdateUI;
    }

    private void Update()
    {
        // --- Toggle panel with B button / P key ---
        if (OVRInput.GetDown(OVRInput.Button.Two) || Input.GetKeyDown(KeyCode.P))
        {
            if (!panelOpen)
                OpenPanel();
            else
                TrySelect();
        }

        if (!panelOpen) return;

        // --- Navigation with right thumbstick ---
        float horizontal = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).x;
        if (Mathf.Abs(horizontal) > 0.5f && Time.time - lastInputTime > inputCooldown)
        {
            int dir = horizontal > 0 ? 1 : -1;
            selectedIndex = Mathf.Clamp(selectedIndex + dir, 0, items.Length - 1);
            UpdateUI();
            lastInputTime = Time.time;
            MarkInteraction();
        }

        // --- Auto-close after inactivity ---
        if (autoClose)
        {
            inactivityTimer += Time.deltaTime;

            bool hasAny = false;
            for (int i = 0; i < items.Length; i++)
            {
                if (stats.HasSeed(items[i].prototypeIndex)) { hasAny = true; break; }
            }

            float limit = hasAny ? autoCloseSeconds : autoCloseWhenEmptySeconds;
            if (inactivityTimer >= limit)
                ClosePanel();
        }
    }

    /// <summary>
    /// Opens the panel, resets selection, locks planting input.
    /// </summary>
    private void OpenPanel()
    {
        panelOpen = true;
        inactivityTimer = 0f;
        selectedIndex = 0;
        UpdateUI();
        panelRoot.SetActive(true);

        if (planter) planter.inputLocked = true;
    }

    /// <summary>
    /// Closes the panel and unlocks planting input.
    /// </summary>
    private void ClosePanel()
    {
        panelOpen = false;
        panelRoot.SetActive(false);

        if (planter) planter.inputLocked = false;
    }

    /// <summary>
    /// Attempts to select the currently highlighted item.
    /// Consumes one seed and forwards the prototype to <see cref="TreePlanter"/>.
    /// </summary>
    private void TrySelect()
    {
        MarkInteraction();
        int proto = items[selectedIndex].prototypeIndex;
        if (stats.HasSeed(proto))
        {
            planter.selectedPrototypeIndex = proto;
            planter.TryPlantSelectedTree();
            ClosePanel();
        }
        else
        {
            Debug.Log("❌ No seeds for this tree type.");
        }
    }

    /// <summary>
    /// Refreshes UI entries (counts and background colors).
    /// </summary>
    private void UpdateUI()
    {
        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            int count = GetSeedCount(item.prototypeIndex);
            item.countText.text = $"x{count}";

            if (count <= 0)
                item.background.color = disabledColor;
            else if (i == selectedIndex)
                item.background.color = selectedColor;
            else
                item.background.color = availableColor;
        }
    }

    /// <summary>
    /// Returns the current count of a seed type.
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

    /// <summary>
    /// Resets inactivity timer after user input.
    /// </summary>
    private void MarkInteraction()
    {
        inactivityTimer = 0f;
    }
}

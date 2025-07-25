using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SeedInventoryUI : MonoBehaviour
{
    [System.Serializable]
    public class UIItem
    {
        public Image background;
        public Image treeImage;
        public TextMeshProUGUI countText;
        public int prototypeIndex;
    }

    public UIItem[] items;
    public GameObject panelRoot;

    public Color selectedColor = new Color(0.47f, 1f, 0.47f);    // سبز
    public Color availableColor = new Color(0.2f, 0.6f, 1f);     // آبی
    public Color disabledColor = new Color(0.67f, 0.67f, 0.67f); // خاکستری

    private int selectedIndex = 0;
    private bool panelOpen = false;
    private float inputCooldown = 0.2f;
    private float lastInputTime;

    private GameStats stats;
    private TreePlanter planter;

    void Start()
    {
        stats = FindObjectOfType<GameStats>();
        planter = FindObjectOfType<TreePlanter>();
        UpdateUI();
        panelRoot.SetActive(false);
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Two)) // B
        {
            if (!panelOpen)
                OpenPanel();
            else
                TrySelect();
        }

        if (!panelOpen) return;

        float horizontal = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).x;

        if (Mathf.Abs(horizontal) > 0.5f && Time.time - lastInputTime > inputCooldown)
        {
            int dir = horizontal > 0 ? 1 : -1;
            selectedIndex = Mathf.Clamp(selectedIndex + dir, 0, items.Length - 1);
            UpdateUI();
            lastInputTime = Time.time;
        }
    }

    void OpenPanel()
    {
        panelOpen = true;
        selectedIndex = 0;
        UpdateUI();
        panelRoot.SetActive(true);
    }

    void ClosePanel()
    {
        panelOpen = false;
        panelRoot.SetActive(false);
    }

    void TrySelect()
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

    void UpdateUI()
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

    int GetSeedCount(int protoIndex)
    {
        foreach (var entry in stats.seedInventory)
        {
            if (entry.prototypeIndex == protoIndex)
                return entry.count;
        }
        return 0;
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PedestrianToggleButton : MonoBehaviour
{
    [SerializeField] private IntersectionManager intersectionManager;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Color activeColor = new Color(0.10f, 0.70f, 0.25f, 1f);
    [SerializeField] private Color inactiveColor = new Color(0.85f, 0.20f, 0.20f, 1f);
    [SerializeField] private string activeText = "CRUCE ON";
    [SerializeField] private string inactiveText = "CRUCE OFF";

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(OnPress);
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnPress);
    }

    private void Start()
    {
        UpdateVisual();
    }

    private void OnPress()
    {
        if (intersectionManager == null) return;
        intersectionManager.TogglePedestrians();
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (intersectionManager == null) return;
        bool active = intersectionManager.PedestriansEnabled;
        if (fillImage != null) fillImage.color = active ? activeColor : inactiveColor;
        if (label != null) label.text = active ? activeText : inactiveText;
    }
}

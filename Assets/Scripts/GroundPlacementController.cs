using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Vuforia;
using Image = UnityEngine.UI.Image;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// AR sin tracker (Ground Plane): el Plane Finder busca planos en el centro de la pantalla (modo automatico,
/// con su indicador visible) y al tocar se coloca el objeto elegido sobre el ultimo plano detectado; si en el
/// centro no hay plano, se prueba un hit test en el punto tocado. El objeto se puede cambiar en cualquier
/// momento: el Ground Plane Stage se clona al colocar (DuplicateStage), asi que cambiar de opcion no afecta
/// a lo que ya esta puesto.
/// </summary>
public class GroundPlacementController : MonoBehaviour
{
    [Header("Vuforia")]
    [SerializeField] private PlaneFinderBehaviour planeFinder;
    [SerializeField] private float tapCooldown = 0.3f;
    [Tooltip("Cuanto tiempo vale un hit test: si el ultimo plano detectado es mas viejo, no se coloca")]
    [SerializeField] private float hitMaxAge = 0.75f;

    [Header("Objetos (hijos del Ground Plane Stage)")]
    [SerializeField] private GameObject[] options;
    [SerializeField] private int startIndex;

    [Header("Botones del selector")]
    [SerializeField] private Image[] optionButtonImages;
    [SerializeField] private Color selectedColor = new Color(0.15f, 0.5f, 0.75f);
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.25f, 0.9f);

    [Header("Estado en pantalla")]
    [SerializeField] private TMP_Text statusText;

    private readonly List<GameObject> placed = new List<GameObject>();
    private ContentPositioningBehaviour contentPositioning;
    private DevicePoseBehaviour devicePose;
    private HitTestResult lastHit;
    private float lastHitTime = -999f;
    private float lastTapTime = -999f;
    private float statusTimer;
    private int hitCount;
    private int tapCount;
    private string lastMessage = "";
    private string lastTrackingLogged = "";

    public int SelectedIndex { get; private set; } = -1;
    public int PlacedCount => placed.Count;

    /// <summary>
    /// El DevicePoseBehaviour que vale es el que Vuforia crea en runtime sobre el ARCamera (VuforiaBehaviour.Awake,
    /// con "Track Device Pose" activo en VuforiaConfiguration): es el unico que recibe el observer de pose.
    /// Uno agregado a mano en la escena nunca sale de NO_POSE / NOT_OBSERVED.
    /// </summary>
    private DevicePoseBehaviour DevicePose
    {
        get
        {
            if (devicePose == null && VuforiaBehaviour.Instance != null)
                devicePose = VuforiaBehaviour.Instance.DevicePoseBehaviour;
            return devicePose;
        }
    }

    private void Awake()
    {
        if (planeFinder == null)
            planeFinder = FindFirstObjectByType<PlaneFinderBehaviour>();

        if (planeFinder != null)
        {
            // Automatico: hit test continuo en el centro de la pantalla, con el indicador del plano visible
            planeFinder.HitTestMode = HitTestMode.AUTOMATIC;
            contentPositioning = planeFinder.GetComponent<ContentPositioningBehaviour>();
        }
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        if (planeFinder != null)
        {
            planeFinder.OnAutomaticHitTest.AddListener(OnAutomaticHit);
            planeFinder.OnInteractiveHitTest.AddListener(OnInteractiveHit);
        }
        if (contentPositioning != null)
            contentPositioning.OnContentPlaced.AddListener(OnContentPlaced);

        VuforiaApplication.Instance.OnVuforiaStarted += ApplyFixedFocus;
        if (VuforiaApplication.Instance.IsRunning) ApplyFixedFocus();
    }

    /// <summary>
    /// Foco fijo solo en Ground Plane. La config global (autofoco continuo, bueno para el Image Target de cerca)
    /// le llega a ARCore como AR_FOCUS_MODE_AUTO, y al reenfocar cambian los intrinsecos de la camara:
    /// el tracking visual pierde puntos ("Insufficient inliers") y tarda mucho en encontrar planos.
    /// ARCore por defecto (y Chrome/WebXR, que en este celu detecta el piso bien) usa foco fijo.
    /// </summary>
    private void ApplyFixedFocus()
    {
        var cam = VuforiaBehaviour.Instance != null ? VuforiaBehaviour.Instance.CameraDevice : null;
        if (cam == null) return;
        bool ok = cam.IsFocusModeSupported(FocusMode.FOCUS_MODE_FIXED) && cam.SetFocusMode(FocusMode.FOCUS_MODE_FIXED);
        Debug.Log("[GroundPlane] foco fijo " + (ok ? "aplicado" : "no soportado") + " (modo actual " + cam.FocusMode + ")");
    }

    private void OnDisable()
    {
        VuforiaApplication.Instance.OnVuforiaStarted -= ApplyFixedFocus;
        EnhancedTouchSupport.Disable();
        if (planeFinder != null)
        {
            planeFinder.OnAutomaticHitTest.RemoveListener(OnAutomaticHit);
            planeFinder.OnInteractiveHitTest.RemoveListener(OnInteractiveHit);
        }
        if (contentPositioning != null)
            contentPositioning.OnContentPlaced.RemoveListener(OnContentPlaced);
    }

    private void Start()
    {
        Select(startIndex);
    }

    private void Update()
    {
        if (Time.time - lastTapTime >= tapCooldown && TryGetTap(out bool overUI, out Vector2 tapPosition) && !overUI)
        {
            lastTapTime = Time.time;
            tapCount++;
            TryPlace(tapPosition);
        }

        statusTimer -= Time.deltaTime;
        if (statusTimer <= 0f)
        {
            statusTimer = 0.2f;
            RefreshStatus();
        }
    }

    /// <summary>Lee un toque nuevo (celular) o un clic (Editor). overUI = el toque cayo sobre un boton.</summary>
    private static bool TryGetTap(out bool overUI, out Vector2 position)
    {
        overUI = false;
        position = Vector2.zero;

        if (Touch.activeTouches.Count > 0 && Touch.activeTouches[0].phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            var touch = Touch.activeTouches[0];
            position = touch.screenPosition;
            overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.touchId);
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            position = Mouse.current.position.ReadValue();
            overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            return true;
        }

        return false;
    }

    private void TryPlace(Vector2 screenPosition)
    {
        if (contentPositioning == null)
        {
            lastMessage = "Falta el ContentPositioning";
            return;
        }

        int before = placed.Count;

        if (lastHit != null && Time.time - lastHitTime <= hitMaxAge)
        {
            // Plano fresco en el centro (donde esta el indicador): se coloca ahi
            Debug.Log("[GroundPlane] colocando en " + lastHit.Position + " (opcion " + SelectedIndex + ", tracking " + TrackingText() + ")");
            contentPositioning.PositionContentAtPlaneAnchor(lastHit);
        }
        else
        {
            // Sin plano en el centro: hit test en el punto tocado. Si encuentra plano dispara OnInteractiveHitTest,
            // que la escena tiene cableado a PositionContentAtPlaneAnchor.
            planeFinder.PerformHitTest(screenPosition);
            if (placed.Count == before && lastHit == null)
            {
                lastMessage = "No hay superficie: apunta a un piso o mesa con textura";
                Debug.Log("[GroundPlane] toque sin superficie (hits " + hitCount + ", tracking " + TrackingText() + ")");
                return;
            }
        }

        // OnContentPlaced es sincronico dentro de PositionContentAtPlaneAnchor: si no sumo, Vuforia no pudo anclar
        // (CheckAnchorsAvailable falla sin device tracking, o el hit test del toque no encontro plano)
        if (placed.Count == before)
        {
            lastMessage = "No se pudo anclar (tracking " + TrackingText() + ")";
            Debug.Log("[GroundPlane] no se pudo anclar (hits " + hitCount + ", tracking " + TrackingText() + ")");
        }
    }

    private void OnAutomaticHit(HitTestResult result)
    {
        lastHit = result;
        lastHitTime = Time.time;
        hitCount++;
        if (hitCount == 1 || hitCount % 200 == 0)
            Debug.Log("[GroundPlane] hit #" + hitCount + " en " + result.Position + " (tracking " + TrackingText() + ")");
    }

    private void OnInteractiveHit(HitTestResult result)
    {
        Debug.Log("[GroundPlane] hit del toque en " + result.Position + " (opcion " + SelectedIndex + ")");
    }

    /// <summary>Activa solo la opcion elegida; lo ya colocado no cambia.</summary>
    public void Select(int index)
    {
        if (options == null || index < 0 || index >= options.Length) return;

        SelectedIndex = index;
        for (int i = 0; i < options.Length; i++)
        {
            if (options[i] != null) options[i].SetActive(i == index);
            if (optionButtonImages != null && i < optionButtonImages.Length && optionButtonImages[i] != null)
                optionButtonImages[i].color = i == index ? selectedColor : normalColor;
        }
    }

    /// <summary>Borra todos los objetos colocados hasta ahora.</summary>
    public void ClearPlaced()
    {
        foreach (var go in placed)
            if (go != null) Destroy(go);
        placed.Clear();
    }

    private void OnContentPlaced(GameObject content)
    {
        if (content == null) return;
        placed.Add(content);
        lastMessage = "Objeto colocado";
        Debug.Log("[GroundPlane] colocado " + content.name + " en " + content.transform.position + " (total " + placed.Count + ")");
    }

    private string TrackingText()
    {
        var pose = DevicePose;
        return pose != null ? pose.TargetStatus.Status + " / " + pose.TargetStatus.StatusInfo : "sin DevicePose";
    }

    private void RefreshStatus()
    {
        string trackingNow = TrackingText();
        if (trackingNow != lastTrackingLogged)
        {
            Debug.Log("[GroundPlane] tracking " + trackingNow);
            lastTrackingLogged = trackingNow;
        }

        if (statusText == null) return;

        bool hasPlane = lastHit != null && Time.time - lastHitTime <= hitMaxAge;
        string state;
        if (hasPlane)
            state = "Superficie detectada: toca para colocar";
        else if (trackingNow.StartsWith("TRACKED"))
            // ARCore solo detecta planos con movimiento (paralaje): con el celular quieto no hay hits
            state = "Buscando superficie: move el celular despacio apuntando al piso";
        else
            state = "Iniciando tracking...";
        statusText.text = state + "\n<size=70%>tracking " + trackingNow + "\nhits " + hitCount + " | toques " + tapCount +
                          " | colocados " + placed.Count + (lastMessage.Length > 0 ? "\n" + lastMessage : "") + "</size>";
    }
}

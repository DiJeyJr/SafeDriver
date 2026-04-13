using UnityEngine;

public class TrackingUIController : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject imageTarget;

    private bool wasTracked;

    private void Start()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0;
    }

    private void Update()
    {
        if (imageTarget == null || canvasGroup == null) return;

        bool isTracked = imageTarget.activeInHierarchy &&
                         imageTarget.transform.childCount > 0 &&
                         imageTarget.transform.GetChild(0).gameObject.activeInHierarchy;

        if (isTracked && !wasTracked)
        {
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        else if (!isTracked && wasTracked)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        wasTracked = isTracked;
    }
}

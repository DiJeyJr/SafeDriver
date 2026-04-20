using System.Collections;
using UnityEngine;
using Vuforia;

[RequireComponent(typeof(CanvasGroup))]
public class TrackingUIController : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private ObserverBehaviour target;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private bool includeExtendedTracked = true;

    private Coroutine fadeCoroutine;
    private bool isVisible;

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        ApplyInstant(false);
    }

    private void OnEnable()
    {
        if (target != null)
            target.OnTargetStatusChanged += OnTargetStatusChanged;
    }

    private void OnDisable()
    {
        if (target != null)
            target.OnTargetStatusChanged -= OnTargetStatusChanged;
    }

    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        bool tracked = status.Status == Status.TRACKED ||
                       (includeExtendedTracked && status.Status == Status.EXTENDED_TRACKED);

        if (tracked == isVisible) return;

        isVisible = tracked;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeTo(tracked ? 1f : 0f));
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        bool interactive = targetAlpha > 0.5f;
        canvasGroup.interactable = interactive;
        canvasGroup.blocksRaycasts = interactive;

        float start = canvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
    }

    private void ApplyInstant(bool show)
    {
        isVisible = show;
        if (canvasGroup == null) return;
        canvasGroup.alpha = show ? 1f : 0f;
        canvasGroup.interactable = show;
        canvasGroup.blocksRaycasts = show;
    }
}

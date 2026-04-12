using UnityEngine;
using System.Collections;

public class TooltipController : MonoBehaviour
{
    [SerializeField] private float displayDuration = 5f;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private CanvasGroup canvasGroup;

    private void Start()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            StartCoroutine(ShowThenFade());
        }
    }

    private IEnumerator ShowThenFade()
    {
        yield return new WaitForSeconds(displayDuration);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - (elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}

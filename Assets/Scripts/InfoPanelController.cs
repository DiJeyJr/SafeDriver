using UnityEngine;
using System.Collections;

public class InfoPanelController : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float appearDuration = 0.3f;
    [SerializeField] private bool startVisible = true;

    private bool isVisible;

    private void Start()
    {
        if (cameraTransform == null)
        {
            var cam = Camera.main;
            if (cam != null) cameraTransform = cam.transform;
        }

        if (startVisible)
        {
            isVisible = true;
            transform.localScale = Vector3.one;
        }
        else
        {
            isVisible = false;
            transform.localScale = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Billboard: face the camera
        Vector3 lookDir = transform.position - cameraTransform.position;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDir.normalized);
        }
    }

    public void ToggleVisibility()
    {
        if (isVisible)
            StartCoroutine(AnimateScale(Vector3.one, Vector3.zero));
        else
            StartCoroutine(AnimateScale(Vector3.zero, Vector3.one));

        isVisible = !isVisible;
    }

    public void Show()
    {
        if (!isVisible)
        {
            isVisible = true;
            StartCoroutine(AnimateScale(Vector3.zero, Vector3.one));
        }
    }

    public void Hide()
    {
        if (isVisible)
        {
            isVisible = false;
            StartCoroutine(AnimateScale(Vector3.one, Vector3.zero));
        }
    }

    private IEnumerator AnimateScale(Vector3 from, Vector3 to)
    {
        float elapsed = 0f;
        while (elapsed < appearDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / appearDuration);
            transform.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }
        transform.localScale = to;
    }
}

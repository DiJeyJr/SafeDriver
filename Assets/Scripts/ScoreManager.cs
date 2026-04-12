using UnityEngine;
using TMPro;
using System.Collections;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private TextMeshPro scoreText;
    [SerializeField] private TextMeshPro floatingTextPrefab;

    private int score;

    private void Start()
    {
        UpdateDisplay();
    }

    public void AddPoints(int amount)
    {
        score += amount;
        UpdateDisplay();

        if (floatingTextPrefab != null)
        {
            StartCoroutine(ShowFloatingText(amount));
        }
    }

    public void ResetScore()
    {
        score = 0;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Puntaje: {score}";
        }
    }

    private IEnumerator ShowFloatingText(int amount)
    {
        string prefix = amount > 0 ? "+" : "";
        var floating = Instantiate(floatingTextPrefab, transform.position + Vector3.up * 0.3f, Quaternion.identity, transform);
        floating.text = $"{prefix}{amount}";
        floating.color = amount > 0 ? Color.green : Color.red;

        float elapsed = 0f;
        float duration = 1.5f;
        Vector3 startPos = floating.transform.localPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            floating.transform.localPosition = startPos + Vector3.up * t * 0.2f;
            floating.alpha = 1f - t;
            yield return null;
        }

        Destroy(floating.gameObject);
    }
}

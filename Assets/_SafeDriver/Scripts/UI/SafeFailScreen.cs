using UnityEngine;
using UnityEngine.UI;

namespace SafeDriver.UI
{
    /// <summary>
    /// Pantalla "Safe Fail" — aparece ante una infraccion grave.
    /// Muestra mensaje pedagogico y ofrece reintentar. NUNCA muestra choque
    /// ni efectos violentos — el diseno de la app promueve aprendizaje seguro.
    /// </summary>
    public class SafeFailScreen : MonoBehaviour
    {
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private Text messageText;

        public void Show(string reason)
        {
            if (rootPanel != null)    rootPanel.SetActive(true);
            if (messageText != null)  messageText.text = reason;
        }

        public void Hide()
        {
            if (rootPanel != null) rootPanel.SetActive(false);
        }
    }
}

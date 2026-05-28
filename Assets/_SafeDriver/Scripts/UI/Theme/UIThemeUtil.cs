using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SafeDriver.UI
{
    /// <summary>
    /// Helpers para aplicar un UITheme a componentes uGUI. Usable tanto desde editor
    /// utilities (restyle de escenas) como desde runtime. Centraliza el "como se ve"
    /// para que ninguna pantalla invente sus propios colores/medidas.
    /// </summary>
    public static class UIThemeUtil
    {
        /// <summary>Rol de un boton segun la senaletica vial.</summary>
        public enum Role { Brand, Success, Danger, Warning, Neutral }

        /// <summary>Jerarquia de un texto.</summary>
        public enum TextKind { Title, Heading, Body, Secondary, ButtonLabel }

        /// <summary>Estiliza una Image como panel/card: sprite redondeado + color de superficie.</summary>
        public static void StyleAsPanel(Image img, UITheme t, bool raised = false)
        {
            if (img == null || t == null) return;
            if (t.roundedSprite != null)
            {
                img.sprite = t.roundedSprite;
                img.type = Image.Type.Sliced;
            }
            img.color = raised ? t.surfaceRaised : t.surface;
        }

        /// <summary>Estiliza una Image de fondo a pantalla completa con el crema base.</summary>
        public static void StyleAsBackground(Image img, UITheme t)
        {
            if (img == null || t == null) return;
            img.color = t.backgroundBase;
        }

        /// <summary>Estiliza una Image como boton (pill redondeado) segun su rol.</summary>
        public static void StyleAsButton(Image img, UITheme t, Role role)
        {
            if (img == null || t == null) return;
            if (t.roundedSprite != null)
            {
                img.sprite = t.roundedSprite;
                img.type = Image.Type.Sliced;
            }
            img.color = ColorFor(t, role);
        }

        /// <summary>Aplica fuente, color y tamanio a un texto segun su jerarquia.</summary>
        public static void StyleText(TMP_Text txt, UITheme t, TextKind kind)
        {
            if (txt == null || t == null) return;
            switch (kind)
            {
                case TextKind.Title:
                    if (t.titleFont != null) txt.font = t.titleFont;
                    txt.fontSize = t.titleSize;
                    txt.color = t.textPrimary;
                    break;
                case TextKind.Heading:
                    if (t.titleFont != null) txt.font = t.titleFont;
                    txt.fontSize = t.headingSize;
                    txt.color = t.textPrimary;
                    break;
                case TextKind.Body:
                    if (t.bodyFont != null) txt.font = t.bodyFont;
                    txt.fontSize = t.bodySize;
                    txt.color = t.textPrimary;
                    break;
                case TextKind.Secondary:
                    if (t.bodyFont != null) txt.font = t.bodyFont;
                    txt.fontSize = t.bodySize;
                    txt.color = t.textSecondary;
                    break;
                case TextKind.ButtonLabel:
                    if (t.bodyFont != null) txt.font = t.bodyFont;
                    txt.fontSize = t.buttonSize;
                    txt.color = t.textOnAccent;
                    txt.fontStyle = FontStyles.Bold;
                    break;
            }
        }

        /// <summary>Agrega o reconfigura una sombra suave (depth approachable).</summary>
        public static void AddSoftShadow(Graphic g, UITheme t)
        {
            if (g == null || t == null) return;
            var sh = g.GetComponent<Shadow>();
            if (sh == null) sh = g.gameObject.AddComponent<Shadow>();
            sh.effectColor = t.shadowColor;
            sh.effectDistance = t.shadowDistance;
        }

        public static Color ColorFor(UITheme t, Role role) => role switch
        {
            Role.Brand   => t.brand,
            Role.Success => t.success,
            Role.Danger  => t.danger,
            Role.Warning => t.warning,
            _            => t.surface,
        };
    }
}

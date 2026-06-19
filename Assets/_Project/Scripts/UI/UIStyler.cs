using UnityEngine;
using UnityEngine.UI;

namespace SimpleQaidah.UI
{
    /// <summary>
    /// Attach to Canvas root. On Awake, walks the hierarchy and applies
    /// rounded-rect sprites, button scale effects, and visual polish.
    /// </summary>
    public class UIStyler : MonoBehaviour
    {
        private static Sprite _buttonSprite;
        private static Sprite _cardSprite;
        private static Sprite _wideButtonSprite;

        private void Awake()
        {
            // Generate canonical sprites once
            if (_buttonSprite == null)
                _buttonSprite = SpriteGenerator.RoundedRect(128, 128, 24);
            if (_cardSprite == null)
                _cardSprite = SpriteGenerator.RoundedRect(128, 128, 16);
            if (_wideButtonSprite == null)
                _wideButtonSprite = SpriteGenerator.RoundedRect(256, 96, 20);

            ApplyRoundedCorners();
            ApplyButtonEffects();
        }

        private void ApplyRoundedCorners()
        {
            foreach (var img in GetComponentsInChildren<Image>(true))
            {
                if (img.sprite != null) continue; // already has a sprite

                string name = img.gameObject.name;

                // Skip elements that shouldn't have rounded corners
                if (name.Contains("Fill") || name.Contains("Background") ||
                    name.Contains("Overlay") && !name.Contains("Lock") && !name.Contains("Check"))
                    continue;

                // Apply to buttons
                if (name.Contains("Button") || name.Contains("Option"))
                {
                    img.sprite = _buttonSprite;
                    img.type = Image.Type.Sliced;
                }
                // Apply to progress bar backgrounds
                else if (name.Contains("ProgressBG") || name.Contains("ProgressBar") && name.Contains("BG"))
                {
                    var smallRound = SpriteGenerator.RoundedRect(64, 32, 12);
                    img.sprite = smallRound;
                    img.type = Image.Type.Sliced;
                }
            }
        }

        private void ApplyButtonEffects()
        {
            foreach (var btn in GetComponentsInChildren<Button>(true))
            {
                if (btn.GetComponent<ButtonScaleEffect>() == null)
                {
                    btn.gameObject.AddComponent<ButtonScaleEffect>();
                }

                // Improve button transition
                var colors = btn.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f);
                colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
                colors.fadeDuration = 0.08f;
                btn.colors = colors;
            }
        }
    }
}

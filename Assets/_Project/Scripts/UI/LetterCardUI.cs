using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SimpleQaidah.Core;
using SimpleQaidah.Data;
using static SimpleQaidah.Core.ArabicFontHelper;

namespace SimpleQaidah.UI
{
    public enum LetterCardState
    {
        Locked,
        Available,
        Completed
    }

    public class LetterCardUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text letterText;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private GameObject checkOverlay;

        [Header("Colors")]
        [SerializeField] private Color availableColor = new Color(0.31f, 0.76f, 0.97f);
        [SerializeField] private Color lockedColor = new Color(0.74f, 0.74f, 0.74f);
        [SerializeField] private Color completedColor = new Color(0.40f, 0.73f, 0.42f);

        private LetterData letterData;
        private bool visualsApplied;

        public void Setup(LetterData data, LetterCardState state)
        {
            AutoWire();

            letterData = data;
            Apply(letterText);
            letterText.text = data.letterArabic;

            if (!visualsApplied)
            {
                ApplyVisuals();
                visualsApplied = true;
            }

            SetState(state);

            button.onClick.RemoveAllListeners();
            if (state != LetterCardState.Locked)
            {
                button.onClick.AddListener(OnTapped);
            }
        }

        private void AutoWire()
        {
            if (button == null) button = GetComponent<Button>();
            if (background == null) background = GetComponent<Image>();

            if (letterText == null)
            {
                var t = transform.Find("LetterText");
                if (t != null) letterText = t.GetComponent<TMP_Text>();
                else letterText = GetComponentInChildren<TMP_Text>();
            }
            if (lockOverlay == null)
            {
                var t = transform.Find("LockOverlay");
                if (t != null) lockOverlay = t.gameObject;
            }
            if (checkOverlay == null)
            {
                var t = transform.Find("CheckOverlay");
                if (t != null) checkOverlay = t.gameObject;
            }
        }

        private void ApplyVisuals()
        {
            // Rounded corners on card background
            if (background != null)
            {
                background.sprite = SpriteGenerator.RoundedRect(128, 128, 20);
                background.type = Image.Type.Sliced;
            }

            // Add shadow behind card
            AddShadow();

            // Apply sprites to lock/check icons (replacing emoji text)
            ApplyIconSprites();
        }

        private void AddShadow()
        {
            // Check if shadow already exists
            var existing = transform.Find("Shadow");
            if (existing != null) return;

            var shadowGO = new GameObject("Shadow");
            shadowGO.transform.SetParent(transform, false);
            shadowGO.transform.SetAsFirstSibling(); // behind everything

            var shadowImg = shadowGO.AddComponent<Image>();
            shadowImg.sprite = SpriteGenerator.RoundedRect(128, 128, 20);
            shadowImg.type = Image.Type.Sliced;
            shadowImg.color = new Color(0, 0, 0, 0.12f);
            shadowImg.raycastTarget = false;

            var shadowRT = shadowGO.GetComponent<RectTransform>();
            shadowRT.anchorMin = Vector2.zero;
            shadowRT.anchorMax = Vector2.one;
            // Offset: slightly down and wider for shadow effect
            shadowRT.offsetMin = new Vector2(-2, -4);
            shadowRT.offsetMax = new Vector2(2, 0);
        }

        private void ApplyIconSprites()
        {
            // Lock icon
            if (lockOverlay != null)
            {
                var lockIconT = lockOverlay.transform.Find("LockIcon");
                if (lockIconT != null)
                {
                    // Remove any TMP_Text (legacy emoji)
                    var tmp = lockIconT.GetComponent<TMP_Text>();
                    if (tmp != null) Destroy(tmp);

                    // Ensure Image component with lock sprite
                    var img = lockIconT.GetComponent<Image>();
                    if (img == null) img = lockIconT.gameObject.AddComponent<Image>();
                    img.sprite = SpriteGenerator.LockIcon(64);
                    img.color = new Color(1, 1, 1, 0.8f);
                    img.preserveAspect = true;
                    img.raycastTarget = false;
                }

                // Round the lock overlay background
                var lockBg = lockOverlay.GetComponent<Image>();
                if (lockBg != null)
                {
                    lockBg.sprite = SpriteGenerator.RoundedRect(128, 128, 20);
                    lockBg.type = Image.Type.Sliced;
                }
            }

            // Check icon
            if (checkOverlay != null)
            {
                var checkIconT = checkOverlay.transform.Find("CheckIcon");
                if (checkIconT != null)
                {
                    // Remove any TMP_Text (legacy emoji)
                    var tmp = checkIconT.GetComponent<TMP_Text>();
                    if (tmp != null) Destroy(tmp);

                    // Ensure Image component with checkmark sprite
                    var img = checkIconT.GetComponent<Image>();
                    if (img == null) img = checkIconT.gameObject.AddComponent<Image>();
                    img.sprite = SpriteGenerator.Checkmark(64);
                    img.color = Color.white;
                    img.preserveAspect = true;
                    img.raycastTarget = false;
                }

                // Round the check overlay background
                var checkBg = checkOverlay.GetComponent<Image>();
                if (checkBg != null)
                {
                    checkBg.sprite = SpriteGenerator.RoundedRect(64, 64, 16);
                    checkBg.type = Image.Type.Sliced;
                }
            }
        }

        public void SetState(LetterCardState state)
        {
            switch (state)
            {
                case LetterCardState.Locked:
                    background.color = lockedColor;
                    if (lockOverlay != null) lockOverlay.SetActive(true);
                    if (checkOverlay != null) checkOverlay.SetActive(false);
                    button.interactable = false;
                    letterText.alpha = 0.4f;
                    break;

                case LetterCardState.Available:
                    background.color = availableColor;
                    if (lockOverlay != null) lockOverlay.SetActive(false);
                    if (checkOverlay != null) checkOverlay.SetActive(false);
                    button.interactable = true;
                    letterText.alpha = 1f;
                    break;

                case LetterCardState.Completed:
                    background.color = completedColor;
                    if (lockOverlay != null) lockOverlay.SetActive(false);
                    if (checkOverlay != null) checkOverlay.SetActive(true);
                    button.interactable = true;
                    letterText.alpha = 1f;
                    break;
            }
        }

        private void OnTapped()
        {
            if (letterData != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLetterAudio(letterData.audioClip);
            }

            // Tap scale feedback
            StartCoroutine(UIAnimations.ScalePop(transform, 0.92f, 1f, 0.2f));
        }
    }
}

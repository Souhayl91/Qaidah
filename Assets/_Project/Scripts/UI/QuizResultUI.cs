using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using SimpleQaidah.Core;

namespace SimpleQaidah.UI
{
    public class QuizResultUI : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text xpGainText;
        [SerializeField] private TMP_Text messageText;

        [Header("Stars")]
        [SerializeField] private Image[] starImages;
        [SerializeField] private Color starFilledColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color starEmptyColor = new Color(0.74f, 0.74f, 0.74f);

        [Header("Buttons")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private TMP_Text continueButtonText;

        private LessonFlowController flowController;
        private bool visualsApplied;

        public void Initialize(int score, int total, string lessonId, int groupIndex, int groupCount, LessonFlowController controller)
        {
            flowController = controller;

            AutoWire();

            if (!visualsApplied)
            {
                ApplyVisuals();
                visualsApplied = true;
            }

            float percentage = total > 0 ? (float)score / total : 0f;
            int stars = CalculateStars(percentage);
            int xpGain = CalculateXP(score, stars);

            // Save progress
            SaveManager.Instance?.UpdateGroupScore(lessonId, groupIndex, score, stars, groupCount);
            SaveManager.Instance?.AddXP(xpGain);

            // Display message
            if (stars == 3)
                messageText.text = "Perfect!";
            else if (stars == 2)
                messageText.text = "Great job!";
            else if (stars == 1)
                messageText.text = "Good work!";
            else
                messageText.text = "Keep practicing!";

            // Check if there's a next group
            bool hasNextGroup = groupIndex + 1 < groupCount;
            bool passed = stars >= 1;

            continueButton.gameObject.SetActive(true);
            if (passed && hasNextGroup)
                continueButtonText.text = "Next Group";
            else
                continueButtonText.text = "Back to Lesson";

            // Wire buttons
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(() => flowController.RetryCurrentGroup());

            continueButton.onClick.RemoveAllListeners();
            if (passed && hasNextGroup)
                continueButton.onClick.AddListener(() => flowController.AdvanceToNextGroup());
            else
                continueButton.onClick.AddListener(() => flowController.ShowAlphabetGrid());

            // Start animations
            StopAllCoroutines();
            StartCoroutine(AnimateResults(score, total, stars, xpGain));
        }

        private void AutoWire()
        {
            if (scoreText == null)
            {
                var t = FindChild(transform, "ScoreText");
                if (t != null) scoreText = t.GetComponent<TMP_Text>();
            }
            if (xpGainText == null)
            {
                var t = FindChild(transform, "XPGainText");
                if (t != null) xpGainText = t.GetComponent<TMP_Text>();
            }
            if (messageText == null)
            {
                var t = FindChild(transform, "MessageText");
                if (t != null) messageText = t.GetComponent<TMP_Text>();
            }
            if (retryButton == null)
            {
                var t = FindChild(transform, "RetryButton");
                if (t != null) retryButton = t.GetComponent<Button>();
            }
            if (continueButton == null)
            {
                var t = FindChild(transform, "ContinueButton");
                if (t != null) continueButton = t.GetComponent<Button>();
            }
            if (continueButtonText == null && continueButton != null)
            {
                continueButtonText = continueButton.GetComponentInChildren<TMP_Text>();
            }

            // Wire star images
            if (starImages == null || starImages.Length == 0)
            {
                var starsArea = FindChild(transform, "StarsArea");
                if (starsArea != null)
                {
                    int count = Mathf.Min(starsArea.childCount, 3);
                    starImages = new Image[count];
                    for (int i = 0; i < count; i++)
                    {
                        starImages[i] = starsArea.GetChild(i).GetComponent<Image>();
                    }
                }
            }
        }

        private void ApplyVisuals()
        {
            // Apply star sprite to star images (replacing emoji text)
            var starSprite = SpriteGenerator.Star(128);
            if (starImages != null)
            {
                for (int i = 0; i < starImages.Length; i++)
                {
                    if (starImages[i] == null) continue;

                    // Remove the "StarLabel" TMP_Text child (legacy emoji)
                    var labelT = starImages[i].transform.Find("StarLabel");
                    if (labelT != null)
                    {
                        var tmp = labelT.GetComponent<TMP_Text>();
                        if (tmp != null) Destroy(tmp);
                        Destroy(labelT.gameObject);
                    }

                    starImages[i].sprite = starSprite;
                    starImages[i].preserveAspect = true;
                    starImages[i].type = Image.Type.Simple;
                }
            }

            // Apply rounded rect to buttons
            var btnSprite = SpriteGenerator.RoundedRect(256, 96, 20);
            if (retryButton != null)
            {
                var img = retryButton.GetComponent<Image>();
                if (img != null && img.sprite == null)
                {
                    img.sprite = btnSprite;
                    img.type = Image.Type.Sliced;
                }
            }
            if (continueButton != null)
            {
                var img = continueButton.GetComponent<Image>();
                if (img != null && img.sprite == null)
                {
                    img.sprite = btnSprite;
                    img.type = Image.Type.Sliced;
                }
            }
        }

        private IEnumerator AnimateResults(int score, int total, int stars, int xpGain)
        {
            // Start with everything hidden/zeroed
            scoreText.text = $"0 / {total}";
            xpGainText.text = "+0 XP";

            // Hide and zero-scale stars
            for (int i = 0; i < starImages.Length; i++)
            {
                starImages[i].color = starEmptyColor;
                starImages[i].transform.localScale = Vector3.zero;
            }

            // Animate message text appearing
            StartCoroutine(UIAnimations.ScalePop(messageText.transform, 0.5f, 1f, 0.3f));
            yield return new WaitForSeconds(0.3f);

            // Animate stars bouncing in one by one
            for (int i = 0; i < starImages.Length; i++)
            {
                Color targetColor = i < stars ? starFilledColor : starEmptyColor;
                starImages[i].color = targetColor;

                StartCoroutine(UIAnimations.Bounce(starImages[i].transform, 0.4f));

                if (i < stars)
                    AudioManager.Instance?.PlaySFX(SFXType.Star);

                yield return new WaitForSeconds(0.35f);
            }

            // Count up score
            yield return new WaitForSeconds(0.2f);
            StartCoroutine(UIAnimations.CountUp(scoreText, 0, score, 0.8f, "{0} / " + total));

            // Count up XP
            yield return new WaitForSeconds(0.3f);
            StartCoroutine(UIAnimations.CountUp(xpGainText, 0, xpGain, 1.0f, "+{0} XP"));

            // Confetti for 2+ stars
            if (stars >= 2)
            {
                yield return new WaitForSeconds(0.2f);
                StartCoroutine(SpawnConfetti(stars >= 3 ? 40 : 25));
            }
        }

        // ─── Confetti Effect ────────────────────────────────────

        private IEnumerator SpawnConfetti(int count)
        {
            var confettiParent = new GameObject("Confetti");
            confettiParent.transform.SetParent(transform, false);
            var parentRT = confettiParent.AddComponent<RectTransform>();
            parentRT.anchorMin = Vector2.zero;
            parentRT.anchorMax = Vector2.one;
            parentRT.offsetMin = Vector2.zero;
            parentRT.offsetMax = Vector2.zero;

            Color[] colors = {
                new Color(1f, 0.84f, 0f),
                new Color(0.31f, 0.76f, 0.97f),
                new Color(0.40f, 0.73f, 0.42f),
                new Color(0.94f, 0.33f, 0.31f),
                new Color(0.67f, 0.28f, 0.74f),
                new Color(1f, 0.65f, 0.15f),
                new Color(0.93f, 0.25f, 0.48f),
            };

            var confettiSprite = SpriteGenerator.RoundedRect(16, 16, 4);

            for (int i = 0; i < count; i++)
            {
                var piece = new GameObject("C" + i);
                piece.transform.SetParent(confettiParent.transform, false);
                var img = piece.AddComponent<Image>();
                img.color = colors[Random.Range(0, colors.Length)];
                img.sprite = confettiSprite;
                img.raycastTarget = false;

                var rt = piece.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(Random.Range(8f, 18f), Random.Range(6f, 14f));
                rt.anchoredPosition = new Vector2(
                    Random.Range(-450f, 450f),
                    Random.Range(600f, 900f)
                );
                rt.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

                StartCoroutine(AnimateConfettiPiece(rt, img));

                if (i % 3 == 0)
                    yield return null;
            }

            yield return new WaitForSeconds(4f);
            if (confettiParent != null)
                Destroy(confettiParent);
        }

        private IEnumerator AnimateConfettiPiece(RectTransform rt, Image img)
        {
            float fallSpeed = Random.Range(250f, 500f);
            float swaySpeed = Random.Range(1.5f, 3.5f);
            float swayAmount = Random.Range(30f, 80f);
            float rotSpeed = Random.Range(90f, 360f);
            float startX = rt.anchoredPosition.x;
            float elapsed = 0f;

            while (rt != null && rt.anchoredPosition.y > -1000)
            {
                elapsed += Time.deltaTime;
                var pos = rt.anchoredPosition;
                pos.y -= fallSpeed * Time.deltaTime;
                pos.x = startX + Mathf.Sin(elapsed * swaySpeed) * swayAmount;
                rt.anchoredPosition = pos;
                rt.Rotate(0, 0, rotSpeed * Time.deltaTime);

                if (pos.y < -400)
                {
                    Color c = img.color;
                    c.a = Mathf.Lerp(1f, 0f, (-400f - pos.y) / 400f);
                    img.color = c;
                }

                yield return null;
            }
        }

        // ─── Helpers ────────────────────────────────────────────

        private static Transform FindChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var found = FindChild(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private int CalculateStars(float percentage)
        {
            if (percentage >= 1f) return 3;
            if (percentage >= 0.8f) return 2;
            if (percentage >= 0.6f) return 1;
            return 0;
        }

        private int CalculateXP(int correctCount, int stars)
        {
            int baseXP = correctCount * 10;
            int bonus = stars switch
            {
                3 => 50,
                2 => 25,
                1 => 10,
                _ => 0
            };
            return baseXP + bonus;
        }
    }
}

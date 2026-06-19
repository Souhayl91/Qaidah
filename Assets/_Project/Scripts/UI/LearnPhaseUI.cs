using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using SimpleQaidah.Core;
using SimpleQaidah.Data;

namespace SimpleQaidah.UI
{
    public class LearnPhaseUI : MonoBehaviour
    {
        private bool fontApplied;
        [Header("Letter Display")]
        [SerializeField] private TMP_Text largeLetterText;
        [SerializeField] private TMP_Text letterNameText;
        [SerializeField] private TMP_Text transliterationText;
        [SerializeField] private Button speakerButton;

        [Header("Navigation")]
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button practiceButton;
        [SerializeField] private TMP_Text counterText;

        [Header("Navigation Dots")]
        [SerializeField] private Transform dotsContainer;
        [SerializeField] private GameObject dotPrefab;
        [SerializeField] private Color activeDotColor = new Color(0.09f, 0.40f, 0.75f);
        [SerializeField] private Color inactiveDotColor = new Color(0.74f, 0.74f, 0.74f);

        private LetterGroupData currentGroup;
        private LessonFlowController flowController;
        private int currentLetterIndex;
        private bool[] viewedLetters;
        private Image[] dotImages;
        private bool visualsApplied;
        private Coroutine letterTransition;

        public void Initialize(LetterGroupData group, LessonFlowController controller)
        {
            currentGroup = group;
            flowController = controller;
            currentLetterIndex = 0;
            viewedLetters = new bool[group.letters.Length];

            AutoWire();

            if (!visualsApplied)
            {
                ApplyVisuals();
                visualsApplied = true;
            }

            SetupButtons();
            BuildDots();
            ShowCurrentLetter();
        }

        private void AutoWire()
        {
            if (largeLetterText == null)
            {
                var t = FindChild(transform, "LargeLetterText");
                if (t != null) largeLetterText = t.GetComponent<TMP_Text>();
            }
            if (letterNameText == null)
            {
                var t = FindChild(transform, "LetterNameText");
                if (t != null) letterNameText = t.GetComponent<TMP_Text>();
            }
            if (transliterationText == null)
            {
                var t = FindChild(transform, "TransliterationText");
                if (t != null) transliterationText = t.GetComponent<TMP_Text>();
            }
            if (speakerButton == null)
            {
                var t = FindChild(transform, "SpeakerButton");
                if (t != null) speakerButton = t.GetComponent<Button>();
            }
            if (prevButton == null)
            {
                var t = FindChild(transform, "PrevButton");
                if (t != null) prevButton = t.GetComponent<Button>();
            }
            if (nextButton == null)
            {
                var t = FindChild(transform, "NextButton");
                if (t != null) nextButton = t.GetComponent<Button>();
            }
            if (practiceButton == null)
            {
                var t = FindChild(transform, "PracticeButton");
                if (t != null) practiceButton = t.GetComponent<Button>();
            }
            if (counterText == null)
            {
                var t = FindChild(transform, "CounterText");
                if (t != null) counterText = t.GetComponent<TMP_Text>();
            }
            if (dotsContainer == null)
            {
                var t = FindChild(transform, "DotsContainer");
                if (t != null) dotsContainer = t;
            }
            if (dotPrefab == null)
            {
                #if UNITY_EDITOR
                dotPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Dot.prefab");
                #endif
            }
        }

        private void ApplyVisuals()
        {
            // Apply rounded rect to speaker button
            if (speakerButton != null)
            {
                var spkImg = speakerButton.GetComponent<Image>();
                if (spkImg != null && spkImg.sprite == null)
                {
                    spkImg.sprite = SpriteGenerator.RoundedRect(128, 64, 16);
                    spkImg.type = Image.Type.Sliced;
                }

                // Replace speaker emoji text with icon sprite
                var speakerTextT = FindChild(speakerButton.transform, "SpeakerText");
                if (speakerTextT != null)
                {
                    // Hide the TMP text (can't destroy + add Image on same frame)
                    var tmp = speakerTextT.GetComponent<TMP_Text>();
                    if (tmp != null) tmp.enabled = false;

                    // Create a new child for the speaker icon
                    var iconGO = new GameObject("SpeakerIconImg");
                    iconGO.transform.SetParent(speakerButton.transform, false);
                    var img = iconGO.AddComponent<Image>();
                    img.sprite = SpriteGenerator.SpeakerIcon(64);
                    img.color = Color.white;
                    img.preserveAspect = true;
                    img.raycastTarget = false;

                    var rt = iconGO.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.2f, 0.15f);
                    rt.anchorMax = new Vector2(0.8f, 0.85f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
            }

            // Apply rounded rect to practice button
            if (practiceButton != null)
            {
                var pracImg = practiceButton.GetComponent<Image>();
                if (pracImg != null && pracImg.sprite == null)
                {
                    pracImg.sprite = SpriteGenerator.RoundedRect(256, 96, 20);
                    pracImg.type = Image.Type.Sliced;
                }
            }

            // Apply rounded rect to nav buttons
            ApplyRoundedToButton(prevButton, 64, 64, 16);
            ApplyRoundedToButton(nextButton, 64, 64, 16);
        }

        private static void ApplyRoundedToButton(Button btn, int w, int h, int r)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null && img.sprite == null)
            {
                img.sprite = SpriteGenerator.RoundedRect(w, h, r);
                img.type = Image.Type.Sliced;
            }
        }

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

        private void SetupButtons()
        {
            speakerButton.onClick.RemoveAllListeners();
            speakerButton.onClick.AddListener(PlayCurrentAudio);

            prevButton.onClick.RemoveAllListeners();
            prevButton.onClick.AddListener(PreviousLetter);

            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(NextLetter);

            practiceButton.onClick.RemoveAllListeners();
            practiceButton.onClick.AddListener(OnPracticeClicked);
            practiceButton.gameObject.SetActive(false);
        }

        private void BuildDots()
        {
            if (dotsContainer == null) return;

            foreach (Transform child in dotsContainer)
                Destroy(child.gameObject);

            dotImages = new Image[currentGroup.letters.Length];
            var circleSprite = SpriteGenerator.Circle(32);

            if (dotPrefab != null)
            {
                for (int i = 0; i < currentGroup.letters.Length; i++)
                {
                    var dot = Instantiate(dotPrefab, dotsContainer);
                    dotImages[i] = dot.GetComponent<Image>();
                    // Apply circle sprite for smooth round dots
                    if (dotImages[i] != null && dotImages[i].sprite == null)
                    {
                        dotImages[i].sprite = circleSprite;
                    }
                }
            }
            else
            {
                for (int i = 0; i < currentGroup.letters.Length; i++)
                {
                    var dotGO = new GameObject($"Dot{i}");
                    dotGO.transform.SetParent(dotsContainer, false);
                    var img = dotGO.AddComponent<Image>();
                    img.color = inactiveDotColor;
                    img.sprite = circleSprite;
                    var le = dotGO.AddComponent<LayoutElement>();
                    le.preferredWidth = 15;
                    le.preferredHeight = 15;
                    dotImages[i] = img;
                }
            }
        }

        private void ShowCurrentLetter()
        {
            if (!fontApplied)
            {
                ArabicFontHelper.Apply(largeLetterText);
                fontApplied = true;
            }

            var letter = currentGroup.letters[currentLetterIndex];

            largeLetterText.text = letter.letterArabic;
            letterNameText.text = letter.letterName;
            transliterationText.text = letter.transliteration;

            if (counterText != null)
                counterText.text = $"{currentLetterIndex + 1} / {currentGroup.letters.Length}";

            viewedLetters[currentLetterIndex] = true;

            prevButton.gameObject.SetActive(currentLetterIndex > 0);
            nextButton.gameObject.SetActive(currentLetterIndex < currentGroup.letters.Length - 1);

            UpdateDots();
            CheckAllViewed();
            PlayCurrentAudio();

            // Scale pop animation for the letter appearing
            if (largeLetterText != null)
            {
                StartCoroutine(UIAnimations.ScalePop(largeLetterText.transform, 0.7f, 1f, 0.3f));
            }
        }

        private void UpdateDots()
        {
            if (dotImages == null) return;
            for (int i = 0; i < dotImages.Length; i++)
            {
                if (dotImages[i] != null)
                    dotImages[i].color = i == currentLetterIndex ? activeDotColor : inactiveDotColor;
            }
        }

        private void CheckAllViewed()
        {
            bool allViewed = true;
            foreach (bool v in viewedLetters)
            {
                if (!v) { allViewed = false; break; }
            }

            if (allViewed && !practiceButton.gameObject.activeSelf)
            {
                practiceButton.gameObject.SetActive(true);
                // Animate practice button appearing
                StartCoroutine(UIAnimations.ScalePop(practiceButton.transform, 0f, 1f, 0.3f));
            }
        }

        private void PlayCurrentAudio()
        {
            var letter = currentGroup.letters[currentLetterIndex];
            AudioManager.Instance?.PlayLetterAudio(letter.audioClip);
        }

        private void NextLetter()
        {
            if (currentLetterIndex < currentGroup.letters.Length - 1)
            {
                currentLetterIndex++;
                AnimateLetterTransition(1);
            }
        }

        private void PreviousLetter()
        {
            if (currentLetterIndex > 0)
            {
                currentLetterIndex--;
                AnimateLetterTransition(-1);
            }
        }

        private void AnimateLetterTransition(int direction)
        {
            if (letterTransition != null)
                StopCoroutine(letterTransition);
            letterTransition = StartCoroutine(DoLetterTransition(direction));
        }

        private IEnumerator DoLetterTransition(int direction)
        {
            if (largeLetterText == null)
            {
                ShowCurrentLetter();
                yield break;
            }

            var rt = largeLetterText.GetComponent<RectTransform>();
            Vector2 originalPos = rt.anchoredPosition;

            // Fade out old letter (quick)
            float fadeOutDuration = 0.1f;
            float elapsed = 0f;
            Color startColor = largeLetterText.color;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeOutDuration;
                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, t);
                largeLetterText.color = c;
                rt.anchoredPosition = originalPos + new Vector2(direction * -50f * t, 0);
                yield return null;
            }

            // Update content
            ShowCurrentLetter();

            // Slide in from other side
            rt.anchoredPosition = originalPos + new Vector2(direction * 80f, 0);
            Color newColor = largeLetterText.color;
            newColor.a = 0f;
            largeLetterText.color = newColor;

            float slideInDuration = 0.2f;
            elapsed = 0f;
            while (elapsed < slideInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / slideInDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                rt.anchoredPosition = Vector2.Lerp(
                    originalPos + new Vector2(direction * 80f, 0),
                    originalPos, eased);
                Color c = largeLetterText.color;
                c.a = eased;
                largeLetterText.color = c;
                yield return null;
            }

            rt.anchoredPosition = originalPos;
            Color final2 = largeLetterText.color;
            final2.a = 1f;
            largeLetterText.color = final2;
            letterTransition = null;
        }

        private void OnPracticeClicked()
        {
            flowController.StartQuiz();
        }
    }
}

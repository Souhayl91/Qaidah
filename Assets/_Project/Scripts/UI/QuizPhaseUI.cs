using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using SimpleQaidah.Core;
using SimpleQaidah.Data;

namespace SimpleQaidah.UI
{
    public enum QuizType
    {
        AudioToLetter,
        LetterToName,
        NameToLetter
    }

    public class QuizPhaseUI : MonoBehaviour
    {
        [Header("Question Display")]
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private TMP_Text promptLetterText;
        [SerializeField] private TMP_Text promptNameText;
        [SerializeField] private Button speakerButton;
        [SerializeField] private GameObject speakerButtonObj;

        [Header("Options")]
        [SerializeField] private Button[] optionButtons;
        [SerializeField] private TMP_Text[] optionTexts;
        [SerializeField] private Image[] optionBackgrounds;

        [Header("Progress")]
        [SerializeField] private Image progressFill;
        [SerializeField] private TMP_Text progressText;

        [Header("Colors")]
        [SerializeField] private Color defaultOptionColor = new Color(0.31f, 0.76f, 0.97f);
        [SerializeField] private Color correctColor = new Color(0.30f, 0.69f, 0.31f);
        [SerializeField] private Color wrongColor = new Color(0.94f, 0.33f, 0.31f);

        private LessonFlowController flowController;
        private List<QuizQuestion> questions;
        private int currentQuestionIndex;
        private int correctCount;
        private bool waitingForAnswer;
        private Color[] originalOptionColors;
        private bool visualsApplied;

        public void Initialize(LetterGroupData group, LessonData lesson, LessonFlowController controller)
        {
            flowController = controller;
            currentQuestionIndex = 0;
            correctCount = 0;

            AutoWire();
            ApplyArabicFonts();

            if (!visualsApplied)
            {
                ApplyVisuals();
                visualsApplied = true;
            }

            questions = GenerateQuestions(group, lesson);
            ShowQuestion();
        }

        private void AutoWire()
        {
            if (questionText == null)
            {
                var t = FindChild(transform, "QuestionText");
                if (t != null) questionText = t.GetComponent<TMP_Text>();
            }
            if (promptLetterText == null)
            {
                var t = FindChild(transform, "PromptLetterText");
                if (t != null) promptLetterText = t.GetComponent<TMP_Text>();
            }
            if (promptNameText == null)
            {
                var t = FindChild(transform, "PromptNameText");
                if (t != null) promptNameText = t.GetComponent<TMP_Text>();
            }
            if (speakerButtonObj == null)
            {
                var t = FindChild(transform, "QuizSpeakerButton");
                if (t != null)
                {
                    speakerButtonObj = t.gameObject;
                    speakerButton = t.GetComponent<Button>();
                }
            }
            if (speakerButton == null && speakerButtonObj != null)
            {
                speakerButton = speakerButtonObj.GetComponent<Button>();
            }
            if (progressFill == null)
            {
                var t = FindChild(transform, "QuizProgressFill");
                if (t != null) progressFill = t.GetComponent<Image>();
            }
            if (progressText == null)
            {
                var t = FindChild(transform, "QuizProgressText");
                if (t != null) progressText = t.GetComponent<TMP_Text>();
            }

            // Wire option buttons from OptionsGrid
            if (optionButtons == null || optionButtons.Length == 0)
            {
                var grid = FindChild(transform, "OptionsGrid");
                if (grid != null)
                {
                    int count = Mathf.Min(grid.childCount, 4);
                    optionButtons = new Button[count];
                    optionTexts = new TMP_Text[count];
                    optionBackgrounds = new Image[count];

                    for (int i = 0; i < count; i++)
                    {
                        var child = grid.GetChild(i);
                        optionButtons[i] = child.GetComponent<Button>();
                        optionBackgrounds[i] = child.GetComponent<Image>();
                        optionTexts[i] = child.GetComponentInChildren<TMP_Text>();
                    }
                }
            }

            // Store original colors from the option backgrounds
            if (optionBackgrounds != null && optionBackgrounds.Length > 0)
            {
                originalOptionColors = new Color[optionBackgrounds.Length];
                for (int i = 0; i < optionBackgrounds.Length; i++)
                {
                    if (optionBackgrounds[i] != null)
                        originalOptionColors[i] = optionBackgrounds[i].color;
                }
            }
        }

        private void ApplyVisuals()
        {
            // Apply rounded rect sprites to option buttons
            var roundedSprite = SpriteGenerator.RoundedRect(256, 128, 20);
            if (optionBackgrounds != null)
            {
                for (int i = 0; i < optionBackgrounds.Length; i++)
                {
                    if (optionBackgrounds[i] != null && optionBackgrounds[i].sprite == null)
                    {
                        optionBackgrounds[i].sprite = roundedSprite;
                        optionBackgrounds[i].type = Image.Type.Sliced;
                    }
                }
            }

            // Apply rounded rect to speaker button
            if (speakerButtonObj != null)
            {
                var spkImg = speakerButtonObj.GetComponent<Image>();
                if (spkImg != null && spkImg.sprite == null)
                {
                    spkImg.sprite = SpriteGenerator.RoundedRect(128, 64, 16);
                    spkImg.type = Image.Type.Sliced;
                }

                // Replace speaker emoji with sprite icon
                var speakerIconT = FindChild(speakerButtonObj.transform, "SpeakerIcon");
                if (speakerIconT != null)
                {
                    var tmp = speakerIconT.GetComponent<TMP_Text>();
                    if (tmp != null && tmp.text.Contains("Play"))
                    {
                        tmp.text = "Play";
                        // Add speaker icon image next to text
                    }
                }
            }

            // Apply rounded rect to progress bar background
            var progBgT = FindChild(transform, "QuizProgressBG");
            if (progBgT != null)
            {
                var progBgImg = progBgT.GetComponent<Image>();
                if (progBgImg != null && progBgImg.sprite == null)
                {
                    progBgImg.sprite = SpriteGenerator.RoundedRect(128, 32, 12);
                    progBgImg.type = Image.Type.Sliced;
                }
            }
        }

        private void ApplyArabicFonts()
        {
            ArabicFontHelper.Apply(promptLetterText);
            if (optionTexts != null)
            {
                foreach (var t in optionTexts)
                    ArabicFontHelper.Apply(t);
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

        private List<QuizQuestion> GenerateQuestions(LetterGroupData group, LessonData lesson)
        {
            var result = new List<QuizQuestion>();
            int count = group.questionsPerQuiz;

            var distractorPool = new List<LetterData>();
            foreach (var letter in lesson.allLetters)
            {
                bool inGroup = false;
                foreach (var gl in group.letters)
                {
                    if (gl == letter) { inGroup = true; break; }
                }
                if (!inGroup) distractorPool.Add(letter);
            }

            QuizType[] types = { QuizType.AudioToLetter, QuizType.LetterToName, QuizType.NameToLetter };

            for (int i = 0; i < count; i++)
            {
                var correctLetter = group.letters[i % group.letters.Length];
                var type = types[i % types.Length];

                var options = new LetterData[4];
                int correctSlot = Random.Range(0, 4);
                options[correctSlot] = correctLetter;

                var usedDistractors = new HashSet<int>();

                var groupDistractors = new List<LetterData>();
                foreach (var gl in group.letters)
                {
                    if (gl != correctLetter) groupDistractors.Add(gl);
                }

                int filled = 0;
                for (int d = 0; d < 4; d++)
                {
                    if (d == correctSlot) continue;

                    if (filled < groupDistractors.Count)
                    {
                        options[d] = groupDistractors[filled];
                        filled++;
                    }
                    else
                    {
                        int attempts = 0;
                        int idx;
                        do
                        {
                            idx = Random.Range(0, distractorPool.Count);
                            attempts++;
                        } while (usedDistractors.Contains(idx) && attempts < 50);
                        usedDistractors.Add(idx);
                        options[d] = distractorPool.Count > 0 ? distractorPool[idx] : correctLetter;
                    }
                }

                result.Add(new QuizQuestion
                {
                    type = type,
                    correctAnswer = correctLetter,
                    options = options,
                    correctOptionIndex = correctSlot
                });
            }

            // Shuffle questions
            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }

            return result;
        }

        private void ShowQuestion()
        {
            if (currentQuestionIndex >= questions.Count)
            {
                flowController.ShowResults(correctCount, questions.Count);
                return;
            }

            var q = questions[currentQuestionIndex];
            waitingForAnswer = true;

            // Reset option colors and scale
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (originalOptionColors != null && i < originalOptionColors.Length)
                    optionBackgrounds[i].color = originalOptionColors[i];
                else
                    optionBackgrounds[i].color = defaultOptionColor;
                optionButtons[i].interactable = true;
                optionButtons[i].transform.localScale = Vector3.one;
            }

            // Set up display based on question type
            switch (q.type)
            {
                case QuizType.AudioToLetter:
                    questionText.text = "Which letter is this?";
                    promptLetterText.gameObject.SetActive(false);
                    promptNameText.gameObject.SetActive(false);
                    speakerButtonObj.SetActive(true);
                    AudioManager.Instance?.PlayLetterAudio(q.correctAnswer.audioClip);
                    for (int i = 0; i < 4; i++)
                        optionTexts[i].text = q.options[i].letterArabic;
                    break;

                case QuizType.LetterToName:
                    questionText.text = "What is this letter called?";
                    promptLetterText.gameObject.SetActive(true);
                    promptLetterText.text = q.correctAnswer.letterArabic;
                    promptNameText.gameObject.SetActive(false);
                    speakerButtonObj.SetActive(false);
                    for (int i = 0; i < 4; i++)
                        optionTexts[i].text = q.options[i].letterName;
                    break;

                case QuizType.NameToLetter:
                    questionText.text = "Find this letter:";
                    promptLetterText.gameObject.SetActive(false);
                    promptNameText.gameObject.SetActive(true);
                    promptNameText.text = q.correctAnswer.letterName;
                    speakerButtonObj.SetActive(false);
                    for (int i = 0; i < 4; i++)
                        optionTexts[i].text = q.options[i].letterArabic;
                    break;
            }

            // Update progress bar
            float progress = (float)currentQuestionIndex / questions.Count;
            if (progressFill != null) progressFill.fillAmount = progress;
            if (progressText != null) progressText.text = $"{currentQuestionIndex + 1} / {questions.Count}";

            // Wire up buttons
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int index = i;
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
            }

            // Wire speaker button
            if (speakerButton != null)
            {
                speakerButton.onClick.RemoveAllListeners();
                speakerButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayLetterAudio(q.correctAnswer.audioClip);
                });
            }
        }

        private void OnOptionSelected(int index)
        {
            if (!waitingForAnswer) return;
            waitingForAnswer = false;

            var q = questions[currentQuestionIndex];
            bool isCorrect = index == q.correctOptionIndex;

            // Disable all buttons
            for (int i = 0; i < optionButtons.Length; i++)
                optionButtons[i].interactable = false;

            if (isCorrect)
            {
                correctCount++;
                // Animated correct feedback
                StartCoroutine(UIAnimations.ColorLerp(
                    optionBackgrounds[index], optionBackgrounds[index].color, correctColor, 0.2f));
                StartCoroutine(UIAnimations.ScalePop(
                    optionButtons[index].transform, 1f, 1.08f, 0.25f));
                AudioManager.Instance?.PlaySFX(SFXType.Correct);
                StartCoroutine(AdvanceAfterDelay(1.0f));
            }
            else
            {
                // Animated wrong feedback: shake + red
                StartCoroutine(UIAnimations.ColorLerp(
                    optionBackgrounds[index], optionBackgrounds[index].color, wrongColor, 0.15f));
                StartCoroutine(UIAnimations.Shake(
                    optionButtons[index].transform, 12f, 0.35f));
                AudioManager.Instance?.PlaySFX(SFXType.Wrong);

                // Delayed reveal of correct answer
                StartCoroutine(DelayedCorrectReveal(q.correctOptionIndex, 0.4f));
                StartCoroutine(AdvanceAfterDelay(1.8f));
            }
        }

        private IEnumerator DelayedCorrectReveal(int correctIndex, float delay)
        {
            yield return new WaitForSeconds(delay);
            StartCoroutine(UIAnimations.ColorLerp(
                optionBackgrounds[correctIndex], optionBackgrounds[correctIndex].color, correctColor, 0.3f));
            StartCoroutine(UIAnimations.ScalePop(
                optionButtons[correctIndex].transform, 1f, 1.05f, 0.2f));
        }

        private IEnumerator AdvanceAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            currentQuestionIndex++;
            ShowQuestion();
        }
    }

    public class QuizQuestion
    {
        public QuizType type;
        public LetterData correctAnswer;
        public LetterData[] options;
        public int correctOptionIndex;
    }
}

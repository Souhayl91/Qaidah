#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using SimpleQaidah.Core;
using SimpleQaidah.Data;
using SimpleQaidah.UI;
using System.Reflection;

namespace SimpleQaidah.Editor
{
    /// <summary>
    /// Wires all SerializeField references in the scenes after they have been created by ProjectSetup.
    /// Run via Qaidah menu > 4. Wire All References.
    /// </summary>
    public static class SceneWiring
    {
        private static readonly string BasePath = "Assets/_Project";

        [MenuItem("Qaidah/4. Wire All References")]
        public static void WireAllScenes()
        {
            WireBootScene();
            WireMainMenuScene();
            WireLessonScene();
            Debug.Log("All scene references wired successfully!");
        }

        private static void WireBootScene()
        {
            var scene = EditorSceneManager.OpenScene($"{BasePath}/Scenes/BootScene.unity");

            // Wire AudioManager's audio sources
            var amGO = GameObject.Find("AudioManager");
            if (amGO != null)
            {
                var am = amGO.GetComponent<AudioManager>();
                var sources = amGO.GetComponents<AudioSource>();
                if (sources.Length >= 2)
                {
                    SetPrivateField(am, "letterSource", sources[0]);
                    SetPrivateField(am, "sfxSource", sources[1]);
                }
            }

            // Wire GameManager's lesson list
            var gmGO = GameObject.Find("GameManager");
            if (gmGO != null)
            {
                var gm = gmGO.GetComponent<GameManager>();
                var lesson1 = AssetDatabase.LoadAssetAtPath<LessonData>($"{BasePath}/Data/Lessons/Lesson1.asset");
                if (lesson1 != null)
                {
                    SetPrivateField(gm, "availableLessons", new LessonData[] { lesson1 });
                }
            }

            EditorSceneManager.SaveScene(scene);
        }

        private static void WireMainMenuScene()
        {
            var scene = EditorSceneManager.OpenScene($"{BasePath}/Scenes/MainMenuScene.unity");

            var canvasGO = GameObject.Find("MainCanvas");
            if (canvasGO == null) return;

            var menuUI = canvasGO.GetComponent<MainMenuUI>();
            if (menuUI == null) return;

            var lesson1 = AssetDatabase.LoadAssetAtPath<LessonData>($"{BasePath}/Data/Lessons/Lesson1.asset");
            SetPrivateField(menuUI, "lesson1", lesson1);

            var l1Btn = FindDeep(canvasGO.transform, "Lesson1Button");
            if (l1Btn != null) SetPrivateField(menuUI, "lesson1Button", l1Btn.GetComponent<Button>());

            var l1Title = FindDeep(canvasGO.transform, "Lesson1TitleText");
            if (l1Title != null) SetPrivateField(menuUI, "lesson1TitleText", l1Title.GetComponent<TMP_Text>());

            var l1Prog = FindDeep(canvasGO.transform, "Lesson1ProgressText");
            if (l1Prog != null) SetPrivateField(menuUI, "lesson1ProgressText", l1Prog.GetComponent<TMP_Text>());

            var progFill = FindDeep(canvasGO.transform, "ProgressBarFill");
            if (progFill != null) SetPrivateField(menuUI, "lesson1ProgressFill", progFill.GetComponent<Image>());

            var xpText = FindDeep(canvasGO.transform, "TotalXPText");
            if (xpText != null) SetPrivateField(menuUI, "totalXPText", xpText.GetComponent<TMP_Text>());

            EditorSceneManager.SaveScene(scene);
        }

        private static void WireLessonScene()
        {
            var scene = EditorSceneManager.OpenScene($"{BasePath}/Scenes/LessonScene.unity");

            var canvasGO = GameObject.Find("LessonCanvas");
            if (canvasGO == null) return;

            // ─── LessonFlowController ────────────────────────────
            var flowGO = FindDeep(canvasGO.transform, "LessonFlowController");
            var flow = flowGO?.GetComponent<LessonFlowController>();
            if (flow != null)
            {
                var gridPanel = FindDeep(canvasGO.transform, "AlphabetGridPanel");
                var learnPanel = FindDeep(canvasGO.transform, "LearnPanel");
                var quizPanel = FindDeep(canvasGO.transform, "QuizPanel");
                var resultPanel = FindDeep(canvasGO.transform, "ResultPanel");

                SetPrivateField(flow, "alphabetGridPanel", gridPanel?.gameObject);
                SetPrivateField(flow, "learnPanel", learnPanel?.gameObject);
                SetPrivateField(flow, "quizPanel", quizPanel?.gameObject);
                SetPrivateField(flow, "resultPanel", resultPanel?.gameObject);

                if (gridPanel != null) SetPrivateField(flow, "alphabetGridUI", gridPanel.GetComponent<AlphabetGridUI>());
                if (learnPanel != null) SetPrivateField(flow, "learnPhaseUI", learnPanel.GetComponent<LearnPhaseUI>());
                if (quizPanel != null) SetPrivateField(flow, "quizPhaseUI", quizPanel.GetComponent<QuizPhaseUI>());
                if (resultPanel != null) SetPrivateField(flow, "quizResultUI", resultPanel.GetComponent<QuizResultUI>());
            }

            // ─── Back button → flow controller ──────────────────
            var backBtnGO = FindDeep(canvasGO.transform, "BackButton");
            if (backBtnGO != null && flow != null)
            {
                var backBtn = backBtnGO.GetComponent<Button>();
                // We need to wire this in code since UnityEvents can't easily be set via reflection
                // The back button will be wired in a runtime helper instead
            }

            // ─── AlphabetGridUI ─────────────────────────────────
            var gridUI = FindDeep(canvasGO.transform, "AlphabetGridPanel")?.GetComponent<AlphabetGridUI>();
            if (gridUI != null)
            {
                var gridContainer = FindDeep(canvasGO.transform, "GridContainer");
                if (gridContainer != null) SetPrivateField(gridUI, "gridContainer", gridContainer);

                var letterCardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{BasePath}/Prefabs/LetterCard.prefab");
                if (letterCardPrefab != null) SetPrivateField(gridUI, "letterCardPrefab", letterCardPrefab);

                var startBtn = FindDeep(canvasGO.transform, "StartButton");
                if (startBtn != null) SetPrivateField(gridUI, "startButton", startBtn.GetComponent<Button>());

                var startText = FindDeep(startBtn, "StartText");
                if (startText != null) SetPrivateField(gridUI, "startButtonText", startText.GetComponent<TMP_Text>());
            }

            // ─── LearnPhaseUI ───────────────────────────────────
            var learnUI = FindDeep(canvasGO.transform, "LearnPanel")?.GetComponent<LearnPhaseUI>();
            if (learnUI != null)
            {
                var learnPanel = FindDeep(canvasGO.transform, "LearnPanel");

                var largeLetter = FindDeep(learnPanel, "LargeLetterText");
                if (largeLetter != null) SetPrivateField(learnUI, "largeLetterText", largeLetter.GetComponent<TMP_Text>());

                var letterName = FindDeep(learnPanel, "LetterNameText");
                if (letterName != null) SetPrivateField(learnUI, "letterNameText", letterName.GetComponent<TMP_Text>());

                var translit = FindDeep(learnPanel, "TransliterationText");
                if (translit != null) SetPrivateField(learnUI, "transliterationText", translit.GetComponent<TMP_Text>());

                var speaker = FindDeep(learnPanel, "SpeakerButton");
                if (speaker != null) SetPrivateField(learnUI, "speakerButton", speaker.GetComponent<Button>());

                var prevBtn = FindDeep(learnPanel, "PrevButton");
                if (prevBtn != null) SetPrivateField(learnUI, "prevButton", prevBtn.GetComponent<Button>());

                var nextBtn = FindDeep(learnPanel, "NextButton");
                if (nextBtn != null) SetPrivateField(learnUI, "nextButton", nextBtn.GetComponent<Button>());

                var practiceBtn = FindDeep(learnPanel, "PracticeButton");
                if (practiceBtn != null) SetPrivateField(learnUI, "practiceButton", practiceBtn.GetComponent<Button>());

                var counter = FindDeep(learnPanel, "CounterText");
                if (counter != null) SetPrivateField(learnUI, "counterText", counter.GetComponent<TMP_Text>());

                var dotsContainer = FindDeep(learnPanel, "DotsContainer");
                if (dotsContainer != null) SetPrivateField(learnUI, "dotsContainer", dotsContainer);

                var dotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{BasePath}/Prefabs/Dot.prefab");
                if (dotPrefab != null) SetPrivateField(learnUI, "dotPrefab", dotPrefab);
            }

            // ─── QuizPhaseUI ────────────────────────────────────
            var quizUI = FindDeep(canvasGO.transform, "QuizPanel")?.GetComponent<QuizPhaseUI>();
            if (quizUI != null)
            {
                var quizPanel = FindDeep(canvasGO.transform, "QuizPanel");

                var qText = FindDeep(quizPanel, "QuestionText");
                if (qText != null) SetPrivateField(quizUI, "questionText", qText.GetComponent<TMP_Text>());

                var promptLetter = FindDeep(quizPanel, "PromptLetterText");
                if (promptLetter != null) SetPrivateField(quizUI, "promptLetterText", promptLetter.GetComponent<TMP_Text>());

                var promptName = FindDeep(quizPanel, "PromptNameText");
                if (promptName != null) SetPrivateField(quizUI, "promptNameText", promptName.GetComponent<TMP_Text>());

                var quizSpeaker = FindDeep(quizPanel, "QuizSpeakerButton");
                if (quizSpeaker != null)
                {
                    SetPrivateField(quizUI, "speakerButton", quizSpeaker.GetComponent<Button>());
                    SetPrivateField(quizUI, "speakerButtonObj", quizSpeaker.gameObject);
                }

                var progressFill = FindDeep(quizPanel, "QuizProgressFill");
                if (progressFill != null) SetPrivateField(quizUI, "progressFill", progressFill.GetComponent<Image>());

                var progressText = FindDeep(quizPanel, "QuizProgressText");
                if (progressText != null) SetPrivateField(quizUI, "progressText", progressText.GetComponent<TMP_Text>());

                // Wire option buttons
                var optionsGrid = FindDeep(quizPanel, "OptionsGrid");
                if (optionsGrid != null)
                {
                    var buttons = new Button[4];
                    var texts = new TMP_Text[4];
                    var bgs = new Image[4];
                    for (int i = 0; i < 4; i++)
                    {
                        var opt = FindDeep(optionsGrid, $"Option{i + 1}");
                        if (opt != null)
                        {
                            buttons[i] = opt.GetComponent<Button>();
                            bgs[i] = opt.GetComponent<Image>();
                            var optText = FindDeep(opt, "OptionText");
                            if (optText != null) texts[i] = optText.GetComponent<TMP_Text>();
                        }
                    }
                    SetPrivateField(quizUI, "optionButtons", buttons);
                    SetPrivateField(quizUI, "optionTexts", texts);
                    SetPrivateField(quizUI, "optionBackgrounds", bgs);
                }
            }

            // ─── QuizResultUI ───────────────────────────────────
            var resultUI = FindDeep(canvasGO.transform, "ResultPanel")?.GetComponent<QuizResultUI>();
            if (resultUI != null)
            {
                var resultPanel = FindDeep(canvasGO.transform, "ResultPanel");

                var scoreText = FindDeep(resultPanel, "ScoreText");
                if (scoreText != null) SetPrivateField(resultUI, "scoreText", scoreText.GetComponent<TMP_Text>());

                var xpGain = FindDeep(resultPanel, "XPGainText");
                if (xpGain != null) SetPrivateField(resultUI, "xpGainText", xpGain.GetComponent<TMP_Text>());

                var msgText = FindDeep(resultPanel, "MessageText");
                if (msgText != null) SetPrivateField(resultUI, "messageText", msgText.GetComponent<TMP_Text>());

                // Star images
                var starsArea = FindDeep(resultPanel, "StarsArea");
                if (starsArea != null)
                {
                    var stars = new Image[3];
                    for (int i = 0; i < 3; i++)
                    {
                        var star = FindDeep(starsArea, $"Star{i + 1}");
                        if (star != null) stars[i] = star.GetComponent<Image>();
                    }
                    SetPrivateField(resultUI, "starImages", stars);
                }

                var retryBtn = FindDeep(resultPanel, "RetryButton");
                if (retryBtn != null) SetPrivateField(resultUI, "retryButton", retryBtn.GetComponent<Button>());

                var contBtn = FindDeep(resultPanel, "ContinueButton");
                if (contBtn != null) SetPrivateField(resultUI, "continueButton", contBtn.GetComponent<Button>());

                var contText = FindDeep(contBtn, "ContinueText");
                if (contText != null) SetPrivateField(resultUI, "continueButtonText", contText.GetComponent<TMP_Text>());
            }

            // ─── Wire back button at runtime ────────────────────
            // Add a small helper component for the back button
            var headerBackBtn = FindDeep(canvasGO.transform, "BackButton");
            if (headerBackBtn != null)
            {
                var helper = headerBackBtn.gameObject.AddComponent<BackButtonHelper>();
                // It will find LessonFlowController at runtime
            }

            EditorSceneManager.SaveScene(scene);
        }

        // ─── HELPERS ────────────────────────────────────────────

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null || value == null) return;

            var type = target.GetType();
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                // Try base classes
                while (type.BaseType != null)
                {
                    type = type.BaseType;
                    field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null) break;
                }
            }

            if (field != null)
            {
                field.SetValue(target, value);
                EditorUtility.SetDirty(target as Object);
            }
            else
            {
                Debug.LogWarning($"Could not find field '{fieldName}' on {target.GetType().Name}");
            }
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent == null) return null;
            if (parent.name == name) return parent;

            foreach (Transform child in parent)
            {
                var result = FindDeep(child, name);
                if (result != null) return result;
            }
            return null;
        }

        private static Transform FindDeep(GameObject go, string name)
        {
            return go == null ? null : FindDeep(go.transform, name);
        }
    }
}
#endif

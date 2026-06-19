using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using SimpleQaidah.Core;
using SimpleQaidah.Data;

namespace SimpleQaidah.UI
{
    public class LessonFlowController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject alphabetGridPanel;
        [SerializeField] private GameObject learnPanel;
        [SerializeField] private GameObject quizPanel;
        [SerializeField] private GameObject resultPanel;

        [Header("UI Controllers")]
        [SerializeField] private AlphabetGridUI alphabetGridUI;
        [SerializeField] private LearnPhaseUI learnPhaseUI;
        [SerializeField] private QuizPhaseUI quizPhaseUI;
        [SerializeField] private QuizResultUI quizResultUI;

        private LessonData currentLesson;
        private int currentGroupIndex;
        private GameObject currentActivePanel;
        private bool isTransitioning;

        private void Start()
        {
            AutoWire();
            EnsureCanvasGroups();

            currentLesson = GameManager.Instance.CurrentLesson;
            if (currentLesson == null)
            {
                Debug.LogError("No lesson set on GameManager!");
                return;
            }

            alphabetGridUI.Initialize(currentLesson, this);
            ShowAlphabetGrid();
        }

        private void AutoWire()
        {
            var canvas = GetComponentInParent<Canvas>()?.transform ?? transform.root;

            if (alphabetGridPanel == null) alphabetGridPanel = FindChild(canvas, "AlphabetGridPanel");
            if (learnPanel == null) learnPanel = FindChild(canvas, "LearnPanel");
            if (quizPanel == null) quizPanel = FindChild(canvas, "QuizPanel");
            if (resultPanel == null) resultPanel = FindChild(canvas, "ResultPanel");

            if (alphabetGridUI == null && alphabetGridPanel != null)
                alphabetGridUI = alphabetGridPanel.GetComponent<AlphabetGridUI>();
            if (learnPhaseUI == null && learnPanel != null)
                learnPhaseUI = learnPanel.GetComponent<LearnPhaseUI>();
            if (quizPhaseUI == null && quizPanel != null)
                quizPhaseUI = quizPanel.GetComponent<QuizPhaseUI>();
            if (quizResultUI == null && resultPanel != null)
                quizResultUI = resultPanel.GetComponent<QuizResultUI>();
        }

        private void EnsureCanvasGroups()
        {
            EnsureCanvasGroup(alphabetGridPanel);
            EnsureCanvasGroup(learnPanel);
            EnsureCanvasGroup(quizPanel);
            EnsureCanvasGroup(resultPanel);
        }

        private static void EnsureCanvasGroup(GameObject panel)
        {
            if (panel != null && panel.GetComponent<CanvasGroup>() == null)
                panel.AddComponent<CanvasGroup>();
        }

        private static GameObject FindChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child.gameObject;
                var found = FindChild(child, name);
                if (found != null) return found;
            }
            return null;
        }

        // ─── Public Navigation ──────────────────────────────────

        public void ShowAlphabetGrid()
        {
            TransitionToPanel(alphabetGridPanel, TransitionDir.Left);
            alphabetGridUI.Refresh();
        }

        public void StartGroup(int groupIndex)
        {
            currentGroupIndex = groupIndex;
            var group = currentLesson.groups[groupIndex];
            TransitionToPanel(learnPanel, TransitionDir.Right, () =>
            {
                learnPhaseUI.Initialize(group, this);
            });
        }

        public void StartQuiz()
        {
            var group = currentLesson.groups[currentGroupIndex];
            TransitionToPanel(quizPanel, TransitionDir.Right, () =>
            {
                quizPhaseUI.Initialize(group, currentLesson, this);
            });
        }

        public void ShowResults(int score, int totalQuestions)
        {
            TransitionToPanel(resultPanel, TransitionDir.Up, () =>
            {
                quizResultUI.Initialize(
                    score,
                    totalQuestions,
                    currentLesson.lessonId,
                    currentGroupIndex,
                    currentLesson.groups.Length,
                    this
                );
            });
        }

        public void RetryCurrentGroup()
        {
            StartGroup(currentGroupIndex);
        }

        public void AdvanceToNextGroup()
        {
            int nextIndex = currentGroupIndex + 1;
            if (nextIndex < currentLesson.groups.Length)
            {
                var progress = SaveManager.Instance.Progress.GetLesson(currentLesson.lessonId);
                if (progress != null && nextIndex < progress.groups.Count && progress.groups[nextIndex].isUnlocked)
                {
                    StartGroup(nextIndex);
                    return;
                }
            }
            ShowAlphabetGrid();
        }

        public void BackToMenu()
        {
            GameManager.Instance.LoadMainMenu();
        }

        // ─── Animated Transitions ───────────────────────────────

        private enum TransitionDir { Left, Right, Up, Down }

        private void TransitionToPanel(GameObject newPanel, TransitionDir direction, System.Action onNewPanelReady = null)
        {
            if (newPanel == null || newPanel == currentActivePanel) return;

            if (isTransitioning)
            {
                // Force-finish: just switch instantly
                ForceSetPanel(newPanel);
                onNewPanelReady?.Invoke();
                return;
            }

            StartCoroutine(DoTransition(currentActivePanel, newPanel, direction, onNewPanelReady));
        }

        private IEnumerator DoTransition(GameObject oldPanel, GameObject newPanel, TransitionDir direction, System.Action onNewPanelReady)
        {
            isTransitioning = true;
            float duration = 0.3f;

            Vector2 slideOffset = direction switch
            {
                TransitionDir.Right => new Vector2(800, 0),
                TransitionDir.Left => new Vector2(-800, 0),
                TransitionDir.Up => new Vector2(0, -800),
                TransitionDir.Down => new Vector2(0, 800),
                _ => new Vector2(800, 0)
            };

            // Prepare new panel (visible but transparent, at offset)
            newPanel.SetActive(true);
            var newCG = newPanel.GetComponent<CanvasGroup>();
            var newRT = newPanel.GetComponent<RectTransform>();
            if (newCG != null)
            {
                newCG.alpha = 0f;
                newCG.blocksRaycasts = false;
            }

            // Initialize new panel content before animation starts
            onNewPanelReady?.Invoke();

            // Store original position
            Vector2 newOriginalPos = newRT.anchoredPosition;
            newRT.anchoredPosition = newOriginalPos + slideOffset;

            // Animate old panel out (if exists)
            CanvasGroup oldCG = null;
            if (oldPanel != null)
            {
                oldCG = oldPanel.GetComponent<CanvasGroup>();
                if (oldCG != null)
                    oldCG.blocksRaycasts = false;
            }

            // Animate both panels simultaneously
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f); // ease-out-cubic

                // New panel slides in and fades in
                newRT.anchoredPosition = Vector2.Lerp(newOriginalPos + slideOffset, newOriginalPos, eased);
                if (newCG != null) newCG.alpha = eased;

                // Old panel fades out
                if (oldCG != null) oldCG.alpha = 1f - eased;

                yield return null;
            }

            // Finalize
            newRT.anchoredPosition = newOriginalPos;
            if (newCG != null)
            {
                newCG.alpha = 1f;
                newCG.blocksRaycasts = true;
            }

            if (oldPanel != null)
            {
                oldPanel.SetActive(false);
                if (oldCG != null)
                {
                    oldCG.alpha = 1f;
                    oldCG.blocksRaycasts = true;
                }
            }

            currentActivePanel = newPanel;
            isTransitioning = false;
        }

        private void ForceSetPanel(GameObject activePanel)
        {
            if (alphabetGridPanel != null) alphabetGridPanel.SetActive(activePanel == alphabetGridPanel);
            if (learnPanel != null) learnPanel.SetActive(activePanel == learnPanel);
            if (quizPanel != null) quizPanel.SetActive(activePanel == quizPanel);
            if (resultPanel != null) resultPanel.SetActive(activePanel == resultPanel);
            currentActivePanel = activePanel;

            // Reset all canvas groups
            ResetCanvasGroup(alphabetGridPanel);
            ResetCanvasGroup(learnPanel);
            ResetCanvasGroup(quizPanel);
            ResetCanvasGroup(resultPanel);
        }

        private static void ResetCanvasGroup(GameObject panel)
        {
            if (panel == null) return;
            var cg = panel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.blocksRaycasts = panel.activeSelf;
            }
        }
    }
}

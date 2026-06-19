using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SimpleQaidah.Core;
using SimpleQaidah.Data;

namespace SimpleQaidah.UI
{
    public class AlphabetGridUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform gridContainer;
        [SerializeField] private GameObject letterCardPrefab;
        [SerializeField] private Button startButton;
        [SerializeField] private TMP_Text startButtonText;

        private LessonData lessonData;
        private LessonFlowController flowController;
        private LetterCardUI[] cards;

        public void Initialize(LessonData lesson, LessonFlowController controller)
        {
            lessonData = lesson;
            flowController = controller;

            AutoWire();
            BuildGrid();
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartClicked);
        }

        private void AutoWire()
        {
            if (gridContainer == null)
            {
                var t = transform.Find("GridContainer");
                if (t != null) gridContainer = t;
            }
            if (startButton == null)
            {
                var t = transform.Find("StartButton");
                if (t != null) startButton = t.GetComponent<Button>();
            }
            if (startButtonText == null && startButton != null)
            {
                startButtonText = startButton.GetComponentInChildren<TMP_Text>();
            }
            if (letterCardPrefab == null)
            {
                letterCardPrefab = Resources.Load<GameObject>("LetterCard");
                if (letterCardPrefab == null)
                {
                    // Try loading from the project path
                    #if UNITY_EDITOR
                    letterCardPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/LetterCard.prefab");
                    #endif
                }
            }
        }

        private void BuildGrid()
        {
            if (gridContainer == null)
            {
                Debug.LogError("AlphabetGridUI: gridContainer is null!");
                return;
            }

            // Clear existing children
            foreach (Transform child in gridContainer)
            {
                Destroy(child.gameObject);
            }

            if (letterCardPrefab == null)
            {
                Debug.LogError("AlphabetGridUI: letterCardPrefab is null! Assign it in the Inspector or place it in a Resources folder.");
                return;
            }

            cards = new LetterCardUI[lessonData.allLetters.Length];
            for (int i = 0; i < lessonData.allLetters.Length; i++)
            {
                var go = Instantiate(letterCardPrefab, gridContainer);
                cards[i] = go.GetComponent<LetterCardUI>();
            }

            Refresh();
        }

        public void Refresh()
        {
            if (lessonData == null || SaveManager.Instance == null) return;

            var progress = SaveManager.Instance.Progress
                .GetOrCreateLesson(lessonData.lessonId, lessonData.groups.Length);

            for (int i = 0; i < lessonData.allLetters.Length; i++)
            {
                var letter = lessonData.allLetters[i];
                var state = GetLetterState(letter, progress);
                cards[i].Setup(letter, state);
            }

            UpdateStartButton(progress);
        }

        private LetterCardState GetLetterState(LetterData letter, LessonProgress progress)
        {
            for (int g = 0; g < lessonData.groups.Length; g++)
            {
                var group = lessonData.groups[g];
                foreach (var gl in group.letters)
                {
                    if (gl == letter)
                    {
                        var gp = progress.groups[g];
                        if (gp.isCompleted) return LetterCardState.Completed;
                        if (gp.isUnlocked) return LetterCardState.Available;
                        return LetterCardState.Locked;
                    }
                }
            }
            return LetterCardState.Locked;
        }

        private void UpdateStartButton(LessonProgress progress)
        {
            int nextGroup = -1;
            for (int i = 0; i < progress.groups.Count; i++)
            {
                if (progress.groups[i].isUnlocked && !progress.groups[i].isCompleted)
                {
                    nextGroup = i;
                    break;
                }
            }

            if (nextGroup >= 0)
            {
                startButton.gameObject.SetActive(true);
                bool isFirstGroup = nextGroup == 0 && progress.CompletedGroupCount == 0;
                startButtonText.text = isFirstGroup ? "Start Learning" : "Continue Learning";
            }
            else if (progress.isCompleted)
            {
                startButton.gameObject.SetActive(true);
                startButtonText.text = "Review Lesson";
            }
            else
            {
                startButton.gameObject.SetActive(false);
            }
        }

        private void OnStartClicked()
        {
            var progress = SaveManager.Instance.Progress
                .GetOrCreateLesson(lessonData.lessonId, lessonData.groups.Length);

            for (int i = 0; i < progress.groups.Count; i++)
            {
                if (progress.groups[i].isUnlocked && !progress.groups[i].isCompleted)
                {
                    flowController.StartGroup(i);
                    return;
                }
            }

            flowController.StartGroup(0);
        }
    }
}

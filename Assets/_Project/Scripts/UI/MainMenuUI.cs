using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using SimpleQaidah.Core;
using SimpleQaidah.Data;

namespace SimpleQaidah.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LessonData lesson1;
        [SerializeField] private Button lesson1Button;
        [SerializeField] private TMP_Text lesson1TitleText;
        [SerializeField] private TMP_Text lesson1ProgressText;
        [SerializeField] private Image lesson1ProgressFill;
        [SerializeField] private TMP_Text totalXPText;

        private bool visualsApplied;

        private void Start()
        {
            AutoWire();

            if (!visualsApplied)
            {
                ApplyVisuals();
                visualsApplied = true;
            }

            UpdateUI();

            if (lesson1Button != null)
                lesson1Button.onClick.AddListener(OnLesson1Clicked);

            // Entrance animation
            StartCoroutine(AnimateEntrance());
        }

        private void AutoWire()
        {
            // Try to load lesson data if not assigned
            if (lesson1 == null)
            {
                #if UNITY_EDITOR
                lesson1 = UnityEditor.AssetDatabase.LoadAssetAtPath<LessonData>("Assets/_Project/Data/Lessons/Lesson1.asset");
                #endif
                if (lesson1 == null)
                {
                    lesson1 = Resources.Load<LessonData>("Lesson1");
                }
                if (lesson1 == null)
                {
                    var allLessons = Resources.FindObjectsOfTypeAll<LessonData>();
                    if (allLessons.Length > 0) lesson1 = allLessons[0];
                }
            }

            if (lesson1Button == null)
            {
                var t = FindChild(transform, "Lesson1Button");
                if (t != null) lesson1Button = t.GetComponent<Button>();
            }
            if (lesson1TitleText == null)
            {
                var t = FindChild(transform, "Lesson1TitleText");
                if (t != null) lesson1TitleText = t.GetComponent<TMP_Text>();
            }
            if (lesson1ProgressText == null)
            {
                var t = FindChild(transform, "Lesson1ProgressText");
                if (t != null) lesson1ProgressText = t.GetComponent<TMP_Text>();
            }
            if (lesson1ProgressFill == null)
            {
                var t = FindChild(transform, "ProgressBarFill");
                if (t != null) lesson1ProgressFill = t.GetComponent<Image>();
            }
            if (totalXPText == null)
            {
                var t = FindChild(transform, "TotalXPText");
                if (t != null) totalXPText = t.GetComponent<TMP_Text>();
            }
        }

        private void ApplyVisuals()
        {
            // Apply rounded rect to lesson button
            if (lesson1Button != null)
            {
                var img = lesson1Button.GetComponent<Image>();
                if (img != null && img.sprite == null)
                {
                    img.sprite = SpriteGenerator.RoundedRect(256, 128, 24);
                    img.type = Image.Type.Sliced;
                }
            }

            // Apply rounded rect to progress bar background
            var progBarBG = FindChild(transform, "ProgressBarBG");
            if (progBarBG != null)
            {
                var bgImg = progBarBG.GetComponent<Image>();
                if (bgImg != null && bgImg.sprite == null)
                {
                    bgImg.sprite = SpriteGenerator.RoundedRect(128, 32, 12);
                    bgImg.type = Image.Type.Sliced;
                }
            }
        }

        private IEnumerator AnimateEntrance()
        {
            // Slide lesson card up from below
            if (lesson1Button != null)
            {
                var rt = lesson1Button.GetComponent<RectTransform>();
                if (rt != null)
                {
                    yield return StartCoroutine(UIAnimations.SlideIn(rt, new Vector2(0, -300), 0.5f));
                }
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

        private void UpdateUI()
        {
            if (lesson1 == null)
            {
                Debug.LogError("MainMenuUI: lesson1 data is null! Cannot display lesson info.");
                return;
            }

            if (lesson1TitleText != null)
                lesson1TitleText.text = $"Lesson {lesson1.lessonNumber}: {lesson1.lessonTitle}";

            var save = SaveManager.Instance;
            if (save == null) return;

            var progress = save.Progress.GetOrCreateLesson(lesson1.lessonId, lesson1.groups.Length);
            int completed = progress.CompletedGroupCount;
            int total = lesson1.groups.Length;
            int stars = progress.TotalStars;

            if (lesson1ProgressText != null)
                lesson1ProgressText.text = $"{completed}/{total} groups  |  {stars} stars";

            float fill = total > 0 ? (float)completed / total : 0f;
            if (lesson1ProgressFill != null)
                lesson1ProgressFill.fillAmount = fill;

            if (totalXPText != null)
                totalXPText.text = $"{save.Progress.totalXP} XP";
        }

        private void OnLesson1Clicked()
        {
            if (lesson1 != null && GameManager.Instance != null)
            {
                GameManager.Instance.LoadLesson(lesson1);
            }
            else
            {
                Debug.LogError("MainMenuUI: Cannot load lesson — lesson1 or GameManager is null!");
            }
        }
    }
}

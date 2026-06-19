using UnityEngine;

namespace SimpleQaidah.Data
{
    [CreateAssetMenu(fileName = "NewLesson", menuName = "Qaidah/Lesson")]
    public class LessonData : ScriptableObject
    {
        public string lessonId;
        public string lessonTitle;
        public int lessonNumber;

        [Tooltip("Letter groups in learning order")]
        public LetterGroupData[] groups;

        [Tooltip("All letters in this lesson (for the alphabet grid)")]
        public LetterData[] allLetters;
    }
}

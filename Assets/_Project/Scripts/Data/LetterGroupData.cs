using UnityEngine;

namespace SimpleQaidah.Data
{
    [CreateAssetMenu(fileName = "NewGroup", menuName = "Qaidah/Letter Group")]
    public class LetterGroupData : ScriptableObject
    {
        [Tooltip("Display name (e.g. Group 1: Alif to Jeem)")]
        public string groupName;

        [Tooltip("Zero-based index within the lesson")]
        public int groupIndex;

        [Tooltip("Letters in this group (4-5 letters)")]
        public LetterData[] letters;

        [Tooltip("Number of quiz questions for this group")]
        public int questionsPerQuiz = 10;
    }
}

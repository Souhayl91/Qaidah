using UnityEngine;

namespace SimpleQaidah.Data
{
    [CreateAssetMenu(fileName = "NewLetter", menuName = "Qaidah/Letter Data")]
    public class LetterData : ScriptableObject
    {
        [Tooltip("The Arabic letter character (e.g. ا)")]
        public string letterArabic;

        [Tooltip("English name of the letter (e.g. Alif)")]
        public string letterName;

        [Tooltip("Transliteration (e.g. a)")]
        public string transliteration;

        [Tooltip("Audio clip for this letter's pronunciation")]
        public AudioClip audioClip;

        [Tooltip("Sort order within the alphabet (0-27)")]
        public int orderIndex;
    }
}

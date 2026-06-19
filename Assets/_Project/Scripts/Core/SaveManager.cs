using System.IO;
using UnityEngine;
using SimpleQaidah.Data;

namespace SimpleQaidah.Core
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        public PlayerProgress Progress { get; private set; }

        private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        public void Save()
        {
            string json = JsonUtility.ToJson(Progress, true);
            File.WriteAllText(SavePath, json);
        }

        public void Load()
        {
            if (File.Exists(SavePath))
            {
                string json = File.ReadAllText(SavePath);
                Progress = JsonUtility.FromJson<PlayerProgress>(json);
            }
            else
            {
                Progress = new PlayerProgress();
            }
        }

        public void UpdateGroupScore(string lessonId, int groupIndex, int score, int stars, int groupCount)
        {
            var lesson = Progress.GetOrCreateLesson(lessonId, groupCount);
            if (groupIndex < 0 || groupIndex >= lesson.groups.Count) return;

            var group = lesson.groups[groupIndex];

            if (score > group.bestScore)
                group.bestScore = score;

            if (stars > group.stars)
                group.stars = stars;

            if (stars >= 1)
            {
                group.isCompleted = true;

                // Unlock next group
                int nextIndex = groupIndex + 1;
                if (nextIndex < lesson.groups.Count)
                {
                    lesson.groups[nextIndex].isUnlocked = true;
                }
            }

            // Check if all groups completed
            lesson.isCompleted = lesson.CompletedGroupCount == lesson.groups.Count;

            Save();
        }

        public void AddXP(int amount)
        {
            Progress.totalXP += amount;
            Save();
        }

        public void ResetProgress()
        {
            Progress = new PlayerProgress();
            Save();
        }
    }
}

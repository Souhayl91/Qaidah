using UnityEngine;
using UnityEngine.SceneManagement;
using SimpleQaidah.Data;

namespace SimpleQaidah.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Lesson Data")]
        [SerializeField] private LessonData[] availableLessons;

        public LessonData CurrentLesson { get; set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public LessonData[] GetAvailableLessons() => availableLessons;

        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        public void LoadMainMenu()
        {
            LoadScene("MainMenuScene");
        }

        public void LoadLesson(LessonData lesson)
        {
            CurrentLesson = lesson;
            LoadScene("LessonScene");
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && SaveManager.Instance != null)
            {
                SaveManager.Instance.Save();
            }
        }

        private void OnApplicationQuit()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Save();
            }
        }
    }
}

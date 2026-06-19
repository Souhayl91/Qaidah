using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using SimpleQaidah.UI;

namespace SimpleQaidah.Core
{
    public class BootLoader : MonoBehaviour
    {
        [SerializeField] private float splashDuration = 1.5f;

        private IEnumerator Start()
        {
            // Fade in
            var canvas = FindAnyObjectByType<Canvas>();
            CanvasGroup cg = null;
            if (canvas != null)
            {
                cg = canvas.gameObject.GetComponent<CanvasGroup>();
                if (cg == null) cg = canvas.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                StartCoroutine(UIAnimations.FadeCanvasGroup(cg, 0f, 1f, 0.5f));
            }

            yield return new WaitForSeconds(splashDuration);

            // Fade out before transition
            if (cg != null)
            {
                yield return StartCoroutine(UIAnimations.FadeCanvasGroup(cg, 1f, 0f, 0.3f));
            }

            SceneManager.LoadScene("MainMenuScene");
        }
    }
}

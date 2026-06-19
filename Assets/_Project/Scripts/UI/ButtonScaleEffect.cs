using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

namespace SimpleQaidah.UI
{
    /// <summary>
    /// Adds tactile press-scale feedback to any button.
    /// Attach to a GameObject with a Button component.
    /// </summary>
    public class ButtonScaleEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float pressScale = 0.93f;
        [SerializeField] private float pressDuration = 0.06f;
        [SerializeField] private float releaseDuration = 0.12f;

        private Vector3 originalScale;
        private Coroutine currentAnim;
        private bool initialized;

        private void Start()
        {
            originalScale = transform.localScale;
            initialized = true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!initialized) originalScale = transform.localScale;

            if (currentAnim != null) StopCoroutine(currentAnim);
            currentAnim = StartCoroutine(AnimateScale(pressScale, pressDuration));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (currentAnim != null) StopCoroutine(currentAnim);
            currentAnim = StartCoroutine(AnimateScaleBack(releaseDuration));
        }

        private IEnumerator AnimateScale(float targetMultiplier, float duration)
        {
            Vector3 startScale = transform.localScale;
            Vector3 target = originalScale * targetMultiplier;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.Lerp(startScale, target, t);
                yield return null;
            }

            transform.localScale = target;
        }

        private IEnumerator AnimateScaleBack(float duration)
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Slight overshoot on release for bounce feel
                float eased = 1f + 2.7f * Mathf.Pow(t - 1f, 3f) + 1.7f * Mathf.Pow(t - 1f, 2f);
                transform.localScale = Vector3.LerpUnclamped(startScale, originalScale, eased);
                yield return null;
            }

            transform.localScale = originalScale;
        }
    }
}

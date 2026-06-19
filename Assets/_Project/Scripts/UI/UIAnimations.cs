using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace SimpleQaidah.UI
{
    /// <summary>
    /// Static utility providing coroutine-based UI animations.
    /// Usage: StartCoroutine(UIAnimations.ScalePop(transform, 0.8f, 1f, 0.25f));
    /// </summary>
    public static class UIAnimations
    {
        // ─── Scale Pop ──────────────────────────────────────────

        /// <summary>
        /// Scale with overshoot (ease-out-back curve).
        /// </summary>
        public static IEnumerator ScalePop(Transform target, float fromScale, float toScale, float duration)
        {
            if (target == null) yield break;

            float elapsed = 0f;
            Vector3 from = Vector3.one * fromScale;
            Vector3 to = Vector3.one * toScale;

            target.localScale = from;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutBack(t);
                target.localScale = Vector3.LerpUnclamped(from, to, eased);
                yield return null;
            }

            target.localScale = to;
        }

        // ─── Fade Canvas Group ──────────────────────────────────

        public static IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
        {
            if (group == null) yield break;

            if (from < to) group.blocksRaycasts = true;
            group.alpha = from;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                group.alpha = Mathf.Lerp(from, to, EaseOutCubic(t));
                yield return null;
            }

            group.alpha = to;
            if (to <= 0.01f) group.blocksRaycasts = false;
        }

        // ─── Slide In ───────────────────────────────────────────

        /// <summary>
        /// Slide from (originalPos + fromOffset) to originalPos.
        /// </summary>
        public static IEnumerator SlideIn(RectTransform target, Vector2 fromOffset, float duration)
        {
            if (target == null) yield break;

            Vector2 originalPos = target.anchoredPosition;
            Vector2 startPos = originalPos + fromOffset;
            target.anchoredPosition = startPos;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                target.anchoredPosition = Vector2.Lerp(startPos, originalPos, EaseOutCubic(t));
                yield return null;
            }

            target.anchoredPosition = originalPos;
        }

        // ─── Slide Out ──────────────────────────────────────────

        /// <summary>
        /// Slide from current position to (originalPos + toOffset).
        /// </summary>
        public static IEnumerator SlideOut(RectTransform target, Vector2 toOffset, float duration)
        {
            if (target == null) yield break;

            Vector2 startPos = target.anchoredPosition;
            Vector2 endPos = startPos + toOffset;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                target.anchoredPosition = Vector2.Lerp(startPos, endPos, EaseInCubic(t));
                yield return null;
            }

            target.anchoredPosition = endPos;
        }

        // ─── Shake ──────────────────────────────────────────────

        public static IEnumerator Shake(Transform target, float intensity = 15f, float duration = 0.4f)
        {
            if (target == null) yield break;

            Vector3 originalPos = target.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float decay = 1f - (elapsed / duration);
                float offsetX = Random.Range(-1f, 1f) * intensity * decay;
                float offsetY = Random.Range(-1f, 1f) * intensity * decay * 0.3f; // mostly horizontal
                target.localPosition = originalPos + new Vector3(offsetX, offsetY, 0);
                yield return null;
            }

            target.localPosition = originalPos;
        }

        // ─── Color Lerp ─────────────────────────────────────────

        public static IEnumerator ColorLerp(Image image, Color from, Color to, float duration)
        {
            if (image == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                image.color = Color.Lerp(from, to, EaseOutCubic(t));
                yield return null;
            }

            image.color = to;
        }

        // ─── Bounce ─────────────────────────────────────────────

        /// <summary>
        /// Scale from 0 → overshoot (1.2) → settle at 1.0.
        /// </summary>
        public static IEnumerator Bounce(Transform target, float duration = 0.5f)
        {
            if (target == null) yield break;

            target.localScale = Vector3.zero;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scale = EaseOutBounce(t);
                target.localScale = Vector3.one * scale;
                yield return null;
            }

            target.localScale = Vector3.one;
        }

        // ─── Count Up ───────────────────────────────────────────

        /// <summary>
        /// Animate a number counting up. Format example: "+{0} XP"
        /// </summary>
        public static IEnumerator CountUp(TMP_Text text, int from, int to, float duration, string format = "{0}")
        {
            if (text == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                int current = Mathf.RoundToInt(Mathf.Lerp(from, to, EaseOutCubic(t)));
                text.text = string.Format(format, current);
                yield return null;
            }

            text.text = string.Format(format, to);
        }

        // ─── Fade Image Alpha ───────────────────────────────────

        public static IEnumerator FadeImage(Image image, float fromAlpha, float toAlpha, float duration)
        {
            if (image == null) yield break;

            float elapsed = 0f;
            Color c = image.color;
            c.a = fromAlpha;
            image.color = c;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                c.a = Mathf.Lerp(fromAlpha, toAlpha, t);
                image.color = c;
                yield return null;
            }

            c.a = toAlpha;
            image.color = c;
        }

        // ─── Fade TMP Text Alpha ────────────────────────────────

        public static IEnumerator FadeText(TMP_Text text, float fromAlpha, float toAlpha, float duration)
        {
            if (text == null) yield break;

            float elapsed = 0f;
            Color c = text.color;
            c.a = fromAlpha;
            text.color = c;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                c.a = Mathf.Lerp(fromAlpha, toAlpha, t);
                text.color = c;
                yield return null;
            }

            c.a = toAlpha;
            text.color = c;
        }

        // ─── Easing Functions ───────────────────────────────────

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private static float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        private static float EaseInCubic(float t)
        {
            return t * t * t;
        }

        private static float EaseOutBounce(float t)
        {
            if (t < 1f / 2.75f)
            {
                return 7.5625f * t * t;
            }
            else if (t < 2f / 2.75f)
            {
                t -= 1.5f / 2.75f;
                return 7.5625f * t * t + 0.75f;
            }
            else if (t < 2.5f / 2.75f)
            {
                t -= 2.25f / 2.75f;
                return 7.5625f * t * t + 0.9375f;
            }
            else
            {
                t -= 2.625f / 2.75f;
                return 7.5625f * t * t + 0.984375f;
            }
        }
    }
}

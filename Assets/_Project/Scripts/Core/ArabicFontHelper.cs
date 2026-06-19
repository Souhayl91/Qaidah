using UnityEngine;
using TMPro;

namespace SimpleQaidah.Core
{
    /// <summary>
    /// Finds and caches the Arabic TMP font asset at runtime.
    /// Call ArabicFontHelper.Apply(tmpText) on any TMP_Text that shows Arabic characters.
    /// </summary>
    public static class ArabicFontHelper
    {
        private static TMP_FontAsset cachedFont;
        private static bool searched;

        public static TMP_FontAsset GetFont()
        {
            if (cachedFont != null) return cachedFont;
            if (searched) return null;

            searched = true;

            // Try loading from known path in editor
            #if UNITY_EDITOR
            cachedFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/NotoNaskhArabic.asset");
            if (cachedFont != null) return cachedFont;
            #endif

            // Try Resources folder
            cachedFont = Resources.Load<TMP_FontAsset>("NotoNaskhArabic");
            if (cachedFont != null) return cachedFont;

            // Last resort: search all loaded font assets
            var allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            foreach (var font in allFonts)
            {
                if (font.name.Contains("Naskh") || font.name.Contains("Arabic"))
                {
                    cachedFont = font;
                    return cachedFont;
                }
            }

            Debug.LogWarning("ArabicFontHelper: Could not find Arabic TMP font asset!");
            return null;
        }

        /// <summary>
        /// Apply the Arabic font to a TMP_Text component.
        /// </summary>
        public static void Apply(TMP_Text text)
        {
            if (text == null) return;
            var font = GetFont();
            if (font != null)
            {
                text.font = font;
            }
        }
    }
}

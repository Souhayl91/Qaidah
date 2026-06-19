#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using SimpleQaidah.UI;
using System.Reflection;

namespace SimpleQaidah.Editor
{
    public static class PrefabWiring
    {
        private static readonly string BasePath = "Assets/_Project";

        [MenuItem("Qaidah/5. Wire Prefab References")]
        public static void WirePrefabs()
        {
            WireLetterCardPrefab();
            Debug.Log("All prefab references wired!");
        }

        private static void WireLetterCardPrefab()
        {
            string prefabPath = $"{BasePath}/Prefabs/LetterCard.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError("LetterCard prefab not found! Run 'Create Prefabs' first.");
                return;
            }

            // Open prefab for editing
            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            var cardUI = prefabRoot.GetComponent<LetterCardUI>();
            if (cardUI == null)
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                return;
            }

            SetField(cardUI, "button", prefabRoot.GetComponent<Button>());
            SetField(cardUI, "background", prefabRoot.GetComponent<Image>());

            var letterText = FindChild(prefabRoot.transform, "LetterText");
            if (letterText != null) SetField(cardUI, "letterText", letterText.GetComponent<TMP_Text>());

            var lockOverlay = FindChild(prefabRoot.transform, "LockOverlay");
            if (lockOverlay != null) SetField(cardUI, "lockOverlay", lockOverlay.gameObject);

            var checkOverlay = FindChild(prefabRoot.transform, "CheckOverlay");
            if (checkOverlay != null) SetField(cardUI, "checkOverlay", checkOverlay.gameObject);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            if (target == null || value == null) return;
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
                EditorUtility.SetDirty(target as Object);
            }
        }

        private static Transform FindChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var result = FindChild(child, name);
                if (result != null) return result;
            }
            return null;
        }
    }
}
#endif

using BlockJam.UI.Common;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BlockJam.Editor
{
    /// <summary>
    /// Shared helpers for the editor tools that generate UI hierarchies.
    /// </summary>
    internal static class UiBuildUtility
    {
        internal const string UiArtFolder = "Assets/Art/UI";
        internal const string UiPrefabFolder = "Assets/Prefabs/UI";

        private static readonly Color PressedTint = new Color(0.8f, 0.8f, 0.8f, 1f);

        internal static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        internal static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        internal static void PlaceLeft(RectTransform rect, float x, Vector2 size)
        {
            Place(rect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(x, 0f), size);
        }

        internal static void PlaceCenter(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            Place(rect, anchor, anchor, new Vector2(0.5f, 0.5f), position, size);
        }

        internal static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        internal static Sprite LoadSprite(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiArtFolder}/{name}.png");
            if (sprite == null)
            {
                Debug.LogError($"[UiBuildUtility] Sprite '{name}' not found. Run 'Tools/Home Screen/1. Setup Project' first.");
            }

            return sprite;
        }

        internal static Image AddImage(RectTransform rect, string spriteName, bool sliced = false, bool raycastTarget = false)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            if (spriteName != null)
            {
                image.sprite = LoadSprite(spriteName);
            }

            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = !sliced && spriteName != null;
            image.raycastTarget = raycastTarget;
            return image;
        }

        internal static Button MakeButton(RectTransform rect, Graphic targetGraphic)
        {
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = targetGraphic;
            ColorBlock colors = button.colors;
            colors.pressedColor = PressedTint;
            button.colors = colors;
            rect.gameObject.AddComponent<PressFeedback>();
            return button;
        }

        internal static TMP_Text CreateText(string name, Transform parent, string text, float fontSize, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }

        internal static void SetReferences(Object target, params (string field, Object value)[] references)
        {
            var serialized = new SerializedObject(target);
            foreach ((string field, Object value) in references)
            {
                serialized.FindProperty(field).objectReferenceValue = value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetArray(Object target, string field, Object[] values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

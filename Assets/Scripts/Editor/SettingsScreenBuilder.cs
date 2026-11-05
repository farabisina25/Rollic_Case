using BlockJam.Core.Settings;
using BlockJam.UI.Settings;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static BlockJam.Editor.UiBuildUtility;

namespace BlockJam.Editor
{
    /// <summary>
    /// Generates the content of the settings popup. Used by the Menu scene builder and to rebuild the prefab in place.
    /// </summary>
    internal static class SettingsScreenBuilder
    {
        internal const string PrefabPath = UiPrefabFolder + "/SettingsScreen.prefab";

        private const string ToggleOnSprite = "btn_toggle_on";
        private const string ToggleOffSprite = "Btn_circle_blue_small";

        private static readonly (SettingType type, string icon)[] Toggles =
        {
            (SettingType.Sound, "Blockjam_Settings_Sound"),
            (SettingType.Music, "Blockjam_Settings_Music"),
            (SettingType.Vibration, "Blockjam_Settings_Vibration"),
        };

        [MenuItem("Tools/Home Screen/3. Rebuild Settings Popup")]
        private static void RebuildPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                Debug.LogError($"[SettingsScreenBuilder] '{PrefabPath}' not found. Run 'Tools/Home Screen/2. Build Menu Scene' first.");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Populate((RectTransform)root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[SettingsScreenBuilder] Settings popup rebuilt.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        internal static SettingsScreenView Populate(RectTransform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(root.GetChild(i).gameObject);
            }

            Stretch(root);

            RectTransform dim = CreateRect("Dim", root);
            Stretch(dim);
            Image dimImage = AddImage(dim, null, raycastTarget: true);
            dimImage.color = new Color(0f, 0f, 0f, 0.6f);
            Button dimButton = dim.gameObject.AddComponent<Button>();
            dimButton.transition = Selectable.Transition.None;

            RectTransform panel = CreateRect("Panel", root);
            PlaceCenter(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 1200f));
            AddImage(panel, "bg_popup", sliced: true, raycastTarget: true);

            RectTransform header = CreateRect("Header", panel);
            PlaceCenter(header, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(700f, 200f));
            AddImage(header, "bg_popup_header");
            TMP_Text title = CreateText("Title", header, "Settings", 80f, Color.white);
            Stretch(title.rectTransform, bottom: 15f);

            RectTransform close = CreateRect("CloseButton", panel);
            PlaceCenter(close, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(130f, 135f));
            Button closeButton = MakeButton(close, AddImage(close, "Popup_Close_Button", raycastTarget: true));

            SettingToggleView[] toggles = CreateToggleRow(panel);

            RectTransform support = CreateRect("SupportButton", panel);
            PlaceCenter(support, new Vector2(0.5f, 1f), new Vector2(0f, -680f), new Vector2(600f, 160f));
            MakeButton(support, AddImage(support, "Btn_popup_green", sliced: true, raycastTarget: true));
            RectTransform supportIcon = CreateRect("Icon", support);
            PlaceCenter(supportIcon, new Vector2(0f, 0.5f), new Vector2(110f, 5f), new Vector2(140f, 100f));
            AddImage(supportIcon, "Support_icon");
            TMP_Text supportLabel = CreateText("Label", support, "Support", 64f, Color.white);
            Stretch(supportLabel.rectTransform, left: 160f, bottom: 10f, right: 30f);

            CreateLinkButton(panel, "PrivacyButton", "Privacy", -185f);
            CreateLinkButton(panel, "TermsButton", "Terms", 185f);

            SettingsScreenView view = root.GetComponent<SettingsScreenView>();
            if (view == null)
            {
                view = root.gameObject.AddComponent<SettingsScreenView>();
            }

            SetReferences(view,
                ("closeButton", closeButton),
                ("dimButton", dimButton));
            SetArray(view, "toggles", toggles);

            return view;
        }

        private static SettingToggleView[] CreateToggleRow(RectTransform panel)
        {
            RectTransform row = CreateRect("ToggleRow", panel);
            Place(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(780f, 330f));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 30f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var toggles = new SettingToggleView[Toggles.Length];
            for (int i = 0; i < Toggles.Length; i++)
            {
                toggles[i] = CreateToggle(row, Toggles[i].type, Toggles[i].icon);
            }

            return toggles;
        }

        private static SettingToggleView CreateToggle(RectTransform parent, SettingType type, string iconSprite)
        {
            RectTransform holder = CreateRect($"Toggle_{type}", parent);
            AddImage(holder, "bg_popup_info_holder", sliced: true);

            RectTransform icon = CreateRect("Icon", holder);
            PlaceCenter(icon, new Vector2(0.5f, 1f), new Vector2(0f, -95f), new Vector2(130f, 130f));
            Image iconImage = AddImage(icon, iconSprite);

            RectTransform toggleButton = CreateRect("ToggleButton", holder);
            PlaceCenter(toggleButton, new Vector2(0.5f, 0f), new Vector2(0f, 95f), new Vector2(136f, 140f));
            Image background = AddImage(toggleButton, ToggleOnSprite, raycastTarget: true);
            Button button = MakeButton(toggleButton, background);

            TMP_Text offLabel = CreateText("OffLabel", toggleButton, "Off", 46f, Color.white);
            Stretch(offLabel.rectTransform, bottom: 8f);
            offLabel.gameObject.SetActive(false);

            SettingToggleView view = holder.gameObject.AddComponent<SettingToggleView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("type").enumValueIndex = (int)type;
            serialized.FindProperty("button").objectReferenceValue = button;
            serialized.FindProperty("background").objectReferenceValue = background;
            serialized.FindProperty("icon").objectReferenceValue = iconImage;
            serialized.FindProperty("offLabel").objectReferenceValue = offLabel;
            serialized.FindProperty("onSprite").objectReferenceValue = LoadSprite(ToggleOnSprite);
            serialized.FindProperty("offSprite").objectReferenceValue = LoadSprite(ToggleOffSprite);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        private static void CreateLinkButton(RectTransform panel, string name, string label, float x)
        {
            RectTransform rect = CreateRect(name, panel);
            PlaceCenter(rect, new Vector2(0.5f, 0f), new Vector2(x, 130f), new Vector2(340f, 130f));
            MakeButton(rect, AddImage(rect, "Btn_popup_blue", sliced: true, raycastTarget: true));
            TMP_Text text = CreateText("Label", rect, label, 52f, Color.white);
            Stretch(text.rectTransform, bottom: 8f);
        }
    }
}

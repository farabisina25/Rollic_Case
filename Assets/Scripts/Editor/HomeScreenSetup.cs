using System;
using System.Collections.Generic;
using BlockJam.Menu;
using BlockJam.UI.Common;
using BlockJam.UI.Home;
using BlockJam.UI.Settings;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static BlockJam.Editor.UiBuildUtility;

namespace BlockJam.Editor
{
    /// <summary>
    /// One-click project setup and Menu scene (Home screen) generation.
    /// Step 1 must be run once before step 2, because step 2 needs sprites and TMP resources.
    /// </summary>
    public static class HomeScreenSetup
    {
        private const string MenuScenePath = "Assets/Scenes/Menu.unity";
        private const string LegacyGameplayScenePath = "Assets/Scenes/Level1.unity";
        private const string GameplayScenePath = "Assets/Scenes/Gameplay.unity";

        private static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);
        private static readonly Color DarkText = new Color(0.42f, 0.26f, 0.16f, 1f);

        // Border order is left, bottom, right, top (pixels).
        private static readonly Dictionary<string, Vector4> SpriteBorders = new Dictionary<string, Vector4>
        {
            { "bg_home_resource_holder", new Vector4(50f, 50f, 50f, 50f) },
            { "bg_home_coin_holder", new Vector4(35f, 35f, 35f, 35f) },
            { "bg_menu", new Vector4(30f, 30f, 30f, 30f) },
            { "bg_popup", new Vector4(120f, 120f, 120f, 120f) },
            { "bg_popup_info_holder", new Vector4(50f, 50f, 50f, 50f) },
            { "Btn_popup_blue", new Vector4(60f, 60f, 60f, 60f) },
            { "Btn_popup_green", new Vector4(70f, 70f, 70f, 70f) },
            { "Btn_level_green", new Vector4(90f, 90f, 90f, 90f) },
            { "ActiveTab", new Vector4(60f, 60f, 60f, 60f) },
        };

        private static readonly (int width, int height, string label)[] GameViewSizes =
        {
            (1080, 1920, "Portrait 9:16"),
            (1080, 2400, "Portrait 20:9"),
        };

        [MenuItem("Tools/Home Screen/1. Setup Project")]
        public static void SetupProject()
        {
            ImportUiSprites();
            RenameGameplayScene();
            ConfigureBuildSettings();
            AddGameViewSizes();
            ImportTmpEssentialsIfMissing();
            Debug.Log("[HomeScreenSetup] Project setup finished.");
        }

        [MenuItem("Tools/Home Screen/2. Build Menu Scene")]
        public static void BuildMenuScene()
        {
            if (!IsTmpReady())
            {
                Debug.LogError("[HomeScreenSetup] TMP Essential Resources are missing. Run 'Tools/Home Screen/1. Setup Project' first.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            ClearSceneExceptCamera(scene);
            EnsureFolder(UiPrefabFolder);

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            RectTransform canvas = CreateCanvas();
            CreateBackground(canvas);

            RectTransform safeArea = CreateRect("SafeArea", canvas);
            Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();

            TopBarRefs topBar = CreateTopBar(safeArea);
            (Button levelButton, TMP_Text levelText) = CreateLevelButton(safeArea);
            CreateTabBar(safeArea);
            SettingsScreenView settingsScreen = CreateSettingsScreen(canvas);

            HomeScreenView homeView = safeArea.gameObject.AddComponent<HomeScreenView>();
            SetReferences(homeView,
                ("levelButton", levelButton),
                ("settingsButton", topBar.SettingsButton),
                ("levelText", levelText),
                ("coinText", topBar.CoinText),
                ("livesText", topBar.LivesText));

            MenuInstaller installer = new GameObject("MenuInstaller").AddComponent<MenuInstaller>();
            SetReferences(installer,
                ("homeView", homeView),
                ("settingsScreen", settingsScreen));

            SaveAsConnectedPrefab(topBar.Root.gameObject);
            SaveAsConnectedPrefab(settingsScreen.gameObject);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[HomeScreenSetup] Menu scene built and saved.");
        }

        #region Project setup

        private static void ImportUiSprites()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { UiArtFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);

                string spriteName = System.IO.Path.GetFileNameWithoutExtension(path);
                importer.spriteBorder = SpriteBorders.TryGetValue(spriteName, out Vector4 border) ? border : Vector4.zero;

                importer.SaveAndReimport();
            }
        }

        private static void RenameGameplayScene()
        {
            bool hasLegacy = AssetDatabase.LoadAssetAtPath<SceneAsset>(LegacyGameplayScenePath) != null;
            bool hasGameplay = AssetDatabase.LoadAssetAtPath<SceneAsset>(GameplayScenePath) != null;
            if (!hasLegacy || hasGameplay)
            {
                return;
            }

            string error = AssetDatabase.MoveAsset(LegacyGameplayScenePath, GameplayScenePath);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"[HomeScreenSetup] Could not rename gameplay scene: {error}");
            }
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuScenePath, true),
                new EditorBuildSettingsScene(GameplayScenePath, true),
            };
        }

        private static bool IsTmpReady() => AssetDatabase.FindAssets("t:TMP_Settings").Length > 0;

        private static void ImportTmpEssentialsIfMissing()
        {
            if (!IsTmpReady())
            {
                TMP_PackageResourceImporter.ImportResources(true, false, false);
            }
        }

        // GameViewSizes is internal, so custom resolutions can only be registered through reflection.
        private static void AddGameViewSizes()
        {
            var editorAssembly = typeof(UnityEditor.Editor).Assembly;
            Type sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
            Type sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
            Type sizeKindType = editorAssembly.GetType("UnityEditor.GameViewSizeType");
            if (sizesType == null || sizeType == null || sizeKindType == null)
            {
                Debug.LogWarning("[HomeScreenSetup] Game view sizes could not be added on this Unity version.");
                return;
            }

            object sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
            object fixedResolution = Enum.Parse(sizeKindType, "FixedResolution");
            var constructor = sizeType.GetConstructor(new[] { sizeKindType, typeof(int), typeof(int), typeof(string) });

            foreach (GameViewSizeGroupType groupType in new[] { GameViewSizeGroupType.Standalone, GameViewSizeGroupType.Android })
            {
                object group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { groupType });
                Type groupClass = group.GetType();

                foreach ((int width, int height, string label) in GameViewSizes)
                {
                    if (HasGameViewSize(group, groupClass, sizeType, width, height))
                    {
                        continue;
                    }

                    object size = constructor.Invoke(new[] { fixedResolution, width, height, (object)label });
                    groupClass.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                }
            }

            sizesType.GetMethod("SaveToHDD").Invoke(sizes, null);
        }

        private static bool HasGameViewSize(object group, Type groupClass, Type sizeType, int width, int height)
        {
            int count = (int)groupClass.GetMethod("GetTotalCount").Invoke(group, null);
            var getSize = groupClass.GetMethod("GetGameViewSize");
            var widthProperty = sizeType.GetProperty("width");
            var heightProperty = sizeType.GetProperty("height");

            for (int i = 0; i < count; i++)
            {
                object size = getSize.Invoke(group, new object[] { i });
                if ((int)widthProperty.GetValue(size) == width && (int)heightProperty.GetValue(size) == height)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Scene building

        private readonly struct TopBarRefs
        {
            public readonly RectTransform Root;
            public readonly Button SettingsButton;
            public readonly TMP_Text CoinText;
            public readonly TMP_Text LivesText;

            public TopBarRefs(RectTransform root, Button settingsButton, TMP_Text coinText, TMP_Text livesText)
            {
                Root = root;
                SettingsButton = settingsButton;
                CoinText = coinText;
                LivesText = livesText;
            }
        }

        private static void ClearSceneExceptCamera(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.TryGetComponent(out Camera camera))
                {
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = new Color(0.33f, 0.45f, 1f, 1f);
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static RectTransform CreateCanvas()
        {
            var go = new GameObject("Canvas_Home", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = LayerMask.NameToLayer("UI");
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            return (RectTransform)go.transform;
        }

        private static void CreateBackground(RectTransform canvas)
        {
            RectTransform background = CreateRect("Background", canvas);
            AddImage(background, "bg_home_screen");
            var fitter = background.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 1f;
        }

        private static TopBarRefs CreateTopBar(RectTransform parent)
        {
            RectTransform topBar = CreateRect("TopBar", parent);
            Place(topBar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(0f, 190f));

            RectTransform profile = CreateRect("ProfileButton", topBar);
            PlaceLeft(profile, 20f, new Vector2(190f, 190f));
            MakeButton(profile, AddImage(profile, "profile_box_blue", raycastTarget: true));

            RectTransform lives = CreateRect("LivesHolder", topBar);
            PlaceLeft(lives, 270f, new Vector2(250f, 110f));
            AddImage(lives, "bg_home_resource_holder", sliced: true);
            RectTransform heart = CreateRect("HeartIcon", lives);
            PlaceCenter(heart, new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(130f, 130f));
            AddImage(heart, "Heart");
            TMP_Text livesText = CreateText("LivesText", lives, "5", 64f, DarkText);
            Stretch(livesText.rectTransform, left: 80f);

            RectTransform coins = CreateRect("CoinHolder", topBar);
            PlaceLeft(coins, 570f, new Vector2(280f, 100f));
            AddImage(coins, "bg_home_coin_holder", sliced: true);
            RectTransform coinIcon = CreateRect("CoinIcon", coins);
            PlaceCenter(coinIcon, new Vector2(0f, 0.5f), new Vector2(5f, 0f), new Vector2(120f, 120f));
            AddImage(coinIcon, "Coin_Single");
            TMP_Text coinText = CreateText("CoinText", coins, "100", 56f, Color.white);
            Stretch(coinText.rectTransform, left: 70f, right: 50f);
            RectTransform addCoin = CreateRect("AddCoinButton", coins);
            PlaceCenter(addCoin, new Vector2(1f, 0.5f), new Vector2(-5f, 0f), new Vector2(80f, 80f));
            MakeButton(addCoin, AddImage(addCoin, "ic_plus", raycastTarget: true));

            RectTransform settings = CreateRect("SettingsButton", topBar);
            Place(settings, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(150f, 155f));
            Button settingsButton = MakeButton(settings, AddImage(settings, "Btn_settings_normal", raycastTarget: true));
            RectTransform settingsIcon = CreateRect("Icon", settings);
            Stretch(settingsIcon, 22f, 22f, 22f, 22f);
            AddImage(settingsIcon, "Settings_Icon");

            return new TopBarRefs(topBar, settingsButton, coinText, livesText);
        }

        private static (Button, TMP_Text) CreateLevelButton(RectTransform parent)
        {
            RectTransform level = CreateRect("LevelButton", parent);
            Place(level, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 300f), new Vector2(620f, 210f));
            Button button = MakeButton(level, AddImage(level, "Btn_level_green", sliced: true, raycastTarget: true));

            TMP_Text text = CreateText("LevelText", level, "Level 1", 90f, Color.white);
            Stretch(text.rectTransform, bottom: 10f);

            return (button, text);
        }

        private static void CreateTabBar(RectTransform parent)
        {
            RectTransform tabBar = CreateRect("TabBar", parent);
            Place(tabBar, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(40f, 230f));
            AddImage(tabBar, "bg_menu", sliced: true, raycastTarget: true);

            RectTransform tabsRoot = CreateRect("Tabs", tabBar);
            Stretch(tabsRoot, 30f, 10f, 30f, 10f);
            var layout = tabsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            GameObject tabPrefab = CreateTabPrefab();
            string[] icons = { "shop", "block_collection", "home_icon", "leaderboard", "lock" };
            string[] names = { "Tab_Shop", "Tab_Collection", "Tab_Home", "Tab_Leaderboard", "Tab_Locked" };
            var tabs = new Button[icons.Length];

            for (int i = 0; i < icons.Length; i++)
            {
                var tab = (GameObject)PrefabUtility.InstantiatePrefab(tabPrefab, tabsRoot);
                tab.name = names[i];
                tab.transform.Find("Icon").GetComponent<Image>().sprite = LoadSprite(icons[i]);
                tab.transform.Find("Separator").gameObject.SetActive(i < icons.Length - 1);
                tabs[i] = tab.GetComponent<Button>();
            }

            const int homeTabIndex = 2;
            RectTransform highlight = CreateRect("ActiveTabHighlight", tabs[homeTabIndex].transform);
            Stretch(highlight, 0f, -10f, 0f, -60f);
            highlight.SetAsFirstSibling();
            AddImage(highlight, "ActiveTab", sliced: true);

            TabBarView tabBarView = tabBar.gameObject.AddComponent<TabBarView>();
            SetArray(tabBarView, "tabs", tabs);
            SetReferences(tabBarView, ("activeHighlight", highlight));

            var serialized = new SerializedObject(tabBarView);
            serialized.FindProperty("defaultTabIndex").intValue = homeTabIndex;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateTabPrefab()
        {
            var root = new GameObject("Tab", typeof(RectTransform));
            root.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)root.transform;

            Image hitArea = rect.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            MakeButton(rect, hitArea);

            RectTransform icon = CreateRect("Icon", rect);
            PlaceCenter(icon, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130f, 130f));
            AddImage(icon, "home_icon");

            RectTransform separator = CreateRect("Separator", rect);
            PlaceCenter(separator, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(6f, 160f));
            AddImage(separator, "bg_home_line");

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{UiPrefabFolder}/Tab.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static SettingsScreenView CreateSettingsScreen(RectTransform canvas)
        {
            RectTransform screen = CreateRect("SettingsScreen", canvas);
            SettingsScreenView view = SettingsScreenBuilder.Populate(screen);
            screen.gameObject.SetActive(false);
            return view;
        }

        #endregion

        private static void SaveAsConnectedPrefab(GameObject instance)
        {
            PrefabUtility.SaveAsPrefabAssetAndConnect(instance, $"{UiPrefabFolder}/{instance.name}.prefab", InteractionMode.AutomatedAction);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}

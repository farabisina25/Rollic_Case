using System;
using System.Linq;
using BlockJam.Gameplay.Data;
using BlockJam.Gameplay.Logic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace BlockJam.Editor.LevelEditor
{
    public sealed class LevelEditorWindow : EditorWindow
    {
        private const float SidePanelWidth = 270f;
        private const float SideLabelWidth = 96f;
        private const float HeaderHeight = 26f;
        private const float FooterHeight = 124f;
        private const float ActionButtonWidth = 110f;
        private const float SwatchSize = 34f;
        private const float ShapeButtonSize = 52f;
        private const int ShapesPerRow = 4;
        private const int MinBoardSize = 2;
        private const int MaxBoardSize = 10;
        private const int MinTimeLimit = 10;
        private const int MaxTimeLimit = 300;
        private const int MaxCoinReward = 100;

        private static readonly string[] ToolNames = { "Block", "Door", "Erase" };

        [SerializeField] private LevelData selectedLevel;
        [SerializeField] private ToolSettings tools = new ToolSettings();

        // Kept across domain reloads (script changes, entering Play mode) so unsaved edits are not lost.
        [SerializeField] private LevelData workingCopy;
        [SerializeField] private bool workingCopyIsDirty;

        private LevelCatalogEditing _catalog;
        private ReorderableList _levelList;
        private BlockShape[] _shapes;
        private LevelEditSession _session;
        private BoardCanvas _canvas;
        private ValidationReport _report;
        private Vector2 _sideScroll;
        private Vector2 _messageScroll;

        [MenuItem("Block Jam/Level Editor")]
        public static void Open()
        {
            var window = GetWindow<LevelEditorWindow>();
            window.titleContent = new GUIContent("Level Editor");
            window.minSize = new Vector2(780f, 560f);
        }

        // Drops unsaved working copies and closes every editor instance. Used by tooling that reloads levels.
        internal static void ForceCloseAll()
        {
            foreach (LevelEditorWindow window in Resources.FindObjectsOfTypeAll<LevelEditorWindow>())
            {
                if (window.workingCopy != null)
                {
                    Undo.ClearUndo(window.workingCopy);
                    DestroyImmediate(window.workingCopy);
                    window.workingCopy = null;
                }

                window.workingCopyIsDirty = false;
                window.Close();
            }
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            saveChangesMessage = "This level has unsaved changes. Save them?";
            Undo.undoRedoPerformed += OnUndoRedo;

            _shapes = AssetDatabase.FindAssets($"t:{nameof(BlockShape)}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<BlockShape>(AssetDatabase.GUIDToAssetPath(guid)))
                .OrderBy(shape => shape.Cells.Count)
                .ThenBy(shape => shape.name)
                .ToArray();
            if (tools.shape == null && _shapes.Length > 0)
            {
                tools.shape = _shapes[0];
            }

            _catalog = LevelCatalogEditing.FindInProject();
            if (_catalog == null)
            {
                return;
            }

            BuildLevelList();
            if (selectedLevel == null && _catalog.Levels.arraySize > 0)
            {
                selectedLevel = _catalog.GetLevel(0);
            }

            if (selectedLevel != null && workingCopy != null)
            {
                StartSession(new LevelEditSession(selectedLevel, workingCopy, workingCopyIsDirty));
            }
            else
            {
                OpenSession(selectedLevel);
            }
        }

        // Runs before every domain reload and before closing. The working copy is only destroyed in OnDestroy.
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            if (_session != null)
            {
                workingCopyIsDirty = _session.IsDirty;
                _session.Changed -= OnSessionChanged;
                _session = null;
            }
        }

        private void OnDestroy()
        {
            if (workingCopy != null)
            {
                Undo.ClearUndo(workingCopy);
                DestroyImmediate(workingCopy);
            }
        }

        public override void SaveChanges()
        {
            if (TrySave())
            {
                base.SaveChanges();
            }
        }

        private void OnGUI()
        {
            if (_catalog == null)
            {
                EditorGUILayout.HelpBox("No Level Catalog asset was found. Create one with Assets > Create > Block Jam > Level Catalog.", MessageType.Error);
                return;
            }

            if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDrag)
            {
                Repaint();
            }

            var sideRect = new Rect(0f, 0f, SidePanelWidth, position.height);
            var mainRect = new Rect(SidePanelWidth, 0f, position.width - SidePanelWidth, position.height);

            GUILayout.BeginArea(sideRect, EditorStyles.helpBox);
            _sideScroll = EditorGUILayout.BeginScrollView(_sideScroll);
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = SideLabelWidth;
            DrawSidePanel();
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();

            if (_session == null || _session.Asset == null || _session.Working == null)
            {
                if (_session != null)
                {
                    CloseSession();
                    selectedLevel = null;
                }

                GUILayout.BeginArea(mainRect);
                EditorGUILayout.HelpBox("Select a level on the left, or press + to make a new one.", MessageType.Info);
                GUILayout.EndArea();
                return;
            }

            var headerRect = new Rect(mainRect.x + 8f, mainRect.y + 4f, mainRect.width - 16f, HeaderHeight);
            var canvasRect = new Rect(mainRect.x, headerRect.yMax, mainRect.width, mainRect.height - HeaderHeight - FooterHeight - 4f);
            var footerRect = new Rect(mainRect.x + 4f, canvasRect.yMax, mainRect.width - 8f, FooterHeight);

            string title = _session.IsDirty ? $"{_session.Asset.name}  (unsaved)" : _session.Asset.name;
            EditorGUI.LabelField(headerRect, title, EditorStyles.boldLabel);
            _canvas.OnGUI(canvasRect);
            DrawFooter(footerRect);
        }

        private void DrawSidePanel()
        {
            _catalog.SerializedCatalog.Update();
            _levelList.DoLayoutList();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("New Level") && ConfirmLeave())
            {
                SwitchLevel(_catalog.CreateLevel());
            }

            using (new EditorGUI.DisabledScope(_session == null))
            {
                if (GUILayout.Button("Delete Selected") && _session != null)
                {
                    DeleteLevel(_session.Asset);
                }
            }

            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(_session == null))
            {
                if (GUILayout.Button("Duplicate Selected Level") && ConfirmLeave())
                {
                    SwitchLevel(_catalog.Duplicate(_session.Asset));
                }
            }

            if (_session == null)
            {
                return;
            }

            LevelData level = _session.Working;
            Header("Board");
            int width = Stepper("Width", level.Width, MinBoardSize, MaxBoardSize);
            int height = Stepper("Height", level.Height, MinBoardSize, MaxBoardSize);
            if (width != level.Width || height != level.Height)
            {
                _session.SetSize(width, height);
            }

            int timeLimit = Stepper("Timer (seconds)", level.TimeLimitSeconds, MinTimeLimit, MaxTimeLimit, largeStep: 10);
            if (timeLimit != level.TimeLimitSeconds)
            {
                _session.SetTimeLimit(timeLimit);
            }

            int coinReward = Stepper("Coin reward", level.CoinReward, 0, MaxCoinReward);
            if (coinReward != level.CoinReward)
            {
                _session.SetCoinReward(coinReward);
            }

            Header("Tool");
            tools.tool = (EditTool)GUILayout.Toolbar((int)tools.tool, ToolNames, GUILayout.Height(24f));

            if (tools.tool != EditTool.Erase)
            {
                Header("Color");
                DrawColorPalette();
            }

            if (tools.tool == EditTool.Block)
            {
                Header("Shape");
                DrawShapePalette();
            }
            else if (tools.tool == EditTool.Door)
            {
                Header("Door");
                tools.doorLength = Stepper("Length (cells)", tools.doorLength, 1, ToolSettings.MaxDoorLength);
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(ToolHint(tools.tool), MessageType.None);
        }

        private void DrawColorPalette()
        {
            EditorGUILayout.BeginHorizontal();
            foreach (BlockColor color in Enum.GetValues(typeof(BlockColor)))
            {
                Rect rect = GUILayoutUtility.GetRect(SwatchSize, SwatchSize, GUILayout.Width(SwatchSize), GUILayout.Height(SwatchSize));
                if (GUI.Button(rect, new GUIContent(string.Empty, color.ToString())))
                {
                    tools.color = color;
                }

                EditorGUI.DrawRect(LevelEditorStyle.Shrink(rect, 4f), LevelEditorStyle.For(color));
                if (tools.color == color)
                {
                    LevelEditorStyle.DrawOutline(rect, LevelEditorStyle.Highlight);
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawShapePalette()
        {
            for (int i = 0; i < _shapes.Length; i += ShapesPerRow)
            {
                EditorGUILayout.BeginHorizontal();
                for (int j = i; j < Mathf.Min(i + ShapesPerRow, _shapes.Length); j++)
                {
                    DrawShapeButton(_shapes[j]);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawShapeButton(BlockShape shape)
        {
            Rect rect = GUILayoutUtility.GetRect(ShapeButtonSize, ShapeButtonSize, GUILayout.Width(ShapeButtonSize), GUILayout.Height(ShapeButtonSize));
            string label = shape.name.Replace("Shape_", string.Empty);
            if (GUI.Button(rect, new GUIContent(string.Empty, label)))
            {
                tools.shape = shape;
            }

            int minX = shape.Cells.Min(cell => cell.x);
            int maxY = shape.Cells.Max(cell => cell.y);
            int span = Mathf.Max(2, shape.Cells.Max(cell => cell.x) - minX + 1, maxY - shape.Cells.Min(cell => cell.y) + 1);
            float cellSize = (ShapeButtonSize - 12f) / span;
            Rect CellRect(Vector2Int cell) =>
                new Rect(rect.x + 6f + (cell.x - minX) * cellSize, rect.y + 6f + (maxY - cell.y) * cellSize, cellSize, cellSize);

            LevelEditorStyle.DrawShape(shape.Cells, CellRect, LevelEditorStyle.For(tools.color), 1f);
            if (tools.shape == shape)
            {
                LevelEditorStyle.DrawOutline(rect, LevelEditorStyle.Highlight);
            }
        }

        private void DrawFooter(Rect rect)
        {
            GUILayout.BeginArea(rect);
            EditorGUILayout.BeginHorizontal();

            _messageScroll = EditorGUILayout.BeginScrollView(_messageScroll, GUILayout.ExpandWidth(true));
            if (_report == null || _report.Messages.Count == 0)
            {
                EditorGUILayout.HelpBox("No problems found. Press Validate to check that the level can be cleared.", MessageType.Info);
            }
            else
            {
                foreach (ValidationMessage message in _report.Messages)
                {
                    EditorGUILayout.HelpBox(message.Text, ToMessageType(message.Severity));
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginVertical(GUILayout.Width(ActionButtonWidth));
            if (GUILayout.Button("Validate", GUILayout.Height(32f)))
            {
                _report = LevelValidator.Validate(_session.Working, checkSolvable: true);
            }

            if (GUILayout.Button("Save", GUILayout.Height(32f)))
            {
                TrySave();
            }

            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Play", GUILayout.Height(32f)) && (!_session.IsDirty || TrySave()))
                {
                    LevelPlaytest.Play(_session.Asset);
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void BuildLevelList()
        {
            _levelList = new ReorderableList(_catalog.SerializedCatalog, _catalog.Levels, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Levels (play order)"),
                drawElementCallback = DrawLevelElement,
                onSelectCallback = list =>
                {
                    if (!ConfirmLeave())
                    {
                        list.index = _catalog.IndexOf(selectedLevel);
                        return;
                    }

                    SwitchLevel(_catalog.GetLevel(list.index));
                },
                onAddCallback = _ =>
                {
                    if (ConfirmLeave())
                    {
                        SwitchLevel(_catalog.CreateLevel());
                    }
                },
                onRemoveCallback = list => DeleteLevel(_catalog.GetLevel(list.index)),
                onReorderCallback = _ => _catalog.SaveCatalog()
            };
            _levelList.index = _catalog.IndexOf(selectedLevel);
        }

        private void DrawLevelElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            LevelData level = _catalog.GetLevel(index);
            string label = level == null
                ? $"{index + 1}. (missing)"
                : $"{index + 1}. {level.name}    {level.Width}x{level.Height}, {level.TimeLimitSeconds}s";
            EditorGUI.LabelField(rect, label);
        }

        private void DeleteLevel(LevelData level)
        {
            if (level == null || !EditorUtility.DisplayDialog("Delete level", $"Delete {level.name}? This removes the level file.", "Delete", "Cancel"))
            {
                return;
            }

            if (level == selectedLevel)
            {
                CloseSession();
                selectedLevel = null;
            }

            int index = _catalog.IndexOf(level);
            _catalog.Delete(level);
            if (selectedLevel == null && _catalog.Levels.arraySize > 0)
            {
                SelectLevel(_catalog.GetLevel(Mathf.Clamp(index - 1, 0, _catalog.Levels.arraySize - 1)));
            }

            _levelList.index = _catalog.IndexOf(selectedLevel);
            GUIUtility.ExitGUI();
        }

        // Used from GUI callbacks: the panel layout changes with the level, so the current GUI pass is ended.
        private void SwitchLevel(LevelData level)
        {
            SelectLevel(level);
            GUIUtility.ExitGUI();
        }

        private void SelectLevel(LevelData level)
        {
            selectedLevel = level;
            _levelList.index = _catalog.IndexOf(level);
            OpenSession(level);
        }

        private void OpenSession(LevelData level)
        {
            CloseSession();
            if (level == null)
            {
                return;
            }

            StartSession(new LevelEditSession(level));
        }

        private void StartSession(LevelEditSession session)
        {
            _session = session;
            workingCopy = session.Working;
            _session.Changed += OnSessionChanged;
            _canvas = new BoardCanvas(_session, tools);
            OnSessionChanged();
        }

        private void CloseSession()
        {
            if (_session == null)
            {
                return;
            }

            _session.Changed -= OnSessionChanged;
            _session.Dispose();
            _session = null;
            workingCopy = null;
            _canvas = null;
            _report = null;
            hasUnsavedChanges = false;
        }

        private void OnSessionChanged()
        {
            _report = LevelValidator.Validate(_session.Working, checkSolvable: false);
            hasUnsavedChanges = _session.IsDirty;
            Repaint();
        }

        private void OnUndoRedo() => _session?.OnUndoRedo();

        // Returns true when it is fine to switch away from the current level.
        private bool ConfirmLeave()
        {
            if (_session == null || !_session.IsDirty)
            {
                return true;
            }

            int choice = EditorUtility.DisplayDialogComplex("Unsaved changes", $"Save changes to {_session.Asset.name}?", "Save", "Cancel", "Discard");
            return choice == 2 || (choice == 0 && TrySave());
        }

        private bool TrySave()
        {
            _report = LevelValidator.Validate(_session.Working, checkSolvable: true);
            if (_report.HasErrors)
            {
                EditorUtility.DisplayDialog("Cannot save", "Fix the errors listed under the board first.", "OK");
                return false;
            }

            if (_report.Solvability != SolveResult.Solvable &&
                !EditorUtility.DisplayDialog("Save level", "The checker could not find a way to clear this level. Save it anyway?", "Save anyway", "Cancel"))
            {
                return false;
            }

            _session.Save();
            return true;
        }

        private static void Header(string text)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
        }

        // Exact integers: the slider thumb is too easy to overshoot, so designers step with buttons.
        private static int Stepper(string label, int value, int min, int max, int largeStep = 0)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            if (largeStep > 0)
            {
                StepButton($"-{largeStep}", ref value, -largeStep, min, max);
            }

            StepButton("−", ref value, -1, min, max);
            GUILayout.Label(value.ToString(), EditorStyles.helpBox, GUILayout.Width(36f), GUILayout.Height(20f));
            StepButton("+", ref value, 1, min, max);
            if (largeStep > 0)
            {
                StepButton($"+{largeStep}", ref value, largeStep, min, max);
            }

            EditorGUILayout.EndHorizontal();
            return Mathf.Clamp(value, min, max);
        }

        private static void StepButton(string caption, ref int value, int delta, int min, int max)
        {
            int next = Mathf.Clamp(value + delta, min, max);
            using (new EditorGUI.DisabledScope(next == value))
            {
                if (GUILayout.Button(caption, GUILayout.Width(caption.Length > 1 ? 36f : 24f), GUILayout.Height(20f)))
                {
                    value = next;
                }
            }
        }

        private static string ToolHint(EditTool tool)
        {
            switch (tool)
            {
                case EditTool.Block:
                    return "Click an empty cell to place the shape.\nDrag a block to move it.\nRight click a block or door to remove it.";
                case EditTool.Door:
                    return "Click a grey wall slot around the board to place a door. It replaces the doors under it.\nRight click a door to remove it.";
                default:
                    return "Click a block or door to remove it.";
            }
        }

        private static MessageType ToMessageType(ValidationSeverity severity)
        {
            switch (severity)
            {
                case ValidationSeverity.Error: return MessageType.Error;
                case ValidationSeverity.Warning: return MessageType.Warning;
                default: return MessageType.Info;
            }
        }
    }
}

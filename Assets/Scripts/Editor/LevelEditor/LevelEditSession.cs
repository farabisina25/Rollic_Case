using System;
using BlockJam.Gameplay.Data;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BlockJam.Editor.LevelEditor
{
    // Edits a copy of a level so the asset only changes on Save. All writes go through SerializedObject,
    // which gives Undo/Redo for free. The copy is a hidden object, so it outlives domain reloads.
    internal sealed class LevelEditSession : IDisposable
    {
        private readonly SerializedObject _serialized;
        private readonly SerializedProperty _width;
        private readonly SerializedProperty _height;
        private readonly SerializedProperty _timeLimit;
        private readonly SerializedProperty _coinReward;
        private readonly SerializedProperty _blocks;
        private readonly SerializedProperty _doors;

        public LevelEditSession(LevelData asset) : this(asset, CreateWorkingCopy(asset), false)
        {
        }

        // Resumes editing a working copy that survived a domain reload.
        public LevelEditSession(LevelData asset, LevelData working, bool isDirty)
        {
            Asset = asset;
            Working = working;
            IsDirty = isDirty;

            _serialized = new SerializedObject(Working);
            _width = _serialized.FindProperty("width");
            _height = _serialized.FindProperty("height");
            _timeLimit = _serialized.FindProperty("timeLimitSeconds");
            _coinReward = _serialized.FindProperty("coinReward");
            _blocks = _serialized.FindProperty("blocks");
            _doors = _serialized.FindProperty("doors");
        }

        public event Action Changed;

        public LevelData Asset { get; }
        public LevelData Working { get; }
        public bool IsDirty { get; private set; }

        public void Dispose()
        {
            Undo.ClearUndo(Working);
            Object.DestroyImmediate(Working);
        }

        public void OnUndoRedo()
        {
            _serialized.Update();
            IsDirty = true;
            Changed?.Invoke();
        }

        public void Save()
        {
            EditorUtility.CopySerialized(Working, Asset);
            Asset.hideFlags = HideFlags.None;
            EditorUtility.SetDirty(Asset);
            AssetDatabase.SaveAssetIfDirty(Asset);
            IsDirty = false;
            Changed?.Invoke();
        }

        public void SetSize(int width, int height)
        {
            _width.intValue = width;
            _height.intValue = height;
            Apply("Resize Board");
        }

        public void SetTimeLimit(int seconds)
        {
            _timeLimit.intValue = seconds;
            Apply("Change Time Limit");
        }

        public void SetCoinReward(int coins)
        {
            _coinReward.intValue = coins;
            Apply("Change Coin Reward");
        }

        public void AddBlock(BlockShape shape, BlockColor color, Vector2Int origin)
        {
            int index = _blocks.arraySize;
            _blocks.InsertArrayElementAtIndex(index);
            SerializedProperty block = _blocks.GetArrayElementAtIndex(index);
            block.FindPropertyRelative(nameof(BlockPlacement.shape)).objectReferenceValue = shape;
            block.FindPropertyRelative(nameof(BlockPlacement.color)).intValue = (int)color;
            block.FindPropertyRelative(nameof(BlockPlacement.origin)).vector2IntValue = origin;
            Apply("Add Block");
        }

        public void MoveBlock(int index, Vector2Int origin)
        {
            _blocks.GetArrayElementAtIndex(index).FindPropertyRelative(nameof(BlockPlacement.origin)).vector2IntValue = origin;
            Apply("Move Block");
        }

        public void RemoveBlock(int index)
        {
            _blocks.DeleteArrayElementAtIndex(index);
            Apply("Remove Block");
        }

        // A new door replaces the doors it overlaps on the same side.
        public void AddDoor(BoardSide side, int start, int length, BlockColor color)
        {
            for (int i = _doors.arraySize - 1; i >= 0; i--)
            {
                DoorPlacement door = Working.Doors[i];
                if (door.side == side && start < door.start + door.length && door.start < start + length)
                {
                    _doors.DeleteArrayElementAtIndex(i);
                }
            }

            int index = _doors.arraySize;
            _doors.InsertArrayElementAtIndex(index);
            SerializedProperty added = _doors.GetArrayElementAtIndex(index);
            added.FindPropertyRelative(nameof(DoorPlacement.side)).intValue = (int)side;
            added.FindPropertyRelative(nameof(DoorPlacement.start)).intValue = start;
            added.FindPropertyRelative(nameof(DoorPlacement.length)).intValue = length;
            added.FindPropertyRelative(nameof(DoorPlacement.color)).intValue = (int)color;
            Apply("Add Door");
        }

        public void RemoveDoor(int index)
        {
            _doors.DeleteArrayElementAtIndex(index);
            Apply("Remove Door");
        }

        public int FindBlockAt(Vector2Int cell)
        {
            for (int i = 0; i < Working.Blocks.Count; i++)
            {
                BlockPlacement block = Working.Blocks[i];
                if (block.shape == null)
                {
                    continue;
                }

                foreach (Vector2Int offset in block.shape.Cells)
                {
                    if (block.origin + offset == cell)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public int FindDoorAt(BoardSide side, int lane)
        {
            for (int i = 0; i < Working.Doors.Count; i++)
            {
                if (Working.Doors[i].Covers(side, lane))
                {
                    return i;
                }
            }

            return -1;
        }

        // True when every cell of the shape is on the board and free. The ignored block may be in the way (it is the one moving).
        public bool CanPlace(BlockShape shape, Vector2Int origin, int ignoredBlock = -1)
        {
            foreach (Vector2Int offset in shape.Cells)
            {
                Vector2Int cell = origin + offset;
                if (cell.x < 0 || cell.y < 0 || cell.x >= Working.Width || cell.y >= Working.Height)
                {
                    return false;
                }

                int occupant = FindBlockAt(cell);
                if (occupant != -1 && occupant != ignoredBlock)
                {
                    return false;
                }
            }

            return true;
        }

        private static LevelData CreateWorkingCopy(LevelData asset)
        {
            LevelData working = Object.Instantiate(asset);
            working.name = asset.name;
            working.hideFlags = HideFlags.HideAndDontSave;
            return working;
        }

        private void Apply(string undoName)
        {
            _serialized.ApplyModifiedProperties();
            Undo.SetCurrentGroupName(undoName);
            IsDirty = true;
            Changed?.Invoke();
        }
    }
}

using BlockJam.Gameplay.Data;
using UnityEditor;
using UnityEngine;

namespace BlockJam.Editor.LevelEditor
{
    // Creates, copies, deletes and orders the level assets listed in the catalog.
    internal sealed class LevelCatalogEditing
    {
        private const string DataFolder = "Assets/Data";
        private const string LevelFolderName = "Levels";
        private const string LevelFolder = DataFolder + "/" + LevelFolderName;

        private readonly SerializedObject _serialized;

        private LevelCatalogEditing(LevelCatalog catalog)
        {
            Catalog = catalog;
            _serialized = new SerializedObject(catalog);
            Levels = _serialized.FindProperty("levels");
        }

        public LevelCatalog Catalog { get; }
        public SerializedProperty Levels { get; }
        public SerializedObject SerializedCatalog => _serialized;

        public static LevelCatalogEditing FindInProject()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(LevelCatalog)}");
            if (guids.Length == 0)
            {
                return null;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]));
            return new LevelCatalogEditing(catalog);
        }

        public LevelData GetLevel(int index) => (LevelData)Levels.GetArrayElementAtIndex(index).objectReferenceValue;

        public int IndexOf(LevelData level)
        {
            for (int i = 0; i < Levels.arraySize; i++)
            {
                if (GetLevel(i) == level)
                {
                    return i;
                }
            }

            return -1;
        }

        public LevelData CreateLevel()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            AssetDatabase.CreateAsset(level, NextLevelPath());
            Insert(level, Levels.arraySize);
            return level;
        }

        public LevelData Duplicate(LevelData source)
        {
            string path = NextLevelPath();
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), path);
            var copy = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            Insert(copy, IndexOf(source) + 1);
            return copy;
        }

        public void Delete(LevelData level)
        {
            int index = IndexOf(level);
            if (index >= 0)
            {
                // Object-reference arrays only drop the slot after the value is cleared.
                Levels.GetArrayElementAtIndex(index).objectReferenceValue = null;
                Levels.DeleteArrayElementAtIndex(index);
                SaveCatalog();
            }

            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(level));
        }

        public void SaveCatalog()
        {
            _serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(Catalog);
            AssetDatabase.SaveAssetIfDirty(Catalog);
        }

        private void Insert(LevelData level, int index)
        {
            Levels.InsertArrayElementAtIndex(index);
            Levels.GetArrayElementAtIndex(index).objectReferenceValue = level;
            SaveCatalog();
        }

        private string NextLevelPath()
        {
            if (!AssetDatabase.IsValidFolder(LevelFolder))
            {
                AssetDatabase.CreateFolder(DataFolder, LevelFolderName);
            }

            return AssetDatabase.GenerateUniqueAssetPath($"{LevelFolder}/Level_{Levels.arraySize + 1:00}.asset");
        }
    }
}

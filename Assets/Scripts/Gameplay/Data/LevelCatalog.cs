using UnityEngine;

namespace BlockJam.Gameplay.Data
{
    [CreateAssetMenu(menuName = "Block Jam/Level Catalog", fileName = "LevelCatalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        [SerializeField] private LevelData[] levels;

        // Level numbers start at 1 and loop back to the first level after the last one.
        public LevelData GetLevel(int levelNumber) => levels[(levelNumber - 1) % levels.Length];
    }
}

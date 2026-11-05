using System;
using BlockJam.Gameplay.Data;
using UnityEngine;

namespace BlockJam.Gameplay.View
{
    [CreateAssetMenu(menuName = "Block Jam/Board Theme", fileName = "BoardTheme")]
    public sealed class BoardTheme : ScriptableObject
    {
        [Serializable]
        private struct ColorPrefabs
        {
            public BlockColor color;
            public GameObject blockCell;
            public GameObject door;
        }

        [SerializeField] private GameObject floorTile;
        [Tooltip("One cell long, pivot on the edge center, facing +Z (outside of the board).")]
        [SerializeField] private GameObject wallSegment;
        [SerializeField] private ColorPrefabs[] colors;

        public GameObject FloorTile => floorTile;
        public GameObject WallSegment => wallSegment;

        public GameObject GetBlockCell(BlockColor color) => Find(color).blockCell;
        public GameObject GetDoor(BlockColor color) => Find(color).door;

        private ColorPrefabs Find(BlockColor color)
        {
            foreach (ColorPrefabs entry in colors)
            {
                if (entry.color == color)
                {
                    return entry;
                }
            }

            throw new ArgumentException($"{name} has no prefabs for {color}.", nameof(color));
        }
    }
}

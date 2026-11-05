using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockJam.Gameplay.Data
{
    [CreateAssetMenu(menuName = "Block Jam/Level", fileName = "Level_")]
    public sealed class LevelData : ScriptableObject
    {
        [SerializeField, Min(2)] private int width = 6;
        [SerializeField, Min(2)] private int height = 6;
        [SerializeField, Min(5)] private int timeLimitSeconds = 60;
        [SerializeField, Min(0)] private int coinReward = 10;
        [SerializeField] private BlockPlacement[] blocks = Array.Empty<BlockPlacement>();
        [SerializeField] private DoorPlacement[] doors = Array.Empty<DoorPlacement>();

        public int Width => width;
        public int Height => height;
        public int TimeLimitSeconds => timeLimitSeconds;
        public int CoinReward => coinReward;
        public IReadOnlyList<BlockPlacement> Blocks => blocks;
        public IReadOnlyList<DoorPlacement> Doors => doors;
    }
}

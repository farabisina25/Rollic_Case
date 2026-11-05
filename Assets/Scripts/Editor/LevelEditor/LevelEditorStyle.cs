using System;
using System.Collections.Generic;
using BlockJam.Gameplay.Data;
using UnityEditor;
using UnityEngine;

namespace BlockJam.Editor.LevelEditor
{
    internal static class LevelEditorStyle
    {
        public static readonly Color Background = new Color(0.16f, 0.16f, 0.18f);
        public static readonly Color Floor = new Color(0.82f, 0.77f, 0.68f);
        public static readonly Color Wall = new Color(0.42f, 0.42f, 0.46f);
        public static readonly Color Valid = new Color(0.3f, 0.95f, 0.4f);
        public static readonly Color Invalid = new Color(1f, 0.25f, 0.2f);
        public static readonly Color Highlight = Color.white;

        private static readonly Vector2Int[] JoinDirections = { Vector2Int.right, Vector2Int.up };

        public static Color For(BlockColor color)
        {
            switch (color)
            {
                case BlockColor.Blue: return new Color(0.22f, 0.45f, 0.95f);
                case BlockColor.Green: return new Color(0.3f, 0.75f, 0.3f);
                case BlockColor.Purple: return new Color(0.62f, 0.36f, 0.88f);
                case BlockColor.Red: return new Color(0.92f, 0.26f, 0.24f);
                default: return Color.gray;
            }
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        public static void DrawOutline(Rect rect, Color color, float thickness = 2f)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        // Draws the cells of a shape as one joined piece. cellRect maps a cell offset to its rect on screen.
        public static void DrawShape(IReadOnlyList<Vector2Int> cells, Func<Vector2Int, Rect> cellRect, Color color, float inset)
        {
            foreach (Vector2Int cell in cells)
            {
                Rect rect = cellRect(cell);
                EditorGUI.DrawRect(Shrink(rect, inset), color);

                foreach (Vector2Int direction in JoinDirections)
                {
                    Vector2Int neighbour = cell + direction;
                    if (!Contains(cells, neighbour))
                    {
                        continue;
                    }

                    Rect other = cellRect(neighbour);
                    Rect joined = Rect.MinMaxRect(
                        Mathf.Min(rect.xMin, other.xMin), Mathf.Min(rect.yMin, other.yMin),
                        Mathf.Max(rect.xMax, other.xMax), Mathf.Max(rect.yMax, other.yMax));
                    EditorGUI.DrawRect(Shrink(joined, inset), color);
                }
            }
        }

        public static Rect Shrink(Rect rect, float amount) =>
            new Rect(rect.x + amount, rect.y + amount, rect.width - amount * 2f, rect.height - amount * 2f);

        private static bool Contains(IReadOnlyList<Vector2Int> cells, Vector2Int cell)
        {
            foreach (Vector2Int candidate in cells)
            {
                if (candidate == cell)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>
    /// Decorations of a map (docs/BanDo/BatQuaiTran.md §5): camp tents, watchtowers, fences, flags… Only pictures –
    /// they never block movement (forbidden zones do that). Stages reference it from <see cref="StageConfig"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Arena3Q/Map Decorations", fileName = "MapDecorations")]
    public sealed class MapDecorConfig : ScriptableObject
    {
        [Serializable]
        public sealed class Piece
        {
            [Tooltip("Picture with its pivot on the ground (a rendered prop sprite).")]
            public Sprite sprite;
            [Tooltip("Logic position of the pivot.")]
            public Vector2 position;
            [Tooltip("3D: turn the model's front (its door, its drum) towards the other side of the map: pieces left of the " +
                     "centre face right (the enemy), pieces right of it face left. Off = the yaw its picture was rendered at.")]
            public bool faceOpponent;
        }

        [SerializeField] private Piece[] pieces = Array.Empty<Piece>();

        public IReadOnlyList<Piece> Pieces => pieces;
    }
}

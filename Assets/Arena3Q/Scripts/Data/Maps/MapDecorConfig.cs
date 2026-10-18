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
        }

        [SerializeField] private Piece[] pieces = Array.Empty<Piece>();

        public IReadOnlyList<Piece> Pieces => pieces;
    }
}

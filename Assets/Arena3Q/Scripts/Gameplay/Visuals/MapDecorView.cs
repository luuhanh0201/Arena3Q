using System.Collections.Generic;
using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// The map's decorations as standing sprites, Y-sorted like units (lower on screen = drawn in front). Pictures
    /// only: they are not forbidden zones (docs/BanDo/BatQuaiTran.md §5).
    /// </summary>
    internal sealed class MapDecorView : MonoBehaviour
    {
        private readonly List<(SpriteRenderer renderer, Vector2 logic)> _pieces = new List<(SpriteRenderer, Vector2)>();
        private BattleController _battle;

        public void Init(BattleController battle, MapDecorConfig decorations)
        {
            _battle = battle;
            var index = 0;
            foreach (var piece in decorations.Pieces)
            {
                if (piece?.sprite == null)
                    continue;
                var renderer = PlaceholderArt.CreateRenderer($"Decor {index++} {piece.sprite.name}", transform,
                    piece.sprite, Color.white, Vector2.one);
                _pieces.Add((renderer, piece.position));
                // 3D: the prop's model stands there instead of its picture.
                if (MapProp3D.TryFind(battle, piece.sprite, out var prop, out var yaw))
                {
                    if (piece.faceOpponent)
                        yaw = MapProp3D.YawFacing(prop, MapProp3D.TowardOpponent(battle, piece.position));
                    var model = MapProp3D.Create(renderer.name + " 3D", transform, prop);
                    model.SetPositionAndRotation(battle.ToView(piece.position), MapProp3D.Rotation(yaw));
                    var shadow = MapProp3D.CreateShadow(transform, prop, battle.View.PropShadowAlpha);
                    shadow.transform.position = battle.ToView(piece.position);
                    renderer.enabled = false;
                }
            }
            _battle.ViewChanged += Reposition;
            Reposition();
        }

        private void OnDestroy()
        {
            if (_battle != null)
                _battle.ViewChanged -= Reposition;
        }

        private void Reposition()
        {
            foreach (var (renderer, logic) in _pieces)
            {
                var view = _battle.ToView(logic);
                renderer.transform.position = view;
                renderer.transform.rotation = _battle.UprightRotation;
                renderer.sortingOrder = Mathf.RoundToInt(-view.y * 20f) * 4;
            }
        }
    }
}

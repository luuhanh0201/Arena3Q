using System.Collections.Generic;
using Arena3Q.Core;
using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// Pictures of the Bát Quái Trận (docs/BanDo/BatQuaiTran.md §6): pillar and slab sprites standing on the field
    /// (Y-sorted like units), slabs rising out of the ground through a mask, rune discs coloured by gate type that
    /// flash before a rotation, and the altar with a glow in the holder's colour and a capture bar. Falls back to flat
    /// placeholder shapes when the map has no sprites. Scene-view gizmos show the logic shapes.
    /// </summary>
    internal sealed class BaguaFormationView : MonoBehaviour
    {
        // Flat pieces sit between the ground art (-32000) and the summon overlays (-31980); the altar sprite stays
        // under every unit so units standing on it are drawn on top.
        private const int RuneOrder = -31985;
        private const int AltarOrder = -31000;
        private const float RuneDiameter = 1.6f;

        private Color PlayerColor { get { var c = _battle.View.PlayerColor; c.a = .85f; return c; } }
        private Color EnemyColor { get { var c = _battle.View.EnemyColor; c.a = .85f; return c; } }
        private static readonly Color NeutralColor = new Color(1f, 1f, 1f, 0.45f);
        private static readonly Color StoneColor = new Color(0.55f, 0.53f, 0.5f);

        private readonly List<(SpriteRenderer renderer, Vector2 logic)> _standing = new List<(SpriteRenderer, Vector2)>();
        private BattleController _battle;
        private BaguaFormationConfig _config;
        private BaguaFormation _formation;
        private SpriteRenderer[] _runes;
        private SpriteRenderer[] _slabs;
        private SpriteMask[] _slabMasks;
        private SpriteRenderer[] _slabPlaceholders;
        private SpriteRenderer _altarRing;
        private Transform _progressRoot;
        private SpriteRenderer _progressFill;

        public void Init(BattleController battle, BaguaFormationConfig config, BaguaFormation formation, Transform groundRoot)
        {
            _battle = battle;
            _config = config;
            _formation = formation;
            var geometry = formation.Geometry;

            BuildAltar(geometry, groundRoot);
            BuildPillars(geometry, groundRoot);
            BuildGates(geometry, groundRoot);

            _battle.ViewChanged += Reposition;
            Reposition();
            Refresh();
        }

        private void OnDestroy()
        {
            if (_battle != null)
                _battle.ViewChanged -= Reposition;
        }

        private void BuildAltar(BaguaFormationGeometry geometry, Transform groundRoot)
        {
            var diameter = geometry.AltarRadius * 2f;
            if (_config.AltarSprite != null)
                AddStanding("Altar", _config.AltarSprite, geometry.AltarCenter, AltarOrder);
            else
                AddGround("Altar", groundRoot, PlaceholderArt.Circle, StoneColor, geometry.AltarCenter,
                    new Vector2(diameter, diameter), RuneOrder - 1);

            _altarRing = AddGround("AltarGlow", groundRoot, PlaceholderArt.Ring, NeutralColor, geometry.AltarCenter,
                new Vector2(diameter + 0.5f, diameter + 0.5f), RuneOrder);

            // Capture bar floating above the altar (view space).
            _progressRoot = new GameObject("CaptureBar").transform;
            _progressRoot.SetParent(transform, false);
            PlaceholderArt.CreateRenderer("Back", _progressRoot, PlaceholderArt.Square, new Color(0f, 0f, 0f, 0.6f),
                new Vector2(2.2f, 0.22f), 30000);
            _progressFill = PlaceholderArt.CreateRenderer("Fill", _progressRoot, PlaceholderArt.Square, PlayerColor,
                new Vector2(2.1f, 0.14f), 30001);
        }

        private void BuildPillars(BaguaFormationGeometry geometry, Transform groundRoot)
        {
            var sprites = _config.PillarSprites;
            for (var i = 0; i < geometry.Pillars.Count; i++)
            {
                var (position, radius) = geometry.Pillars[i];
                // "Xoay Y ngẫu nhiên": a seeded pick among the rendered yaws, the same every battle.
                var sprite = sprites.Count > 0 ? sprites[new System.Random(7919 * (i + 1)).Next(sprites.Count)] : null;
                if (sprite != null)
                    AddStanding($"Pillar {i}", sprite, position, 0);
                else
                    AddGround($"Pillar {i}", groundRoot, PlaceholderArt.Circle, StoneColor, position,
                        new Vector2(radius * 2f, radius * 2f), RuneOrder + 2);
            }
        }

        private void BuildGates(BaguaFormationGeometry geometry, Transform groundRoot)
        {
            var count = geometry.Gates.Count;
            _runes = new SpriteRenderer[count];
            _slabs = new SpriteRenderer[count];
            _slabMasks = new SpriteMask[count];
            _slabPlaceholders = new SpriteRenderer[count];
            for (var k = 0; k < count; k++)
            {
                var gate = geometry.Gates[k];
                _runes[k] = AddGround($"Rune {k}", groundRoot, PlaceholderArt.Circle, Color.white, gate.Position,
                    new Vector2(RuneDiameter, RuneDiameter), RuneOrder);

                var rotY = k < _config.Gates.Count ? _config.Gates[k].rotY : 0f;
                var sprite = _config.SlabSpriteFor(rotY);
                if (sprite != null)
                {
                    _slabs[k] = AddStanding($"Slab {k}", sprite, gate.Position, 0);
                    _slabs[k].maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                    _slabMasks[k] = new GameObject($"Slab {k} Mask").AddComponent<SpriteMask>();
                    _slabMasks[k].transform.SetParent(transform, false);
                    _slabMasks[k].sprite = PlaceholderArt.Square;
                    _slabMasks[k].isCustomRangeActive = true;
                }
                else
                {
                    var across = gate.Across;
                    _slabPlaceholders[k] = AddGround($"Slab {k}", groundRoot, PlaceholderArt.Square, StoneColor,
                        gate.Position, new Vector2(geometry.SlabLength, geometry.SlabThickness), RuneOrder + 1);
                    _slabPlaceholders[k].transform.localRotation =
                        Quaternion.Euler(0f, 0f, Mathf.Atan2(across.y, across.x) * Mathf.Rad2Deg);
                }
            }
        }

        /// <summary>Colours, slab heights and the altar state; called every battle step.</summary>
        public void Refresh()
        {
            if (_formation == null)
                return;
            var flash = _formation.IsWarning ? 0.5f + 0.5f * Mathf.Sin(_battle.BattleTime * 12f) : 1f;
            for (var k = 0; k < _formation.GateCount; k++)
            {
                var color = _config.RuneColor(_formation.IsGateClosed(k) ? BaguaGateKind.Closed : _formation.KindOf(k));
                color.a = Mathf.Lerp(0.25f, 0.7f, flash);
                _runes[k].color = color;

                var raise = _formation.SlabRaise(k);
                if (_slabs[k] != null)
                    PlaceSlab(k, raise);
                if (_slabPlaceholders[k] != null)
                {
                    _slabPlaceholders[k].enabled = raise > 0f;
                    _slabPlaceholders[k].color = new Color(StoneColor.r, StoneColor.g, StoneColor.b, raise);
                }
            }

            _altarRing.color = _formation.AltarOwner switch
            {
                Team.Player => PlayerColor,
                Team.Enemy => EnemyColor,
                _ => NeutralColor
            };
            var capturing = _formation.CapturingTeam != null;
            _progressRoot.gameObject.SetActive(capturing);
            if (capturing)
            {
                var fraction = Mathf.Clamp01(_formation.CaptureProgress);
                _progressFill.color = _formation.CapturingTeam == Team.Player ? PlayerColor : EnemyColor;
                _progressFill.transform.localScale = new Vector3(2.1f * fraction, 0.14f, 1f);
                _progressFill.transform.localPosition = new Vector3(-1.05f * (1f - fraction), 0f, 0f);
            }
        }

        private void PlaceSlab(int gate, float raise)
        {
            var slab = _slabs[gate];
            if (PlaceSlabModel(slab, raise))
            {
                _slabMasks[gate].enabled = false;
                return;
            }
            var logic = _formation.Geometry.Gates[gate].Position;
            var basePosition = _battle.ToView(logic);
            // Heights show at sin(view angle) on screen; seen straight from above the slab only fades.
            var heightScale = Mathf.Sqrt(Mathf.Max(0f, 1f - _battle.GroundProjection * _battle.GroundProjection));
            var sink = (1f - raise) * _config.SlabSinkDepth * heightScale;
            slab.enabled = raise > 0f;
            slab.transform.position = basePosition - _battle.ScreenUp * sink;
            slab.transform.rotation = _battle.UprightRotation;
            slab.color = heightScale < 0.05f ? new Color(1f, 1f, 1f, raise) : Color.white;

            // The mask is the slab's own picture at full height: what has not risen above its base yet stays hidden.
            var mask = _slabMasks[gate];
            var bounds = slab.sprite.bounds;
            mask.transform.position = basePosition + _battle.UprightRotation * bounds.center;
            mask.transform.rotation = _battle.UprightRotation;
            mask.transform.localScale = new Vector3(bounds.size.x, bounds.size.y, 1f);
            mask.frontSortingOrder = slab.sortingOrder;
            mask.backSortingOrder = slab.sortingOrder - 1;
        }

        private void Reposition()
        {
            foreach (var (renderer, logic) in _standing)
            {
                var view = _battle.ToView(logic);
                renderer.transform.position = view;
                renderer.transform.rotation = _battle.UprightRotation;
                if (renderer.sortingOrder != AltarOrder)
                    renderer.sortingOrder = Mathf.RoundToInt(-view.y * 20f) * 4;
            }
            if (_progressRoot != null)
            {
                var geometry = _formation.Geometry;
                _progressRoot.position = _battle.ToView(geometry.AltarCenter + new Vector2(0f, geometry.AltarRadius))
                                         + _battle.ScreenUp * 0.9f;
                _progressRoot.rotation = _battle.UprightRotation;
            }
            Refresh();
        }

        private SpriteRenderer AddStanding(string objectName, Sprite sprite, Vector2 logic, int order)
        {
            var renderer = PlaceholderArt.CreateRenderer(objectName, transform, sprite, Color.white, Vector2.one, order);
            _standing.Add((renderer, logic));
            // 3D: the prop's model stands there instead; the picture stays (disabled) as the data reference.
            if (MapProp3D.TryFind(_battle, sprite, out var prop, out var yaw))
            {
                var model = MapProp3D.Create(objectName + " 3D", transform, prop);
                model.SetPositionAndRotation(_battle.ToView(logic), MapProp3D.Rotation(yaw));
                var shadow = MapProp3D.CreateShadow(transform, prop, _battle.View.PropShadowAlpha);
                shadow.transform.position = _battle.ToView(logic);
                _models[renderer] = (model, shadow, prop.scale);
                renderer.enabled = false;
            }
            return renderer;
        }

        private readonly Dictionary<SpriteRenderer, (Transform model, SpriteRenderer shadow, float scale)> _models =
            new Dictionary<SpriteRenderer, (Transform, SpriteRenderer, float)>();

        // A 3D slab grows out of the ground (its height scales with the raise) and is gone when fully down.
        private bool PlaceSlabModel(SpriteRenderer slab, float raise)
        {
            if (!_models.TryGetValue(slab, out var model))
                return false;
            slab.enabled = false;
            var up = Mathf.SmoothStep(0f, 1f, raise);
            model.model.gameObject.SetActive(raise > 0f);
            model.model.localScale = new Vector3(model.scale, model.scale * Mathf.Max(0.001f, up), model.scale);
            model.shadow.enabled = raise > 0f;
            model.shadow.color = new Color(0f, 0f, 0f, _battle.View.PropShadowAlpha * up);
            return true;
        }

        private static SpriteRenderer AddGround(string objectName, Transform groundRoot, Sprite sprite, Color color,
            Vector2 logic, Vector2 size, int order)
        {
            var renderer = PlaceholderArt.CreateRenderer(objectName, groundRoot, sprite, color, size, order);
            renderer.transform.localPosition = logic;
            return renderer;
        }

        // ---- Scene-view gizmos (§7): pillars, gaps, triggers coloured by type, slabs, altar ----

        private void OnDrawGizmos()
        {
            if (_formation == null || _battle == null)
                return;
            var geometry = _formation.Geometry;
            Gizmos.color = Color.gray;
            foreach (var (position, radius) in geometry.Pillars)
                DrawCircle(position, radius);

            for (var k = 0; k < _formation.GateCount; k++)
            {
                var gate = geometry.Gates[k];
                var color = _config.RuneColor(_formation.IsGateClosed(k) ? BaguaGateKind.Closed : _formation.KindOf(k));
                Gizmos.color = color;
                var across = gate.Across * (geometry.TriggerWidth * 0.5f);
                var through = new Vector2(-gate.Across.y, gate.Across.x) * (geometry.TriggerDepth * 0.5f);
                DrawPolygon(gate.Position - across - through, gate.Position + across - through,
                    gate.Position + across + through, gate.Position - across + through);
                var slab = _formation.Slab(k);
                Gizmos.color = slab.IsActive ? Color.cyan : new Color(0f, 1f, 1f, 0.25f);
                Gizmos.DrawLine(_battle.ToView(slab.A), _battle.ToView(slab.B));
            }

            Gizmos.color = _formation.AltarOwner switch
            {
                Team.Player => PlayerColor,
                Team.Enemy => EnemyColor,
                _ => Color.white
            };
            DrawCircle(geometry.AltarCenter, geometry.AltarRadius);
        }

        private void DrawCircle(Vector2 center, float radius)
        {
            const int segments = 32;
            var previous = _battle.ToView(center + new Vector2(radius, 0f));
            for (var i = 1; i <= segments; i++)
            {
                var angle = i * Mathf.PI * 2f / segments;
                var next = _battle.ToView(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }

        private void DrawPolygon(params Vector2[] corners)
        {
            for (var i = 0; i < corners.Length; i++)
                Gizmos.DrawLine(_battle.ToView(corners[i]), _battle.ToView(corners[(i + 1) % corners.Length]));
        }
    }
}

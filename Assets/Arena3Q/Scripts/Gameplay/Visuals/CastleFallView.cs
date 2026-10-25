using System.Collections.Generic;
using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>
    /// A castle falling (<see cref="CastleArtConfig"/>): the intact picture shakes and flashes, then bursts into dust
    /// and debris, swaps to the ruined picture with a small drop and keeps smoking for a while. The ruin stays on the
    /// field. Runs on real time (Update), so it plays on after the battle has ended. Pictures only.
    /// </summary>
    internal sealed class CastleFallView : MonoBehaviour
    {
        private sealed class Particle
        {
            public SpriteRenderer Renderer;
            public Vector3 Velocity;
            public float Gravity;
            public float Growth;
            public float Life;
            public float Age;
            public Color Color;
        }

        private readonly List<Particle> _particles = new List<Particle>();
        private System.Random _random;
        private BattleController _battle;
        private CastleArtConfig _art;
        private Vector2 _logic;
        private SpriteRenderer _body;
        private float _time;
        private float _nextSmoke;
        private bool _collapsed;

        /// <summary>Seconds since the castle fell.</summary>
        public float Elapsed => _time;
        public bool HasCollapsed => _collapsed;

        public void Init(BattleController battle, CastleArtConfig art, Vector2 logic, int seed)
        {
            _battle = battle;
            _art = art;
            _logic = logic;
            _random = new System.Random(seed);
            _body = PlaceholderArt.CreateRenderer("Castle", transform, art.IntactSprite, Color.white, Vector2.one);
            UseModel(art.IntactSprite);
            _battle.ViewChanged += Place;
            Place();
            if (art.ShakeSeconds <= 0f)
                Collapse();
        }

        private void OnDestroy()
        {
            if (_battle != null)
                _battle.ViewChanged -= Place;
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            _time += deltaTime;
            var basePosition = Vector3.zero; // local: the view sits on the castle and faces the camera

            if (!_collapsed)
            {
                // Shaking harder and flashing faster until it gives way.
                var t = Mathf.Clamp01(_time / _art.ShakeSeconds);
                var shake = _art.ShakeAmplitude * t;
                _body.transform.localPosition = basePosition + new Vector3(Range(-shake, shake), Range(-shake, shake) * 0.5f, 0f);
                _body.color = Color.Lerp(Color.white, _art.FlashColor, 0.5f + 0.5f * Mathf.Sin(_time * (12f + 30f * t)));
                ShowModel(_body.transform.localPosition, _body.color);
                if (_time >= _art.ShakeSeconds)
                    Collapse();
            }
            else
            {
                // The ruin drops a little and settles back.
                var settle = Mathf.Clamp01((_time - _art.ShakeSeconds) / _art.CollapseSeconds);
                var drop = _art.CollapseDrop * Mathf.Sin(settle * Mathf.PI) * (1f - settle * 0.5f);
                _body.transform.localPosition = basePosition - new Vector3(0f, drop, 0f);
                _body.color = Color.white;
                ShowModel(_body.transform.localPosition, _body.color);

                var smokeAge = _time - _art.ShakeSeconds;
                if (smokeAge < _art.SmokeSeconds && _time >= _nextSmoke)
                {
                    _nextSmoke = _time + _art.SmokeInterval;
                    var fade = 1f - smokeAge / _art.SmokeSeconds;
                    var color = _art.SmokeColor;
                    color.a *= fade;
                    Spawn(PlaceholderArt.Circle, color, basePosition + Offset(0.9f, 0.4f, 0.6f), Range(0.5f, 0.8f),
                        new Vector3(Range(-0.15f, 0.15f), Range(0.5f, 0.8f), 0f), 0f, 0.6f, Range(2f, 3f));
                }
            }

            for (var i = _particles.Count - 1; i >= 0; i--)
            {
                var particle = _particles[i];
                particle.Age += deltaTime;
                if (particle.Age >= particle.Life)
                {
                    Destroy(particle.Renderer.gameObject);
                    _particles.RemoveAt(i);
                    continue;
                }
                particle.Velocity.y -= particle.Gravity * deltaTime;
                var particleTransform = particle.Renderer.transform;
                particleTransform.localPosition += particle.Velocity * deltaTime;
                particleTransform.localScale += Vector3.one * (particle.Growth * deltaTime);
                var color = particle.Color;
                color.a *= 1f - particle.Age / particle.Life;
                particle.Renderer.color = color;
            }
        }

        private void Collapse()
        {
            _collapsed = true;
            if (_art.DestroyedSprite != null)
            {
                _body.sprite = _art.DestroyedSprite;
                UseModel(_art.DestroyedSprite);
            }
            var basePosition = Vector3.zero; // local: the view sits on the castle and faces the camera
            var width = _art.IntactSprite.bounds.size.x * 0.5f;

            for (var i = 0; i < _art.DustPuffs; i++)
            {
                var angle = Range(0f, Mathf.PI * 2f);
                var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.5f, 0f);
                Spawn(PlaceholderArt.Circle, _art.DustColor, basePosition + direction * Range(0.2f, width * 0.6f),
                    Range(0.5f, 0.9f), direction * Range(0.8f, 1.6f) + Vector3.up * 0.3f, 0f, 0.9f, Range(0.8f, 1.3f));
            }
            for (var i = 0; i < _art.DebrisPieces; i++)
            {
                var side = Range(-1f, 1f);
                Spawn(PlaceholderArt.Square, _art.DebrisColor, basePosition + Offset(width * 0.8f, 0.3f, 0.8f),
                    Range(0.1f, 0.22f), new Vector3(side * Range(1f, 2.5f), Range(2f, 4f), 0f), 9f, 0f, Range(0.7f, 1.1f));
            }
            _nextSmoke = _time;
        }

        private void Spawn(Sprite sprite, Color color, Vector3 position, float size, Vector3 velocity, float gravity,
            float growth, float life)
        {
            var renderer = PlaceholderArt.CreateRenderer("Particle", transform, sprite, color, Vector2.one * size,
                _body.sortingOrder + 1);
            renderer.transform.localPosition = position;
            _particles.Add(new Particle
            {
                Renderer = renderer, Velocity = velocity, Gravity = gravity, Growth = growth, Life = life, Color = color
            });
        }

        private void Place()
        {
            var view = _battle.ToView(_logic);
            transform.position = view;
            transform.rotation = _battle.UprightRotation;
            _body.transform.localPosition = Vector3.zero;
            _body.sortingOrder = Mathf.RoundToInt(-view.y * 20f) * 4;
            ShowModel(Vector3.zero, Color.white);
        }

        // 3D: the command post model shakes and flashes in place of the picture, then the ruin model replaces it.
        private Transform _model;
        private MeshRenderer _modelRenderer;
        private float _modelYaw;
        private MaterialPropertyBlock _block;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void UseModel(Sprite sprite)
        {
            if (!MapProp3D.TryFind(_battle, sprite, out var prop, out var yaw))
                return;
            if (_model != null)
                Destroy(_model.gameObject);
            _model = MapProp3D.Create("Castle 3D", transform, prop, _battle.View.DecorModelScale);
            _modelRenderer = _model.GetComponentInChildren<MeshRenderer>();
            // The ruin keeps the side the standing command post faced.
            _modelYaw = _art.FaceOpponent ? MapProp3D.YawFacing(prop, MapProp3D.TowardOpponent(_battle, _logic)) : yaw;
            _body.enabled = false;
            ShowModel(_body.transform.localPosition, _body.color);
        }

        // Same screen-plane offset as the picture would get (the view faces the camera), standing on the ground.
        private void ShowModel(Vector3 offset, Color tint)
        {
            if (_model == null)
                return;
            _model.localPosition = offset;
            _model.rotation = MapProp3D.Rotation(_modelYaw);
            _block ??= new MaterialPropertyBlock();
            _block.SetColor(BaseColorId, tint);
            var teamColor = _logic.x <= _battle.Stage.MapBounds.center.x ? _battle.View.PlayerColor : _battle.View.EnemyColor;
            _block.SetColor("_FactionColor", teamColor);
            _block.SetFloat("_FactionMask", teamColor.b > teamColor.r ? 1f : 0f);
            _modelRenderer.SetPropertyBlock(_block);
        }

        private Vector3 Offset(float x, float yMin, float yMax) => new Vector3(Range(-x, x), Range(yMin, yMax), 0f);

        private float Range(float min, float max) => min + (float)_random.NextDouble() * (max - min);
    }
}

using System;
using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>
    /// The Bát Quái Trận of a map (docs/BanDo/BatQuaiTran.md): ring geometry imported from BatTran_Layout.json, the
    /// rotation and altar rules, the Bát Môn gate effects and the sprites. Stages reference it from
    /// <see cref="StageConfig"/>; leave the stage field empty for maps without a formation.
    /// </summary>
    [CreateAssetMenu(menuName = "Arena3Q/Bagua Formation", fileName = "BaguaFormation")]
    public sealed class BaguaFormationConfig : ScriptableObject
    {
        public const int GateCount = 8;

        [Serializable]
        public sealed class GateMarker
        {
            public Vector2 position;
            [Tooltip("Marker rotation from the layout (Unity rotY, degrees); its local X axis runs across the gap.")]
            public float rotY;
        }

        [Serializable]
        public sealed class GateType
        {
            public Color runeColor = Color.white;
            public BaguaGateEffect effect;
        }

        [Serializable]
        public sealed class SlabSprite
        {
            [Tooltip("Model yaw the sprite was rendered at (degrees, 0–360); each gate uses the one nearest its rotY.")]
            public float yaw;
            public Sprite sprite;
        }

        [Header("Geometry (BatTran_Layout.json)")]
        [SerializeField] private Vector2 altarCenter = Vector2.zero;
        [SerializeField, Min(0.1f)] private float altarRadius = 2f;
        [SerializeField] private Vector2[] pillars = Array.Empty<Vector2>();
        [SerializeField, Min(0.1f)] private float pillarRadius = 1f;
        [Tooltip("Gate k at angle k·45° (0 = east, towards the enemy side); opposite gates are k and k + 4.")]
        [SerializeField] private GateMarker[] gates = Array.Empty<GateMarker>();
        [Tooltip("Trigger rectangle: x = across the gap, y = depth through it.")]
        [SerializeField] private Vector2 triggerSize = new Vector2(2.4f, 1f);
        [SerializeField, Min(0.1f)] private float slabLength = 2.5f;
        [SerializeField, Min(0.05f)] private float slabThickness = 0.4f;

        [Header("Rotation (✅ 20 s, 3 s warning, clockwise)")]
        [SerializeField, Min(1f)] private float rotationPeriodSeconds = 20f;
        [SerializeField, Min(0f)] private float warningSeconds = 3f;
        [Tooltip("Clockwise: gate k takes the type of gate k + 1 (gates are numbered anticlockwise).")]
        [SerializeField] private bool clockwise = true;
        [SerializeField, Min(0f)] private float slabRiseSeconds = 0.6f;
        [SerializeField, Min(0f)] private float slabLowerSeconds = 0.6f;
        [Tooltip("Gate types at the start; gates k and k + 4 must match.")]
        [SerializeField] private BaguaGateKind[] initialKinds =
        {
            BaguaGateKind.Haste, BaguaGateKind.Life, BaguaGateKind.Death, BaguaGateKind.Closed,
            BaguaGateKind.Haste, BaguaGateKind.Life, BaguaGateKind.Death, BaguaGateKind.Closed
        };

        [Header("Altar (✅ 5 s alone to hold it, +50 % Sinh / Khai, no Tử)")]
        [SerializeField, Min(0f)] private float captureSeconds = 5f;
        [SerializeField, Min(1f)] private float blessingMultiplier = 1.5f;

        [Header("Gate types (✅ values; cooldowns of Khai / Tử 🟡)")]
        [SerializeField] private GateType life = new GateType
        {
            runeColor = new Color(0.35f, 0.85f, 0.35f),
            effect = new BaguaGateEffect { HealFraction = 0.08f, CooldownSeconds = 5f }
        };
        [SerializeField] private GateType haste = new GateType
        {
            runeColor = new Color(1f, 0.85f, 0.25f),
            effect = new BaguaGateEffect { MoveSpeedUp = 0.3f, MoveSpeedUpSeconds = 4f, CooldownSeconds = 5f }
        };
        [SerializeField] private GateType death = new GateType
        {
            runeColor = new Color(0.9f, 0.2f, 0.18f),
            effect = new BaguaGateEffect { DamageFraction = 0.1f, Slow = 0.3f, SlowSeconds = 2f, CooldownSeconds = 5f }
        };
        [SerializeField] private Color closedRuneColor = new Color(0.55f, 0.55f, 0.58f);

        [Header("Visuals (sprites rendered by Arena3Q > Render Prop Sprites)")]
        [Tooltip("Each pillar picks one at random (seeded, the same every battle).")]
        [SerializeField] private Sprite[] pillarSprites = Array.Empty<Sprite>();
        [SerializeField] private SlabSprite[] slabSprites = Array.Empty<SlabSprite>();
        [SerializeField] private Sprite altarSprite;
        [Tooltip("How deep the slab sinks under the ground when open (logic units; the rules say 1.9).")]
        [SerializeField, Min(0f)] private float slabSinkDepth = 1.9f;

        public Vector2 AltarCenter => altarCenter;
        public float AltarRadius => altarRadius;
        public IReadOnlyList<Vector2> Pillars => pillars;
        public float PillarRadius => pillarRadius;
        public IReadOnlyList<GateMarker> Gates => gates;
        public IReadOnlyList<Sprite> PillarSprites => pillarSprites;
        public Sprite AltarSprite => altarSprite;
        public float SlabSinkDepth => slabSinkDepth;

        public Color RuneColor(BaguaGateKind kind) => kind switch
        {
            BaguaGateKind.Life => life.runeColor,
            BaguaGateKind.Haste => haste.runeColor,
            BaguaGateKind.Death => death.runeColor,
            _ => closedRuneColor
        };

        /// <summary>The slab sprite rendered nearest to the gate's rotation (null if none).</summary>
        public Sprite SlabSpriteFor(float rotY)
        {
            Sprite best = null;
            var bestDelta = float.MaxValue;
            foreach (var slab in slabSprites)
            {
                if (slab?.sprite == null)
                    continue;
                var delta = Mathf.Abs(Mathf.DeltaAngle(slab.yaw, rotY));
                if (delta < bestDelta)
                {
                    bestDelta = delta;
                    best = slab.sprite;
                }
            }
            return best;
        }

        public BaguaFormationGeometry CreateGeometry()
        {
            var geometry = new BaguaFormationGeometry
            {
                AltarCenter = altarCenter,
                AltarRadius = altarRadius,
                TriggerWidth = triggerSize.x,
                TriggerDepth = triggerSize.y,
                SlabLength = slabLength,
                SlabThickness = slabThickness
            };
            foreach (var pillar in pillars)
                geometry.Pillars.Add((pillar, pillarRadius));
            foreach (var gate in gates)
                geometry.Gates.Add(new BaguaGateSpot(gate.position, BaguaGateSpot.AcrossFromRotY(gate.rotY)));
            return geometry;
        }

        public BaguaFormationSettings CreateSettings() => new BaguaFormationSettings
        {
            RotationPeriodSeconds = rotationPeriodSeconds,
            WarningSeconds = warningSeconds,
            Clockwise = clockwise,
            SlabRiseSeconds = slabRiseSeconds,
            SlabLowerSeconds = slabLowerSeconds,
            CaptureSeconds = captureSeconds,
            BlessingMultiplier = blessingMultiplier,
            InitialKinds = (BaguaGateKind[])initialKinds.Clone(),
            Life = life.effect,
            Haste = haste.effect,
            Death = death.effect
        };

        /// <summary>A fresh runtime formation (one per battle, like every other runtime state).</summary>
        public BaguaFormation CreateFormation() => new BaguaFormation(CreateGeometry(), CreateSettings());

        public void Validate(List<string> errors)
        {
            var owner = $"Bagua formation '{name}'";
            if (gates == null || gates.Length != GateCount)
                errors.Add($"{owner}: needs exactly {GateCount} gates.");
            if (pillars == null || pillars.Length != GateCount)
                errors.Add($"{owner}: needs exactly {GateCount} pillars.");
            if (initialKinds == null || initialKinds.Length != GateCount)
            {
                errors.Add($"{owner}: initialKinds needs {GateCount} entries.");
                return;
            }
            for (var k = 0; k < GateCount / 2; k++)
            {
                if (initialKinds[k] != initialKinds[k + GateCount / 2])
                    errors.Add($"{owner}: gates {k} and {k + GateCount / 2} face each other and must start with the same type.");
            }
            if (triggerSize.x <= 0f || triggerSize.y <= 0f)
                errors.Add($"{owner}: triggerSize must be positive.");
        }
    }
}

using System;
using UnityEngine;

namespace Arena3Q.Lobby2D
{
    [CreateAssetMenu(menuName = "Arena3Q/Lobby 2D/Lu Bu Cutout Rig")]
    public sealed class LuBuCutoutDefinition : ScriptableObject
    {
        [Serializable] public sealed class Part
        {
            public string name, layer, bone, textureId;
            public Texture2D texture;
            public Vector2[] vertices, uv;
            public int[] triangles;
            public Vector2 attachment;
            public float flexibility, flexLength, phase;
            public bool eyelids;
            public int onlyClip = -1, exceptClip = -1;
        }
        [Serializable] public sealed class Joint { public string name; public Vector2 point; }
        [Serializable] public sealed class Clip
        {
            public string name; public float duration; public bool loop;
            public AnimationCurve lean, crouch, hipShift, weaponAngle, gripBlend;
        }
        public Part[] parts = Array.Empty<Part>();
        public LuBuSlashDefinition slash;
        public Joint[] joints = Array.Empty<Joint>();
        public Clip[] clips = Array.Empty<Clip>();
        public Vector2 sourceSize = new Vector2(1254, 1254), sourceGround = new Vector2(650, 1140);
        public Vector2 frameSize = new Vector2(2048, 2048), framePivot = new Vector2(.5f, .2f);
        public float breathHeight = 2.8f, breathLean = .25f, headLean = .4f;
        public float runLean = -12, runCrouch = -30, runBob = 10, runStride = 190, runLift = 105, runStance = .42f;
        public float runCounterRotation = 1.3f, runArmSwing = 23;
        public Vector2 runOriginR = new Vector2(60,0), runOriginL = new Vector2(95,25);
        public float runBobPhase = .46f;
        public float impactNormalizedTime = 13f/24f;
        public float restWeaponAngle = -29.3f, gripSeparation = 96;
        public Vector2 attackGripCenter;
        public float blinkPeriod = 4.7f, blinkTime = 1.1f, blinkClose = .065f, blinkHold = .045f, blinkOpen = .095f;
        public float transitionSeconds = .16f;
        public float recoveryArc = 55;
        public float attackWindPeak = 3.5f, attackSettleStart = .75f;
        public Vector2 JointPoint(string name)
        {
            foreach (var joint in joints) if (joint.name == name) return joint.point;
            throw new InvalidOperationException("Rig is missing joint " + name);
        }
    }
}

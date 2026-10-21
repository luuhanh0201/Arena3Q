using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>Optional 3D presentation; gameplay and the battle pool own its clock.</summary>
    public interface IMacAnhSpatialEffect
    {
        void Begin(MacAnhFxQuality quality,float duration);
        void Advance(float delta);
        void Stop();
        void SetBattlePresentation(float radius,int order,float groundProjection,MacAnhFxKind kind);
        void SetCharge();
    }

    /// <summary>3D presentation: rotation that makes an upright picture face the camera (identity in the flat view).</summary>
    public interface IMacAnhBillboard
    {
        void SetUpright(Quaternion upright);
        /// <summary>How many effect radii nearer the camera upright pictures are drawn (BattleViewConfig.EffectDepthBias).</summary>
        void SetDepthBias(float effectRadii);
        /// <summary>How far the hottest light is pushed past 1 for the battle's bloom (BattleViewConfig.EffectBloomBoost).</summary>
        void SetBloomBoost(float boost);
    }
}

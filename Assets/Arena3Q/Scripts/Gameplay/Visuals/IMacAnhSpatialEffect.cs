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
}

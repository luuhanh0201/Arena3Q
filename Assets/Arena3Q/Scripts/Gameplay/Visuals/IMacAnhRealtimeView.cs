using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    /// <summary>Optional realtime presentation; cannot write the hero's logic position.</summary>
    public interface IMacAnhRealtimeView
    {
        bool Initialize(HeroUnit hero,SpriteRenderer sprite);
        void Sample(MacAnhAction action,float seconds,float facingDegrees);
        void SetOpacity(float opacity);
        bool CaptureAfterimage(MacAnhFx effect,int sortingOrder);
        bool TrySocket(MacAnhSocket socket,out Vector3 position);
    }
}

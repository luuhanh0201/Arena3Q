using UnityEngine;

namespace Arena3Q.Graphics3D
{
    /// <summary>Animation Events are visual timing only. They never apply combat damage.</summary>
    public sealed class MacAnhAnimationEvents : MonoBehaviour
    {
        public MacAnhGraphicsPreview preview;
        public void OnMacAnhAnimationEvent(AnimationEvent mark)
        {
            if(preview!=null) preview.OnAnimationMark(mark.stringParameter,mark.intParameter);
        }
    }
}

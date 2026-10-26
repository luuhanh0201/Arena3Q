using UnityEngine;
using UnityEngine.Events;
namespace Arena3Q.Lobby2D
{
    public sealed class LuBuAnimationEventRelay:MonoBehaviour
    {
        public UnityEvent impact=new UnityEvent();
        public void OnAttackImpact(){impact.Invoke();}
    }
}

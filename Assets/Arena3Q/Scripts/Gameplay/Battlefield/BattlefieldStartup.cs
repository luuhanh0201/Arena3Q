using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    [DefaultExecutionOrder(-200)]
    public sealed class BattlefieldStartup : MonoBehaviour
    {
        public BattleController battle;
        public BattleViewConfig view;
        private void Awake(){if(battle!=null&&view!=null)battle.UseView(view);}
    }
}

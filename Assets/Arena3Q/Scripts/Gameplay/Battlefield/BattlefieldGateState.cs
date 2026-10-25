using UnityEngine;

namespace Arena3Q.Gameplay
{
    public sealed class BattlefieldGateState : MonoBehaviour
    {
        public BattleController battle;
        public MeshRenderer[] runes = new MeshRenderer[8];
        private MaterialPropertyBlock _block;
        private readonly Color[] _last = new Color[8];
        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        private void LateUpdate()
        {
            if (battle == null || battle.Formation == null || battle.Stage.Formation == null) return;
            _block ??= new MaterialPropertyBlock();
            for (int i = 0; i < runes.Length; i++)
            {
                if (runes[i] == null) continue;
                var color = battle.Stage.Formation.RuneColor(battle.Formation.KindOf(i));
                color.a = battle.Formation.IsWarning ? .6f + .3f * Mathf.Sin(battle.State.ElapsedTime * 9f) : .8f;
                if (color == _last[i]) continue;
                _last[i] = color; _block.SetColor(ColorId, color); runes[i].SetPropertyBlock(_block);
            }
        }
    }
}

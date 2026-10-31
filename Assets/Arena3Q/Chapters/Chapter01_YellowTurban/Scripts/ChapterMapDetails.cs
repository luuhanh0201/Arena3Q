using System.Collections.Generic;
using Arena3Q.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace Arena3Q.Exploration
{
    /// <summary>Preprojected decoration meshes. No colliders, navigation or progression changes.</summary>
    public sealed class ChapterMapDetails : MonoBehaviour
    {
        private ChapterMapController _map;
        private readonly List<(Renderer renderer,Material[] originals)> _ground=new List<(Renderer,Material[])>();
        private readonly List<(Renderer renderer,int stage)> _details=new List<(Renderer,int)>();
        private MaterialPropertyBlock _block;
        public int RendererCount=>_details.Count;
        public void Configure(ChapterMapController map)
        {
            _map=map;var config=map.Config.details;if(config==null||!config.enabled)return;
            _block=new MaterialPropertyBlock();
            foreach(var renderer in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                var original=renderer.sharedMaterials;var replacement=(Material[])original.Clone();bool changed=false;
                for(int i=0;i<replacement.Length;i++)if(replacement[i]==config.groundSource){replacement[i]=config.groundReplacement;changed=true;}
                if(changed){_ground.Add((renderer,original));renderer.sharedMaterials=replacement;}
            }
            for(int stage=0;stage<config.stageMeshes.Length;stage++)
            {
                var mesh=config.stageMeshes[stage];if(mesh==null)continue;
                var root=new GameObject("Ground details "+(stage+1));root.transform.SetParent(transform,false);
                root.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=root.AddComponent<MeshRenderer>();
                renderer.sharedMaterial=config.detailsMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;
                _details.Add((renderer,stage));
            }
            RefreshVisibility();
        }
        private void LateUpdate()=>RefreshVisibility();
        public void RefreshVisibility()
        {
            foreach(var item in _details)
            {
                float visible=_map.World==null?0:_map.World.Visibility(item.stage);
                item.renderer.enabled=visible>0;
                _block.SetFloat("_Visibility",visible);item.renderer.SetPropertyBlock(_block);
            }
        }
        private void OnDestroy()
        {
            foreach(var item in _ground)if(item.renderer!=null)item.renderer.sharedMaterials=item.originals;
            foreach(var item in _details)if(item.renderer!=null)Destroy(item.renderer.gameObject);
        }
    }
}

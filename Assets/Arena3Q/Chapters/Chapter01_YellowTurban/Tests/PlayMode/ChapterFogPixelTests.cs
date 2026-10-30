#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Arena3Q.Exploration.Tests
{
    public sealed partial class ChapterMapFlowTests
    {
        [UnityTest] public IEnumerator FogPixelsStayContinuousAcrossLockedBorderAndRevealEndpoints()
        {
            // Measure the production shader as black fog on white. Geometry, lighting and HUD
            // edges are excluded; the three regions are open / revealing / locked.
            var camera=_map.View.Camera;
            var screen=(Transform)typeof(ChapterMapWorld).GetField("_screen",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(_map.World);
            var renderer=screen.GetComponent<Renderer>();var original=renderer.sharedMaterial;
            var material=new Material(original);renderer.sharedMaterial=material;
            var data=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            var oldPost=data.renderPostProcessing;var oldLayer=screen.gameObject.layer;var oldMask=camera.cullingMask;
            var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;var oldSize=camera.orthographicSize;
            var oldClear=camera.clearFlags;var oldColor=camera.backgroundColor;var oldTarget=camera.targetTexture;
            var oldActive=RenderTexture.active;bool oldBatching=UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching;
            var rt=new RenderTexture(960,480,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);rt.Create();
            var texture=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false,true);
            var regions=new Vector4[16];regions[0]=new Vector4(-20,0,1,0);regions[2]=new Vector4(20,0,0,0);
            int maxTemporal=0,maxSpatial=0,maxEndpoint=0;string worst="";
            var report=new System.Text.StringBuilder("preset\tframe\tmax_temporal_byte\tmax_adjacent_byte\n");
            try
            {
                UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching=false;
                screen.gameObject.layer=31;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.white;
                camera.transform.SetPositionAndRotation(new Vector3(0,100,0),Quaternion.Euler(90,0,0));camera.orthographicSize=20;
                camera.targetTexture=rt;data.renderPostProcessing=false;
                material.SetFloat("_Count",3);material.SetFloat("_Clock",0);material.SetFloat("_Darkening",1);material.SetFloat("_Opacity",1);material.SetColor("_FogColor",Color.black);
                // Non-Properties shader uniforms are not retained by a cloned material.
                var motion=_map.Config.motion;
                material.SetTexture("_CloudTex",motion.fogTexture);material.SetFloat("_Boundary",motion.fogBoundaryWidth);
                material.SetFloat("_Scale",motion.fogTextureScale);material.SetFloat("_Drift",motion.fogDrift);
                material.SetFloat("_RegionRadius",motion.fogRegionRadius);material.SetFloat("_RevealEdge",motion.fogRevealEdge);
                Directory.CreateDirectory(CaptureDirectory);
                foreach(int rate in new[]{30,60,120})
                {
                    material.SetFloat("_Layers",_map.Config.motion.fogLayers[rate==30?0:rate==60?1:2]);
                    Color32[] previous=null;int initialContrast=0;
                    for(int frame=0;frame<=120;frame++)
                    {
                        regions[1]=new Vector4(0,0,frame/120f,0);material.SetVectorArray("_Regions",regions);
                        camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();
                        var pixels=texture.GetPixels32();int temporal=0,spatial=0,low=255,high=0;
                        for(int y=0;y<rt.height;y++)for(int x=0;x<rt.width;x++)
                        {
                            int i=y*rt.width+x,v=pixels[i].r;low=Mathf.Min(low,v);high=Mathf.Max(high,v);
                            if(previous!=null)temporal=Mathf.Max(temporal,Mathf.Abs(v-previous[i].r));
                            if(x>0)spatial=Mathf.Max(spatial,Mathf.Abs(v-pixels[i-1].r));
                            if(y>0)spatial=Mathf.Max(spatial,Mathf.Abs(v-pixels[i-rt.width].r));
                        }
                        if(frame==0)initialContrast=high-low;
                        if(temporal>maxTemporal){maxTemporal=temporal;worst=rate+" FPS frame "+frame;}
                        maxSpatial=Mathf.Max(maxSpatial,spatial);if(frame==1||frame==120)maxEndpoint=Mathf.Max(maxEndpoint,temporal);
                        report.AppendLine($"{rate}\t{frame}\t{temporal}\t{spatial}");
                        if(frame==0||frame==60||frame==119||frame==120)File.WriteAllBytes(CaptureDirectory+"/fog-mask-"+rate+"-"+frame+".png",texture.EncodeToPNG());
                        previous=pixels;
                    }
                    Assert.Greater(initialContrast,200,"The fixture must contain open and covered ground, not an empty render.");
                }
                File.WriteAllText(CaptureDirectory+"/fog-pixel-continuity.tsv",report.ToString());
                Assert.LessOrEqual(maxTemporal,64,"A 1/120 reveal step changed over 25% luminance at "+worst);
                Assert.LessOrEqual(maxEndpoint,4,"The first/last reveal step must not change the boundary suddenly.");
                Assert.LessOrEqual(maxSpatial,32,"Adjacent pixels contain a hard seam (over 12.5% luminance).");
            }
            finally
            {
                renderer.sharedMaterial=original;Object.Destroy(material);screen.gameObject.layer=oldLayer;camera.cullingMask=oldMask;
                camera.transform.SetPositionAndRotation(oldPosition,oldRotation);camera.orthographicSize=oldSize;
                camera.clearFlags=oldClear;camera.backgroundColor=oldColor;camera.targetTexture=oldTarget;data.renderPostProcessing=oldPost;
                RenderTexture.active=oldActive;rt.Release();Object.Destroy(rt);Object.Destroy(texture);UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching=oldBatching;
            }
            yield return null;
        }
    }
}
#endif

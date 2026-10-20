using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Arena3Q.Gameplay;
using UnityEngine;

namespace Arena3Q.Graphics3D
{
    /// <summary>Measures the actual rendered preview, including the unmodified dense skinned mesh.</summary>
    public sealed class MacAnhGraphicsBenchmark : MonoBehaviour
    {
        [Serializable] public sealed class Sample
        {public string quality;public int frames;public float seconds,fps,medianMs,p95Ms;}
        [Serializable] public sealed class Report
        {
            public string utc,platform,device,gpu,cpu,unity,graphicsApi;
            public int ramMB,width,height,sourceTriangles=2953808;
            public string workload="1 original skinned character walking + 1 active cyclone; no battle units; capped at display refresh / 120 Hz maximum";
            public Sample[] samples;
        }
        public bool Running {get;private set;}
        public IEnumerator Run()
        {
            if(Running) yield break;Running=true;
            int oldCap=Application.targetFrameRate,oldVsync=QualitySettings.vSyncCount;
            QualitySettings.vSyncCount=0;Application.targetFrameRate=120;
            var report=new Report{utc=DateTime.UtcNow.ToString("o"),platform=Application.platform.ToString(),device=SystemInfo.deviceModel,
                gpu=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,ramMB=SystemInfo.systemMemorySize,width=Screen.width,height=Screen.height,unity=Application.unityVersion,graphicsApi=SystemInfo.graphicsDeviceType.ToString(),samples=new Sample[8]};
            var preview=GetComponent<MacAnhGraphicsPreview>();
            for(int q=0;q<8;q++)
            {
                var quality=(MacAnhFxQuality)Mathf.Min(q,2);
                bool body=q!=4&&q!=7,wind=q!=3&&q!=7,shadow=q!=4&&q!=5&&q!=7,depth=q!=6&&q!=7;
                preview.SetBenchmarkState(quality,body,wind,shadow,depth);
                float warmEnd=Time.realtimeSinceStartup+4;
                while(Time.realtimeSinceStartup<warmEnd) yield return null;
                var times=new List<float>(2400);float start=Time.realtimeSinceStartup,last=start;
                while(Time.realtimeSinceStartup-start<(q<3?15:10))
                {yield return null;float now=Time.realtimeSinceStartup;times.Add((now-last)*1000);last=now;}
                float elapsed=Time.realtimeSinceStartup-start;times.Sort();
                string[] names={"Low","Medium","High","BodyOnly_High","VfxOnly_High","NoShadow_High","NoDepth_High","StageOnly"};
                report.samples[q]=new Sample{quality=names[q],frames=times.Count,seconds=elapsed,fps=times.Count/elapsed,
                    medianMs=times[times.Count/2],p95Ms=times[Mathf.Min(times.Count-1,Mathf.FloorToInt(times.Count*.95f))]};
                Debug.Log("[MacAnhBenchmark] "+JsonUtility.ToJson(report.samples[q]));
            }
            var json=JsonUtility.ToJson(report,true);
            string path=Path.Combine(Application.persistentDataPath,"MacAnhGraphicsBenchmark.json");File.WriteAllText(path,json);
            Debug.Log("[MacAnhBenchmarkReport] "+json+"\nSaved: "+path);
            Application.targetFrameRate=oldCap;QualitySettings.vSyncCount=oldVsync;Running=false;preview.EndBenchmark();
        }
    }
}

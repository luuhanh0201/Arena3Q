using System.Collections.Generic;
using System.Linq;
using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Exploration
{
    /// <summary>Bounded audio hooks. Null clips stay silent; no substitute ambience is invented.</summary>
    public sealed class ChapterMapAudio : MonoBehaviour
    {
        private sealed class Slot { public AudioSource source; public MapAudioAnchor anchor; public float gain; }
        private ChapterMapController _map;
        private MapAudioConfig _config;
        private Slot[] _slots;
        private readonly Dictionary<MapSound,float> _last=new Dictionary<MapSound,float>();
        private readonly Dictionary<MapSound,int> _requests=new Dictionary<MapSound,int>();
        private Vector3 _lastFoot;
        private float _distance;
        private Arena3Q.Frontend.UIAudio _uiAudio;
        private AudioListener _ownedListener;
        private readonly List<MapAudioAnchor> _wanted=new List<MapAudioAnchor>();
        public int SourceCount=>_slots?.Length??0;
        public int PlayingCount=>_slots?.Count(s=>s.source.isPlaying)??0;
        public int Requested(MapSound sound)=>_requests.TryGetValue(sound,out int count)?count:0;
        public void Configure(ChapterMapController map,MapAudioConfig config)
        {
            _map=map;_config=config;_uiAudio=map.GetComponent<Arena3Q.Frontend.UIAudio>();if(config==null)return;
            if(FindFirstObjectByType<AudioListener>()==null)_ownedListener=map.View.Camera.gameObject.AddComponent<AudioListener>();
            _slots=new Slot[map.Config.motion.maxAudioSources];
            for(int i=0;i<_slots.Length;i++)
            {
                var child=new GameObject("Map audio "+i);child.transform.SetParent(transform,false);
                var source=child.AddComponent<AudioSource>();source.playOnAwake=false;source.volume=0;source.spatialBlend=0;
                source.ignoreListenerPause=false;source.ignoreListenerVolume=false;source.mute=Muted;
                _slots[i]=new Slot{source=source};
            }
            _lastFoot=map.Motor.transform.position;
        }
        private bool Muted => (_uiAudio!=null&&_uiAudio.Muted)||(Application.isEditor&&_config.muteInEditor);
        public bool Play(MapSound sound,Vector3 position)
        {
            _requests[sound]=Requested(sound)+1;
            var cue=_config?.Find(sound);
            if(cue?.clip==null||_slots==null||Muted)return false;
            if(_last.TryGetValue(sound,out float last)&&Time.unscaledTime-last<cue.cooldown)return false;
            var slot=_slots.Skip(_config.ambienceSlots).FirstOrDefault(s=>!s.source.isPlaying);
            if(slot==null)return false; // Never cut an in-flight cue or exceed the pool.
            _last[sound]=Time.unscaledTime;slot.source.transform.position=position;slot.source.loop=false;slot.source.clip=cue.clip;slot.source.volume=cue.volume;Pan(slot.source,position);slot.source.Play();return true;
        }
        private void Update()
        {
            if(_slots==null||_map.Motor==null)return;
            foreach(var slot in _slots)slot.source.mute=Muted;
            var position=_map.Motor.transform.position;
            float step=Vector3.Distance(position,_lastFoot);_lastFoot=position;
            if(_map.Motor.Moving&&!_map.Busy){_distance+=step;if(_distance>=_config.footstepDistance){_distance%= _config.footstepDistance;Play(MapSound.Footstep,position);}}
            else _distance=0;
            var focus=_map.View.Focus;
            // Orthographic camera sits far above the scene: attenuation follows its ground focus,
            // stereo placement follows its screen-right vector rather than camera altitude.
            _wanted.Clear();
            foreach(var anchor in _config.anchors)
            {
                if(_config.Find(anchor.sound)?.clip==null||!Visible(anchor.stage))continue;
                float gain=Gain(anchor,focus);if(gain<=0)continue;
                int at=0;while(at<_wanted.Count&&Gain(_wanted[at],focus)>=gain)at++;
                if(at>=_config.ambienceSlots)continue;
                _wanted.Insert(at,anchor);if(_wanted.Count>_config.ambienceSlots)_wanted.RemoveAt(_wanted.Count-1);
            }
            for(int i=0;i<_config.ambienceSlots;i++)
            {
                var slot=_slots[i];bool retained=slot.anchor!=null&&_wanted.Contains(slot.anchor);
                float target=retained?Gain(slot.anchor,focus):0;
                slot.gain=Mathf.MoveTowards(slot.gain,target,Time.unscaledDeltaTime/_config.fadeSeconds);
                if(slot.anchor!=null)
                {
                    slot.source.volume=slot.gain*_config.Find(slot.anchor.sound).volume;Pan(slot.source,slot.anchor.position);
                    if(!retained&&slot.gain<=0){slot.source.Stop();slot.anchor=null;}
                }
                if(slot.anchor==null)
                {
                    MapAudioAnchor next=null;
                    foreach(var candidate in _wanted)
                    {
                        bool used=false;for(int j=0;j<_config.ambienceSlots;j++)if(_slots[j].anchor==candidate){used=true;break;}
                        if(!used){next=candidate;break;}
                    }
                    if(next==null)continue;
                    slot.anchor=next;slot.source.transform.position=next.position;slot.source.clip=_config.Find(next.sound).clip;slot.source.loop=true;slot.source.volume=0;slot.gain=0;slot.source.Play();
                }
            }
        }
        private bool Visible(int stage)=>_map.World!=null&&_map.World.MarkersVisible(stage);
        private static float Gain(MapAudioAnchor anchor,Vector3 focus){var delta=anchor.position-focus;delta.y=0;float t=Mathf.Clamp01(1-delta.magnitude/anchor.radius);return t*t;}
        private void Pan(AudioSource source,Vector3 position)
        {source.panStereo=Mathf.Clamp(Vector3.Dot(position-_map.View.Focus,_map.View.Camera.transform.right)/_config.panDistance,-1,1);}
        private void OnDestroy()
        {
            if(_slots!=null)foreach(var slot in _slots)if(slot.source!=null){slot.source.Stop();Destroy(slot.source.gameObject);}
            if(_ownedListener!=null)Destroy(_ownedListener);
        }
    }
}

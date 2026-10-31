using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arena3Q.Data
{
    public enum MapSound { Village, WindBirds, River, Campfire, Camp, Footstep, SelectNpc, AcceptQuest, FogReveal, OpenChest, Boss }
    [Serializable] public sealed class MapAudioCue
    {
        public MapSound sound;
        public AudioClip clip;
        [Range(0,1)] public float volume=.3f;
        public float cooldown=.1f;
    }
    [Serializable] public sealed class MapAudioAnchor
    {
        public string id;
        public int stage;
        public MapSound sound;
        public Vector3 position;
        public float radius=25;
    }
    [CreateAssetMenu(menuName="Arena3Q/Exploration/Map Audio")]
    public sealed class MapAudioConfig : ScriptableObject
    {
        public MapAudioCue[] cues=Array.Empty<MapAudioCue>();
        public MapAudioAnchor[] anchors=Array.Empty<MapAudioAnchor>();
        public int ambienceSlots=2;
        public float fadeSeconds=.6f, footstepDistance=.85f, panDistance=22;
        public bool muteInEditor=true;
        public MapAudioCue Find(MapSound sound)
        {
            if(cues!=null)foreach(var cue in cues)if(cue!=null&&cue.sound==sound)return cue;
            return null;
        }
        public void Validate(List<string> errors,int stages,int maxSources)
        {
            if(ambienceSlots<0||ambienceSlots>=maxSources||fadeSeconds<=0||footstepDistance<=0||panDistance<=0)errors.Add("Invalid map audio pool or distances.");
            if(cues==null||anchors==null){errors.Add("Map audio arrays are required.");return;}
            var seen=new HashSet<MapSound>();foreach(var cue in cues)if(cue==null||!seen.Add(cue.sound)||cue.volume<0||cue.volume>1||cue.cooldown<0)errors.Add("Invalid or duplicate map audio cue.");
            var ids=new HashSet<string>();foreach(var anchor in anchors)if(anchor==null||string.IsNullOrWhiteSpace(anchor.id)||!ids.Add(anchor.id)||anchor.stage<0||anchor.stage>=stages||anchor.radius<=0)errors.Add("Invalid map audio anchor.");
        }
    }
}

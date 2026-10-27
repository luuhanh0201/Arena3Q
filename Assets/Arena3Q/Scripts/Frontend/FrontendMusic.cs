using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Frontend
{
    /// <summary>One looping music source for the whole frontend, starting at Loading and ending on scene exit.</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class FrontendMusic : MonoBehaviour
    {
        private const string VolumeKey = "Arena3Q.Music.Volume", MuteKey = "Arena3Q.Music.Muted";
        private AudioSource _source;
        private bool _previewMuted;
        public float Volume { get; private set; }
        public bool Muted { get; private set; }
        public bool Started { get; private set; }
        public AudioSource Source => _source;
        public void Configure(StartupUiConfig config)
        {
            _source = GetComponent<AudioSource>(); _source.playOnAwake = false;
            _source.clip = config.music; _source.loop = true; _source.spatialBlend = 0;
            _source.ignoreListenerPause = true;
            _previewMuted = Application.isEditor && config.muteMusicInEditor;
            Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, config.musicVolume));
            Muted = PlayerPrefs.GetInt(MuteKey, 0) != 0; Apply();
        }
        public void Play()
        {
            if (Started || _source == null || _source.clip == null) return;
            Started = true; _source.Play();
        }
        public void SetVolume(float volume)
        {
            Volume = Mathf.Clamp01(volume); PlayerPrefs.SetFloat(VolumeKey, Volume); Apply();
        }
        public void SetMuted(bool muted)
        {
            Muted = muted; PlayerPrefs.SetInt(MuteKey, muted ? 1 : 0); Apply(); PlayerPrefs.Save();
        }
        private void Apply() { if (_source != null) { _source.volume = Volume; _source.mute = Muted || _previewMuted; } }
        private void OnDestroy() { if (_source != null) _source.Stop(); PlayerPrefs.Save(); }
    }
}

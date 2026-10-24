using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Frontend
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class UIAudio : MonoBehaviour
    {
        private const string MuteKey = "Arena3Q.UI.Muted";
        private StartupUiConfig _config; private AudioSource _source;
        private readonly float[] _last = { -100, -100, -100, -100, -100, -100 };
        public bool Muted { get; private set; }
        public void Configure(StartupUiConfig config)
        {
            _config = config; _source = GetComponent<AudioSource>(); _source.playOnAwake = false;
            _source.spatialBlend = 0; _source.ignoreListenerPause = true; _source.ignoreListenerVolume = true;
            Muted = PlayerPrefs.GetInt(MuteKey, 0) != 0;
        }
        public void SetMuted(bool muted)
        {
            Muted = muted; PlayerPrefs.SetInt(MuteKey, muted ? 1 : 0); PlayerPrefs.Save();
            if (muted && _source != null) _source.Stop();
        }
        public bool Play(UiSound sound)
        {
            var index = (int)sound;
            if (Muted || _config == null || _source == null || index >= _config.sounds.Length || _config.sounds[index] == null) return false;
            var time = Time.unscaledTime;
            if (time - _last[index] < _config.sameClipInterval) return false;
            _last[index] = time; _source.PlayOneShot(_config.sounds[index], _config.soundVolume); return true;
        }
    }
}

using System;
using UnityEngine;

namespace Arena3Q.Gameplay
{
    public sealed class MacAnhFx : MonoBehaviour
    {
        [SerializeField] private MacAnhFxKind kind;
        [SerializeField] private Mesh[] high = new Mesh[3], medium = new Mesh[3], low = new Mesh[3];
        [SerializeField] private Material material;
        [SerializeField] private AudioClip startSound;
        [SerializeField] private MonoBehaviour spatialEffect;
        private IMacAnhSpatialEffect _spatial;
        private MaterialPropertyBlock _block;
        private MeshRenderer[] _renderers;
        private Transform[] _layers;
        private Action<MacAnhFx> _release;
        private Transform _follow;
        private float _seconds, _duration;
        private bool _running;
        private bool _charge;
        public bool ManualClock { get; set; }
        private SpriteRenderer _ghost;
        private GameObject _capturedPose;
        private Renderer _capturedRenderer;
        private Action _clearCapturedPose;
        public MacAnhFxKind Kind => kind;
        public bool IsRunning => _running;
        public int Generation { get; private set; }
        public void ConfigureSpatial(MacAnhFxKind type,MonoBehaviour effect) {kind=type;spatialEffect=effect;}
        public void Configure(MacAnhFxKind type, Material shared, Mesh[] highMeshes, Mesh[] mediumMeshes, Mesh[] lowMeshes)
        {
            kind = type; material = shared; high = highMeshes; medium = mediumMeshes; low = lowMeshes;
            EnsureLayers();
        }
        private void EnsureLayers()
        {
            if (_renderers != null) return;
            _block = new MaterialPropertyBlock();
            _renderers = new MeshRenderer[3]; _layers = new Transform[3];
            for (var i = 0; i < 3; i++)
            {
                var layer = transform.Find("Layer " + i);
                if (layer == null)
                {
                    layer = new GameObject("Layer " + i, typeof(MeshFilter), typeof(MeshRenderer)).transform;
                    layer.SetParent(transform, false);
                }
                _layers[i] = layer;
                _renderers[i] = layer.GetComponent<MeshRenderer>();
                _renderers[i].sharedMaterial = material;
                layer.GetComponent<MeshFilter>().sharedMesh = high[i];
            }
        }
        public void Begin(MacAnhFxQuality quality, float seconds, int order, Action<MacAnhFx> release, Transform follow = null)
        {
            ClearCapturedPose();
            if(_ghost!=null)_ghost.gameObject.SetActive(false);
            _spatial=spatialEffect as IMacAnhSpatialEffect;
            if(_spatial!=null)
            {
                _seconds=0;_duration=Mathf.Max(.05f,seconds);_release=release;_follow=follow;_running=true;_charge=false;Generation++;
                float radius=transform.localScale.x;
                bool ground=kind==MacAnhFxKind.MagicArray||kind==MacAnhFxKind.Cyclone||kind==MacAnhFxKind.Ultimate;
                float projection=ground?transform.localScale.y/Mathf.Max(.001f,radius):.819152f;
                transform.localScale=Vector3.one;
                _spatial.Begin(quality,_duration);_spatial.SetBattlePresentation(radius,order,projection,kind);return;
            }
            EnsureLayers(); _seconds = 0f; _duration = Mathf.Max(.05f, seconds); _release = release; _follow = follow; _running = true; _charge = false;
            Generation++;
            var meshes = quality == MacAnhFxQuality.Low ? low : quality == MacAnhFxQuality.Medium ? medium : high;
            for (var i = 0; i < 3; i++)
            {
                _layers[i].localPosition = Vector3.zero; _layers[i].localRotation = Quaternion.identity; _layers[i].localScale = Vector3.one;
                _layers[i].GetComponent<MeshFilter>().sharedMesh = meshes[i];
                _renderers[i].sortingOrder = order + i;
                _layers[i].gameObject.SetActive(quality != MacAnhFxQuality.Low || i < 2);
            }
            if (_ghost != null) _ghost.gameObject.SetActive(false);
            if (startSound != null)
            {
                var source = GetComponent<AudioSource>();
                if (source == null) source = gameObject.AddComponent<AudioSource>();
                source.spatialBlend = 0f; source.PlayOneShot(startSound);
            }
            ApplyFade(0f);
        }
        public void SetAfterimage(Sprite sprite, Material shared, int order)
        {
            if (sprite == null) return;
            _spatial?.Stop();transform.rotation=Quaternion.identity;transform.localScale=Vector3.one;
            if (_ghost == null)
            {
                var go = new GameObject("Character afterimage"); go.transform.SetParent(transform, false);
                _ghost = go.AddComponent<SpriteRenderer>();
            }
            _ghost.sprite = sprite; _ghost.sharedMaterial = shared; _ghost.sortingOrder = order;
            _ghost.transform.localPosition=Vector3.zero;_ghost.transform.localRotation=Quaternion.identity;_ghost.transform.localScale=Vector3.one;
            _ghost.gameObject.SetActive(true);
            ApplyPoseFade(0);
        }
        public void SetCapturedAfterimage(GameObject pose,Action cleanup)
        {
            ClearCapturedPose();_spatial?.Stop();transform.rotation=Quaternion.identity;transform.localScale=Vector3.one;
            _capturedPose=pose;_clearCapturedPose=cleanup;pose.transform.SetParent(transform,false);
            _capturedRenderer=pose.GetComponent<Renderer>();ApplyPoseFade(0);
        }
        private void ClearCapturedPose()
        {
            _clearCapturedPose?.Invoke();_clearCapturedPose=null;_capturedPose=null;_capturedRenderer=null;
        }
        private void ApplyPoseFade(float fade)
        {
            if(_ghost!=null)_ghost.color=new Color(.84f,.67f,1,fade*.65f);
            if(_capturedRenderer!=null)
            {
                _block=_block??new MaterialPropertyBlock();_block.Clear();_block.SetFloat("_Opacity",fade);_capturedRenderer.SetPropertyBlock(_block);
            }
        }
        private void Update()
        {
            if (!ManualClock) Advance(Time.deltaTime);
        }
        public void SetCharge() { _charge = true;if(_spatial!=null)_spatial.SetCharge(); }
        public void SetGroundLayers()
        {
            if(_spatial!=null)return;
            if (kind != MacAnhFxKind.MagicArray && kind != MacAnhFxKind.Ultimate) return;
            for (var i = 0; i < 2; i++) _renderers[i].sortingOrder = -5000 + i;
        }
        public void Advance(float delta)
        {
            if (!_running) return;
            _seconds += delta;
            if (_follow != null) transform.position = _follow.position;
            if (_seconds >= _duration) { Stop(); return; }
            var fade = Mathf.SmoothStep(0f, 1f, _seconds / .07f) * Mathf.Clamp01((_duration - _seconds) / Mathf.Min(.22f, _duration * .5f));
            if(_spatial!=null) {_spatial.Advance(delta);ApplyPoseFade(fade);return;}
            ApplyFade(fade);
            for (var i = 0; i < 3; i++)
            {
                var turn = kind == MacAnhFxKind.MagicArray ? _seconds * (i % 2 == 0 ? 20f : -28f) :
                    kind == MacAnhFxKind.Cyclone ? Mathf.Sin(_seconds * 8f + i) * 6f : 0f;
                _layers[i].localRotation = Quaternion.Euler(0, 0, turn);
                if (kind == MacAnhFxKind.MagicArray || kind == MacAnhFxKind.Ultimate)
                    _layers[i].localScale = Vector3.one * (_charge ? Mathf.Lerp(1f, .45f, _seconds / _duration) :
                        Mathf.Lerp(.85f, 1f, Mathf.Clamp01(_seconds / .55f)));
            }
        }
        private void ApplyFade(float fade)
        {
            _block.SetFloat("_Fade", fade);
            foreach (var renderer in _renderers) renderer.SetPropertyBlock(_block);
            ApplyPoseFade(fade);
        }
        public void Stop()
        {
            if (!_running) return;
            _running = false; _follow = null;
            if(_spatial!=null) _spatial.Stop();
            if (_ghost != null) _ghost.sprite = null;
            ClearCapturedPose();
            var source = GetComponent<AudioSource>();
            if (source != null) source.Stop();
            _release?.Invoke(this); _release = null;
        }
        private void OnDisable() { if(_spatial!=null) _spatial.Stop();ClearCapturedPose(); _running = false; _follow = null; }
    }
}

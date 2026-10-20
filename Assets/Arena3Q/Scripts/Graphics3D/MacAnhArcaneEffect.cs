using System;
using UnityEngine;
using Arena3Q.Gameplay;

namespace Arena3Q.Graphics3D
{
    /// <summary>All clocks are explicit, including particles, for pause, pooling and seeking.</summary>
    public sealed class MacAnhArcaneEffect : MonoBehaviour,IMacAnhSpatialEffect
    {
        public enum PartKind { Circle, Body, Ribbon, Runes, Impact, Beam }
        [Serializable] public sealed class Part
        {
            public Transform transform;
            public PartKind kind;
            public float spin;
            public Vector3 scale = Vector3.one;
            [NonSerialized] public Renderer renderer;
        }
        [Serializable] public sealed class Tier
        {
            public GameObject root;
            public Part[] parts;
            public ParticleSystem particles, feathers, smoke, burst;
            public Material previewParticleMaterial,battleParticleMaterial;
            [NonSerialized] public float[] lifetimeMin,lifetimeMax;
        }
        [SerializeField] private Tier[] tiers;
        [SerializeField] private bool charge;
        [SerializeField] private bool smokeDissolve;
        [SerializeField,Min(.01f)]private float riseSeconds=.32f;
        private Tier _active;
        private ParticleSystem[] _particleSystems;
        private MaterialPropertyBlock _block;
        private float _age, _duration;
        private bool _playing;
        private bool _battlePresentation,_chargeOverride;
        private int _releasedParticles;
        private bool _depthFading=true;
        private float _emissionStop;
        public bool ManualClock { get; set; }
        public float Age => _age;
        public bool IsPlaying => _playing;
        public MacAnhFxQuality Quality { get; private set; }
        public int ActiveParticleCount {get {int count=0;if(_particleSystems!=null) foreach(var ps in _particleSystems) if(ps!=null) count+=ps.particleCount;return count;}}
        public void Configure(Tier[] levels) { tiers=levels; }
        public void ConfigureCharge(bool hold) {charge=hold;}
        public void ConfigureRise(float seconds){riseSeconds=Mathf.Max(.01f,seconds);}
        public void ConfigureSmokeDissolve() {smokeDissolve=true;}
        public void Begin(MacAnhFxQuality quality, float duration=3f)
        {
            if (tiers == null || tiers.Length!=3) return;
            Quality=quality; _duration=Mathf.Max(.05f,duration); _age=0; _playing=true;_releasedParticles=0;_battlePresentation=false;_chargeOverride=false;
            _block = _block ?? new MaterialPropertyBlock();
            for(var i=0;i<tiers.Length;i++) tiers[i].root.SetActive(i==(int)quality);
            _active=tiers[(int)quality];
            _particleSystems=new[]{_active.particles,_active.feathers,_active.smoke,_active.burst};
            if(_active.previewParticleMaterial!=null)
            { _active.particles.GetComponent<Renderer>().sharedMaterial=_active.previewParticleMaterial;_active.burst.GetComponent<Renderer>().sharedMaterial=_active.previewParticleMaterial; }
            if(_active.lifetimeMin==null)
            {
                _active.lifetimeMin=new float[4];_active.lifetimeMax=new float[4];
                for(int i=0;i<4;i++) if(_particleSystems[i]!=null)
                {_active.lifetimeMin[i]=_particleSystems[i].main.startLifetime.constantMin;_active.lifetimeMax[i]=_particleSystems[i].main.startLifetime.constantMax;}
            }
            float longest=0;
            for(int i=0;i<4;i++) if(_particleSystems[i]!=null)
            {
                float max=Mathf.Min(_active.lifetimeMax[i],Mathf.Max(.02f,_duration-.02f)),min=Mathf.Min(_active.lifetimeMin[i],max);
                var main=_particleSystems[i].main;main.startLifetime=new ParticleSystem.MinMaxCurve(min,max);if(i<3)longest=Mathf.Max(longest,max);
            }
            _emissionStop=Mathf.Max(.005f,_duration-longest);
            foreach(var p in _active.parts) {p.transform.gameObject.SetActive(true);p.renderer=p.transform.GetComponent<Renderer>();}
            foreach(var ps in _particleSystems)
            { if(ps!=null) {ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);if(ps==_active.burst){var e=ps.emission;e.enabled=true;}ps.Simulate(0,true,true,true);} }
            Evaluate();
        }
        public void Seek(float age)
        {
            if (_active==null) return;
            float target=Mathf.Clamp(age,0,_duration);_age=0;_releasedParticles=0;_playing=true;_active.root.SetActive(true);
            foreach(var ps in _particleSystems)if(ps!=null){ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.Simulate(0,true,true,false);}
            // Replay the same explicit clock used in battle, including the gradual
            // dissolve emission. A seek must never invent a single late burst.
            int steps=Mathf.CeilToInt(target*60)+1;
            for(int i=0;i<steps && target-_age>.000001f;i++)Advance(Mathf.Min(1f/60,target-_age));
            Evaluate();
        }
        public void SetDepthFading(bool enabled)
        {
            _depthFading=enabled;
            if(_active==null)return;
            Evaluate();
            foreach(var ps in _particleSystems) if(ps!=null)
            {var r=ps.GetComponent<Renderer>();r.GetPropertyBlock(_block);_block.SetFloat("_SoftDistance",enabled&&!_battlePresentation?.12f:0);r.SetPropertyBlock(_block);}
        }
        public void SetBattlePresentation(float radius,int order,float groundProjection,MacAnhFxKind kind)
        {
            ManualClock=true;
            _battlePresentation=true;
            if(_active!=null && _active.battleParticleMaterial!=null)
            {_active.particles.GetComponent<Renderer>().sharedMaterial=_active.battleParticleMaterial;_active.burst.GetComponent<Renderer>().sharedMaterial=_active.battleParticleMaterial;}
            // The battle logic ground uses XY with a 35 degree view tilt. The
            // preview keeps the original upright XZ construction for free orbit.
            float angle=-Mathf.Asin(Mathf.Clamp(groundProjection,.05f,.95f))*Mathf.Rad2Deg;
            bool projectile=kind==MacAnhFxKind.WindSlash||kind==MacAnhFxKind.TripleFeather||kind==MacAnhFxKind.CounterSlash||kind==MacAnhFxKind.Impact;
            transform.localRotation=Quaternion.Euler(angle,0,0);
            transform.localScale=new Vector3(radius,(projectile?radius:1)/Mathf.Cos(angle*Mathf.Deg2Rad),radius);
            if(_active==null) return;
            foreach(var p in _active.parts)
                p.renderer.sortingOrder=p.kind==PartKind.Circle||p.kind==PartKind.Impact?-4990:order;
            foreach(var ps in _particleSystems) if(ps!=null)
            {var r=ps.GetComponent<Renderer>();r.sortingOrder=order+1;_block.Clear();_block.SetFloat("_SoftDistance",0);r.SetPropertyBlock(_block);}
            Evaluate();
        }
        public void SetCharge()
        {
            _chargeOverride=true;if(_active==null)return;
            foreach(var p in _active.parts) if(p.kind==PartKind.Body||p.kind==PartKind.Impact) p.transform.gameObject.SetActive(false);
            var emission=_active.burst.emission;emission.enabled=false;_active.burst.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);Evaluate();
        }
        public void Advance(float delta)
        {
            if(!_playing || _active==null || delta<=0) return;
            _age=Mathf.Min(_duration,_age+delta);
            SimulateParticles(delta,false); Evaluate();
            if(_age>=_duration) Stop();
        }
        private void SimulateParticles(float dt,bool restart)
        {
            bool emit=_age<_emissionStop;
            for(int i=0;i<3;i++)
            {
                var ps=_particleSystems[i];
                if(ps==null) continue;
                var emission=ps.emission;
                if(restart)
                {
                    emission.enabled=true;ps.Simulate(Mathf.Min(dt,_emissionStop),true,true,false);
                    if(dt>_emissionStop) {emission.enabled=false;ps.Simulate(dt-_emissionStop,true,false,false);}
                }
                else {emission.enabled=emit;ps.Simulate(dt,true,false,false);}
            }
            _active.burst.Simulate(dt,true,restart,false);
            if(!charge && !_chargeOverride && _duration>1.5f && _age>=_duration-1f)
            {
                int count=Quality==MacAnhFxQuality.Low?8:Quality==MacAnhFxQuality.Medium?16:24;
                int desired=Mathf.FloorToInt(count*Mathf.SmoothStep(0,1,Mathf.InverseLerp(_duration-1f,_duration-.4f,_age)));
                for(int i=_releasedParticles;i<desired;i++)
                {
                    float h=.2f+1.6f*((i*.618034f)%1),a=i*2.39996f,r=.18f+h*.35f;
                    var radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                    var p=new ParticleSystem.EmitParams{position=radial*r+Vector3.up*h,velocity=radial*.45f+Vector3.up*.35f,
                        startLifetime=.22f+.16f*((i*.37f)%1),startSize=.024f+.02f*((i*.43f)%1),startColor=new Color(.84f,.73f,1,1)};
                    _active.burst.Emit(p,1);
                }
                _releasedParticles=desired;
            }
        }
        private void Evaluate()
        {
            float rise=Mathf.SmoothStep(0,1,_age/Mathf.Min(riseSeconds,_duration*.18f));
            bool hold=charge||_chargeOverride;
            float tail=Mathf.Min(1f,_duration*.4f);
            float end=hold?1:Mathf.SmoothStep(0,1,(_duration-_age)/tail);
            float phase=_age;
            if(!hold && !smokeDissolve && _age>_duration-tail)
            {
                float u=Mathf.Clamp01((_age-(_duration-tail))/tail);
                phase=_duration-tail+tail*(u-u*u+u*u*u/3);
            }
            foreach(var p in _active.parts)
            {
                float fade=rise*end;
                bool drifting=smokeDissolve&&!hold&&(p.kind==PartKind.Body||p.kind==PartKind.Ribbon||p.kind==PartKind.Runes);
                if(drifting)fade=rise*Mathf.SmoothStep(0,1,(_duration-_age)/Mathf.Min(.2f,tail));
                p.transform.localRotation=Quaternion.Euler(0,phase*p.spin,0);
                if(p.kind==PartKind.Beam&&!_battlePresentation&&Camera.main!=null)p.transform.rotation=Camera.main.transform.rotation;
                Vector3 scale=p.scale;
                if(hold && p.kind==PartKind.Circle) scale*=Mathf.Lerp(1.1f,.78f,Mathf.Clamp01(_age/_duration));
                if(p.kind==PartKind.Body || p.kind==PartKind.Ribbon || p.kind==PartKind.Runes)
                    scale=Vector3.Scale(scale,new Vector3(Mathf.Lerp(.2f,1,rise)*(drifting?1:Mathf.Lerp(.04f,1,end)),Mathf.Lerp(.35f,1,rise)*(drifting?1:Mathf.Lerp(.25f,1,end)),Mathf.Lerp(.2f,1,rise)*(drifting?1:Mathf.Lerp(.04f,1,end))));
                if(p.kind==PartKind.Impact)
                { float f=Mathf.Clamp01(_age/Mathf.Min(.65f,_duration)); scale*=Mathf.Lerp(.15f,1.8f,f); fade=Mathf.Sin(f*Mathf.PI); }
                if(smokeDissolve && p.kind==PartKind.Impact)p.renderer.enabled=fade>.001f;
                p.transform.localScale=scale;
                _block.Clear(); _block.SetFloat("_Age",phase); _block.SetFloat("_Fade",fade);
                _block.SetFloat("_Dissolve",drifting?Mathf.Clamp01((_age-(_duration-tail))/tail):1-end);
                _block.SetFloat("_SoftDistance",_depthFading&&!_battlePresentation?.12f:0);
                p.renderer.SetPropertyBlock(_block);
            }
        }
        private void Update() { if(!ManualClock) Advance(Time.deltaTime); }
        public void Stop()
        {
            _playing=false;
            if(_active==null) return;
            _active.root.SetActive(false);
            foreach(var ps in _particleSystems) if(ps!=null) ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        private void OnDisable() { Stop(); }
    }
}

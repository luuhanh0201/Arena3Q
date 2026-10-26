using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arena3Q.Lobby2D
{
    // Rigid armor/weapon transforms, analytical two-bone IK, cloth-only secondary deformation.
    public sealed class LuBuCutoutPose
    {
        public readonly Dictionary<string, Matrix4x4> bones = new Dictionary<string, Matrix4x4>();
        public readonly Dictionary<string, Vector2> points = new Dictionary<string, Vector2>();
        public float time, normalized, blink, maximumReachError, rigidScaleError;
        public string reachErrorBone;
        public int clipIndex;
        private LuBuCutoutDefinition _rig;
        private LuBuCutoutPose _blendSource;
        private float _blend=1;
        private Dictionary<string,Vector2[]> _frozenFlexible;
        public void Evaluate(LuBuCutoutDefinition rig, int index, float seconds)
        {
            _rig = rig; _blendSource=null;_blend=1;_frozenFlexible=null;clipIndex = Mathf.Clamp(index, 0, rig.clips.Length - 1); time = seconds;
            var clip = rig.clips[clipIndex];
            normalized = clip.loop ? Mathf.Repeat(seconds / clip.duration, 1) : Mathf.Clamp01(seconds / clip.duration);
            var phase = normalized * Mathf.PI * 2;
            maximumReachError = 0; reachErrorBone = null; rigidScaleError = 0; bones.Clear(); points.Clear();
            var lean = clip.lean.Evaluate(normalized); var bob = clip.crouch.Evaluate(normalized);
            var shift = clip.hipShift.Evaluate(normalized);
            if (clipIndex == 0) { bob += Mathf.Sin(phase) * rig.breathHeight; lean += Mathf.Sin(phase) * rig.breathLean; }
            if (clipIndex == 1) {
                lean += rig.runLean + Mathf.Sin(phase) * rig.runCounterRotation;
                bob += rig.runCrouch + Mathf.Cos((normalized - rig.runBobPhase) * Mathf.PI * 4) * rig.runBob;
            }
            var torso = Rigid(rig.JointPoint("Pelvis"), rig.JointPoint("Pelvis") + new Vector2(shift, bob), lean);
            // Compliance at the pelvis keeps planted feet within the fixed projected leg lengths.
            foreach(var side in new[]{"R","L"}){
                var hip=XY(torso.MultiplyPoint3x4(rig.JointPoint("Hip_"+side)));
                var ankle=(clipIndex==1?RunFoot(side,normalized):rig.JointPoint("Foot_"+side))+rig.JointPoint("Ankle_"+side)-rig.JointPoint("Foot_"+side);
                var reach=Vector2.Distance(rig.JointPoint("Hip_"+side),rig.JointPoint("Knee_"+side))+Vector2.Distance(rig.JointPoint("Knee_"+side),rig.JointPoint("Ankle_"+side))-.1f;
                var dx=hip.x-ankle.x;var limit=ankle.y+Mathf.Sqrt(Mathf.Max(0,reach*reach-dx*dx));
                if(hip.y>limit)torso=Matrix4x4.Translate(Vector2.down*(hip.y-limit))*torso;
            }
            bones["Torso"] = torso;
            bones["Head"] = torso * Rigid(rig.JointPoint("Neck"), rig.JointPoint("Neck"), Mathf.Sin(phase - .35f) * rig.headLean);
            foreach (var side in new[] { "R", "L" })
            {
                var hip = XY(torso.MultiplyPoint3x4(rig.JointPoint("Hip_" + side)));
                var foot = rig.JointPoint("Foot_" + side);
                if (clipIndex == 1) foot = RunFoot(side, normalized);
                var ankleOffset = rig.JointPoint("Ankle_" + side) - rig.JointPoint("Foot_" + side);
                var ankle = foot + ankleOffset;
                SolveChain("Hip_"+side, "Knee_"+side, "Ankle_"+side, hip, ankle, "Thigh_"+side, "Shin_"+side, side == "R" ? -1 : 1);
                bones["Foot_"+side] = Rigid(rig.JointPoint("Foot_"+side), foot, 0);
                points["Foot_"+side] = foot;
            }
            var angle = rig.restWeaponAngle + lean + clip.weaponAngle.Evaluate(normalized);
            var gripBlend = Mathf.Clamp01(clip.gripBlend.Evaluate(normalized));
            var rightGrip = XY(torso.MultiplyPoint3x4(rig.JointPoint("Grip_R")));
            var leftGrip = XY(torso.MultiplyPoint3x4(rig.JointPoint("Grip_L")));
            var weaponDirection = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            if (gripBlend > 0)
            {
                var center = XY(torso.MultiplyPoint3x4(rig.attackGripCenter));
                rightGrip = Vector2.Lerp(rightGrip, center - weaponDirection * rig.gripSeparation * .5f, gripBlend);
                leftGrip = Vector2.Lerp(leftGrip, center + weaponDirection * rig.gripSeparation * .5f, gripBlend);
                if(clipIndex==2&&normalized>.75f){
                    var arc=XY(torso.MultiplyVector(Vector2.down))*Mathf.Sin((1-gripBlend)*Mathf.PI)*rig.recoveryArc;
                    rightGrip+=arc;leftGrip+=arc;
                }
            }
            if(gripBlend>0)FitGrips(torso,angle-rig.restWeaponAngle,ref rightGrip,ref leftGrip);
            var rightHand = Rigid(rig.JointPoint("Grip_R"), rightGrip, angle - rig.restWeaponAngle);
            Matrix4x4 leftHand;
            if (gripBlend > 0) leftHand = Rigid(rig.JointPoint("Grip_L"), leftGrip, angle - rig.restWeaponAngle);
            else leftHand = torso * Rigid(rig.JointPoint("Shoulder_L"), rig.JointPoint("Shoulder_L"),
                clipIndex == 1 ? Mathf.Sin(phase) * rig.runArmSwing : 0);
            bones["Hand_R"] = rightHand; bones["Hand_L"] = leftHand;
            bones["Weapon"] = Rigid(rig.JointPoint("Grip_R"), rightGrip, angle - rig.restWeaponAngle);
            foreach (var side in new[] { "R", "L" })
            {
                var shoulder = XY(torso.MultiplyPoint3x4(rig.JointPoint("Shoulder_"+side)));
                var wrist = XY(bones["Hand_"+side].MultiplyPoint3x4(rig.JointPoint("Wrist_"+side)));
                SolveChain("Shoulder_"+side, "Elbow_"+side, "Wrist_"+side, shoulder, wrist, "UpperArm_"+side, "Forearm_"+side, side == "R" ? -1 : 1);
                points["Wrist_"+side] = wrist;
            }
            var blinkPhase = Mathf.Repeat(seconds, rig.blinkPeriod) - rig.blinkTime;
            blink = clipIndex == 0 ? Blink(blinkPhase, rig) : 0;
            foreach (var matrix in bones.Values)
                rigidScaleError = Mathf.Max(rigidScaleError, Mathf.Abs(matrix.MultiplyVector(Vector3.right).magnitude - 1), Mathf.Abs(matrix.MultiplyVector(Vector3.up).magnitude - 1));
        }
        private Vector2 RunFoot(string side, float normalizedTime)
        {
            var phase = Mathf.Repeat(normalizedTime + (side == "R" ? 0 : .5f), 1);
            float x, y;
            if (phase < _rig.runStance) { x = _rig.runStride * (.5f - phase / _rig.runStance); y = 0; }
            else {
                var u = (phase - _rig.runStance) / (1 - _rig.runStance);
                var tangent=-_rig.runStride*(1-_rig.runStance)/_rig.runStance;
                x=(2*u*u*u-3*u*u+1)*(-_rig.runStride*.5f)+(u*u*u-2*u*u+u)*tangent+(-2*u*u*u+3*u*u)*(_rig.runStride*.5f)+(u*u*u-u*u)*tangent;
                y = Mathf.Pow(Mathf.Max(0,Mathf.Sin(u * Mathf.PI)),1.5f) * _rig.runLift;
            }
            return (side == "L" ? _rig.runOriginL : _rig.runOriginR) + new Vector2(x,y);
        }
        private void FitGrips(Matrix4x4 torso,float angle,ref Vector2 right,ref Vector2 left)
        {
            var rotation=Quaternion.Euler(0,0,angle);
            for(var iteration=0;iteration<24;iteration++)foreach(var side in new[]{"R","L"}){
                var shoulder=XY(torso.MultiplyPoint3x4(_rig.JointPoint("Shoulder_"+side)));
                var offset=XY(rotation*(_rig.JointPoint("Wrist_"+side)-_rig.JointPoint("Grip_"+side)));
                var grip=side=="R"?right:left;var wrist=grip+offset;
                var first=Vector2.Distance(_rig.JointPoint("Shoulder_"+side),_rig.JointPoint("Elbow_"+side));
                var second=Vector2.Distance(_rig.JointPoint("Elbow_"+side),_rig.JointPoint("Wrist_"+side));
                var reach=first+second-.1f;var inner=Mathf.Abs(first-second)+.1f;
                var delta=wrist-shoulder;
                if(delta.magnitude>reach){var correction=shoulder+delta.normalized*reach-wrist;right+=correction;left+=correction;}
                else if(delta.magnitude<inner){var correction=shoulder+delta.normalized*inner-wrist;right+=correction;left+=correction;}
            }
        }
        private void SolveChain(string a, string b, string c, Vector2 start, Vector2 target, string upper, string lower, float pole)
        {
            var ra = _rig.JointPoint(a); var rb = _rig.JointPoint(b); var rc = _rig.JointPoint(c);
            var first = Vector2.Distance(ra, rb); var second = Vector2.Distance(rb, rc);
            var delta = target - start; var rawDistance = delta.magnitude;
            var distance = Mathf.Clamp(rawDistance, Mathf.Abs(first-second)+.01f, first+second-.01f);
            var reachError=Mathf.Abs(rawDistance-distance);
            if(reachError>maximumReachError){maximumReachError=reachError;reachErrorBone=upper;}
            var direction = delta / Mathf.Max(.001f, rawDistance);
            var along = (first*first - second*second + distance*distance) / (2*distance);
            var height = Mathf.Sqrt(Mathf.Max(0,first*first-along*along));
            var elbow = start + direction * along + new Vector2(-direction.y,direction.x) * height * pole;
            var solvedTarget = start + direction * distance;
            bones[upper] = Rigid(ra,start,Angle(elbow-start)-Angle(rb-ra));
            bones[lower] = Rigid(rb,elbow,Angle(solvedTarget-elbow)-Angle(rc-rb));
            points[a] = start; points[b] = elbow; points[c] = solvedTarget;
        }
        public Vector2 Vertex(LuBuCutoutDefinition.Part part, int index)
        {
            var point=FlexibleVertex(part,index);
            return XY(bones[part.bone].MultiplyPoint3x4(point));
        }
        private Vector2 FlexibleVertex(LuBuCutoutDefinition.Part part,int index)
        {
            if(_frozenFlexible!=null)return _frozenFlexible[part.name][index];
            var point = part.vertices[index];
            if (part.flexibility > 0)
            {
                var weight = Mathf.Clamp01(Vector2.Distance(point,part.attachment)/Mathf.Max(1,part.flexLength)); weight *= weight;
                var speed = clipIndex == 0 ? Mathf.PI*2/_rig.clips[0].duration : Mathf.PI*2/_rig.clips[1].duration;
                var settle=clipIndex==2?Mathf.SmoothStep(0,1,Mathf.InverseLerp(_rig.attackSettleStart,1,normalized)):0;
                var multiplier=clipIndex==0?1:clipIndex==1?3:Mathf.Lerp(_rig.attackWindPeak,1,settle);
                if(clipIndex==2)multiplier=Mathf.Lerp(1,multiplier,Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.32f,normalized)));
                var amplitude=part.flexibility*multiplier;
                var swing=Mathf.Lerp(Mathf.Sin(time*speed-weight*1.4f+part.phase),Mathf.Sin(-weight*1.4f+part.phase),settle);
                var vertical=Mathf.Lerp(Mathf.Sin(time*speed*2+part.phase),Mathf.Sin(part.phase),settle);
                point += new Vector2(swing,vertical*.2f)*amplitude*weight;
            }
            if(_blendSource!=null)point=Vector2.Lerp(_blendSource.FlexibleVertex(part,index),point,_blend);
            return point;
        }
        public LuBuCutoutPose Snapshot()
        {
            var result=new LuBuCutoutPose{_rig=_rig,time=time,normalized=normalized,clipIndex=clipIndex,blink=blink,_frozenFlexible=new Dictionary<string,Vector2[]>()};
            foreach(var item in bones)result.bones.Add(item.Key,item.Value);
            foreach(var item in points)result.points.Add(item.Key,item.Value);
            foreach(var part in _rig.parts){var vertices=new Vector2[part.vertices.Length];for(var i=0;i<vertices.Length;i++)vertices[i]=FlexibleVertex(part,i);result._frozenFlexible.Add(part.name,vertices);}
            return result;
        }
        public void BlendFrom(LuBuCutoutPose previous,float blend)
        {
            _blendSource=previous;_blend=Mathf.Clamp01(blend);
            var torso=BlendRigid(previous.bones["Torso"],bones["Torso"],_rig.JointPoint("Pelvis"),_blend);
            var oldHeadAngle=MatrixAngle(previous.bones["Head"])-MatrixAngle(previous.bones["Torso"]);
            var nextHeadAngle=MatrixAngle(bones["Head"])-MatrixAngle(bones["Torso"]);
            var feet=new Dictionary<string,Vector2>();
            foreach(var side in new[]{"R","L"}){
                feet[side]=Vector2.Lerp(previous.points["Foot_"+side],points["Foot_"+side],_blend);
                var hip=XY(torso.MultiplyPoint3x4(_rig.JointPoint("Hip_"+side)));
                var ankle=feet[side]+_rig.JointPoint("Ankle_"+side)-_rig.JointPoint("Foot_"+side);
                var reach=Vector2.Distance(_rig.JointPoint("Hip_"+side),_rig.JointPoint("Knee_"+side))+Vector2.Distance(_rig.JointPoint("Knee_"+side),_rig.JointPoint("Ankle_"+side))-.1f;
                var dx=hip.x-ankle.x;var limit=ankle.y+Mathf.Sqrt(Mathf.Max(0,reach*reach-dx*dx));
                if(hip.y>limit)torso=Matrix4x4.Translate(Vector2.down*(hip.y-limit))*torso;
            }
            bones["Torso"]=torso;bones["Head"]=torso*Rigid(_rig.JointPoint("Neck"),_rig.JointPoint("Neck"),Mathf.LerpAngle(oldHeadAngle,nextHeadAngle,_blend));
            foreach(var side in new[]{"R","L"}){
                var hip=XY(torso.MultiplyPoint3x4(_rig.JointPoint("Hip_"+side)));
                var ankle=feet[side]+_rig.JointPoint("Ankle_"+side)-_rig.JointPoint("Foot_"+side);
                SolveChain("Hip_"+side,"Knee_"+side,"Ankle_"+side,hip,ankle,"Thigh_"+side,"Shin_"+side,side=="R"?-1:1);
                bones["Foot_"+side]=Rigid(_rig.JointPoint("Foot_"+side),feet[side],0);points["Foot_"+side]=feet[side];
                var restGrip=_rig.JointPoint("Grip_"+side);
                var hand=BlendRigid(previous.bones["Hand_"+side],bones["Hand_"+side],restGrip,_blend);
                var shoulder=XY(torso.MultiplyPoint3x4(_rig.JointPoint("Shoulder_"+side)));
                var wrist=XY(hand.MultiplyPoint3x4(_rig.JointPoint("Wrist_"+side)));
                var first=Vector2.Distance(_rig.JointPoint("Shoulder_"+side),_rig.JointPoint("Elbow_"+side));
                var second=Vector2.Distance(_rig.JointPoint("Elbow_"+side),_rig.JointPoint("Wrist_"+side));
                var delta=wrist-shoulder;var radius=Mathf.Clamp(delta.magnitude,Mathf.Abs(first-second)+.1f,first+second-.1f);
                var corrected=shoulder+delta.normalized*radius;
                hand=Matrix4x4.Translate(corrected-wrist)*hand;bones["Hand_"+side]=hand;
                SolveChain("Shoulder_"+side,"Elbow_"+side,"Wrist_"+side,shoulder,corrected,"UpperArm_"+side,"Forearm_"+side,side=="R"?-1:1);
            }
            bones["Weapon"]=bones["Hand_R"];blink=Mathf.Lerp(previous.blink,blink,_blend);
        }
        private static float MatrixAngle(Matrix4x4 matrix)=>Angle(matrix.MultiplyVector(Vector3.right));
        private static Matrix4x4 BlendRigid(Matrix4x4 from,Matrix4x4 to,Vector2 pivot,float blend)=>
            Rigid(pivot,Vector2.Lerp(XY(from.MultiplyPoint3x4(pivot)),XY(to.MultiplyPoint3x4(pivot)),blend),Mathf.LerpAngle(MatrixAngle(from),MatrixAngle(to),blend));
        private static float Blink(float t, LuBuCutoutDefinition r)
        {
            if(t<0||t>r.blinkClose+r.blinkHold+r.blinkOpen)return 0;
            if(t<r.blinkClose)return t/r.blinkClose;
            if(t<r.blinkClose+r.blinkHold)return 1;
            return 1-(t-r.blinkClose-r.blinkHold)/r.blinkOpen;
        }
        private static float Angle(Vector2 direction) => Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg;
        private static Vector2 XY(Vector3 point) => new Vector2(point.x,point.y);
        private static Matrix4x4 Rigid(Vector2 rest, Vector2 current, float degrees) =>
            Matrix4x4.Translate(current) * Matrix4x4.Rotate(Quaternion.Euler(0,0,degrees)) * Matrix4x4.Translate(-rest);
    }
}

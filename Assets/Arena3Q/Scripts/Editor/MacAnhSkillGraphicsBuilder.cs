using System;
using System.Linq;
using System.Collections.Generic;
using Arena3Q.Data;
using Arena3Q.Gameplay;
using Arena3Q.Graphics3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Arena3Q.Editor
{
    // New sibling assets only. The original nine prototype prefabs are retained.
    public static class MacAnhSkillGraphicsBuilder
    {
        const string Root=MacAnhGraphicsBuilder.Root;
        static T Save<T>(T asset,string name) where T:UnityEngine.Object
        {
            string path=Root+"/"+name;var previous=AssetDatabase.LoadAssetAtPath<T>(path);
            if(previous==null) {AssetDatabase.CreateAsset(asset,path);return asset;}
            EditorUtility.CopySerialized(asset,previous);UnityEngine.Object.DestroyImmediate(asset);return previous;
        }
        static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+name+".mat");
        static Mesh Arc(string name,int count,float radius,float width)
        {
            var g=new MacAnhArcaneGeometry();
            for(int s=0;s<=count;s++)
            {
                float t=(float)s/count,a=Mathf.Lerp(-2.08f,1.74f,t);
                float w=width*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.65f)*(1.12f-.3f*t+.12f*Mathf.Sin(t*9));
                for(int e=0;e<3;e++)
                {
                    float f=e*.5f,r=radius*(1+.055f*Mathf.Sin(a*1.7f))+(f-.5f)*w;
                    g.Vertex(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,Mathf.Sin(a*1.6f)*.085f+Mathf.Sin(f*Mathf.PI)*.025f),new Vector2(t,f),Color.white);
                }
                if(s>0) for(int e=0;e<2;e++) {int n=s*3+e;g.Quad(n-3,n-2,n+1,n);}
            }
            return g.Finish(name);
        }
        static Mesh Tube(string name,int sides,float radius,float height)
        {
            var g=new MacAnhArcaneGeometry();int rows=8;
            for(int y=0;y<=rows;y++) for(int s=0;s<=sides;s++)
            {
                float t=(float)y/rows,u=(float)s/sides,a=u*Mathf.PI*2;
                float r=radius*(.85f+.15f*Mathf.Sin(t*Mathf.PI))*(1+.12f*Mathf.Sin(a*3+t*5));
                float crown=1+.075f*Mathf.Sin(a+height)+.05f*Mathf.Sin(a*3-height);
                g.Vertex(new Vector3(Mathf.Cos(a)*r+.06f*t*t,t*height*crown,Mathf.Sin(a)*r-.035f*t),new Vector2(u,t),Color.white);
                if(y>0&&s>0) {int n=y*(sides+1)+s;g.Quad(n-sides-2,n-sides-1,n,n-1);}
            }
            return g.Finish(name);
        }
        static Mesh Sphere(string name,int sides)
        {
            var g=new MacAnhArcaneGeometry();int rows=sides/2;
            for(int y=0;y<=rows;y++) for(int s=0;s<=sides;s++)
            {
                float t=(float)y/rows,u=(float)s/sides,a=u*Mathf.PI*2,b=t*Mathf.PI;
                g.Vertex(new Vector3(Mathf.Cos(a)*Mathf.Sin(b)*.73f,.86f-Mathf.Cos(b)*.86f,Mathf.Sin(a)*Mathf.Sin(b)*.73f),new Vector2(u,t),Color.white);
                if(y>0&&s>0) {int n=y*(sides+1)+s;g.Quad(n-sides-2,n-sides-1,n,n-1);}
            }
            return g.Finish(name);
        }
        static Mesh TransformMesh(Mesh mesh,Vector3 position,Vector3 rotation,Vector3 scale)
        {
            var matrix=Matrix4x4.TRS(position,Quaternion.Euler(rotation),scale);
            mesh.vertices=mesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static Mesh Delayed(Mesh mesh,float seconds)
        {mesh.SetUVs(1,Enumerable.Repeat(new Vector4(seconds,0,0,0),mesh.vertexCount).ToList());return mesh;}
        static Mesh Dome(string name,int sides)
        {
            var g=new MacAnhArcaneGeometry();int rows=sides/2;
            for(int y=0;y<=rows;y++)for(int s=0;s<=sides;s++)
            {
                float t=(float)y/rows,u=(float)s/sides,a=u*Mathf.PI*2,b=t*Mathf.PI*.5f;
                g.Vertex(new Vector3(Mathf.Cos(a)*Mathf.Sin(b)*1.05f,Mathf.Cos(b)*1.85f,Mathf.Sin(a)*Mathf.Sin(b)*1.05f),new Vector2(u,t),Color.white);
                if(y>0&&s>0){int n=y*(sides+1)+s;g.Quad(n-sides-2,n-sides-1,n,n-1);}
            }
            return g.Finish(name);
        }
        public static void Build()
        {
            MacAnhEnergyAssets.Build();
            BuildOne("Attack01");BuildOne("Attack02");BuildOne("Skill01");BuildOne("Skill03");BuildOne("Ultimate");BuildOne("DodgeAttack");
            BuildOne("Prepare");BuildOne("ArrayPrepare");BuildOne("UltimatePrepare");BuildOne("Impact");BuildGhost();AssetDatabase.SaveAssets();
        }
        static void BuildOne(string style)
        {
            var go=new GameObject("FX_MacAnh_"+style+"3D");var fx=go.AddComponent<MacAnhArcaneEffect>();fx.ConfigureCharge(style.Contains("Prepare"));var tiers=new MacAnhArcaneEffect.Tier[3];
            if(style=="Attack01"||style=="Attack02"||style=="DodgeAttack")fx.ConfigureRise(.065f);
            for(int q=0;q<3;q++)
            {
                string suffix=style+"_"+q;var root=new GameObject(((MacAnhFxQuality)q).ToString());root.transform.SetParent(go.transform,false);
                var parts=new List<MacAnhArcaneEffect.Part>();
                Action<string,Mesh,Material,MacAnhArcaneEffect.PartKind,float,Vector3> add=(name,mesh,material,kind,spin,pos)=>{
                    mesh=Save(mesh,"Meshes/"+mesh.name+".asset");var part=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));part.transform.SetParent(root.transform,false);part.transform.localPosition=pos;
                    part.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=part.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                    parts.Add(new MacAnhArcaneEffect.Part{transform=part.transform,kind=kind,spin=spin,scale=Vector3.one});};
                int segments=q==0?32:q==1?48:64;
                if(style=="Attack01"||style=="Attack02"||style=="DodgeAttack")
                {
                    float radius=style=="DodgeAttack"?.5f:.95f;
                    if(style=="Attack02")
                    {
                        add("Feather shadow contour",TransformMesh(MacAnhArcaneGeometry.Feather("FeatherShadow"+suffix),Vector3.zero,new Vector3(0,0,-90),new Vector3(5.5f,8.7f,1.8f)),Mat("BladeShadow"),MacAnhArcaneEffect.PartKind.Ribbon,0,new Vector3(-.015f,0,.035f));
                        add("Feather energy blade",TransformMesh(MacAnhArcaneGeometry.Feather("FeatherBlade"+suffix),Vector3.zero,new Vector3(0,0,-90),new Vector3(4.4f,8.5f,1.8f)),Mat("CycloneSilk"),MacAnhArcaneEffect.PartKind.Ribbon,0,Vector3.zero);
                        add("Feather luminous spine",TransformMesh(MacAnhArcaneGeometry.Feather("FeatherSpine"+suffix),Vector3.zero,new Vector3(0,0,-90),new Vector3(.8f,8.4f,1.8f)),Mat("CycloneCore"),MacAnhArcaneEffect.PartKind.Ribbon,0,new Vector3(0,0,-.025f));
                    }
                    else
                    {
                        add("Crescent shadow contour",Arc("CrescentShadow"+suffix,segments,radius,.36f),Mat("BladeShadow"),MacAnhArcaneEffect.PartKind.Ribbon,0,new Vector3(0,0,.035f));
                        add("Crescent energy body",Arc("Crescent"+suffix,segments,radius,.26f),Mat("CycloneSilk"),MacAnhArcaneEffect.PartKind.Ribbon,0,Vector3.zero);
                        add("Lavender core",Arc("CrescentCore"+suffix,segments,radius+.015f,.085f),Mat("CycloneCore"),MacAnhArcaneEffect.PartKind.Ribbon,0,new Vector3(.015f,0,-.025f));
                    }
                    if(q>0) add("Secondary wind curl",TransformMesh(Arc("CrescentTail"+suffix,segments,.68f,.18f),new Vector3(-.25f,-.12f,.12f),new Vector3(8,-12,12),Vector3.one),Mat("CycloneSilk"),MacAnhArcaneEffect.PartKind.Ribbon,0,Vector3.zero);
                }
                else if(style=="Skill03")
                {
                    var shield=new Material(Mat("CycloneInner")){name="ProtectiveHexShield"};shield.SetFloat("_Mode",3);shield.SetFloat("_Opacity",.5f);shield.SetFloat("_Emission",3.8f);
                    var shieldMat=Save(shield,"Materials/ProtectiveHexShield.mat");
                    add("Translucent hexagonal shield",Dome("Shield"+suffix,segments),shieldMat,MacAnhArcaneEffect.PartKind.Body,12,Vector3.zero);
                    add("Protective ground seal",MacAnhArcaneGeometry.Circle("ShieldSeal"+suffix,1.08f),Mat("ArrayGold"),MacAnhArcaneEffect.PartKind.Circle,18,Vector3.up*.025f);
                    add("Orbiting guardian seals",MacAnhArcaneGeometry.Runes("ShieldRunes"+suffix,q==0?6:12),Mat("RunesGold"),MacAnhArcaneEffect.PartKind.Runes,-24,Vector3.zero);
                }
                else
                {
                    bool prepare=style=="Prepare",ult=style=="Ultimate"||style=="UltimatePrepare",charge=style.Contains("Prepare"),impact=style=="Impact";
                    float radius=prepare?.22f:ult?1.5f:impact?.6f:1.1f;
                    add("Formation / shadow",MacAnhArcaneGeometry.Circle("FormationShadow"+suffix,radius*1.015f),Mat("FormationShadow"),MacAnhArcaneEffect.PartKind.Circle,25,Vector3.up*.018f);
                    add("Formation / gold",MacAnhArcaneGeometry.Circle("Formation"+suffix,radius),Mat(ult?"AerialGold":"ArrayGold"),impact?MacAnhArcaneEffect.PartKind.Impact:MacAnhArcaneEffect.PartKind.Circle,25,Vector3.up*.025f);
                    add("Formation / violet",MacAnhArcaneGeometry.Circle("FormationViolet"+suffix,radius*.86f),Mat(ult?"AerialViolet":"ArrayViolet"),MacAnhArcaneEffect.PartKind.Circle,-38,Vector3.up*.04f);
                    if(!prepare&&!impact)
                    {
                        add("Raised floating runes",MacAnhArcaneGeometry.Runes("RaisedRunes"+suffix,q==0?8:16),Mat("RunesGold"),MacAnhArcaneEffect.PartKind.Runes,35,Vector3.zero);
                        int columns=ult?(q==0?4:q==1?6:9):(q==0?3:q==1?5:7);
                        if(!charge) for(int k=0;k<columns;k++)
                        {
                            float a=k*2.399963f+.21f;float spread=.28f+.24f*((k*.618034f+.31f)%1);
                            var pos=new Vector3(Mathf.Cos(a)*radius*spread,0,Mathf.Sin(a)*radius*spread);
                            float energy=.82f+.25f*((k*.414214f+.27f)%1);
                            float delay=(ult?.08f:.3f)+.3f*((k*.618034f+.17f)%1);
                            add("Energy column aura "+k,Delayed(Tube("ColumnAura"+suffix+"_"+k,segments,.14f*energy,(ult?3.15f:1.9f)*energy),delay),Mat("BeamAura"),MacAnhArcaneEffect.PartKind.Body,23+k*7,pos);
                            add("Descending luminous core "+k,Delayed(Tube("ColumnCore"+suffix+"_"+k,Mathf.Max(12,segments/2),.045f*energy,(ult?3.15f:1.9f)*energy),delay),Mat("BeamCore"),MacAnhArcaneEffect.PartKind.Body,23+k*7,pos);
                            add("Column ground impact "+k,MacAnhArcaneGeometry.Circle("ColumnImpact"+suffix+"_"+k,.23f*energy),Mat("ArrayViolet"),MacAnhArcaneEffect.PartKind.Impact,0,pos+Vector3.up*.04f);
                        }
                        if(ult)
                        {
                            add("Aerial upper formation",TransformMesh(MacAnhArcaneGeometry.Circle("AerialSeal"+suffix,1.3f),Vector3.zero,new Vector3(7,0,-5),Vector3.one),Mat("AerialGold"),MacAnhArcaneEffect.PartKind.Circle,-35,Vector3.up*2.65f);
                            if(q>0) add("Aerial counter formation",MacAnhArcaneGeometry.Circle("AerialSealInner"+suffix,1.02f),Mat("AerialViolet"),MacAnhArcaneEffect.PartKind.Circle,45,Vector3.up*2.42f);
                        }
                        if(!charge) add("Activation shockwave",MacAnhArcaneGeometry.Circle("Activation"+suffix,radius),Mat("CircleViolet"),MacAnhArcaneEffect.PartKind.Impact,0,Vector3.up*.035f);
                    }
                    if(prepare||impact) add(impact?"Collision flash":"Energy gathering",TransformMesh(Sphere("Gathering"+suffix,segments),Vector3.zero,Vector3.zero,Vector3.one*(impact?.25f:.16f)),Mat("CycloneCore"),impact?MacAnhArcaneEffect.PartKind.Impact:MacAnhArcaneEffect.PartKind.Body,80,Vector3.zero);
                }
                var feathers=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/Feather_"+q+".asset");
                int count=style.Contains("Prepare")?6:style=="Ultimate"?20:12;count*=q+1;
                tiers[q]=new MacAnhArcaneEffect.Tier{root=root,parts=parts.ToArray(),previewParticleMaterial=Mat("RisingMotesGraph"),battleParticleMaterial=Mat("RisingMotesBattle"),
                    particles=MacAnhGraphicsBuilder.Particles(root.transform,"Rising motes",Mat("RisingMotesGraph"),count,null,false),
                    feathers=MacAnhGraphicsBuilder.Particles(root.transform,"Feathers",Mat("MagicFeathers"),Mathf.Max(2,count/5),feathers,false),
                    smoke=MacAnhGraphicsBuilder.Particles(root.transform,"Wind wisps",Mat("RollingWind"),style.Contains("Attack")?4:0,null,false,true),
                    burst=MacAnhGraphicsBuilder.Particles(root.transform,"Activation impact",Mat("RisingMotesGraph"),style.Contains("Prepare")?0:count,null,true)};
                if(style.Contains("Prepare")||style.Contains("Attack")) foreach(var ps in new[]{tiers[q].particles,tiers[q].feathers,tiers[q].smoke})
                {var main=ps.main;main.startLifetime=new ParticleSystem.MinMaxCurve(.18f,.4f);}
                root.SetActive(false);
            }
            fx.Configure(tiers);PrefabUtility.SaveAsPrefabAsset(go,Root+"/"+go.name+".prefab");UnityEngine.Object.DestroyImmediate(go);
        }
        static void BuildGhost()
        {
            var profile=AssetDatabase.LoadAssetAtPath<MacAnhPerformance>("Assets/Arena3Q/Art/Heroes/MacAnh/MacAnhPerformance.asset");
            var clip=profile.FindSprite(MacAnhAction.DodgeAttack);var sprite=clip.frames[clip.Index(6,10/30f)];
            var material=new Material(Mat("MagicFeathers")){name="AfterimageSilhouette"};material.SetTexture("_MainTex",sprite.texture);material.SetColor("_Color",new Color(.67f,.48f,1,1));material.SetFloat("_Opacity",.5f);material.SetFloat("_Emission",2.6f);material.SetFloat("_DstBlend",(float)BlendMode.One);
            material=Save(material,"Materials/AfterimageSilhouette.mat");
            var mesh=new Mesh{name="AfterimageQuad"};mesh.vertices=sprite.vertices.Select(v=>new Vector3(v.x,v.y,0)).ToArray();mesh.uv=sprite.uv;mesh.triangles=sprite.triangles.Select(v=>(int)v).ToArray();mesh.colors=Enumerable.Repeat(Color.white,mesh.vertexCount).ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();mesh=Save(mesh,"Meshes/AfterimageQuad.asset");
            var go=new GameObject("FX_MacAnh_Afterimage3D");var fx=go.AddComponent<MacAnhArcaneEffect>();var tiers=new MacAnhArcaneEffect.Tier[3];
            for(int q=0;q<3;q++)
            {
                var root=new GameObject(((MacAnhFxQuality)q).ToString());root.transform.SetParent(go.transform,false);
                var ghost=new GameObject("Captured pose silhouette",typeof(MeshFilter),typeof(MeshRenderer));ghost.transform.SetParent(root.transform,false);ghost.GetComponent<MeshFilter>().sharedMesh=mesh;ghost.GetComponent<MeshRenderer>().sharedMaterial=material;ghost.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                tiers[q]=new MacAnhArcaneEffect.Tier{root=root,parts=new[]{new MacAnhArcaneEffect.Part{transform=ghost.transform,kind=MacAnhArcaneEffect.PartKind.Beam,scale=Vector3.one}},
                    particles=MacAnhGraphicsBuilder.Particles(root.transform,"Motes",Mat("RisingMotesGraph"),0,null,false),feathers=MacAnhGraphicsBuilder.Particles(root.transform,"Feathers",Mat("MagicFeathers"),0,null,false),smoke=MacAnhGraphicsBuilder.Particles(root.transform,"Wisps",Mat("RollingWind"),0,null,false),burst=MacAnhGraphicsBuilder.Particles(root.transform,"Burst",Mat("RisingMotesGraph"),0,null,true)};
                root.SetActive(false);
            }
            fx.Configure(tiers);PrefabUtility.SaveAsPrefabAsset(go,Root+"/"+go.name+".prefab");UnityEngine.Object.DestroyImmediate(go);
        }
    }
}

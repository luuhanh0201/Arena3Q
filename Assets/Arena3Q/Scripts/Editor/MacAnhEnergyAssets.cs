using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Arena3Q.Editor
{
    /// <summary>Readable cores and distinct formations for the battlefield palette.</summary>
    internal static class MacAnhEnergyAssets
    {
        const string Root=MacAnhGraphicsBuilder.Root;
        static T Save<T>(T value,string path) where T:UnityEngine.Object
        {
            var previous=AssetDatabase.LoadAssetAtPath<T>(Root+"/"+path);
            if(previous==null){AssetDatabase.CreateAsset(value,Root+"/"+path);return value;}
            EditorUtility.CopySerialized(value,previous);UnityEngine.Object.DestroyImmediate(value);return previous;
        }
        static Color Tint(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var color);return color.linear;}
        static Material Energy(string name,string color,float emission,float opacity,int mode,bool additive,Texture mask=null)
        {
            var material=new Material(Shader.Find("Arena3Q/MacAnh/Arcane Flow 3D")){name=name};
            material.SetColor("_Color",Tint(color));material.SetFloat("_Emission",emission);material.SetFloat("_Opacity",opacity);
            material.SetFloat("_Mode",mode);material.SetFloat("_DstBlend",(float)(additive?BlendMode.One:BlendMode.OneMinusSrcAlpha));
            if(name=="BladeShadow")material.SetFloat("_CoreHighlight",0);
            material.SetTexture("_NoiseTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/ArcaneNoise.asset"));
            material.SetTexture("_CloudTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/CycloneWindDensity.png"));
            if(mask!=null)material.SetTexture("_MainTex",mask);
            return Save(material,"Materials/"+name+".mat");
        }
        static float Line(Vector2 p,Vector2 a,Vector2 b)
        {var delta=b-a;return Vector2.Distance(p,a+delta*Mathf.Clamp01(Vector2.Dot(p-a,delta)/Mathf.Max(.000001f,delta.sqrMagnitude)));}
        static Vector2 Polar(float angle,float radius)=>new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
        static Texture2D Seal(string name,bool aerial)
        {
            const int size=512;var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                var p=new Vector2((x+.5f)/size*2-1,(y+.5f)/size*2-1);float r=p.magnitude,a=Mathf.Atan2(p.y,p.x);
                float d=Mathf.Min(Mathf.Abs(r-.91f),Mathf.Abs(r-(aerial?.76f:.59f)));
                if(aerial)
                {
                    // Unequal feather arrows point inward from the aerial ring.
                    for(int k=0;k<11;k++)
                    {
                        float angle=k*2.399963f,radius=.77f+.05f*Mathf.Sin(k*1.7f);
                        var tip=Polar(angle,.28f+.045f*Mathf.Sin(k*2.1f));var outer=Polar(angle,radius);
                        d=Mathf.Min(d,Line(p,tip,outer));
                        d=Mathf.Min(d,Line(p,tip,Polar(angle+.08f,.49f)));
                        d=Mathf.Min(d,Line(p,tip,Polar(angle-.08f,.49f)));
                    }
                }
                else
                {
                    d=Mathf.Min(d,Mathf.Abs(r-.42f));
                    for(int k=0;k<9;k++)
                    {
                        float angle=k*2.399963f+.13f;
                        d=Mathf.Min(d,Line(p,Polar(angle,.64f),Polar(angle,.83f+.035f*Mathf.Sin(k*2.1f))));
                        var middle=Polar(angle,.74f);var tangent=Polar(angle+Mathf.PI*.5f,.045f);
                        d=Mathf.Min(d,Line(p,middle-tangent,middle+tangent));
                    }
                }
                float t=Mathf.InverseLerp(.007f,.022f,d);float alpha=1-Mathf.SmoothStep(0,1,t);
                float halo=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.02f,.049f,d));
                float gaps=.62f+.38f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.6f,.6f,Mathf.Sin(a*3+.5f)*Mathf.Cos(a*5)));
                pixels[y*size+x]=new Color(1,1,1,Mathf.Clamp01(alpha+halo*.14f)*gaps);
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,true,true){name=name,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels(pixels);texture.Apply();return Save(texture,"Textures/"+name+".asset");
        }
        public static void Build()
        {
            Energy("CycloneSilk","9657EF",2.8f,.72f,1,true);
            Energy("CycloneCore","F3ECFF",6.5f,.95f,4,true);
            Energy("BladeShadow","27113D",.18f,.75f,4,false);
            Energy("BeamCore","F3ECFF",7.5f,.95f,5,true);
            Energy("BeamAura","9657EF",4,.3f,5,true);
            var array=Seal("ArrayRadiatingSeal",false);var aerial=Seal("AerialFeatherSeal",true);
            Energy("ArrayGold","E6C47C",1.7f,.95f,2,true,array);
            Energy("ArrayViolet","D6B9FF",1.2f,.65f,2,true,array);
            Energy("AerialGold","E6C47C",1.8f,.95f,2,true,aerial);
            Energy("AerialViolet","D6B9FF",1.4f,.7f,2,true,aerial);
            Energy("FormationShadow","27113D",.6f,.65f,2,false,array);
            Energy("RunesGold","E6C47C",3.2f,.95f,2,true,AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/FloatingRune.asset"));
        }
    }
}

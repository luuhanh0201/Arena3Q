using UnityEditor;
using UnityEngine;

namespace Arena3Q.Editor
{
    internal static class MacAnhVolumeTexture
    {
        public static Texture2D BuildFlow()
        {
            const string path=MacAnhGraphicsBuilder.Root+"/Textures/CycloneFlowNoise.asset";
            var existing=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(existing!=null)return existing;
            const int size=128;var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                var p=new Vector3((float)x/size,(float)y/size,.37f);
                float n=Noise(p*4,4,17)*.7f+Noise(p*8,8,71)*.3f;
                pixels[y*size+x]=new Color(n,n,n,1);
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,true,true){name="CycloneFlowNoise",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear};
            texture.SetPixels32(pixels);texture.Apply(true,true);AssetDatabase.CreateAsset(texture,path);return texture;
        }
        // Tileable value-noise octaves, without the old analytic sine stripes.
        public static Texture3D BuildCloud()
        {
            const string path=MacAnhGraphicsBuilder.Root+"/Textures/CycloneCloud3D.asset";
            var existing=AssetDatabase.LoadAssetAtPath<Texture3D>(path);if(existing!=null)return existing;
            const int size=64;var pixels=new Color32[size*size*size];
            for(int z=0;z<size;z++)for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                var p=new Vector3(x,y,z)/size;
                float a=Noise(p*4,4,17),b=Noise(p*8,8,71),c=Noise(p*16,16,139),d=Noise(p*4,4,283);
                pixels[(z*size+y)*size+x]=new Color(a*.6f+b*.3f+c*.1f,d*.7f+b*.3f,c,1);
            }
            var texture=new Texture3D(size,size,size,TextureFormat.RGBA32,true){name="CycloneCloud3D",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear};
            texture.SetPixels32(pixels);texture.Apply(true,true);AssetDatabase.CreateAsset(texture,path);return texture;
        }
        static float Hash(int x,int y,int z,int period,uint seed)
        {
            unchecked
            {
                uint n=(uint)(x%period)*73856093u^(uint)(y%period)*19349663u^(uint)(z%period)*83492791u^seed;
                n^=n>>16;n*=0x7feb352du;n^=n>>15;n*=0x846ca68bu;n^=n>>16;return(n&0x00ffffff)/16777215f;
            }
        }
        static float Noise(Vector3 p,int period,uint seed)
        {
            int x=Mathf.FloorToInt(p.x),y=Mathf.FloorToInt(p.y),z=Mathf.FloorToInt(p.z);
            var f=new Vector3(p.x-x,p.y-y,p.z-z);f=new Vector3(f.x*f.x*(3-2*f.x),f.y*f.y*(3-2*f.y),f.z*f.z*(3-2*f.z));
            float lo=Mathf.Lerp(Mathf.Lerp(Hash(x,y,z,period,seed),Hash(x+1,y,z,period,seed),f.x),Mathf.Lerp(Hash(x,y+1,z,period,seed),Hash(x+1,y+1,z,period,seed),f.x),f.y);
            float hi=Mathf.Lerp(Mathf.Lerp(Hash(x,y,z+1,period,seed),Hash(x+1,y,z+1,period,seed),f.x),Mathf.Lerp(Hash(x,y+1,z+1,period,seed),Hash(x+1,y+1,z+1,period,seed),f.x),f.y);
            return Mathf.Lerp(lo,hi,f.z);
        }
        public static Texture3D Build()
        {
            const string path=MacAnhGraphicsBuilder.Root+"/Textures/TurbulentWind3D.asset";
            var existing=AssetDatabase.LoadAssetAtPath<Texture3D>(path);if(existing!=null) return existing;
            const int size=64;var pixels=new Color32[size*size*size];
            for(int z=0;z<size;z++) for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                float a=x*Mathf.PI*2/size,b=y*Mathf.PI*2/size,c=z*Mathf.PI*2/size;
                float main=a*2+b*3+Mathf.Sin(c*2),secondary=c*3-b*2+Mathf.Cos(a*3);
                float n=.5f+.17f*Mathf.Sin(main)+.13f*Mathf.Sin(secondary)+.09f*Mathf.Sin(a*7+b*5+c*3)+.055f*Mathf.Sin(a*13-b*9+c*7);
                float light=.5f+.27f*Mathf.Sin(main+.9f)+.17f*Mathf.Sin(secondary+.55f);
                float fine=.5f+.25f*Mathf.Sin(a*5-b*7+c*9)+.15f*Mathf.Cos(c*13+a*8);
                pixels[(z*size+y)*size+x]=new Color(Mathf.Clamp01(n),Mathf.Clamp01(light),Mathf.Clamp01(fine),1);
            }
            var texture=new Texture3D(size,size,size,TextureFormat.RGBA32,true){name="TurbulentWind3D",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear};
            texture.SetPixels32(pixels);texture.Apply(true,true);AssetDatabase.CreateAsset(texture,path);return texture;
        }
    }
}

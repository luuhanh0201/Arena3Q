using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arena3Q.Editor
{
    internal sealed class MacAnhArcaneGeometry
    {
        private readonly List<Vector3> v=new List<Vector3>();
        private readonly List<Vector2> uv=new List<Vector2>();
        private readonly List<Color> colors=new List<Color>();
        private readonly List<int> indices=new List<int>();
        public int Vertex(Vector3 p,Vector2 t,Color c) { v.Add(p); uv.Add(t); colors.Add(c); return v.Count-1; }
        public void Quad(int a,int b,int c,int d) { indices.AddRange(new[]{a,b,c,a,c,d}); }
        public Mesh Finish(string name)
        {
            var m=new Mesh { name=name }; m.SetVertices(v); m.SetUVs(0,uv); m.SetColors(colors); m.SetTriangles(indices,0);
            m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
        public static float Radius(float t) => .16f+.94f*Mathf.Pow(Mathf.Clamp01(t),.72f);
        private static Vector3 Point(float a,float t,float radiusScale=1)
        {
            float r=Radius(t)*radiusScale*(1+.075f*Mathf.Sin(a*3+t*7)+.055f*Mathf.Sin(a*2-t*13));
            return new Vector3(Mathf.Cos(a)*r,t*2.12f+.065f,Mathf.Sin(a)*r);
        }
        public static Mesh Funnel(string name,int sides,int rows,float radiusScale=1)
        {
            var g=new MacAnhArcaneGeometry();
            for(int y=0;y<=rows;y++) for(int s=0;s<=sides;s++)
            {
                float t=(float)y/rows, u=(float)s/sides;
                g.Vertex(Point(u*Mathf.PI*2,t,radiusScale),new Vector2(u,t),Color.white);
                if(y>0 && s>0) { int n=y*(sides+1)+s; g.Quad(n-sides-2,n-sides-1,n,n-1); }
            }
            return g.Finish(name);
        }
        public static Mesh VolumeBounds(string name)
        {
            // The shader defines the funnel density. Its raster cage only needs
            // to be watertight, even under the battle's nonuniform projection.
            var mesh=UnityEngine.Object.Instantiate(Resources.GetBuiltinResource<Mesh>("Cube.fbx"));mesh.name=name;
            var vertices=mesh.vertices;
            for(int i=0;i<vertices.Length;i++)vertices[i]=Vector3.Scale(vertices[i],new Vector3(3.5f,3f,3.5f))+Vector3.up*1.4f;
            mesh.vertices=vertices;var triangles=mesh.triangles;
            for(int i=0;i<triangles.Length;i+=3){int t=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=t;}
            mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public static Mesh Ribbons(string name,int segments,int count,float width,float turns,float radiusScale=1)
        {
            var g=new MacAnhArcaneGeometry();
            for(int band=0;band<count;band++)
            {
                int start=g.v.Count;
                for(int s=0;s<=segments;s++)
                {
                    float t=(float)s/segments;
                    float a=t*Mathf.PI*2*turns+band*Mathf.PI*2/count;
                    float w=width*(.3f+.7f*Mathf.Sin(t*Mathf.PI))*(.75f+.25f*Mathf.Sin(t*18+band));
                    var centre=Point(a,t,radiusScale);
                    // Broad vertical cross section follows the curved funnel; every band
                    // has real front/back displacement, not a camera-facing LineRenderer.
                    var tangent=(Point(a+.01f,Mathf.Min(1,t+.002f),radiusScale)-centre).normalized;
                    var radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                    var across=Vector3.Cross(tangent,radial).normalized;
                    for(int edge=0;edge<3;edge++)
                    {
                        float z=edge*.5f;
                        var color=Color.Lerp(new Color(.58f,.34f,.94f),new Color(.84f,.73f,1),t*.6f);
                        g.Vertex(centre+across*((z-.5f)*w)+radial*(Mathf.Sin(z*Mathf.PI)*.035f),new Vector2(t,z),color);
                    }
                    if(s>0) for(int edge=0;edge<2;edge++) {int n=start+s*3+edge; g.Quad(n-3,n-2,n+1,n);}
                }
            }
            return g.Finish(name);
        }
        public static Mesh Circle(string name,float radius)
        {
            var g=new MacAnhArcaneGeometry();
            g.Vertex(new Vector3(-radius,0,-radius),new Vector2(0,0),Color.white);
            g.Vertex(new Vector3(-radius,0,radius),new Vector2(0,1),Color.white);
            g.Vertex(new Vector3(radius,0,radius),new Vector2(1,1),Color.white);
            g.Vertex(new Vector3(radius,0,-radius),new Vector2(1,0),Color.white);g.Quad(0,1,2,3);
            return g.Finish(name);
        }
        // Two disconnected, tapered wisps in one draw. Neither spans a full turn
        // or the full height; the volume, rather than these accents, carries shape.
        public static Mesh CycloneWisps(string name,int segments)
        {
            var g=new MacAnhArcaneGeometry();var motion=new List<Vector4>();
            for(int band=0;band<3;band++)
            {
                int start=g.v.Count;bool accent=band==1;
                for(int s=0;s<=segments;s++)
                {
                    float t=(float)s/segments;
                    float h=(accent?.19f:band==2?.72f:.49f)+t*(accent?.24f:band==2?.20f:.36f);
                    float a=(accent?2.7f:band==2?2.1f:-.8f)+t*(accent?1.75f:band==2?1.4f:2.3f)+.12f*Mathf.Sin(t*7+band);
                    float width=(accent?.11f:band==2?.13f:.22f)*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.8f)*(1+.28f*Mathf.Sin(t*11+band*2));
                    var centre=Point(a,h,accent?1.015f:.94f);
                    centre.y+=.15f*Mathf.Sin(a*1.7f+.9f)+.10f*Mathf.Sin(t*8.3f+band*1.9f);
                    var radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                    var tangent=new Vector3(-Mathf.Sin(a)*Radius(h),.3f,Mathf.Cos(a)*Radius(h));
                    var across=Vector3.Cross(tangent,radial).normalized;
                    for(int k=0;k<3;k++)
                    {
                        float u=k*.5f;
                        var tint=accent?new Color(1,1,1,.9f):new Color(.65f,.4f,.92f,.65f);
                        g.Vertex(centre+across*((u-.5f)*width)+radial*(Mathf.Sin(u*Mathf.PI)*.018f),new Vector2(t,u),tint);
                        motion.Add(new Vector4(.23f+band*.51f,accent?1:.55f,0,0));
                    }
                    if(s>0)for(int k=0;k<2;k++){int n=start+s*3+k;g.Quad(n-3,n-2,n+1,n);}
                }
            }
            var mesh=g.Finish(name);mesh.SetUVs(1,motion);
            mesh.bounds=new Bounds(new Vector3(0,1.1f,0),new Vector3(2.5f,2.4f,2.5f));return mesh;
        }
        public static Mesh CycloneRunes(string name,int count)
        {
            var g=new MacAnhArcaneGeometry();var motion=new List<Vector4>();
            for(int k=0;k<count;k++)
            {
                float seed=(k*.618034f)%1,h=.28f+1.48f*seed,a=k*2.39996f;
                float r=Radius(h/2.12f)*(.58f+.24f*((k*.37f)%1));
                var centre=new Vector3(Mathf.Cos(a)*r,h,Mathf.Sin(a)*r);
                var x=new Vector3(-Mathf.Sin(a),0,Mathf.Cos(a))*(k%3==0?.06f:.045f);
                var y=Vector3.up*.078f;int n=g.v.Count;
                g.Vertex(centre-x-y,new Vector2(0,0),Color.white);g.Vertex(centre-x+y,new Vector2(0,1),Color.white);
                g.Vertex(centre+x+y,new Vector2(1,1),Color.white);g.Vertex(centre+x-y,new Vector2(1,0),Color.white);g.Quad(n,n+1,n+2,n+3);
                for(int i=0;i<4;i++)motion.Add(new Vector4(seed,0,0,0));
            }
            var mesh=g.Finish(name);mesh.SetUVs(1,motion);
            mesh.bounds=new Bounds(new Vector3(0,1.1f,0),new Vector3(2.5f,2.4f,2.5f));return mesh;
        }
        public static Mesh Runes(string name,int count)
        {
            var g=new MacAnhArcaneGeometry();
            for(int k=0;k<count;k++)
            {
                float h=.17f+1.72f*((k*.618034f)%1);
                float a=k*2.39996f, r=Radius(h/2.12f)+.12f;
                var centre=new Vector3(Mathf.Cos(a)*r,h,Mathf.Sin(a)*r);
                var x=new Vector3(-Mathf.Sin(a),0,Mathf.Cos(a))*(k%3==0?.065f:.045f);
                var y=Vector3.up*.085f; int n=g.v.Count;
                g.Vertex(centre-x-y,new Vector2(0,0),Color.white);g.Vertex(centre-x+y,new Vector2(0,1),Color.white);
                g.Vertex(centre+x+y,new Vector2(1,1),Color.white);g.Vertex(centre+x-y,new Vector2(1,0),Color.white); g.Quad(n,n+1,n+2,n+3);
            }
            return g.Finish(name);
        }
        public static Mesh Feather(string name)
        {
            var g=new MacAnhArcaneGeometry();
            for(int k=0;k<=12;k++)
            {
                float t=k/12f, w=Mathf.Sin(t*Mathf.PI)*.04f;
                g.Vertex(new Vector3(-w,t*.18f,Mathf.Sin(t*3)*.014f),new Vector2(0,t),Color.white);
                g.Vertex(new Vector3(w,t*.18f,Mathf.Sin(t*3)*.014f),new Vector2(1,t),Color.white);
                if(k>0) {int n=k*2;g.Quad(n-2,n-1,n+1,n);}
            }
            return g.Finish(name);
        }
    }
}

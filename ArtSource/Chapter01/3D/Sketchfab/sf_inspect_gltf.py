import json, os, sys, struct, glob
root = sys.argv[1]
def mat_mul(a,b): return [[sum(a[i][k]*b[k][j] for k in range(4)) for j in range(4)] for i in range(4)]
def node_matrix(n):
    if 'matrix' in n:
        m=n['matrix']; return [[m[c*4+r] for c in range(4)] for r in range(4)]
    t=n.get('translation',[0,0,0]); r=n.get('rotation',[0,0,0,1]); s=n.get('scale',[1,1,1])
    x,y,z,w=r
    R=[[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]]
    return [[R[i][0]*s[0],R[i][1]*s[1],R[i][2]*s[2],t[i]] for i in range(3)]+[[0,0,0,1]]
def img_size(p):
    try:
        with open(p,'rb') as f: d=f.read(64*1024)
        if d[:8]==b'\x89PNG\r\n\x1a\n': return struct.unpack('>II',d[16:24])
        i=2
        while i<len(d):
            if d[i]!=0xFF: i+=1; continue
            mk=d[i+1]
            if mk in (0xC0,0xC1,0xC2): h,w=struct.unpack('>HH',d[i+5:i+9]); return (w,h)
            i+=2+struct.unpack('>H',d[i+2:i+4])[0]
    except Exception as e: return ('?',str(e))
    return ('?','?')
for d in sorted(glob.glob(os.path.join(root,'SF[0-9]*'))):
    code=os.path.basename(d)
    g=glob.glob(os.path.join(d,'*.gltf'))
    if not g: print(code,'NO GLTF'); continue
    j=json.load(open(g[0],encoding='utf-8'))
    acc=j.get('accessors',[]); meshes=j.get('meshes',[]); nodes=j.get('nodes',[])
    tris=0; prims=0
    lo=[1e30]*3; hi=[-1e30]*3; inst=0
    def walk(i,M):
        global tris,prims,inst
        n=nodes[i]; M=mat_mul(M,node_matrix(n))
        if 'mesh' in n:
            inst+=1
            for p in meshes[n['mesh']]['primitives']:
                prims+=1
                if p.get('mode',4)!=4: continue
                c = acc[p['indices']]['count'] if 'indices' in p else acc[p['attributes']['POSITION']]['count']
                tris+=c//3
                a=acc[p['attributes']['POSITION']]; mn,mx=a['min'],a['max']
                for cx in (mn[0],mx[0]):
                    for cy in (mn[1],mx[1]):
                        for cz in (mn[2],mx[2]):
                            v=[M[r][0]*cx+M[r][1]*cy+M[r][2]*cz+M[r][3] for r in range(3)]
                            for k in range(3): lo[k]=min(lo[k],v[k]); hi[k]=max(hi[k],v[k])
        for c in n.get('children',[]): walk(c,M)
    I=[[1,0,0,0],[0,1,0,0],[0,0,1,0],[0,0,0,1]]
    sc=j['scenes'][j.get('scene',0)]
    for r in sc['nodes']: walk(r,I)
    size=[hi[k]-lo[k] for k in range(3)]
    # glTF: Y up. pivot = origin vs bottom-centre
    piv=( (lo[0]+hi[0])/2, lo[1], (lo[2]+hi[2])/2 )
    texs=[]
    for im in j.get('images',[]):
        u=im.get('uri')
        if u: texs.append(img_size(os.path.join(d,u.replace('%20',' '))))
    big=max([max(t) for t in texs if isinstance(t[0],int)] or [0])
    exts=j.get('extensionsUsed',[])
    lic=open(os.path.join(d,'license.txt'),encoding='utf-8',errors='replace').read() if os.path.exists(os.path.join(d,'license.txt')) else ''
    licn='CC0' if 'CC0' in lic or 'publicdomain' in lic else ('CC-BY' if 'by/4.0' in lic or 'CC-BY' in lic else ('?' if lic else 'NO LICENSE'))
    mb=sum(os.path.getsize(os.path.join(dp,f)) for dp,_,fs in os.walk(d) for f in fs)/1048576
    print(f"{code}|tris={tris}|meshes={len(meshes)} inst={inst} prims={prims}|mats={len(j.get('materials',[]))}|imgs={len(texs)} max={big}|size(x,y_up,z)=({size[0]:.2f},{size[1]:.2f},{size[2]:.2f})|bottomCentre=({piv[0]:.2f},{piv[1]:.2f},{piv[2]:.2f})|skins={len(j.get('skins',[]))} anims={len(j.get('animations',[]))}|ext={exts}|lic={licn}|{mb:.1f}MB")

import bpy
import os
import json
import sys
from mathutils import Vector
from mathutils.bvhtree import BVHTree
root=os.path.dirname(os.path.abspath(__file__))
path=os.path.join(root,'Arena3Q_LuBu_BuildAll_UVSafe_7Animations.py')
source=open(path,encoding='utf-8-sig').read(); ns={}
exec(compile(source[:source.rindex('\nLOG.clear()')],path,'exec'),ns)
ns['play'](sys.argv[sys.argv.index('--clip')+1] if '--clip' in sys.argv else 'Idle')
if '--frame' in sys.argv: bpy.context.scene.frame_set(int(sys.argv[sys.argv.index('--frame')+1]))
body=bpy.data.objects['LuBu_Body']; weapon=bpy.data.objects['LuBu_Weapon']
evaluated=body.evaluated_get(bpy.context.evaluated_depsgraph_get()); mesh=evaluated.to_mesh()
vertices=[v.co.copy() for v in mesh.vertices]; evaluated.to_mesh_clear()
polys=ns['_body_polys'](body); tree=BVHTree.FromPolygons(vertices,polys)
inverse=body.matrix_world.inverted(); matrix=weapon.matrix_world
rays=[Vector(v).normalized() for v in ((1,.13,.07),(-.1,-1,.2),(.05,.1,1))]
hits=[]
for index in range(0,len(weapon.data.vertices),max(1,len(weapon.data.vertices)//1200)):
    local=weapon.data.vertices[index].co
    if abs(local.y)<.07: continue
    point=inverse @ (matrix @ local)
    near,normal,face,distance=tree.find_nearest(point)
    inside=sum(1 for ray in rays if (lambda hit:hit[0] is not None and hit[1].dot(ray)>0)(tree.ray_cast(point,ray)))
    if inside>=2:
        owners={}
        for vertex in polys[face]:
            for group in body.data.vertices[vertex].groups:
                name=body.vertex_groups[group.group].name; owners[name]=owners.get(name,0)+group.weight
        hits.append({'point':[round(v,3) for v in point],'near':[round(v,3) for v in near],
                     'distance':round(distance,4),'owners':sorted(owners.items(),key=lambda p:p[1],reverse=True)[:2]})
hits.sort(key=lambda h:-h['distance'])
print(json.dumps(hits[:20]),flush=True)

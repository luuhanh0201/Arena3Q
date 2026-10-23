import bpy,sys,math,numpy as np
sys.path.insert(0,'D:/UnityProjects/Arena3Q/Library/MacAnhTools/Python')
from scipy.optimize import least_squares
from scipy.spatial.transform import Rotation
from mathutils import Matrix,Vector
r=bpy.data.objects['MacAnh_Rig'];o=bpy.data.objects[r['source_body_object']];fan=bpy.data.objects['MacAnh_Fan']
r.animation_data.action=bpy.data.actions['MacAnh_Idle'];bpy.context.scene.frame_set(20);bpy.context.view_layer.update()
dg=bpy.context.evaluated_depsgraph_get();ob=o.evaluated_get(dg);m=ob.to_mesh();inv=fan.matrix_world.inverted()
groups={}
for digit in ('Thumb','Index','Middle','Ring','Little'):
 ids=[v.index for v in o.data.vertices if sum(g.weight for g in v.groups if o.vertex_groups[g.group].name.startswith('Right'+digit))>.35]
 groups[digit]=np.array([inv@(o.matrix_world@m.vertices[i].co) for i in ids])
ob.to_mesh_clear()
def distances(x,p):
 q=(p-x[:3])@Rotation.from_rotvec(x[3:]).as_matrix()
 radial=np.abs(np.linalg.norm(q[:,:2],axis=1)-.013)
 axial=np.maximum(0,np.abs(q[:,2])-.042)
 return np.sqrt(radial**2+axial**2)
def fun(x):
 res=[]
 for p in groups.values():res.extend(np.maximum(0,np.sort(distances(x,p))[:3]-.001))
 res.extend(x[:3]*.08);res.extend(x[3:]*.001)
 return res
result=least_squares(fun,np.full(6,.001),bounds=([-.04]*3+[-.8]*3,[.04]*3+[.8]*3),max_nfev=200)
print('SOCKET_FIT',result.x.tolist(),{d:np.sort(distances(result.x,p))[:3].tolist() for d,p in groups.items()},flush=True)
rotation=Matrix(Rotation.from_rotvec(result.x[3:]).as_matrix().tolist()).to_4x4()
delta=Matrix.Translation(Vector(result.x[:3]))@rotation
if '--sculpt-hand' in sys.argv:
 # Fit the hand to the existing grip; leave fan clearance and wrist anchor fixed.
 transform=fan.matrix_world@delta.inverted()@fan.matrix_world.inverted()
 ids={i for p,a in zip(o.data.polygons,o.data.attributes['GarmentRegion'].data) if a.value==3 for i in p.vertices}
 wrist=r.pose.bones['RightHand'].head.copy()
 for i in ids:
  v=o.data.vertices[i];skin=Matrix([[0]*4 for _ in range(4)])
  for g in v.groups:
   b=r.pose.bones.get(o.vertex_groups[g.group].name)
   if b:skin+=(b.matrix@b.bone.matrix_local.inverted())*g.weight
  p=skin@v.co;t=max(0,min(1,((p-wrist).length-.014)/.040));t=t*t*(3-2*t)
  v.co=skin.inverted()@p.lerp(transform@p,t)
 o.data.update()
 bpy.ops.wm.save_as_mainfile(filepath='D:/UnityProjects/Arena3Q/Library/MacAnh_HandFit.blend',compress=True)
else:
 fan.matrix_basis=fan.matrix_basis@delta
 bpy.ops.wm.save_as_mainfile(filepath='D:/UnityProjects/Arena3Q/Library/MacAnh_SocketFit.blend',compress=True)

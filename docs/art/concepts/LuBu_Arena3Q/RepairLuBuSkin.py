"""Solve heat weights on a closed temporary proxy, transfer back without touching source UVs."""
import bpy
import os
import sys
import numpy as np
from mathutils.kdtree import KDTree
sys.stdout.reconfigure(line_buffering=True)
root = os.path.dirname(os.path.abspath(__file__))
body = bpy.data.objects['LuBu_Body']
rig = bpy.data.objects['LuBu_Rig']
rig.animation_data_clear()
for bone in rig.pose.bones: bone.matrix_basis.identity()
bpy.context.view_layer.update()
proxy = body.copy()
proxy.data = body.data.copy()
proxy.name = 'Temporary_Closed_Weight_Proxy'
bpy.context.scene.collection.objects.link(proxy)
proxy.parent = None
proxy.modifiers.clear()
proxy.vertex_groups.clear()
proxy.matrix_world = body.matrix_world.copy()
for obj in bpy.context.view_layer.objects: obj.select_set(False)
proxy.select_set(True)
bpy.context.view_layer.objects.active = proxy
remesh = proxy.modifiers.new('Closed skinning proxy', 'REMESH')
remesh.mode = 'VOXEL'
remesh.voxel_size = 0.007
bpy.ops.object.modifier_apply(modifier=remesh.name)
print('PROXY_VERTICES', len(proxy.data.vertices), flush=True)
# Fingers, feathers and cloth use the dedicated region solver after the torso/limbs.
original = {b.name:b.use_deform for b in rig.data.bones}
digits = ('Thumb','Index','Middle','Ring','Little')
for bone in rig.data.bones:
    if any(d in bone.name for d in digits) or bone.name.startswith(('Cape','Feather')): bone.use_deform=False
for obj in (proxy,rig): obj.scale=(10,10,10)
proxy.select_set(True); rig.select_set(True); bpy.context.view_layer.objects.active=rig
bpy.ops.object.parent_set(type='ARMATURE_AUTO')
for obj in (proxy,rig): obj.scale=(1,1,1)
missing = sum(sum(g.weight for g in v.groups) < 0.0001 for v in proxy.data.vertices)
print('PROXY_UNWEIGHTED', missing, flush=True)
# Tiny isolated remesh specks are excluded from transfer; never accept a failed solve.
assert missing / len(proxy.data.vertices) < 0.001, 'Closed proxy heat solve failed.'
valid=[v for v in proxy.data.vertices if sum(g.weight for g in v.groups)>0.0001]
tree=KDTree(len(valid))
for vertex in valid: tree.insert(vertex.co,vertex.index)
tree.balance()
names=[group.name for group in proxy.vertex_groups]
indices=np.full((len(body.data.vertices),4),-1,dtype=np.int32)
values=np.zeros((len(body.data.vertices),4),dtype=np.float32)
for vertex in body.data.vertices:
    _,nearest,_=tree.find(vertex.co)
    groups=sorted(proxy.data.vertices[nearest].groups,key=lambda g:g.weight,reverse=True)[:4]
    total=sum(g.weight for g in groups)
    for i,group in enumerate(groups):
        indices[vertex.index,i]=group.group
        values[vertex.index,i]=group.weight/total
np.savez_compressed(os.path.join(root,'LuBu_HeatWeights.npz'),names=names,indices=indices,values=values)
for bone in rig.data.bones: bone.use_deform=original[bone.name]
print('LUBU_PROXY_WEIGHTS_SUCCESS',flush=True)

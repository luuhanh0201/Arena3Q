"""Uncut-LOD trial with continuous, spatially disjoint four-bone supports."""
import bpy,sys,json
import numpy as np
from pathlib import Path
root=Path(__file__).resolve().parent
rig=bpy.data.objects['MacAnh_Rig'];body=bpy.data.objects[rig['source_body_object']]
co=np.array([v.co[:] for v in body.data.vertices]);x,y,z=co.T
def smooth(a,b,v):
    t=np.clip((v-a)/(b-a),0,1);return t*t*(3-2*t)
def distance(name):
    bone=rig.data.bones[name];start=np.array(bone.head_local);delta=np.array(bone.tail_local)-start
    t=np.clip(((co-start)*delta).sum(axis=1)/(delta@delta),0,1)
    return np.linalg.norm(co-start-t[:,None]*delta,axis=1)
head=smooth(.81,.95,z)
chest=np.maximum(smooth(.30,.86,z),smooth(-.08,.26,y)*smooth(.15,.48,z))
influences={}
for side,sign in [('Right',-1),('Left',1)]:
    fingers=np.stack([distance(side+digit+str(i)) for digit in ('Thumb','Index','Middle','Ring','Little') for i in (1,2,3)]+[distance(side+'Hand')],axis=1).min(axis=1)
    hand=(1-smooth(.016,.15,fingers))*smooth(.08,.23,x*sign)
    sleeve=.85*smooth(.13,.37,x*sign)*smooth(.20,.42,z)*(1-smooth(.70,.80,z))*(1-smooth(.08,.28,y))
    sleeve*=1-hand
    foot=(1-smooth(.02,.10,distance(side+'Foot')))*(1-smooth(.13,.24,z))
    influences[side+'Hand']=hand;influences['Sleeve_'+side+'_2']=sleeve;influences[side+'Foot']=foot
remaining=1-sum(influences.values());assert remaining.min()>-1e-6
influences['Head']=head*remaining;influences['Chest']=chest*(1-head)*remaining;influences['Pelvis']=(1-chest)*(1-head)*remaining
assert max(np.count_nonzero(np.array(list(influences.values())).T>1e-9,axis=1))<=4
body.vertex_groups.clear()
for name,weights in influences.items():
    group=body.vertex_groups.new(name=name)
    for i in np.flatnonzero(weights>1e-9):group.add([int(i)],float(weights[i]),'REPLACE')
for modifier in body.modifiers:
    if modifier.type=='ARMATURE':modifier.use_deform_preserve_volume=False
label=sys.argv[sys.argv.index('--lod')+1] if '--lod' in sys.argv else 'Battle'
destination=root/'Production/LOD'/('MacAnh_'+label+'_UncutFourTrial.blend')
bpy.ops.wm.save_as_mainfile(filepath=str(destination),compress=True)
print('UNCUT_FOUR_TRIAL_COMPLETE',flush=True)

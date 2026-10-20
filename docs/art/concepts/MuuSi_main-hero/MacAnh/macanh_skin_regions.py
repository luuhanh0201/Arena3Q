"""Continuous geometric bone families: texture colour must not split a finger or robe."""
import numpy as np
def refine_regions(co,indices,weights,names,armature):
    ids={str(n):i for i,n in enumerate(names)}
    def assign(mask,candidates,sharp):
        rows=np.flatnonzero(mask)
        if not len(rows): return
        points=co[rows]; distances=[]
        for name in candidates:
            bone=armature.bones[name]
            a=np.array(bone.head_local,dtype=np.float32); d=np.array(bone.tail_local-bone.head_local,dtype=np.float32)
            t=np.clip(((points-a)*d).sum(axis=1)/max(float(d@d),1e-8),0,1)
            distances.append(np.sqrt(((points-a-t[:,None]*d)**2).sum(axis=1)+1e-10))
        distance=np.stack(distances,axis=1); best=np.argsort(distance,axis=1)[:,:4]
        selected=np.take_along_axis(distance,best,axis=1)
        value=np.exp(-sharp*(selected-selected[:,0:1])); value/=value.sum(axis=1,keepdims=True)
        indices[rows]=0; weights[rows]=0
        indices[rows,:best.shape[1]]=np.array([ids[n] for n in candidates],dtype=np.int16)[best]
        weights[rows,:best.shape[1]]=value
    x,y,z=co.T
    hair_ids=[i for n,i in ids.items() if n.startswith(('Hair_','Ribbon_'))]
    is_hair=(weights*np.isin(indices,hair_ids)).sum(axis=1)>.5
    torso=(abs(x)<.225)&(z>.565)&(z<1.015)&~is_hair
    assign(torso,['Pelvis','Spine','Chest','Neck','Head'],22)
    counts={'torso':int(torso.sum())}
    digits=('Thumb','Index','Middle','Ring','Little')
    for side,sign in [('Right',-1),('Left',1)]:
        half=x*sign>0
        # Include the original red/dark finger seam pixels: they are still skin.
        hand=half&(abs(x)>.32)&(abs(x)<.47)&(z>.535)&(z<.655)&(y<-.075)&(y>-.25)
        assign(hand,[side+'Hand']+[side+d+str(i+1) for d in digits for i in range(3)],155)
        back_cloth=half&(abs(x)>.205)&(y>=-.035)&(z<.86)&~is_hair&~hand
        low=back_cloth&(z<.62)
        assign(low,['Pelvis']+['Robe_'+side+'_'+r+'_'+str(i+1) for r in ('Inner','Outer') for i in range(3)],24)
        assign(back_cloth&~low,['Spine','Chest',side+'Shoulder'],20)
        counts[side+'_hand']=int(hand.sum()); counts[side+'_back_cloth']=int(back_cloth.sum())
    return counts

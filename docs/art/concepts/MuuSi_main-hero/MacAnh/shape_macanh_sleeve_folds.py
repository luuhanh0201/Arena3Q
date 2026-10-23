"""Shape draped upper sleeves without changing authored arm or fan motion."""
import bpy,sys,math
from pathlib import Path
from mathutils import Vector
r=bpy.data.objects['MacAnh_Rig'];o=bpy.data.objects[r['source_body_object']];m=o.data
mi=next(i for i,mat in enumerate(m.materials) if mat.name=='MacAnh_ContinuousInnerSleeves')
ids={i for p in m.polygons if p.material_index==mi for i in p.vertices}
count=0
for i in ids:
 v=m.vertices[i];names=[o.vertex_groups[g.group].name for g in v.groups if g.weight>.99]
 if len(names)!=1 or not names[0].endswith('UpperArm'):continue
 b=r.data.bones[names[0]];axis=(b.tail_local-b.head_local).normalized()
 along=(v.co-b.head_local).dot(axis);t=along/b.length
 if not 0<t<1:continue
 centre=b.head_local+axis*along;radial=v.co-centre
 bell=math.sin(math.pi*t)**2
 # Broad cloth volume, gathered toward the elbow; gravity gives the underside
 # a hanging contour instead of the straight edge of a cylindrical bridge.
 factor=1+bell*(.25+.18*math.sin(math.tau*2*t))
 down=Vector((0,0,-1));down-=axis*down.dot(axis)
 v.co=centre+radial*factor+down*.028*bell
 count+=1
m.update()
out=Path(sys.argv[sys.argv.index('--output')+1]).resolve();assert out!=Path(bpy.data.filepath).resolve()
bpy.ops.wm.save_as_mainfile(filepath=str(out),compress=True)
print('SLEEVE_FOLDS',count,flush=True)

"""Fixed-grip fan gesture and loose-sleeve clearance on the shared 87-bone rig."""
import math
import bpy
from mathutils import Vector, Matrix

def fan_rotation(up=(-.4,.3,1), roll=-60):
    up=Vector(up).normalized()
    facing=Vector((0,-.66,.75))
    normal=-(facing-up*facing.dot(up)).normalized()
    axis=normal.cross(up).normalized()
    return (Matrix((axis,normal,up)).transposed()@Matrix.Rotation(math.radians(roll),3,'Z')).to_quaternion()

def gesture(name,frame,last):
    t=frame/max(1,last)
    if name in ('Idle','Walk','Run'):
        drive=.22*math.sin(t*math.tau)
    else:
        releases={'Attack01':[14],'Attack02':[15,24,33],'Skill01':[33],
                  'Skill02':[30],'Skill03':[27],'Ultimate':[75],'DodgeAttack':[24]}.get(name,[12])
        drive=sum(math.exp(-((frame-f+2)/5)**2)-.5*math.exp(-((frame-f-5)/7)**2) for f in releases)
        drive*=math.sin(math.pi*t)**2
    return 45+2*drive,fan_rotation((-.4-.06*drive,.3+.025*drive,1),-60+8*drive)

def apply_pose(rig, fan, turn=45, rotation=None):
    """Rotate nondeforming grip socket; rotate loose cloth about its attachment.

    No translation of the grip, no extra bones, and no changes to skin weights.
    All three sleeve bones inherit one rigid delta; cloth edge strain is unchanged.
    """
    socket=rig.pose.bones['Socket_Hand_R']
    bind=socket.matrix.inverted()@rig.matrix_world.inverted()@fan.matrix_world
    grip=fan.matrix_world.translation.copy()
    rotation=rotation or fan_rotation()
    socket.matrix=rig.matrix_world.inverted()@Matrix.Translation(grip)@rotation.to_matrix().to_4x4()@bind.inverted()
    first=rig.pose.bones['Sleeve_Right_1'];head=first.head.copy()
    first.matrix=Matrix.Translation(head)@Matrix.Rotation(math.radians(turn),4,'X')@Matrix.Translation(-head)@first.matrix
    bpy.context.view_layer.update()

def sweep_tassels(amount):
    tassels=bpy.data.objects['Fan_Tassels']
    delta=amount-float(tassels.get('clearance_sweep_m',0))
    for vertex in tassels.data.vertices:
        depth=max(0,min(1,(-.025-vertex.co.z)/.13))
        vertex.co.y-=delta*depth*depth
    tassels.data.update()
    tassels['clearance_sweep_m']=amount

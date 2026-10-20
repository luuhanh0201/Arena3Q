"""Bespoke, in-place poses for the measured MacAnh skeleton (no LuBu assets)."""
import bpy,math
from mathutils import Vector,Matrix,Quaternion,Euler

class MacAnhMotion:
    def __init__(self):
        self.rig=bpy.data.objects['MacAnh_Rig']
        self.body=bpy.data.objects[self.rig['source_body_object']]
        self.fan=bpy.data.objects['MacAnh_Fan']
        # Preserve the original metre-scale rest mesh. Never retain the temporary
        # heat solver's scale in a stale parent-inverse matrix.
        self.body.matrix_parent_inverse.identity(); self.body.matrix_basis.identity()
        self.rig.animation_data_clear()
        for p in self.rig.pose.bones: p.matrix_basis.identity()
        bpy.context.view_layer.update()
        self.fan.matrix_world=Matrix.Translation(self.rig.data.bones['Socket_Hand_R'].head_local)
        from macanh_fan_clearance import sweep_tassels
        sweep_tassels(.04)
        self.bind={}
        for side in ('Right','Left'):
            hand=self.rig.data.bones[side+'Hand'].matrix_local
            grip=Matrix.Translation(self.rig.data.bones['Socket_Hand_'+side[0]].head_local)
            self.bind[side]=hand.inverted() @ grip
        self.roll={}
        for prefix in ('Right','Left'):
            for chain in (('UpperArm','LowerArm'),('Thigh','Shin')):
                a,b=[self.rig.data.bones[prefix+n] for n in chain]
                normal=(a.tail_local-a.head_local).cross(b.tail_local-b.head_local)
                if normal.length<.0001: normal=Vector((1,0,0))
                normal.normalize()
                for rest in (a,b):
                    y=(rest.tail_local-rest.head_local).normalized()
                    x=y.cross(normal).normalized(); z=x.cross(y).normalized()
                    self.roll[rest.name]=Matrix((x,y,z)).transposed().inverted() @ rest.matrix_local.to_3x3()

    def update(self): bpy.context.view_layer.update()
    def rotate(self,name,angles):
        p=self.rig.pose.bones[name]; rest=p.bone.matrix_local.to_3x3()
        delta=Euler(tuple(math.radians(a) for a in angles),'XYZ').to_matrix()
        p.rotation_quaternion=(rest.inverted() @ delta @ rest).to_quaternion()

    def aim(self,p,target,normal):
        direction=(target-p.head).normalized()
        rest=p.bone.matrix_local.to_3x3()
        rest_direction=(p.bone.tail_local-p.bone.head_local).normalized()
        rotation=rest_direction.rotation_difference(direction).to_matrix() @ rest
        p.matrix=Matrix.Translation(p.head) @ rotation.to_4x4(); p.scale=(1,1,1)
        self.update()

    def two_bone(self,first,second,goal,pole):
        a=self.rig.pose.bones[first]; b=self.rig.pose.bones[second]
        start=a.head.copy(); delta=goal-start; axis=delta.normalized()
        l1=a.bone.length; l2=b.bone.length
        length=max(abs(l1-l2)+.003,min(delta.length,(l1+l2)*.994))
        goal=start+axis*length
        pole=(pole-axis*pole.dot(axis)).normalized()
        along=(l1*l1-l2*l2+length*length)/(2*length)
        elbow=start+axis*along+pole*math.sqrt(max(0,l1*l1-along*along))
        normal=(elbow-start).cross(goal-elbow).normalized()
        self.aim(a,elbow,normal); self.aim(b,goal,normal)

    def arm(self,side,point,up,front=(0,-1,0)):
        chest=self.rig.pose.bones['Chest']
        chest_rotation=chest.matrix.to_3x3() @ chest.bone.matrix_local.to_3x3().inverted()
        base=Vector((0,.015,.77))
        point=chest.head+chest_rotation @ (Vector(point)-base)
        z=(chest_rotation @ Vector(up)).normalized()
        y=-(chest_rotation @ Vector(front)); y=(y-z*y.dot(z)).normalized()
        x=y.cross(z).normalized(); y=z.cross(x).normalized()
        desired=Matrix((x,y,z)).transposed()
        hand_rotation=desired @ self.bind[side].to_3x3().inverted()
        wrist=point-hand_rotation @ self.bind[side].to_translation()
        clavicle=self.rig.pose.bones[side+'Shoulder']
        axis=(clavicle.tail-clavicle.head).normalized()
        turn=axis.rotation_difference((wrist-clavicle.head).normalized())
        turn=Quaternion(turn.axis,min(turn.angle,math.radians(32)))
        clavicle.matrix=Matrix.Translation(clavicle.head) @ (turn.to_matrix() @ clavicle.matrix.to_3x3()).to_4x4()
        clavicle.scale=(1,1,1); self.update()
        sign=-1 if side=='Right' else 1
        pole=chest_rotation @ Vector((sign*.6,.2,-1))
        self.two_bone(side+'UpperArm',side+'LowerArm',wrist,pole)
        hand=self.rig.pose.bones[side+'Hand']
        forearm=self.rig.pose.bones[side+'LowerArm']
        relaxed=(forearm.matrix @ forearm.bone.matrix_local.inverted() @ hand.bone.matrix_local).to_quaternion()
        requested=hand_rotation.to_quaternion()
        relative=relaxed.rotation_difference(requested)
        # The fan path used to force 85–106 degree wrist bends. Let the arm
        # carry the gesture and keep wrist articulation inside a natural range.
        if relative.angle>math.radians(40):
            requested=relaxed @ Quaternion(relative.axis,math.radians(40))
            hand_rotation=requested.to_matrix()
        hand.matrix=Matrix.Translation(self.rig.pose.bones[side+'LowerArm'].tail) @ hand_rotation.to_4x4()
        hand.scale=(1,1,1); self.update()

    def fingers(self,side,amount):
        sign=-1 if side=='Right' else 1
        palm_normal=Vector((-sign,0,.1)).normalized()
        for digit in ('Thumb','Index','Middle','Ring','Little'):
            for i,angle in enumerate((34,48,22)):
                p=self.rig.pose.bones[side+digit+str(i+1)]
                direction=(p.bone.tail_local-p.bone.head_local).normalized()
                axis=direction.cross(palm_normal).normalized()
                local=p.bone.matrix_local.to_3x3().inverted() @ axis
                value=angle*amount*(.5 if digit=='Thumb' else 1)
                p.rotation_quaternion=Quaternion(local,math.radians(value))

    def leg(self,side,ankle,foot_pitch=0):
        self.two_bone(side+'Thigh',side+'Shin',Vector(ankle),Vector((0,-1,.1)))
        foot=self.rig.pose.bones[side+'Foot']
        rotation=Matrix.Rotation(math.radians(foot_pitch),3,'X') @ foot.bone.matrix_local.to_3x3()
        foot.matrix=Matrix.Translation(self.rig.pose.bones[side+'Shin'].tail) @ rotation.to_4x4()
        foot.scale=(1,1,1); self.update()

    @staticmethod
    def sample(keys,frame,index):
        interval=next((i for i in range(len(keys)-1) if frame<=keys[i+1][0]),len(keys)-2)
        a,b=keys[interval],keys[interval+1]
        t=max(0,min(1,(frame-a[0])/(b[0]-a[0])))
        span=b[0]-a[0]
        if index==2:
            # Interpolating an orientation as Cartesian coordinates accelerates
            # sharply when that vector passes near zero during a fan flip.
            return Vector(a[index]).normalized().slerp(Vector(b[index]).normalized(),t*t*(3-2*t))
        # Shape-preserving Hermite tangents keep the hand moving through a
        # gesture. Independent smoothstep segments stopped at every waypoint.
        def tangent(k,axis):
            if k==0 or k==len(keys)-1: return 0
            p,q,r=keys[k-1],keys[k],keys[k+1]
            h0=q[0]-p[0];h1=r[0]-q[0]
            d0=(q[index][axis]-p[index][axis])/h0
            d1=(r[index][axis]-q[index][axis])/h1
            if d0*d1<=0:return 0
            w0=2*h1+h0;w1=h1+2*h0
            return (w0+w1)/(w0/d0+w1/d1)
        return Vector((2*t**3-3*t*t+1)*a[index][axis]+(t**3-2*t*t+t)*span*tangent(interval,axis)
                      +(-2*t**3+3*t*t)*b[index][axis]+(t**3-t*t)*span*tangent(interval+1,axis)
                      for axis in range(3))

    def pose(self,name,frame,last):
        for p in self.rig.pose.bones: p.matrix_basis.identity(); p.scale=(1,1,1)
        t=frame/max(1,last); cycle=math.sin(t*math.tau)
        right=Vector((-.36,-.30,.82)); right_up=Vector((-.43,-.04,.90))
        left=Vector((.335,-.19,.67)); left_up=Vector((.12,-.05,.99))
        yaw=0; lean=0; crouch=0; gait=name in ('Walk','Run')
        if name=='Idle':
            self.rotate('Spine',(0.45*cycle,0,.30*cycle)); self.rotate('Head',(.4*cycle,0,-.3*cycle))
            right.z+=.0025*cycle; left.z+=.003*cycle
        elif gait:
            run=name=='Run'; crouch=.055 if run else .018; lean=8 if run else 2
            crouch+=(.006 if run else .003)*math.cos(t*math.tau*2)
            lean+=(1 if run else .4)*math.sin(t*math.tau*2)
            yaw=(2.8 if run else 1.6)*cycle
            self.rotate('Head',(-lean*.22,0,-yaw*.55))
            right=Vector((-.36,-.29,.80 if run else .82)); right_up=Vector((-.4,-.15,.9))
            left=Vector((.31,-.17+(.07 if run else .035)*cycle,.68+.025*cycle))
        elif name in ('Dodge','DodgeAttack'):
            amount=math.sin(math.pi*min(1,frame/20))
            crouch=.06*amount; yaw=-12*amount; lean=5*amount
            right+=Vector((-.015,.035,-.04))*amount
            if name=='DodgeAttack' and frame>=15:
                keys=[(15,(-.38,-.27,.75),(-.6,0,.8)),(24,(-.26,-.38,.74),(.94,-.2,.22)),(33,(-.36,-.30,.82),(-.43,-.04,.90))]
                right=self.sample(keys,frame,1); right_up=self.sample(keys,frame,2)
        else:
            paths={
              'Attack01':[(0,tuple(right),tuple(right_up)),(4,tuple(right),tuple(right_up)),
                 (7,(-.43,-.25,.91),(-.65,.05,.76)),
                 (14,(-.25,-.40,.72),(.95,-.26,.10)),(19,(-.17,-.35,.74),(.88,-.10,.35)),(24,tuple(right),tuple(right_up))],
              'Attack02':[(0,tuple(right),tuple(right_up)),(8,(-.38,-.28,.95),(-.55,0,.83)),
                 (15,(-.26,-.39,.74),(.95,-.22,.15)),(19,(-.37,-.28,.83),(.3,-.35,.88)),
                 (24,(-.25,-.39,.73),(.95,-.2,.18)),(28,(-.37,-.27,.87),(.35,-.1,.93)),
                 (33,(-.23,-.40,.76),(.94,-.2,.22)),(45,tuple(right),tuple(right_up))],
              'Skill01':[(0,tuple(right),tuple(right_up)),(12,(-.36,-.30,.97),(-.5,-.05,.86)),
                 (30,(-.35,-.32,.98),(-.45,-.15,.88)),(33,(-.30,-.39,.77),(-.15,-.75,.65)),
                 (44,(-.31,-.37,.79),(-.2,-.65,.73)),(54,tuple(right),tuple(right_up))],
              'Skill02':[(0,tuple(right),tuple(right_up)),(12,(-.39,-.27,.72),(-.65,0,.76)),
                 (22,(-.25,-.37,.88),(.5,-.35,.79)),(30,(-.35,-.31,1.02),(-.3,-.1,.95)),
                 (48,(-.36,-.30,.96),(-.4,-.1,.91)),(60,tuple(right),tuple(right_up))],
              'Skill03':[(0,tuple(right),tuple(right_up)),(10,(-.32,-.33,.88),(-.6,0,.8)),
                 (27,(-.35,-.32,1.02),(-.5,-.1,.86)),(35,(-.35,-.31,.98),(-.5,-.1,.86)),(42,tuple(right),tuple(right_up))],
              'Ultimate':[(0,tuple(right),tuple(right_up)),(15,(-.32,-.31,.91),(-.5,0,.86)),
                 (30,(-.34,-.31,1.04),(-.4,0,.92)),(45,(-.43,-.25,.96),(-.3,0,.95)),
                 (60,(-.44,-.25,.96),(-.3,0,.95)),(67,(-.39,-.30,1.00),(-.5,0,.86)),
                 (75,(-.28,-.40,.74),(.8,-.55,.15)),(90,(-.31,-.37,.80),(-.2,-.6,.77)),(105,tuple(right),tuple(right_up))]}
            right=self.sample(paths[name],frame,1); right_up=self.sample(paths[name],frame,2)
            active=math.sin(math.pi*t)**2
            yaw=(-12 if name in ('Attack01','Attack02') else 4)*active
            lean=3*active; crouch=.015*active
            releases={'Attack01':[14],'Attack02':[15,24,33],'Skill01':[33],
                      'Skill02':[30],'Skill03':[27],'Ultimate':[75]}[name]
            # Hips initiate the wind-up; chest and free arm follow through after
            # release. Small unequal offsets retain the composed strategist pose.
            drive=sum(math.exp(-((frame-f+2)/4.5)**2)-.52*math.exp(-((frame-f-4)/6)**2) for f in releases)
            yaw+=-5.5*drive
            lean+=2.5*drive
            crouch+=.009*abs(drive)
            left=Vector((.29,-.33,.84)) if name!='Ultimate' else Vector((.40,-.23,.96))
            left=Vector((.335,-.19,.67)).lerp(left,min(1,active*2))
            left+=Vector((.008,-.015,.012))*drive
            self.rotate('Head',(-lean*.28,0,-yaw*.38))
        self.rotate('Pelvis',(lean*.25,0,yaw*.45))
        self.rig.pose.bones['Pelvis'].location=Vector((0,-crouch,0))
        self.rotate('Spine',(lean*.4,0,yaw*.35)); self.rotate('Chest',(lean*.35,0,yaw*.2))
        self.update()
        self.arm('Right',right,right_up); self.arm('Left',left,left_up)
        self.fingers('Right',.65); self.fingers('Left',.12 if name in ('Idle','Walk','Run') else .03)
        for side,sign,offset in [('Right',-1,0),('Left',1,.5)]:
            phase=(t+offset)%1
            step=0; lift=0; pitch=0
            if gait:
                run=name=='Run'; stance=.43 if run else .62; amplitude=.195 if run else .105
                if phase<stance: step=-amplitude+2*amplitude*phase/stance
                else:
                    swing=(phase-stance)/(1-stance)
                    step=amplitude*math.cos(math.pi*swing)
                    lift=(.075 if run else .045)*math.sin(math.pi*swing)
                    pitch=-12*math.sin(math.pi*swing)
            self.leg(side,(sign*.108,-.005+step,.08+lift),pitch)
        seconds=frame/30
        slow=t*math.tau if name in ('Idle','Walk','Run') else seconds*3
        fast=t*math.tau if name in ('Idle','Walk','Run') else seconds*4
        for side,sign in [('Right',-1),('Left',1)]:
            for i in range(4): self.rotate('Hair_'+side+'_'+str(i+1),(1.2*math.sin(fast-i*.55),.5*math.sin(slow-i*.5),sign*.7*math.sin(slow-i*.6)))
            for i in range(3):
                self.rotate('Ribbon_'+side+'_'+str(i+1),(2.5*math.sin(fast-i*.6),0,sign*1.5*math.sin(slow-i*.4)))
                self.rotate('Sleeve_'+side+'_'+str(i+1),(.7*math.sin(fast-i*.6),0,0))
                for row in ('Inner','Outer'): self.rotate('Robe_'+side+'_'+row+'_'+str(i+1),(1.1*math.sin(fast-i*.6),0,sign*.4*math.sin(slow-i*.4)))
        self.update()
        chest=self.rig.pose.bones['Chest']
        chest_delta=chest.matrix @ chest.bone.matrix_local.inverted()
        for side in ('Right','Left'):
            sleeve=self.rig.pose.bones['Sleeve_'+side+'_1']
            arm=self.rig.pose.bones[side+'UpperArm']
            arm_delta=arm.matrix @ arm.bone.matrix_local.inverted()
            forearm=self.rig.pose.bones[side+'LowerArm']
            forearm_delta=forearm.matrix @ forearm.bone.matrix_local.inverted()
            head=arm_delta @ sleeve.bone.head_local
            cuff=forearm_delta @ sleeve.bone.tail_local
            direction=(cuff-head).normalized()
            reference=chest_delta.to_3x3() @ sleeve.bone.matrix_local.to_3x3().col[0]
            axis=(reference-direction*reference.dot(direction)).normalized()
            rotation=Matrix((axis,direction,axis.cross(direction))).transposed()
            sleeve.matrix=Matrix.Translation(head) @ rotation.to_4x4();sleeve.scale=(1,1,1)
            self.update()
            first_delta=(sleeve.matrix @ sleeve.bone.matrix_local.inverted()).to_quaternion()
            # The cuff rises with the forearm, but the hanging hem bends back
            # towards gravity. Spread that bend over two joints, avoiding an
            # antiparallel pair that collapses under Unity linear skinning.
            for index,gravity in [(2,.5),(3,1.)]:
                child=self.rig.pose.bones['Sleeve_'+side+'_'+str(index)]
                parent=self.rig.pose.bones['Sleeve_'+side+'_'+str(index-1)]
                delta=first_delta.slerp(chest_delta.to_quaternion(),gravity)
                flutter=Euler((math.radians(.7*math.sin(fast-index*.6)),0,0),'XYZ').to_matrix()
                orientation=delta.to_matrix() @ child.bone.matrix_local.to_3x3() @ flutter
                child.matrix=Matrix.Translation(parent.tail) @ orientation.to_4x4();child.scale=(1,1,1)
                self.update()
        self.update()
        # Keep the feather crown outside the hair and the loose cuff behind the
        # held handle. The socket is nondeforming; the palm stays at its grip.
        from macanh_fan_clearance import apply_pose,gesture
        turn,rotation=gesture(name,frame,last)
        apply_pose(self.rig,self.fan,turn,rotation)


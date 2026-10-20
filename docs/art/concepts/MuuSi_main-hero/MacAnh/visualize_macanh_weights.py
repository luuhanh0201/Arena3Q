"""Data visualization of saved weights; no changes to any model or texture."""
from pathlib import Path
import sys,numpy as np,json
root=Path(__file__).resolve().parent;project=root.parents[4]
sys.path.insert(0,str(project/'Library/MacAnhTools/Python'))
from PIL import Image,ImageDraw
d=np.load(project/'Library/MacAnhRigAudit/actual_saved_weights_before.npz');co=d['co'];mass=d['arm'];hair=d['hair'];x,y,z=co.T
panels=[]
for title,axis,depth in [('FRONT: red = arm influence',x,y),('SIDE: red = arm influence',y,-x)]:
    w,h=650,1100;px=np.clip(((axis+.56)*550+15).astype(int),0,w-1);py=np.clip(((1.75-z)*550+60).astype(int),0,h-1);flat=py*w+px
    order=np.lexsort((depth,flat));unique,first=np.unique(flat[order],return_index=True);ids=order[first]
    pixels=np.full((h*w,3),34,np.uint8);a=np.clip(mass[ids],0,1)
    pixels[unique]=np.stack([130+125*a,140*(1-a),155*(1-a)],axis=1).astype(np.uint8)
    im=Image.fromarray(pixels.reshape(h,w,3));draw=ImageDraw.Draw(im);draw.text((12,10),title,fill='white')
    for level in (.3,.45,.565,.7):
        yy=int((1.75-level)*550+60);draw.line((0,yy,w,yy),fill=(70,70,70));draw.text((8,yy+2),str(level)+' m',fill='white')
    panels.append(im)
im=Image.new('RGB',(1300,1100));im.paste(panels[0],(0,0));im.paste(panels[1],(650,0));im.save(root/'Production/DodgeAttackFix/WeightLeak_Before.png')
for width in (.12,.16,.18,.20,.23):
    m=(abs(x)<width)&(z<.565)&(z>.1)&(hair<.1)
    bad=m&(mass>.01)
    print('LOWER_REGION',width,int(m.sum()),int(bad.sum()),float(mass[m].max(initial=0)),flush=True)

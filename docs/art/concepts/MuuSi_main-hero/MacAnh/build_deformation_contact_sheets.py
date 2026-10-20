"""Assemble untouched Unity captures into labelled review sheets."""
from pathlib import Path
import sys
root=Path(__file__).resolve().parent;sys.path.insert(0,str(root.parents[4]/'Library/MacAnhTools/Python'))
from PIL import Image,ImageDraw
folder=root/'Production/DeformationFix/UnityCaptures'
for key in ['Attack01','Attack02','Skill01','Skill02','Skill03','Ultimate','DodgeAttack']:
    paths=[folder/f'FX_{key}_High_t{time}_v{view}.png' for time in [35,120,270] for view in [0,1,2,3]]
    if not all(p.exists() for p in paths):continue
    sheet=Image.new('RGB',(4*420,3*446),(30,32,40));draw=ImageDraw.Draw(sheet)
    for i,path in enumerate(paths):
        image=Image.open(path);image.thumbnail((420,420))
        x=(i%4)*420;y=(i//4)*446
        sheet.paste(image,(x,y+26));draw.text((x+8,y+5),path.stem,fill='white')
    sheet.save(folder/f'Review_FX_{key}.jpg',quality=94)
for key in ['Idle','Walk','Run','Dodge','Attack01','Attack02','Skill01','Skill02','Skill03','Ultimate','DodgeAttack']:
    paths=sorted(folder.glob(key+'_f*_a*.png'))
    if not paths:continue
    sheet=Image.new('RGB',(8*260,3*351),(30,32,40));draw=ImageDraw.Draw(sheet)
    for i,path in enumerate(paths):
        image=Image.open(path);image.thumbnail((260,325))
        x=(i%8)*260;y=(i//8)*351
        sheet.paste(image,(x,y+26));draw.text((x+5,y+5),path.stem,fill='white')
    sheet.save(folder/f'Review_{key}.jpg',quality=95)
print('CONTACT_SHEETS_COMPLETE',flush=True)

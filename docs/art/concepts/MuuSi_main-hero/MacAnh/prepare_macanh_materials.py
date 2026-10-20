"""Flatten the read-only Blender material manifest for Unity's JsonUtility."""
import json
from pathlib import Path
root=Path(__file__).resolve().parent
project=root.parents[4]
source=json.loads((root/'Production/GraphicsUpgrade/SourceExport.json').read_text(encoding='utf-8'))
items=[]
for m in source['materials']:
    p=m['properties']; base=p['Base Color']; images={n['name']:n['image'] for n in m['nodes'] if n['image']}
    image=images.get(base['links'][0]['node'],'') if base['links'] else ''
    items.append(dict(name=m['name'],color=base['value'],baseMap=image,
        metallic=p['Metallic']['value'],roughness=p['Roughness']['value'],
        emission=p['Emission Color']['value'],strength=p['Emission Strength']['value'],
        sourceORM=m['name']=='Material_0.001'))
path=project/'Assets/Arena3Q/Art/Heroes/MacAnh/Graphics3D/Source/MaterialBridge.json'
path.write_text(json.dumps(dict(materials=items),indent=2),encoding='utf-8')
print(path)

"""Resume bonus actions / defeat weapon drop without rebuilding finished locomotion."""
import bpy
import os
import sys
sys.stdout.reconfigure(line_buffering=True)
root=os.path.dirname(os.path.abspath(__file__))
path=os.path.join(root,'Arena3Q_LuBu_BuildAll_UVSafe_7Animations.py')
source=open(path,encoding='utf-8-sig').read()
ns={'__file__':path}
exec(compile(source[:source.rindex('\nLOG.clear()')],path,'exec'),ns)
ns['finishall']()
print('LUBU_RIG_SUCCESS',flush=True)

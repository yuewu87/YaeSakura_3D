import socket, json, os

PMX = r"E:\崩坏三模型\崩坏3\八重樱\八重樱_礼服\礼服八重樱-纸月寒绯\礼服八重樱2.0.pmx"
FBX_OUT = r"E:\Study_Projects\Yae_sakura_3D\YaeSakura_3D\Assets\Models\yae_sakura.fbx"

def send_cmd(cmd_type, params={}):
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.settimeout(300)
    msg = json.dumps({'type': cmd_type, 'params': params})
    sock.connect(('127.0.0.1', 9876))
    sock.sendall(msg.encode('utf-8'))
    resp = b''
    try:
        while True:
            chunk = sock.recv(65536)
            if not chunk: break
            resp += chunk
            if len(chunk) < 65536: break
    except: pass
    sock.close()
    return json.loads(resp.decode('utf-8'))

# Step 1: Import PMX
print('Step 1: Importing PMX...')
code = f'''
import bpy
bpy.ops.mmd_tools.import_model(
    filepath="{PMX.replace(chr(92), chr(92)+chr(92))}",
    scale=0.08,
    types={{'MESH', 'ARMATURE'}},
    log=False
)
print("Import done")
'''
result = send_cmd('execute_code', {'code': code})
print(f'  Status: {result.get("status")}')
if result.get('status') == 'error':
    print(f'  Error: {result.get("message", "unknown")}')

# Step 2: Check objects
print('Step 2: Checking objects...')
result = send_cmd('get_scene_info')
objects = result.get('result', {}).get('objects', [])
print(f'  Objects: {len(objects)}')
for o in objects:
    nm = o.get('name', '?')
    tp = o.get('type', '?')
    print(f'    - {nm} ({tp})')

# Step 3: Export FBX
print('Step 3: Exporting FBX...')
os.makedirs(os.path.dirname(FBX_OUT), exist_ok=True)
export_code = f'''
import bpy, os
os.makedirs(r"{os.path.dirname(FBX_OUT)}", exist_ok=True)
bpy.ops.export_scene.fbx(
    filepath=r"{FBX_OUT}",
    use_selection=False,
    global_scale=1.0,
    apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_UNITS',
    object_types={{'ARMATURE', 'MESH'}},
    use_mesh_modifiers=True,
    mesh_smooth_type='OFF',
    use_armature_deform_only=True,
    add_leaf_bones=False,
    axis_forward='-Z',
    axis_up='Y',
    path_mode='COPY',
    embed_textures=False,
)
print("FBX export done")
'''
result = send_cmd('execute_code', {'code': export_code})
print(f'  Status: {result.get("status")}')

print('\nDone!')

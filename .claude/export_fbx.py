import socket, json, os

FBX_OUT = "E:/Study_Projects/Yae_sakura_3D/YaeSakura_3D/Assets/Models/yae_sakura.fbx"

def send_cmd(cmd_type, params={}):
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.settimeout(60)
    msg = json.dumps({'type': cmd_type, 'params': params}, ensure_ascii=False)
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

# Check scene
print('Checking scene...')
result = send_cmd('get_scene_info')
objects = result.get('result', {}).get('objects', [])
print(f'Objects: {len(objects)}')
for o in objects[:20]:
    nm = o.get('name', '?')
    tp = o.get('type', '?')
    vc = o.get('vertex_count', '?')
    print(f'  - {nm} ({tp}) vertices: {vc}')

# Create output directory
os.makedirs(os.path.dirname(FBX_OUT), exist_ok=True)

# Export FBX
print('\nExporting FBX...')
result = send_cmd('execute_code', {
    'code': f'''
import bpy, os
os.makedirs(r"{os.path.dirname(FBX_OUT)}", exist_ok=True)
bpy.ops.export_scene.fbx(
    filepath=r"{FBX_OUT}",
    use_selection=False,
    global_scale=1.0,
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_UNITS",
    object_types={{'ARMATURE', 'MESH'}},
    use_mesh_modifiers=True,
    mesh_smooth_type="OFF",
    use_armature_deform_only=True,
    add_leaf_bones=False,
    axis_forward="-Z",
    axis_up="Y",
    path_mode="COPY",
    embed_textures=False,
)
print("FBX_EXPORT_OK")
'''
})
print(f'Status: {result.get("status")}')
res = result.get('result', {})
output = res.get('result', '') if isinstance(res, dict) else str(res)
print(f'Output: {output}')

# Verify file
if os.path.exists(FBX_OUT):
    sz = os.path.getsize(FBX_OUT)
    print(f'\nFBX created: {FBX_OUT} ({sz/1024:.1f} KB)')
else:
    print('\nERROR: FBX file not found')

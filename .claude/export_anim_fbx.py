import socket, json, os

FBX_OUT = r"E:\Study_Projects\Yae_sakura_3D\YaeSakura_3D\Assets\Models\Characters\YaeSakura_Dress\yae_sakura_idle.fbx"

def send_cmd(cmd_type, params={}):
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.settimeout(120)
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

code = f'''
import bpy, os

bpy.ops.object.select_all(action='SELECT')

out = r"{os.path.dirname(FBX_OUT)}"
os.makedirs(out, exist_ok=True)

bpy.ops.export_scene.fbx(
    filepath=r"{FBX_OUT}",
    use_selection=True,
    object_types={{'ARMATURE', 'MESH'}},
    bake_anim=True,
    bake_anim_use_all_bones=True,
    bake_anim_use_nla_strips=True,
    bake_anim_use_all_actions=True,
    bake_anim_force_startend_keying=True,
    bake_anim_step=1.0,
    use_mesh_modifiers=True,
    mesh_smooth_type='OFF',
    use_armature_deform_only=True,
    add_leaf_bones=False,
    axis_forward='-Z',
    axis_up='Y',
    path_mode='COPY',
    embed_textures=False,
)
print("FBX exported with animation")
'''

result = send_cmd('execute_code', {'code': code})
print(f"Status: {result.get('status')}")
print(result.get('result', {}).get('result', ''))
print(f"\nOutput: {FBX_OUT}")
if os.path.exists(FBX_OUT):
    print(f"Size: {os.path.getsize(FBX_OUT)/1024:.1f} KB")

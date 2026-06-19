import socket, json, os

def send_cmd(cmd_type, params={}):
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.settimeout(300)
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

out_dir = r"E:/Study_Projects/Yae_sakura_3D/YaeSakura_3D/Assets/Models/Characters/YaeSakura_NR"
os.makedirs(out_dir, exist_ok=True)

code = f'''
import bpy, os
bpy.ops.object.select_all(action='SELECT')
out_dir = r"{out_dir}"
os.makedirs(out_dir, exist_ok=True)
bpy.ops.export_scene.fbx(
    filepath=out_dir + "/yae_nr_scene.fbx",
    use_selection=True,
    object_types={{'MESH'}},
    use_mesh_modifiers=False,
    mesh_smooth_type='OFF',
    axis_forward='-Z',
    axis_up='Y',
    path_mode='COPY',
    embed_textures=False,
)
print("FBX exported")
'''

result = send_cmd('execute_code', {'code': code})
print(f"FBX: {result.get('status')}")
print(result.get('result', {}).get('result', ''))

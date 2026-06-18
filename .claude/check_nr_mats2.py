import socket, json, os

def send_cmd_raw(cmd_type, params={}):
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.settimeout(30)
    msg = json.dumps({'type': cmd_type, 'params': params}, ensure_ascii=False)
    sock.connect(('127.0.0.1', 9876))
    sock.sendall(msg.encode('utf-8'))
    resp = b''
    while True:
        try:
            chunk = sock.recv(65536)
            if not chunk: break
            resp += chunk
        except: break
    sock.close()
    return resp

code = '''
import bpy, json, os
data = []
for obj in bpy.data.objects:
    if obj.type != 'MESH' or len(obj.data.materials) == 0:
        continue
    mats = []
    for idx, mat in enumerate(obj.data.materials):
        if mat is None:
            mats.append({"idx": idx, "name": None})
            continue
        textures = []
        if mat.use_nodes:
            for node in mat.node_tree.nodes:
                if node.type == 'TEX_IMAGE' and node.image:
                    textures.append({"name": node.image.name, "path": node.image.filepath})
        mats.append({"idx": idx, "name": mat.name, "textures": textures})
    data.append({"obj": obj.name, "materials": mats})

out = r"E:/Study_Projects/Yae_sakura_3D/YaeSakura_3D/.claude/nr_mats.json"
os.makedirs(os.path.dirname(out), exist_ok=True)
with open(out, 'w', encoding='utf-8') as f:
    json.dump(data, f, ensure_ascii=False, indent=2)
print("Dumped to " + out)
'''

resp = send_cmd_raw('execute_code', {'code': code})
print(resp.decode('utf-8', errors='replace')[:500])

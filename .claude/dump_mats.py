import socket, json

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

# Export material info to a JSON file (avoids console encoding issues)
result = send_cmd('execute_code', {
    'code': '''
import bpy, json

mats_info = []
for obj in bpy.data.objects:
    if 'mesh' not in obj.name.lower():
        continue
    if obj.type != 'MESH':
        continue

    for idx, mat in enumerate(obj.data.materials):
        if mat is None:
            mats_info.append({"obj": obj.name, "slot": idx, "mat_name": None, "textures": []})
            continue

        textures = []
        if mat.use_nodes:
            for node in mat.node_tree.nodes:
                if node.type == 'TEX_IMAGE' and node.image:
                    textures.append({
                        "name": node.image.name,
                        "filepath": node.image.filepath,
                    })

        mats_info.append({
            "obj": obj.name,
            "slot": idx,
            "mat_name": mat.name,
            "textures": textures,
        })

out_path = r"E:/Study_Projects/Yae_sakura_3D/YaeSakura_3D/.claude/blender_mats.json"
import os
os.makedirs(os.path.dirname(out_path), exist_ok=True)
with open(out_path, 'w', encoding='utf-8') as f:
    json.dump(mats_info, f, ensure_ascii=False, indent=2)

print(f"Dumped {len(mats_info)} material slots to {out_path}")
'''
})
print(result.get('result', {}).get('result', ''))

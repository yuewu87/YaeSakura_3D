import socket, json

def send_cmd(cmd_type, params={}):
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.settimeout(30)
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
    return resp.decode('utf-8', errors='replace')

code = '''
import bpy, json

data = []
for obj in bpy.data.objects:
    if obj.type != "MESH":
        continue

    info = {
        "name": obj.name,
        "vertices": len(obj.data.vertices),
        "materials": []
    }

    for idx, mat in enumerate(obj.data.materials):
        if mat is None:
            info["materials"].append({"idx": idx, "name": None})
            continue

        tex_info = {"idx": idx, "mat_name": mat.name, "textures": []}
        if mat.use_nodes:
            for node in mat.node_tree.nodes:
                if node.type == "TEX_IMAGE" and node.image:
                    tex_info["textures"].append({
                        "name": node.image.name,
                        "path": node.image.filepath,
                        "size": list(node.image.size)
                    })

        info["materials"].append(tex_info)

    data.append(info)

# Save to file to avoid encoding issues
import os
out = r"E:/Study_Projects/Yae_sakura_3D/YaeSakura_3D/.claude/nr_scene_info.json"
os.makedirs(os.path.dirname(out), exist_ok=True)
with open(out, "w", encoding="utf-8") as f:
    json.dump(data, f, ensure_ascii=False, indent=2)
print("Dumped " + str(len(data)) + " meshes")
'''

resp = send_cmd('execute_code', {'code': code})
print(resp[:500])

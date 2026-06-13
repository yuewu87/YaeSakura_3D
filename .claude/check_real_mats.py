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

# Check the main SkinnedMeshRenderer object
result = send_cmd('execute_code', {
    'code': '''
import bpy

# The main model mesh (not rigid body parts)
for obj in bpy.data.objects:
    if 'mesh' in obj.name.lower() and 'rigid' not in obj.name.lower():
        print(f"\\n=== {obj.name} (type={obj.type}) ===")
        print(f"  Material slots: {len(obj.data.materials)}")
        for idx, mat in enumerate(obj.data.materials):
            if mat is None:
                print(f"  [{idx}] None")
                continue
            print(f"  [{idx}] {mat.name}")
            if mat.use_nodes:
                for node in mat.node_tree.nodes:
                    if node.type == 'TEX_IMAGE' and node.image:
                        print(f"       TEX: {node.image.name} file={node.image.filepath}")
            else:
                print(f"       (no nodes, use_nodes=False)")

# Also check for any material with texture nodes
print("\\n=== All materials with textures ===")
for mat in bpy.data.materials:
    if mat.use_nodes:
        for node in mat.node_tree.nodes:
            if node.type == 'TEX_IMAGE' and node.image:
                print(f"  Material: {mat.name} -> {node.image.name} ({node.image.filepath})")
'''
})
output = result.get('result', {}).get('result', result.get('message', ''))
print(output[:8000])

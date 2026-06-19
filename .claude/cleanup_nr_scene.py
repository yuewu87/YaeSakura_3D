import socket, json

NR_DIR = r"E:\EXE_THINGS\NR_output\2026.06.18_19.56.49_MuMuVMMHeadless.exe_9628\frame_0"

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

# Step 1: Clean up — keep the best 6-texture mesh, delete all duplicates
print("Step 1: Cleaning duplicates, keeping best mesh...")
result = send_cmd('execute_code', {'code': f'''
import bpy, os

NR_DIR = r"{NR_DIR}"

# Reload textures from NR output
reloaded = 0
for img in bpy.data.images:
    name = img.name
    dds_path = os.path.join(NR_DIR, name)
    if os.path.exists(dds_path):
        try:
            img.filepath = dds_path
            img.reload()
            reloaded += 1
        except:
            pass

print(f"Reloaded images: {{reloaded}}")

# Find the best mesh: 9429 verts + 6 textures
best_mesh = None
for obj in bpy.data.objects:
    if obj.type != 'MESH':
        continue
    if len(obj.data.vertices) == 9429:
        mat_count = 0
        tex_count = 0
        for mat in obj.data.materials:
            if mat and mat.use_nodes:
                for node in mat.node_tree.nodes:
                    if node.type == 'TEX_IMAGE' and node.image:
                        tex_count += 1
        if tex_count >= 6:
            best_mesh = obj
            print(f"Best mesh: {{obj.name}} with {{tex_count}} textures")
            break

if best_mesh is None:
    print("No 9429-vert 6-texture mesh found")
else:
    # Delete all OTHER meshes
    deleted_dups = 0
    deleted_small = 0
    for obj in list(bpy.data.objects):
        if obj.type != 'MESH':
            continue
        if obj.name == best_mesh.name:
            continue
        # Keep also the visible ones (props/scene)
        if obj.name in ['mesh_1543_814.nr', 'mesh_1544_815.nr', 'mesh_1545_816.nr',
                        'mesh_1546_817.nr', 'mesh_1547_818.nr', 'mesh_1548_819.nr',
                        'mesh_1549_820.nr', 'mesh_1550_821.nr', 'mesh_1551_822.nr',
                        'mesh_1552_823.nr']:
            continue
        bpy.data.objects.remove(obj, do_unlink=True)
        deleted_dups += 1

    print(f"Deleted duplicate frames: {{deleted_dups}}")
    print(f"Kept: best character + 10 scene/prop meshes")

# Scene info
print(f"Remaining objects: {{len(bpy.data.objects)}}")
'''})
print(f"  {result.get('result', {}).get('result', result.get('message', ''))[:1000]}")

# Step 2: Check material display
print("\nStep 2: Checking materials...")
result = send_cmd('execute_code', {'code': '''
import bpy

for obj in bpy.data.objects:
    if obj.type != 'MESH' or len(obj.data.materials) == 0:
        continue
    mat = obj.data.materials[0]
    if mat is None:
        continue
    print(f"{obj.name}: {len(obj.data.vertices)}v, mat={mat.name}")
    if mat.use_nodes:
        for node in mat.node_tree.nodes:
            if node.type == 'TEX_IMAGE' and node.image:
                img = node.image
                print(f"  TEX: {img.name} size={list(img.size)} has_data={img.has_data}")
'''})
print(f"  {result.get('result', {}).get('result', result.get('message', ''))[:2000]}")

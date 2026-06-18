import socket, json, os

PMX_PATH = "E:/崩坏三模型/崩坏3/八重樱/八重樱_礼服/礼服八重樱-纸月寒绯/礼服八重樱2.0.pmx"
VRM_OUT = "E:/Study_Projects/Yae_sakura_3D/YaeSakura_3D/Assets/Models/Characters/YaeSakura_VRM/yae_sakura.vrm"

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

# Step 1: Clear scene and import PMX
print("Step 1: Importing PMX...")
code = f'''
import bpy
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

pmx = r"{PMX_PATH}"
bpy.ops.mmd_tools.import_model(filepath=pmx, scale=0.08, types={{'MESH', 'ARMATURE'}})
print("PMX imported")

arm_count = 0
mesh_count = 0
for obj in bpy.data.objects:
    if obj.type == 'ARMATURE':
        arm_count += 1
        print("ARMATURE: " + obj.name)
    elif obj.type == 'MESH':
        mesh_count += 1
        if len(obj.data.materials) > 0:
            print("MESH: " + obj.name + " (" + str(len(obj.data.materials)) + " materials)")

print("Total: " + str(arm_count) + " armatures, " + str(mesh_count) + " meshes")
'''
result = send_cmd('execute_code', {'code': code})
output = result.get('result', {}).get('result', result.get('message', ''))
print(f"  {output[:800]}")

# Step 2: Convert materials (mmd_tools → standard Blender materials)
print("\nStep 2: Converting MMD materials...")
result = send_cmd('execute_code', {'code': '''
import bpy

# Convert all MMD materials to EEVEE compatible
for mat in bpy.data.materials:
    if mat.name.startswith("mmd_tools"):
        continue
    try:
        # Refresh to ensure textures are loaded
        if mat.use_nodes:
            for node in mat.node_tree.nodes:
                if node.type == 'TEX_IMAGE' and node.image:
                    node.image.reload()
    except Exception as e:
        print("  Warning on " + mat.name + ": " + str(e)[:100])

print("Materials processed. Count: " + str(len(bpy.data.materials)))
'''})
print(f"  {result.get('result', {}).get('result', result.get('message', ''))[:500]}")

# Step 3: Select all and export VRM
print("\nStep 3: Exporting VRM...")
os.makedirs(os.path.dirname(VRM_OUT), exist_ok=True)

code = f'''
import bpy, os

os.makedirs(r"{os.path.dirname(VRM_OUT)}", exist_ok=True)

# Select all objects
bpy.ops.object.select_all(action='SELECT')

# Export VRM
try:
    bpy.ops.export_scene.vrm(
        filepath=r"{VRM_OUT}",
    )
    print("VRM_EXPORT_OK: " + r"{VRM_OUT}")
except Exception as e:
    print("VRM_EXPORT_FAILED: " + str(e))
    # Try to see what VRM export operators are available
    for name in dir(bpy.ops.export_scene):
        if 'vrm' in name.lower():
            print("  Available: bpy.ops.export_scene." + name)
'''
result = send_cmd('execute_code', {'code': code})
output = result.get('result', {}).get('result', result.get('message', ''))
print(f"  {output[:600]}")

# Check if file was created
if os.path.exists(VRM_OUT):
    sz = os.path.getsize(VRM_OUT)
    print(f"\nDone! VRM created: {VRM_OUT} ({sz/1024:.1f} KB)")
else:
    print("\nVRM not created. Checking for alternative export operators...")

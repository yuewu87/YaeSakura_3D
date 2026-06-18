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
    return json.loads(resp.decode('utf-8'))

# Step 1: Check current scene and the mesh's blend shapes
print("Checking blend shapes on the mesh...")
result = send_cmd('execute_code', {'code': '''
import bpy, json

# Find the SkinnedMeshRenderer mesh
data = []
for obj in bpy.data.objects:
    if obj.type == "MESH" and obj.data.shape_keys:
        key = obj.data.shape_keys.key_blocks
        data.append({
            "obj": obj.name,
            "blendshape_count": len(key),
            "blendshapes": [k.name for k in key]
        })

print(json.dumps(data, ensure_ascii=False))
'''})
output = result.get('result', {}).get('result', '')
print(f"  {output[:2000]}")

# Step 2: Map MMD morphs to VRM expressions using VRM addon API
print("\nSetting up VRM expressions...")
result = send_cmd('execute_code', {'code': '''
import bpy

# Find the main mesh with shape keys
mesh_obj = None
for obj in bpy.data.objects:
    if obj.type == "MESH" and obj.data.shape_keys:
        mesh_obj = obj
        break

if mesh_obj is None:
    print("ERROR: No mesh with shape keys found")
else:
    key_blocks = mesh_obj.data.shape_keys.key_blocks
    print(f"Mesh: {mesh_obj.name}, {len(key_blocks)} shape keys")

    # Mapping: VRM expression → MMD morph name
    mappings = {
        "A": ["あ", "あ２"],
        "I": ["い"],
        "U": ["う"],
        "E": ["え"],
        "O": ["お"],
        "Joy": ["笑い", "にこり"],
        "Angry": ["怒り"],
        "Sorrow": ["困る"],
        "Fun": ["にっこり"],
        "Blink": ["まばたき"],
        "Surprised": ["びっくり", "惊"],
    }

    # Check which morphs exist
    morph_names = [k.name for k in key_blocks]
    print(f"Available morphs: {morph_names[:20]}...")

    for vrm_name, mmd_names in mappings.items():
        found = [n for n in mmd_names if n in morph_names]
        if found:
            print(f"  {vrm_name} → {found[0]} ({'found' if found else 'NOT FOUND'})")
        else:
            print(f"  {vrm_name} → NOT FOUND (tried: {mmd_names})")
'''})
print(f"  {result.get('result', {}).get('result', '')[:1500]}")

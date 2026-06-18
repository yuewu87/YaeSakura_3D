import socket, json, os

NR_DIR = r"E:\EXE_THINGS\NR_output\2026.06.13_19.45.44_BH3.exe_48660\frame_0"
FBX_OUT = r"E:\Study_Projects\Yae_sakura_3D\YaeSakura_3D\Assets\Models\Characters\YaeSakura_NR\yae_nr.fbx"
TEX_OUT = r"E:\Study_Projects\Yae_sakura_3D\YaeSakura_3D\Assets\Models\Characters\YaeSakura_NR\Textures"

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

os.makedirs(os.path.dirname(FBX_OUT), exist_ok=True)
os.makedirs(TEX_OUT, exist_ok=True)

# Step 1: Convert DDS textures to PNG
print("Step 1: Converting DDS to PNG...")
code = f'''
import bpy, os
from pathlib import Path

nr_dir = r"{NR_DIR}"
tex_out = r"{TEX_OUT}"
os.makedirs(tex_out, exist_ok=True)

converted = 0
for mat in bpy.data.materials:
    if not mat.use_nodes:
        continue
    for node in mat.node_tree.nodes:
        if node.type != 'TEX_IMAGE' or not node.image:
            continue
        dds_path = node.image.filepath
        png_name = os.path.splitext(os.path.basename(dds_path))[0] + ".png"
        png_path = os.path.join(tex_out, png_name)

        if os.path.exists(png_path):
            continue

        # Save as PNG
        try:
            node.image.filepath_raw = png_path
            node.image.file_format = 'PNG'
            node.image.save()
            converted += 1
        except Exception as e:
            print(f"  Failed: " + os.path.basename(dds_path) + " - " + str(e))

print(f"Converted " + str(converted) + " textures to PNG")
'''
result = send_cmd('execute_code', {'code': code})
print(f"  Status: {result.get('status')}")
res = result.get('result', {})
print(f"  {res.get('result', res.get('message', ''))[:500]}")

# Step 2: Join meshes and export FBX
print("\nStep 2: Exporting FBX...")
code2 = f'''
import bpy, os
os.makedirs(r"{os.path.dirname(FBX_OUT)}", exist_ok=True)

# Select all mesh objects
bpy.ops.object.select_all(action='SELECT')

# Export FBX
bpy.ops.export_scene.fbx(
    filepath=r"{FBX_OUT}",
    use_selection=True,
    global_scale=1.0,
    apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_UNITS',
    object_types={{'MESH'}},
    use_mesh_modifiers=True,
    mesh_smooth_type='OFF',
    axis_forward='-Z',
    axis_up='Y',
    path_mode='COPY',
    embed_textures=False,
)
print("FBX exported: " + r"{FBX_OUT}")
'''
result = send_cmd('execute_code', {'code': code2})
print(f"  Status: {result.get('status')}")
res = result.get('result', {})
print(f"  {res.get('result', res.get('message', ''))[:300]}")

print(f"\nDone! FBX: {FBX_OUT}")
print(f"Textures: {TEX_OUT}")

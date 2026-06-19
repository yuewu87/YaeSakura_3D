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

print("Analyzing meshes...")

# Get all meshes with their signatures
result = send_cmd('execute_code', {'code': '''
import bpy, json
from collections import defaultdict

groups = defaultdict(list)

for obj in bpy.data.objects:
    if obj.type != 'MESH':
        continue
    verts = len(obj.data.vertices)

    # Count textures
    tex_count = 0
    tex_sizes = []
    for mat in obj.data.materials:
        if mat and mat.use_nodes:
            for node in mat.node_tree.nodes:
                if node.type == 'TEX_IMAGE' and node.image:
                    tex_count += 1
                    s = node.image.size
                    tex_sizes.append(f"{s[0]}x{s[1]}")

    key = f"{verts}v_{tex_count}tex"
    groups[key].append({
        "name": obj.name,
        "vertices": verts,
        "tex_count": tex_count,
        "tex_sizes": tex_sizes
    })

# Summary
summary = []
for key, items in sorted(groups.items()):
    summary.append({
        "key": key,
        "count": len(items),
        "vertices": items[0]["vertices"],
        "tex_count": items[0]["tex_count"],
        "tex_sizes": items[0]["tex_sizes"],
        "keep": items[0]["name"],
    })

print(json.dumps(summary, ensure_ascii=False))
'''})

summary = json.loads(result.get('result', {}).get('result', ''))
print(f"\nUnique mesh groups: {len(summary)}")
for g in summary:
    dup_str = f" ({g['count']} duplicates)" if g['count'] > 1 else ""
    print(f"  {g['key']}: keep {g['keep']}{dup_str}")
    if g['tex_sizes']:
        print(f"    textures: {g['tex_sizes'][:5]}")

# Now delete duplicates, keeping one per group
print("\nDeleting duplicates...")
result = send_cmd('execute_code', {'code': '''
import bpy
from collections import defaultdict

groups = defaultdict(list)
for obj in bpy.data.objects:
    if obj.type != 'MESH':
        continue
    verts = len(obj.data.vertices)
    tex_count = 0
    for mat in obj.data.materials:
        if mat and mat.use_nodes:
            for node in mat.node_tree.nodes:
                if node.type == 'TEX_IMAGE' and node.image:
                    tex_count += 1
    key = f"{verts}v_{tex_count}tex"
    groups[key].append(obj)

deleted = 0
kept = 0
for key, objs in groups.items():
    # Keep first, delete rest
    for obj in objs[1:]:
        bpy.data.objects.remove(obj, do_unlink=True)
        deleted += 1
    kept += 1

print(f"Kept: {kept} unique meshes")
print(f"Deleted: {deleted} duplicates")
print(f"Remaining: {len(bpy.data.objects)}")
'''})
print(f"  {result.get('result', {}).get('result', result.get('message', ''))}")

import socket, json
from collections import defaultdict

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

print("Grouping by vertex count + world position...")

result = send_cmd('execute_code', {'code': '''
import bpy

# Group: (vertex_count, center_x_rounded, center_y_rounded, center_z_rounded)
groups = {}
for obj in bpy.data.objects:
    if obj.type != 'MESH':
        continue
    verts = len(obj.data.vertices)

    # Compute approximate center
    vlist = obj.data.vertices
    n = min(len(vlist), 100)
    cx = sum((obj.matrix_world @ vlist[i].co).x for i in range(n)) / n
    cy = sum((obj.matrix_world @ vlist[i].co).y for i in range(n)) / n
    cz = sum((obj.matrix_world @ vlist[i].co).z for i in range(n)) / n

    # Round to 3 decimal places for position matching
    key = (verts, round(cx, 3), round(cy, 3), round(cz, 3))
    if key not in groups:
        groups[key] = []
    groups[key].append(obj)

# Delete duplicates - keep first, remove rest
deleted = 0
kept = 0
for key, objs in groups.items():
    # Keep the first object
    kept += 1
    for obj in objs[1:]:
        bpy.data.objects.remove(obj, do_unlink=True)
        deleted += 1

print(f"Kept: {kept} (unique position+vertex)")
print(f"Deleted: {deleted} (overlapping duplicates)")
print(f"Remaining in scene: {len(bpy.data.objects)}")
'''})

print(f"  {result.get('result', {}).get('result', result.get('message', ''))}")

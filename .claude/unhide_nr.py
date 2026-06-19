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
import bpy

for obj in bpy.data.objects:
    obj.hide_viewport = False
    obj.hide_set(False)
    obj.select_set(True)
    verts = len(obj.data.vertices) if obj.type == "MESH" else 0
    print("  " + obj.name + ": " + str(verts) + "v")

bpy.context.view_layer.objects.active = bpy.data.objects[0]
print("Total: " + str(len(bpy.data.objects)))
print("All visible now. In Blender: View -> Frame Selected")
'''

resp = send_cmd('execute_code', {'code': code})
print(resp[:2000])

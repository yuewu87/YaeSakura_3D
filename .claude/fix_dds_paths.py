import socket, json, os

NR_DIR = r"E:\EXE_THINGS\NR_output\2026.06.13_19.45.44_BH3.exe_48660\frame_0"

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

code = f'''
import bpy, os

NR_DIR = r"{NR_DIR}"

fixed = 0
for img in bpy.data.images:
    name = img.name
    if name.endswith('.dds'):
        dds_path = os.path.join(NR_DIR, name)
        if os.path.exists(dds_path):
            img.filepath = dds_path
            img.reload()
            fixed += 1
            print("Fixed: " + name + " size now: " + str(img.size[0]) + "x" + str(img.size[1]))

print("Fixed " + str(fixed) + " images")
'''

resp = send_cmd('execute_code', {'code': code})
print(resp[:4000])

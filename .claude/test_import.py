import socket, json

PMX_PATH = "E:/崩坏三模型/崩坏3/八重樱/八重樱_礼服/礼服八重樱-纸月寒绯/礼服八重樱2.0.pmx"

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

# Clear scene first
print('Clearing scene...')
result = send_cmd('execute_code', {
    'code': 'import bpy; bpy.ops.object.select_all(action="SELECT"); bpy.ops.object.delete(use_global=False); print("cleared")'
})
print(f'Clear: {result.get("status")}')

# Import PMX
print('Importing PMX...')
code = '''import bpy
from pathlib import Path
p = Path("E:/崩坏三模型/崩坏3/八重樱/八重樱_礼服/礼服八重樱-纸月寒绯/礼服八重樱2.0.pmx")
print(f"EXISTS: {p.exists()}")
try:
    bpy.ops.mmd_tools.import_model(filepath=str(p), scale=0.08)
    print("IMPORT_OK")
except Exception as e:
    print(f"IMPORT_FAIL: {e}")
'''

result = send_cmd('execute_code', {'code': code})
msg = result.get('message', '')
res = result.get('result', {})
output = res.get('result', '') if isinstance(res, dict) else str(res)
print(f'Status: {result.get("status")}')
print(f'Output: {output}')
if msg:
    print(f'Message: {msg}')

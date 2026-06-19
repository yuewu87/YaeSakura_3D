import socket, json

PMX = r"E:\崩坏三模型\崩坏3\八重樱\八重樱_礼服\礼服八重樱-纸月寒绯\礼服八重樱2.0.pmx"
VMD = r"E:\EXE_THINGS\NR_output\站立待机.vmd"

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

code = f'''
import bpy

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

bpy.ops.mmd_tools.import_model(filepath=r"{PMX}", scale=0.08, types={{'MESH', 'ARMATURE'}})
print("PMX imported")

arm = None
for obj in bpy.data.objects:
    if obj.type == 'ARMATURE':
        arm = obj
        break

if arm:
    bpy.ops.object.select_all(action='DESELECT')
    arm.select_set(True)
    bpy.context.view_layer.objects.active = arm

    bpy.ops.mmd_tools.import_vmd(filepath=r"{VMD}")
    print("VMD imported")

    if arm.animation_data and arm.animation_data.action:
        a = arm.animation_data.action
        frames = int(a.frame_range[1] - a.frame_range[0])
        print(f"Action: {{a.name}}, {{frames}} frames, FPS: {{a.frame_range[1]}}")
    else:
        print("No animation data")
else:
    print("No armature")
'''

result = send_cmd('execute_code', {'code': code})
print(f"Status: {result.get('status')}")
print(result.get('result', {}).get('result', ''))

import asyncio
import websockets
import sys
import json

async def hello(uri):
    full_result = []
    async with websockets.connect(uri) as websocket:
        i = 0
        if not websocket.open and i < 3:
            try:
                websocket = await websocket.connect(uri)
                i += 1
            except:
                i += 1
                pass
                
        wf = open(sys.argv[1], "rb")
        while True:
            data = wf.read(8000)

            if len(data) == 0:
                break

            await websocket.send(data)
            new_result = json.loads(await websocket.recv())
            if 'result' in new_result.keys():
                full_result += new_result['result']

        await websocket.send('{"eof" : 1}')
        return full_result

try:
    full_result = asyncio.get_event_loop().run_until_complete(
        hello(sys.argv[2]))
    print(full_result)
except Exception as e:
    print('Exception occured: {}'.format(e))
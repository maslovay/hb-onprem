import asyncio
import json
import os
import websockets
from flask import Flask, Response
from flask_restplus import reqparse

app = Flask(__name__)
loop = asyncio.get_event_loop()
websocket_host = os.environ['WEBSOCKET'] or 'ws://localhost:2700'


async def hello(uri, path=None):
    full_result = []
    async with websockets.connect(uri) as websocket:
        print("Connection is ok")
        if path is not None:
            wf = open(path, "rb")
            while True:
                data = wf.read(8000)

                if len(data) == 0:
                    break

                await websocket.send(data)
                new_result = json.loads(await websocket.recv())
                if 'result' in new_result.keys():
                    # if 'word' in new_result['result'].keys():
                    print(new_result)
                    full_result += new_result['result']
        await websocket.send('{"eof" : 1}')
        return full_result


def create_parser():
    parser = reqparse.RequestParser()
    parser.add_argument('Path', type=str, location='form', required=False)
    return parser


@app.route('/stt', methods=['GET', 'POST'])
def asr():
    print('Function started')
    try:
        args = create_parser().parse_args()
        print(args)
        path = args['Path']
        print(path)

        if path is not None:
            result = loop.run_until_complete(
                hello(websocket_host, path=path)
            )
            print(result)
            return Response(json.dumps({"result": result}, ensure_ascii=False), status=200)
        else:
            return Response("Error in params", status=400)
    except Exception as e:
        print("Exception occured {}".format(e))
        return Response("Exception occured {}".format(e), status=400)


if __name__ == '__main__':
    app.run(debug=True, host='0.0.0.0', port='8118')


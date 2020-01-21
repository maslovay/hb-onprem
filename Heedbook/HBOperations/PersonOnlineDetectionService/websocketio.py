import socketio
import argparse
import sys
import time

def handler(data):
    print(data)

def parse_args(parser):
    parser.add_argument('--room', help='Room number')
    parser.add_argument('--companyId', help='CompanyId')
    parser.add_argument('--tabletId', help='TabletId')
    parser.add_argument('--role', help='Role name')
    parser.add_argument('--clientId', help='Role name')

    args=parser.parse_args()
    data = {}
    data["room"] = args.room
    data["companyId"] = args.companyId
    data["tabletId"] = args.tabletId
    data["role"] = args.role

    client_data = {}
    client_data["clientId"] = args.clientId
    client_data["type"] = "client"
    return data,  client_data
        
if __name__ == '__main__':
    socket_io = "https://websocket-service-test.azurewebsites.net/"
    parser=argparse.ArgumentParser()
    
    data, client_data = parse_args(parser)
    
    print(data)
    print(client_data)
    connect_event = "connectToRoom"
    connect_event_status = "connectToRoomStatus"
    send_push_event = "sendPush"

    sio = socketio.Client()
    sio.connect(socket_io)
    sio.emit(event=connect_event, data=data)
    sio.emit(event=send_push_event, data=client_data)
    sio.disconnect()
    print('WebSocket finished')

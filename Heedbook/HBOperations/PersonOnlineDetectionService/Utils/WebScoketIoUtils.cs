using System.IO;
using HBLib.Utils;

namespace PersonOnlineDetectionService.Utils
{
    public class WebSocketIoUtils
    {
        public WebSocketIoUtils()
        {
        }

        public string Execute(string room, string companyId, string tabletId, string role, string clientId)
        {
            var cmd = new CMDWithOutput();
            var path = Directory.GetCurrentDirectory(); 
            System.Console.WriteLine(path);

            var args = $"websocketio.py --room {room} --companyId {companyId} --tabletId {tabletId} --role {role} --clientId {clientId}";
            var output = cmd.runCMD("python3", args);
            return output;

        }
    }

}
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using HBLib.Data;
using System.Drawing;
using System.Net.Sockets;
using STAN.Client;

namespace HBLib.Utils
{
    public static class HeedbookMessengerStatic
    {
        //variables
        static string blobStorageConnectionString = EnvVar.Get("heedbook_STORAGE");
        static string serviceBusConnectionString = EnvVar.Get("heedbook_SERVICEBUS");
        static string oneSignalAppId = EnvVar.Get("OneSignalAppId");
        static string mongoDBConnectionString = EnvVar.Get("MongoDBConnectionString");
        static string mongoDBDataBase = EnvVar.Get("MongoDBDataBase");        

        //accounts
        //static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(blobStorageConnectionString);
        
        static void SocketConfigurator(Socket s) => s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        

        //clients
        public static HttpClient httpClient = new HttpClient();
        //public static CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
        //public static TopicClient topicClient;
        [ThreadStatic] private static RecordsContext _context;
        public static RecordsContext context
        {
            get
            {
                if (_context == null)
                {
                    _context = new RecordsContext();
                }
                return _context;
            }
        }
        public static MemoryStream memoryStream = new MemoryStream();
        
        //TODO write docs
        //TODO testing
        //SERVICE BUS
        public static class MQMessenger
        {
            public delegate void MessageHandler(object sender, string message);
            public static bool isInited { get; private set; } = false;

            public static string _srvUrl { get; private set; }
            public static string _appName { get; private set; }
            public static string _cstrName { get; private set; }

            private static StanConnectionFactory _connFact = new StanConnectionFactory();
            private static IStanConnection _conn = null;
            private static bool _isSubsSaved = true;
            
            #region SubsActStore
            private struct SubscriptionAction
            {
                public string topic;
                public MessageHandler handler;
                public string qName;
                public string dName;
            };
            private static List<SubscriptionAction> _subsActs = new List<SubscriptionAction>();
            #endregion

            #region InitReinit
            public static void Init(string srvUrl, string appName, string cstrName, bool isSubsSaved = true)
            {
                if (isInited) throw new InvalidOperationException("MQ messenger has already been initialised.");
                if (srvUrl is null) throw new ArgumentNullException("srvUrl", "\"srvUrl\" argument can't be null.");
                if (appName is null) throw new ArgumentNullException("appName", "\"appName\" argument can't be null.");
                if (cstrName is null) throw new ArgumentNullException("cstrName", "\"cstrName\" argument can't be null.");

                _srvUrl = srvUrl;
                _appName = appName;
                _cstrName = cstrName;
                _isSubsSaved = isSubsSaved;

                var opts = StanOptions.GetDefaultOptions();
                opts.NatsURL = srvUrl;
                try
                {
                    _conn = _connFact.CreateConnection(_cstrName, _appName, opts);
                }
                catch (Exception e)
                {
                    throw new InitialisationException("Error on making connection to server.", e);
                }
                isInited = true;
            }
            public static async Task InitAsync(string srvUrl, string appName, string cstrName, bool isSubsSaved = true)
            {
                if (isInited) throw new InvalidOperationException("MQ messenger has already been initialised.");
                if (srvUrl is null) throw new ArgumentNullException("srvUrl", "\"srvUrl\" argument can't be null.");
                if (appName is null) throw new ArgumentNullException("appName", "\"appName\" argument can't be null.");
                if (cstrName is null) throw new ArgumentNullException("cstrName", "\"cstrName\" argument can't be null.");

                _srvUrl = srvUrl;
                _appName = appName;
                _cstrName = cstrName;
                _isSubsSaved = isSubsSaved;

                var opts = StanOptions.GetDefaultOptions();
                opts.NatsURL = srvUrl;
                try
                {
                    _conn = _connFact.CreateConnection(_cstrName, _appName, opts);
                }
                catch (Exception e)
                {
                    throw new InitialisationException("Error on making connection to server.", e);
                }
                isInited = true;

                return;
            }

            private static void RestoreSubs()
            {
                foreach (var action in _subsActs)
                {
                    EventHandler<StanMsgHandlerArgs> inHndl = (obj, inMsg) =>
                    {
                        string msg = Encoding.UTF8.GetString(inMsg.Message.Data);
                        action.handler(obj, msg);
                    };
                    var opts = StanSubscriptionOptions.GetDefaultOptions();
                    opts.DurableName = action.dName ?? opts.DurableName;
                    if (action.qName is null) _conn.Subscribe(action.topic, opts, inHndl);
                    else _conn.Subscribe(action.topic, action.qName, opts, inHndl);
                }
            }
            public static void ReInit(string srvUrl = null, string appName = null, string cstrName = null, bool isSubsSaved = true)
            {
                if (!isInited) throw new InvalidOperationException("MQ messenger has not been initialised.");

                try
                {
                    _conn.NATSConnection.Dispose();
                    _conn.Dispose();
                }
                catch (StanCloseRequestException e)
                {
                    Console.WriteLine($"Error: {e}"); //TODO make logging
                }

                _srvUrl = srvUrl ?? _srvUrl;
                _appName = appName ?? _appName;
                _cstrName = cstrName ?? _cstrName;
                _isSubsSaved = isSubsSaved;

                var opts = StanOptions.GetDefaultOptions();
                opts.NatsURL = _srvUrl;
                try
                {
                    _conn = _connFact.CreateConnection(_cstrName, _appName, opts);
                }
                catch (Exception e)
                {
                    throw new ReinitialisationException("Error on making connection to server.", e);
                }
                if (_isSubsSaved)
                {
                    try
                    {
                        RestoreSubs();
                    }
                    catch (Exception e)
                    {
                        throw new ReinitialisationException("Couldn't restore subs", e);
                    }
                }
            }
            public static async Task ReInitAsync(string srvUrl = null, string appName = null, string cstrName = null, bool isSubsSaved = true)
            {
                if (!isInited) throw new InvalidOperationException("MQ messenger has not been initialised.");

                try
                {
                    _conn.NATSConnection.Dispose();
                    _conn.Dispose();
                }
                catch (StanCloseRequestException e)
                {
                    Console.WriteLine($"Error: {e}"); //TODO make logging
                }

                _srvUrl = srvUrl ?? _srvUrl;
                _appName = appName ?? _appName;
                _cstrName = cstrName ?? _cstrName;
                _isSubsSaved = isSubsSaved;

                var opts = StanOptions.GetDefaultOptions();
                opts.NatsURL = _srvUrl;
                try
                {
                    _conn = _connFact.CreateConnection(_cstrName, _appName, opts);
                }
                catch (Exception e)
                {
                    throw new ReinitialisationException("Error on making connection to server.", e);
                }
                if (_isSubsSaved)
                {
                    try
                    {
                        RestoreSubs();
                    }
                    catch (Exception e)
                    {
                        throw new ReinitialisationException("Couldn't restore subs", e);
                    }
                }

                return;
            }
            #endregion

            #region PubSub
            public static void Publish(string topic, string msg)
            {
                if (!isInited) throw new InvalidOperationException("MQ messenger has not been initialised.");
                try
                {
                    _conn.Publish(topic, Encoding.UTF8.GetBytes(msg));
                }
                catch (StanException)
                {
                    try
                    {
                        ReInit();
                        _conn.Publish(topic, Encoding.UTF8.GetBytes(msg));
                    }
                    catch (StanException e)
                    {
                        //TODO log something
                        throw new PublishException(msg, e);
                    }
                }
            }

            public static void Subscribe(string topic, MessageHandler handler, string dName = null, string qName = null)
            {
                if (topic is null) throw new ArgumentNullException("topic", "Topic name can't be null.");
                if (!isInited) throw new InvalidOperationException("MQ messenger has not been initialised.");
                EventHandler<StanMsgHandlerArgs> inHndl = (obj, inMsg) =>
                {
                    string msg = Encoding.UTF8.GetString(inMsg.Message.Data);
                    handler(obj, msg);
                };
                var opts = StanSubscriptionOptions.GetDefaultOptions();
                opts.DurableName = dName ?? opts.DurableName;

                try
                {
                    if (qName is null) _conn.Subscribe(topic, opts, inHndl);
                    else _conn.Subscribe(topic, qName, opts, inHndl);
                    _subsActs.Add(new SubscriptionAction
                    {
                        topic = topic,
                        handler = handler,
                        qName = qName,
                        dName = dName
                    });
                }
                catch (StanException)
                {
                    try
                    {
                        ReInit();
                        if (qName is null) _conn.Subscribe(topic, opts, inHndl);
                        else _conn.Subscribe(topic, qName, opts, inHndl);
                        _subsActs.Add(new SubscriptionAction
                        {
                            topic = topic,
                            handler = handler,
                            qName = qName,
                            dName = dName
                        });
                    }
                    catch (StanException e)
                    {
                        //TODO log something
                        throw new SubscribeException("Error when subscribing to MQ.", e);
                    }
                }
            }
            #endregion

            #region Dispose
            public static void Dispose()
            {
                if (!isInited) throw new InvalidOperationException("MQ messenger has not been initialised.");

                try
                {
                    _conn.NATSConnection.Dispose();
                    _conn.Dispose();
                }
                catch (StanCloseRequestException e)
                {
                    Console.WriteLine($"Error: {e}"); //TODO make logging
                }
            }
            #endregion

            #region Exceptions
            public class InitialisationException : Exception
            {
                private static string baseMsg = "Error on MQ messenger's initialisation.";

                public InitialisationException() : base(baseMsg) { }
                public InitialisationException(string errMsg) : base(baseMsg + "\n" + errMsg) { }
                public InitialisationException(string errMsg, Exception innerException) : base(baseMsg + "\n" + errMsg, innerException) { }
                public InitialisationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
            }

            public class ReinitialisationException : Exception
            {
                private static string baseMsg = "Error on MQ messenger's reinitialisation.";

                public ReinitialisationException() : base(baseMsg) { }
                public ReinitialisationException(string errMsg) : base(baseMsg + "\n" + errMsg) { }
                public ReinitialisationException(string errMsg, Exception innerException) : base(baseMsg + "\n" + errMsg, innerException) { }
                public ReinitialisationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
            }

            public class PublishException : Exception
            {
                private static string baseMsg = "Error when publishing the message to MQ.";
                private string MQMessage = null;

                public PublishException() : base(baseMsg) { }
                public PublishException(string MQMessage) : base(baseMsg)
                {
                    this.MQMessage = MQMessage;
                }
                public PublishException(string MQMessage, Exception innerException) : base(baseMsg, innerException)
                {
                    this.MQMessage = MQMessage;
                }
                public PublishException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

                public override string ToString()
                {
                    return base.ToString() + "\nMessage in MQ: " + (MQMessage) ?? "[NOT PRESENTED]";
                }
            }

            public class SubscribeException : Exception
            {
                private static string baseMsg = "Error when subscribing to MQ.";

                public SubscribeException() : base(baseMsg) { }
                public SubscribeException(string msg) : base(baseMsg + "\n" + msg) { }
                public SubscribeException(string msg, Exception innerException) : base(baseMsg + "\n" + msg, innerException) { }
                public SubscribeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
            }
            #endregion
        }

        //PUSH NOTIFICATION 
        public class PushNotificationMessenger
        {
            public static string oneSignalUrl = "https://onesignal.com/api/v1/notifications";

            //create and send push notification using OneSignal
            public static async void Push(string[] oneSignalId, string messageTitle, string messageText, string messageLink)
            {
                httpClient.DefaultRequestHeaders.Accept.Clear();
                var request = new
                {
                    app_id = oneSignalAppId,
                    include_player_ids = oneSignalId,
                    url = messageLink,
                    headings = new
                    {
                        en = messageTitle,
                    },
                    contents = new
                    {
                        en = messageText,
                    }
                };
                HttpResponseMessage response = await httpClient.PostAsync(oneSignalUrl, new StringContent(JsonConvert.SerializeObject(request).ToString(), Encoding.UTF8, "application/json"));
            }

            //create and send push notification using OneSignal
            public static async void SendNotification(string[] oneSignalId, string messageTitle, string messageText, string messageLink)
            {
                httpClient.DefaultRequestHeaders.Accept.Clear();
                var request = new
                {
                    app_id = oneSignalAppId,
                    include_player_ids = oneSignalId,
                    url = messageLink,
                    headings = new
                    {
                        en = messageTitle,
                    },
                    contents = new
                    {
                        en = messageText,
                    }
                };
                HttpResponseMessage response = await httpClient.PostAsync(oneSignalUrl, new StringContent(JsonConvert.SerializeObject(request).ToString(), Encoding.UTF8, "application/json"));
            }

            //send push notifiation to companies managers
            public static void SendNotificationToCompanyManagers(int CompanyId, string mesHeading, string mesContent, string url = null)
            {
                //todo! MVP - first user in company is Manager
                var user = context.ApplicationUsers.Where(p => p.CompanyId == CompanyId).OrderBy(p => p.CreationDate).First();
                List<string> OneSignalIds = new List<string>();

                if (user.OneSignalId != null)
                {
                    List<string> UserIds = JsonConvert.DeserializeObject<List<string>>(user.OneSignalId);
                    OneSignalIds.AddRange(UserIds);
                }

                var oneSignalIdsArray = OneSignalIds.ToArray();
                SendNotification(oneSignalIdsArray, mesHeading, mesContent, url);
            }

            //send notifaction to user
            public static void SendNotificationToUser(string applicationUserId, string mesHeading, string mesContent, string url = null)
            {
                //get oneSignalIds
                var user = context.ApplicationUsers.First(p => p.Id == applicationUserId);
                if (user.OneSignalId != null)
                {
                    List<string> OneSignalIds = JsonConvert.DeserializeObject<List<string>>(user.OneSignalId);

                    var oneSignalIdsArray = OneSignalIds.ToArray();
                    SendNotification(oneSignalIdsArray, mesHeading, mesContent, url);
                }
            }

        }
        
    }
}

        

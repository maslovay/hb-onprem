using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HBLib.Data;
using System.Net.Sockets;
using RabbitMQ.Client;
//using STAN.Client;

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
            public static bool IsInited { get; private set; } = false;
            public static readonly int ThreadNoDefault = 350;

            public static string _srvUrl { get; private set; }
            public static string _appName { get; private set; }
            public static string _cstrName { get; private set; }

            private static ConnectionFactory _connFact = new ConnectionFactory();
            private static IConnection _conn = null;
            private static bool _isInReinit = false;
            private static int _threadNo = ThreadNoDefault;
            private static int _threadNoMax = 1950;
            private static IModel _channel = null;


            #region Settings
            public class MQSettings
            {
                public bool AuthomaticRecoveryEnabled { get; set; } = true;
                public TimeSpan ContinuationTimeout { get; set; } = new TimeSpan(0, 0, 5);
                public string HostName { get; set; } = "localhost";
                public TimeSpan NetworkRecoveryInterval { get; set; } = new TimeSpan(0, 0, 15);
                public string Password { get; set; } = "guest";
                public int Port { get; set; } = AmqpTcpEndpoint.UseDefaultPort;
                public int RequestedConnectionTimeout { get; set; } = 5000;
                public ushort RequestedHeartbeat { get; set; } = 15;
                public string UserName { get; set; } = "guest";

                public int ThreadNo { get; set; } = 0;
            }
            public static MQSettings _settings { get; private set; } = new MQSettings();
            #endregion

            #region SubsActStore
            private struct SubscriptionAction
            {
                public string topic;
                public MessageHandler handler;
            };
            private static List<SubscriptionAction> _subsActs = new List<SubscriptionAction>();
            #endregion

            #region InitReinit
            public static void Init(MQSettings settings)
            {
                if (IsInited) throw new InvalidOperationException("MQ messenger has already been initialised.");
                if (settings.ThreadNo < 0 || settings.ThreadNo > _threadNoMax) throw new ArgumentOutOfRangeException("settings.threadNo", settings.ThreadNo, "Invalid number of threads.");
                _connFact = new ConnectionFactory()
                {
                    AutomaticRecoveryEnabled = settings.AuthomaticRecoveryEnabled,
                    ContinuationTimeout = settings.ContinuationTimeout,
                    HostName = settings.HostName,
                    NetworkRecoveryInterval = settings.NetworkRecoveryInterval,
                    Password = settings.Password,
                    Port = settings.Port,
                    RequestedConnectionTimeout = settings.RequestedConnectionTimeout,
                    RequestedHeartbeat = settings.RequestedHeartbeat,
                    UserName = settings.UserName
                };
                _settings = settings;

                _threadNo = settings.ThreadNo == 0 ? _threadNo : settings.ThreadNo;
                
                try
                {
                    _conn = _connFact.CreateConnection();
                }
                catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException e)
                {
                    throw new InitialisationException("Error: unable to connect to server.", e);
                }
                try
                {
                    _channel = _conn.CreateModel();
                }
                catch (Exception e)
                {
                    throw new InitialisationException("Error: unable to create a channel.", e);
                }
                IsInited = true;
            }
            public static void Init() => Init(_settings);
            public static async Task InitAsync(MQSettings settings)
            {
                await Task.Run(() => Init(settings) );
                return;
            }
            public static async Task InitAsync() => await InitAsync(_settings);

            private static void RestoreSubs()
            {
                foreach (var action in _subsActs)
                {
                    _channel.QueueDeclare(action.topic, durable: true, exclusive: false, autoDelete: false);
                    var consumer = new RabbitMQ.Client.Events.EventingBasicConsumer(_channel);
                    consumer.Received += (model, inMsg) =>
                    {
                        while (_threadNo <= 0) { } //waiting for free thread 
                        _threadNo--;               //signal for locked thread

                                                   //fire-and-forget pattern
                        Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                string msg = Encoding.UTF8.GetString(inMsg.Body);
                                action.handler(model, msg);
                                _channel.BasicAck(inMsg.DeliveryTag, multiple: false);
                            }
                            catch { }
                            finally
                            {
                                _threadNo++;       //free thread
                            }
                        });
                    };

                    _channel.BasicConsume(action.topic, autoAck: false, consumer: consumer);
                }
            }
            public static void ReInit(MQSettings settings, bool dropSubs = false)
            {
                if (!IsInited) throw new InvalidOperationException("MQ messenger has not been initialised.");
                if (settings.ThreadNo < 0 || settings.ThreadNo > _threadNoMax) throw new ArgumentOutOfRangeException("settings.threadNo", settings.ThreadNo, "Invalid number of threads.");

                _isInReinit = true;
                var oldConn = _conn;
                var oldSet = _settings;
                var oldCF = _connFact;
                var oldThreadNo = _threadNo;

                _threadNo = settings.ThreadNo == 0 ? ThreadNoDefault : settings.ThreadNo;
                
                _connFact = new ConnectionFactory()
                {
                    AutomaticRecoveryEnabled = settings.AuthomaticRecoveryEnabled,
                    ContinuationTimeout = settings.ContinuationTimeout,
                    HostName = settings.HostName,
                    NetworkRecoveryInterval = settings.NetworkRecoveryInterval,
                    Password = settings.Password,
                    Port = settings.Port,
                    RequestedConnectionTimeout = settings.RequestedConnectionTimeout,
                    RequestedHeartbeat = settings.RequestedHeartbeat,
                    UserName = settings.UserName
                };
                _settings = settings;

                try
                {
                    _conn = _connFact.CreateConnection();
                    try
                    {
                        _channel = _conn.CreateModel();
                    }
                    catch (Exception e)
                    {
                        _conn.Close();
                        _conn = oldConn;
                        _settings = oldSet;
                        _connFact = oldCF;
                        _threadNo = oldThreadNo;
                        throw new ReinitialisationException("Error: unable to create a channel.", e);
                    }
                    try
                    {
                        oldConn.Close();
                    }
                    catch (IOException)
                    {
                        //do nothing
                        //better implement logging here 
                    }
                    if (!dropSubs)
                    {
                        try
                        {
                            RestoreSubs();
                        }
                        catch (Exception e)
                        {
                            throw new SubscriptionsRestoreException("Couldn't restore subs", e);
                        }
                    }
                }
                catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException e)
                {
                    _conn = oldConn;
                    _settings = oldSet;
                    _connFact = oldCF;
                    _threadNo = oldThreadNo;
                    throw new ReinitialisationException("Error: unable to connect to server.", e);
                }
                finally
                {
                    _isInReinit = false;
                }
                
            }
            public static void ReInit() => ReInit(_settings);
            public static async Task ReInitAsync(MQSettings settings)
            {
                await Task.Run(() => ReInit(settings) );
                return;
            }
            public static async Task ReInitAsync() => await ReInitAsync(_settings);
            #endregion

            #region PubSub
            public static void Publish(string topic, string msg)
            {
                if (topic is null) throw new ArgumentNullException("topic", "Topic name can't be null.");
                if (!IsInited) throw new InvalidOperationException("MQ messenger has not been initialised.");
                while (_isInReinit) { } //waiting for get out of Reinit
                var rawMsg = Encoding.UTF8.GetBytes(msg);

                if (!_conn.IsOpen)
                {
                    try
                    {
                        _conn.Close();
                        _conn.Dispose();
                        ReInit();
                    }
                    catch (ReinitialisationException e)
                    {
                        throw new SubscribeException("No connection to server.", e);
                    }
                }
                try
                {
                    var model = _conn.CreateModel();
                    model.QueueDeclare(topic, durable: true, exclusive: false, autoDelete: false);
                    var props = model.CreateBasicProperties();
                    props.DeliveryMode = 2; //persistent message
                    model.BasicPublish("", topic, props, rawMsg);
                    model.Close();
                }
                catch (Exception e)
                {
                    throw new PublishException("Error on publishing.", e);
                }
            }

            public static void Subscribe(string topic, MessageHandler handler, string dName = null, string gName = null)
            {
                if (topic is null) throw new ArgumentNullException("topic", "Topic name can't be null.");
                if (!IsInited) throw new InvalidOperationException("MQ messenger has not been initialised.");
                if (_channel is null) throw new SubscribeException("No channel to connect to. Consider reinitialisation.");
                while (_isInReinit) { } //waiting for get out of Reinit
                
                if (!_conn.IsOpen)
                {
                    try
                    {
                        _conn.Close();
                        _conn.Dispose();
                        ReInit();
                    }
                    catch (ReinitialisationException e)
                    {
                        throw new SubscribeException("No connection to server.", e);
                    }
                }
                try
                {
                    _channel.QueueDeclare(topic, durable: true, exclusive: false, autoDelete: false);
                    var consumer = new RabbitMQ.Client.Events.EventingBasicConsumer(_channel);
                    consumer.Received += (model, inMsg) =>
                    {
                        while (_threadNo <= 0) { } //waiting for free thread 
                        _threadNo--;               //signal for locked thread
                                                   //fire-and-forget pattern
                        Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                string msg = Encoding.UTF8.GetString(inMsg.Body);
                                handler(model, msg);
                                _channel.BasicAck(inMsg.DeliveryTag, multiple: false);
                            }
                            catch { }
                            finally
                            {
                                _threadNo++;       //free thread
                            }
                        });
                    };

                    _channel.BasicConsume(topic, autoAck: false, consumer: consumer);
                }
                catch (Exception e)
                {
                    throw new SubscribeException("Error on subscribing.", e);
                }
                _subsActs.Add(new SubscriptionAction
                {
                    topic = topic,
                    handler = handler
                });
            }
            #endregion

            #region Dispose
            public static void Dispose()
            {
                if (!IsInited) throw new InvalidOperationException("MQ messenger has not been initialised.");

                try
                {
                    _conn.Close();
                }
                catch (IOException e)
                {
                    Console.WriteLine($"Error: {e}"); //TODO make logging
                }
                finally
                {
                    _conn.Dispose();
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

            public class SubscriptionsRestoreException : Exception
            {
                private static string baseMsg = "Error when restoring subscriptions.";

                public SubscriptionsRestoreException() : base(baseMsg) { }
                public SubscriptionsRestoreException(string errMsg) : base(baseMsg + "\n" + errMsg) { }
                public SubscriptionsRestoreException(string errMsg, Exception innerException) : base(baseMsg + "\n" + errMsg, innerException) { }
                public SubscriptionsRestoreException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
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

        

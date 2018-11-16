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
        //SERVICE BUS
        /// <summary>
        /// Static class for MQ subscribing/publishing. Shared between threads.
        /// </summary>
        public static class MQMessenger
        {
            /// <summary>
            /// Delegate for message handling.
            /// </summary>
            /// <param name="sender">Standard C# inheritted sender object.</param>
            /// <param name="message">String, which is message sent via MQ.</param>
            public delegate void MessageHandler(object sender, string message);
            /// <summary>
            /// Indicates whether class was initialized.
            /// </summary>
            public static bool IsInited { get; private set; } = false;
            /// <summary>
            /// Default number of worker threads.
            /// </summary>
            public static readonly int ThreadNoDefault = 350;

            private static ConnectionFactory _connFact = new ConnectionFactory();
            private static IConnection _conn = null;
            /// <summary>
            /// Used for synchronization 
            /// </summary>
            private static bool _isInReinit = false;
            /// <summary>
            /// Maximum allowed number of worker threads.
            /// </summary>
            private static int _threadNo = ThreadNoDefault;
            /// <summary>
            /// Maximum possible number of worker threads.
            /// </summary>
            private static int _threadNoMax = 1950;
            /// <summary>
            /// Channel, used for RabbitMQ subscribing.
            /// </summary>
            private static IModel _channel = null;


            #region Settings
            /// <summary>
            /// Connection and class settings.
            /// <seealso cref="http://rabbitmq.github.io/rabbitmq-dotnet-client/api/RabbitMQ.Client.ConnectionFactory.html#properties"/>
            /// </summary>
            public class MQSettings
            {
                /// <summary>
                /// Authomatically recover connection if it is lost. RabbitMQ setting. Default to true.
                /// </summary>
                public bool AuthomaticRecoveryEnabled { get; set; } = true;
                /// <summary>
                /// Timeout for network delay in ordinary actions (e.g. subscribing, publishing).
                /// </summary>
                public TimeSpan ContinuationTimeout { get; set; } = new TimeSpan(0, 0, 5);
                /// <summary>
                /// Name of host to connect to.
                /// </summary>
                public string HostName { get; set; } = "localhost";
                /// <summary>
                /// Amount of time client will wait for before re-trying to recover connection.
                /// </summary>
                public TimeSpan NetworkRecoveryInterval { get; set; } = new TimeSpan(0, 0, 15);
                /// <summary>
                /// Password to connect to MQ.
                /// </summary>
                public string Password { get; set; } = "guest";
                /// <summary>
                /// AMQP port to connect to.
                /// </summary>
                public int Port { get; set; } = AmqpTcpEndpoint.UseDefaultPort;
                /// <summary>
                /// Timeout setting for connection attempts (in milliseconds).
                /// </summary>
                public int RequestedConnectionTimeout { get; set; } = 5000;
                /// <summary>
                /// Heartbeat timeout to use when negotiating with the server (in seconds).
                /// <para>Heartbeat is needed to keep TCP connection alive.</para>
                /// </summary>
                public ushort RequestedHeartbeat { get; set; } = 15;
                /// <summary>
                /// User's name to connect to MQ.
                /// </summary>
                public string UserName { get; set; } = "guest";
                /// <summary>
                /// virtual host to connect to.
                /// </summary>
                public string VHost { get; set; } = "/";

                /// <summary>
                /// Maximum allowed number of worker threads. 0 if default.
                /// </summary>
                public int ThreadNo { get; set; } = 0;
            }
            /// <summary>
            /// Current connection and class settings.
            /// </summary>
            public static MQSettings Settings { get; private set; } = new MQSettings();
            #endregion

            #region SubsActStore
            /// <summary>
            /// Represents action on receiving message.
            /// </summary>
            private struct SubscriptionAction
            {
                public string topic;
                public MessageHandler handler;
            };
            /// <summary>
            /// Stores actions on subscriptions. Used when reinitializing (e.g. in case of severe loss of connection).
            /// </summary>
            private static List<SubscriptionAction> _subsActs = new List<SubscriptionAction>();
            #endregion

            #region InitReinit
            /// <summary>
            /// Initialization method. Makes a connection to MQ server with given settings.
            /// </summary>
            /// <exception cref="InvalidOperationException">Thrown when class has been already initialized.</exception>
            /// <exception cref="ArgumentOutOfRangeException">Thrown when bad settings are given (e.g. invalid value of <see cref="MQSettings.ThreadNo"/> was given).</exception>
            /// <exception cref="InitializationException">Thrown when one couldn't connect to MQ server.</exception>
            /// <param name="settings">Settings to use.<seealso cref="MQSettings"/></param>
            public static void Init(MQSettings settings)
            {
                if (IsInited) throw new InvalidOperationException("MQ messenger has already been initialized.");
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
                    UserName = settings.UserName,
                    VirtualHost = settings.VHost
                };
                Settings = settings;

                _threadNo = settings.ThreadNo == 0 ? _threadNo : settings.ThreadNo;
                
                try
                {
                    _conn = _connFact.CreateConnection();
                }
                catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException e)
                {
                    throw new InitializationException("Error: unable to connect to server.", e);
                }
                try
                {
                    _channel = _conn.CreateModel();
                }
                catch (Exception e)
                {
                    throw new InitializationException("Error: unable to create a channel.", e);
                }
                IsInited = true;
            }
            /// <summary>
            /// Initialization method. Makes a connection to MQ server with current (default) settings.
            /// </summary>
            /// <exception cref="InvalidOperationException">Thrown when class has been already initialized.</exception>
            /// <exception cref="ArgumentOutOfRangeException">Thrown when bad settings are given (e.g. invalid value of <see cref="MQSettings.ThreadNo"/> was given).</exception>
            /// <exception cref="InitializationException">Thrown when one couldn't connect to MQ server.</exception>
            public static void Init() => Init(Settings);
            /// <summary>
            /// Async initialization method. Makes a connection to MQ server with given settings.
            /// </summary>
            /// <exception cref="InvalidOperationException">Thrown when class has been already initialized.</exception>
            /// <exception cref="ArgumentOutOfRangeException">Thrown when bad settings are given (e.g. invalid value of <see cref="MQSettings.ThreadNo"/> was given).</exception>
            /// <exception cref="InitializationException">Thrown when one couldn't connect to MQ server.</exception>
            /// <param name="settings">Settings to use.<seealso cref="MQSettings"/></param>
            public static async Task InitAsync(MQSettings settings)
            {
                await Task.Run(() => Init(settings) );
                return;
            }
            /// <summary>
            /// Async initialization method. Makes a connection to MQ server with current (default) settings.
            /// </summary>
            /// <exception cref="InvalidOperationException">Thrown when class has been already initialized.</exception>
            /// <exception cref="ArgumentOutOfRangeException">Thrown when bad settings are given (e.g. invalid value of <see cref="MQSettings.ThreadNo"/> was given).</exception>
            /// <exception cref="InitializationException">Thrown when one couldn't connect to MQ server.</exception>
            public static async Task InitAsync() => await InitAsync(Settings);

            /// <summary>
            /// Restore actions on subscriptions when reinitializing.
            /// </summary>
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

            /// <summary>
            /// Reinitialization method. Kills old connection and sets up new one with current settings.
            /// <para>If one can't establish new connection, old one is keeped open. Previous settings are also saved.</para>
            /// </summary>
            /// <exception cref="InvalidOperationException">Thrown when class has not been yet initialized.</exception>
            /// <exception cref="ArgumentOutOfRangeException">Thrown when bad settings are given (e.g. invalid value of <see cref="MQSettings.ThreadNo"/> was given).</exception>
            /// <exception cref="ReinitializationException">Thrown when one couldn't connect to MQ server.</exception>
            /// <exception cref="SubscriptionsRestoreException">Thrown when one can't resubscribe previous action on new connection.</exception>
            /// <param name="settings">Settings to use.<seealso cref="MQSettings"/></param>
            /// <param name="dropSubs">Set true to not restore actions on subscriptions. Default set to false.</param>
            public static void ReInit(MQSettings settings, bool dropSubs = false)
            {
                if (!IsInited) throw new InvalidOperationException("MQ messenger has not been initialized.");
                if (settings.ThreadNo < 0 || settings.ThreadNo > _threadNoMax) throw new ArgumentOutOfRangeException("settings.threadNo", settings.ThreadNo, "Invalid number of threads.");

                _isInReinit = true;
                var oldConn = _conn;
                var oldSet = Settings;
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
                Settings = settings;

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
                        Settings = oldSet;
                        _connFact = oldCF;
                        _threadNo = oldThreadNo;
                        throw new ReinitializationException("Error: unable to create a channel.", e);
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
                    else
                    {
                        _subsActs = new List<SubscriptionAction>();
                    }
                }
                catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException e)
                {
                    _conn = oldConn;
                    Settings = oldSet;
                    _connFact = oldCF;
                    _threadNo = oldThreadNo;
                    throw new ReinitializationException("Error: unable to connect to server.", e);
                }
                finally
                {
                    _isInReinit = false;
                }

            }
            /// <summary>
            /// Reinitialization method. Kills old connection and sets up new one with current settings.
            /// <para>If one can't establish new connection, old one is keeped open. Previous settings are also saved.</para>
            /// </summary>
            /// <exception cref="InvalidOperationException">Thrown when class has not been yet initialized.</exception>
            /// <exception cref="ArgumentOutOfRangeException">Thrown when bad settings are given (e.g. invalid value of <see cref="MQSettings.ThreadNo"/> was given).</exception>
            /// <exception cref="ReinitializationException">Thrown when one couldn't connect to MQ server.</exception>
            /// <exception cref="SubscriptionsRestoreException">Thrown when one can't resubscribe previous action on new connection.</exception>
            /// <param name="dropSubs">Set true to not restore actions on subscriptions. Default set to false.</param>
            public static void ReInit(bool dropSubs = false) => ReInit(Settings, dropSubs);
            /// <summary>
            /// Async reinitialization method. Kills old connection and sets up new one with current settings.
            /// <para>If one can't establish new connection, old one is keeped open. Previous settings are also saved.</para>
            /// </summary>
            /// <exception cref="InvalidOperationException">Thrown when class has not been yet initialized.</exception>
            /// <exception cref="ArgumentOutOfRangeException">Thrown when bad settings are given (e.g. invalid value of <see cref="MQSettings.ThreadNo"/> was given).</exception>
            /// <exception cref="ReinitializationException">Thrown when one couldn't connect to MQ server.</exception>
            /// <exception cref="SubscriptionsRestoreException">Thrown when one can't resubscribe previous action on new connection.</exception>
            /// <param name="settings">Settings to use.<seealso cref="MQSettings"/></param>
            /// <param name="dropSubs">Set true to not restore actions on subscriptions. Default set to false.</param>
            public static async Task ReInitAsync(MQSettings settings, bool dropSubs = false)
            {
                await Task.Run(() => ReInit(settings, dropSubs) );
                return;
            }
            /// <summary>
            /// Async reinitialization method. Kills old connection and sets up new one with current settings.
            /// <para>If one can't establish new connection, old one is keeped open. Previous settings are also saved.</para>
            /// </summary>
            /// <exception cref="InvalidOperationException">Thrown when class has not been yet initialized.</exception>
            /// <exception cref="ArgumentOutOfRangeException">Thrown when bad settings are given (e.g. invalid value of <see cref="MQSettings.ThreadNo"/> was given).</exception>
            /// <exception cref="ReinitializationException">Thrown when one couldn't connect to MQ server.</exception>
            /// <exception cref="SubscriptionsRestoreException">Thrown when one can't resubscribe previous action on new connection.</exception>
            /// <param name="dropSubs">Set true to not restore actions on subscriptions. Default set to false.</param>
            public static async Task ReInitAsync(bool dropSubs = false) => await ReInitAsync(Settings, dropSubs);
            #endregion

            #region PubSub
            /// <summary>
            /// Publish a message on topic (publisher/subscribe scheme).
            /// </summary>
            /// <param name="topic">Topic to which is message sent.</param>
            /// <param name="msg">Message to send.</param>
            /// <exception cref="ArgumentNullException">Thrown when topic or message are null.</exception>
            /// <exception cref="InvalidOperationException">Thrown when class has not been yet initialized.</exception>
            /// <exception cref="PublishException">Thrown when one couldn't send message to MQ.</exception>
            public static void Publish(string topic, string msg)
            {
                if (topic is null) throw new ArgumentNullException("topic", "Topic name can't be null.");
                if (msg is null) throw new ArgumentNullException("msg", "Message can't be null.");
                if (!IsInited) throw new InvalidOperationException("MQ messenger has not been initialized.");
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
                    catch (ReinitializationException e)
                    {
                        throw new PublishException("No connection to server.", e);
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

            /// <summary>
            /// Subscribe an action to topic (publisher/subscribe scheme).
            /// <para>Received message is processed by fire-and-forget pattern.</para>
            /// </summary>
            /// <param name="topic">Topic to subscribe on.</param>
            /// <param name="handler">Action to be done on message receiving.</param>
            /// <exception cref="ArgumentNullException">Thrown when topic or handler are null.</exception>
            /// <exception cref="InvalidOperationException">Thrown when class has not been yet initialized.</exception>
            /// <exception cref="SubscribeException">Thrown when one couldn't subscribe to MQ.</exception>
            public static void Subscribe(string topic, MessageHandler handler)
            {
                if (topic is null) throw new ArgumentNullException("topic", "Topic name can't be null.");
                if (handler is null) throw new ArgumentNullException("handler", "Message handler can't be null.");
                if (!IsInited) throw new InvalidOperationException("MQ messenger has not been initialized.");
                if (_channel is null) throw new SubscribeException("No channel to connect to. Consider reinitialization.");
                while (_isInReinit) { } //waiting for get out of Reinit
                
                if (!_conn.IsOpen)
                {
                    try
                    {
                        _conn.Close();
                        _conn.Dispose();
                        ReInit();
                    }
                    catch (ReinitializationException e)
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
                            catch
                            {
                                _channel.BasicAck(inMsg.DeliveryTag, multiple: false);
                            }
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
            /// <summary>
            /// Use this method to properly close connection to MQ server.
            /// </summary>
            /// <exception cref="InvalidOperationException">Thrown when class has not been yet initialized.</exception>
            public static void Dispose()
            {
                if (!IsInited) return;

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
            /// <summary>
            /// Thrown when errors occured on initialization.
            /// </summary>
            public class InitializationException : Exception
            {
                private static string baseMsg = "Error on MQ messenger's initialization.";

                public InitializationException() : base(baseMsg) { }
                public InitializationException(string errMsg) : base(baseMsg + "\n" + errMsg) { }
                public InitializationException(string errMsg, Exception innerException) : base(baseMsg + "\n" + errMsg, innerException) { }
                public InitializationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
            }

            /// <summary>
            /// Thrown when errors occured on subscription.
            /// </summary>
            public class SubscriptionsRestoreException : Exception
            {
                private static string baseMsg = "Error when restoring subscriptions.";

                public SubscriptionsRestoreException() : base(baseMsg) { }
                public SubscriptionsRestoreException(string errMsg) : base(baseMsg + "\n" + errMsg) { }
                public SubscriptionsRestoreException(string errMsg, Exception innerException) : base(baseMsg + "\n" + errMsg, innerException) { }
                public SubscriptionsRestoreException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
            }

            /// <summary>
            /// Thrown when errors occured on reinitialization.
            /// </summary>
            public class ReinitializationException : Exception
            {
                private static string baseMsg = "Error on MQ messenger's reinitialization.";

                public ReinitializationException() : base(baseMsg) { }
                public ReinitializationException(string errMsg) : base(baseMsg + "\n" + errMsg) { }
                public ReinitializationException(string errMsg, Exception innerException) : base(baseMsg + "\n" + errMsg, innerException) { }
                public ReinitializationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
            }

            /// <summary>
            /// Thrown when errors occured on publishing.
            /// </summary>
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

            /// <summary>
            /// Thrown when errors occured on subscribing.
            /// </summary>
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

        

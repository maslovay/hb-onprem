using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace HBLib.Utils
{
    public class HeedbookMessenger
    {
        public BlobStorageMessenger blobStorage;
        public MongoDBMessenger mongoDB;
        public OneSignalMessenger oneSignal;
        public ServiceBusMessenger serviceBus;
        public SlackMessenger slack;

        public HeedbookMessenger(String blobStorageConnectionString = null,
            String servisBusConnectionString = null,
            String oneSignalAppId = null,
            String mongoDBConnectionString = null,
            String mongoDBDatabase = null,
            String slackWebhookURL = null)
        {
            blobStorageConnectionString = blobStorageConnectionString ?? EnvVar.Get("heedbook_STORAGE");
            blobStorage = new BlobStorageMessenger(blobStorageConnectionString);

            servisBusConnectionString = servisBusConnectionString ?? EnvVar.Get("heedbook_SERVICEBUS");
            serviceBus = new ServiceBusMessenger(servisBusConnectionString);

            oneSignalAppId = oneSignalAppId ?? EnvVar.Get("OneSignalAppId");
            oneSignal = new OneSignalMessenger(oneSignalAppId);

            mongoDBConnectionString = mongoDBConnectionString ?? EnvVar.Get("MongoDBConnectionString");
            mongoDBDatabase = mongoDBDatabase ?? EnvVar.Get("MongoDBDatabase");
            mongoDB = new MongoDBMessenger(mongoDBConnectionString, mongoDBDatabase);

            slackWebhookURL = slackWebhookURL ?? EnvVar.Get("SlackWebhookAlertURL");
            slack = new SlackMessenger(slackWebhookURL);
        }

        //create and post a Blob to heedbook Blob container
        public void SendBlob(String containerName, String fileName, Stream blobStream,
            IDictionary<String, String> metadata = null, String queueName = null, String topicName = null)
        {
            blobStorage.SendBlob(containerName, fileName, blobStream, metadata);
            if (queueName != null) SendQueue(queueName, $"{containerName}/{fileName}");
            if (topicName != null) Publish(topicName, $"{containerName}/{fileName}");
        }

        //create and post a Blob to heedbook Blob container
        public void SendBlob(String containerName, String fileName, String fn,
            IDictionary<String, String> metadata = null, String queueName = null, String topicName = null)
        {
            blobStorage.SendBlob(containerName, fileName, fn, metadata);
            if (queueName != null) SendQueue(queueName, $"{containerName}/{fileName}");
            if (topicName != null) Publish(topicName, $"{containerName}/{fileName}");
        }

        //return the SAS for blob
        public String GetBlobSASUrl(String containerName, String blobName)
        {
            return blobStorage.GetBlobSASUrl(containerName, blobName);
        }

        //return the SAS for blobs
        public List<String> GetBlobsSASUrl(String containerName, List<String> blobNames)
        {
            return blobStorage.GetBlobsSASUrl(containerName, blobNames);
        }

        public CloudBlockBlob GetBlob(String containerName, String blobName)
        {
            return blobStorage.GetBlob(containerName, blobName);
        }

        public CloudBlockBlob GetBlob(String path)
        {
            var msgSplit = Regex.Split(path, "/");
            var containerName = msgSplit[0];
            var blobName = msgSplit[1];
            return GetBlob(containerName, blobName);
        }

        public void PublishViaQueues(String topic, String msg)
        {
            // fix topic name
            topic = topic.Replace(".", "-");

            var settings = EnvVar.GetAll();
            var key = $"sub-{topic}";

            if (!settings.ContainsKey(key)) throw new Exception($"No subs found for topic {topic}");

            var subNames = Regex.Split(settings[key], ",");

            foreach (var subName in subNames)
                Retry.Do(() => blobStorage.SendQueue(subName, msg), TimeSpan.FromSeconds(1), 5);
        }

        //create and post a Queue to heedbook service bus
        public void Publish(String topic, String msg, Boolean sendRaw = false)
        {
            Retry.Do(() => serviceBus.Publish(topic, msg, sendRaw), TimeSpan.FromSeconds(1), 5);
        }

        //create and post a Queue to heedbook service bus
        public void SendQueue(String queue, String msg)
        {
            Retry.Do(() => serviceBus.SendQueue(queue, msg), TimeSpan.FromSeconds(1), 5);
        }

        public void SendQueueBatch(String queue, List<String> msgs)
        {
            Retry.Do(() => serviceBus.SendQueueBatch(queue, msgs), TimeSpan.FromSeconds(1), 5);
        }


        //todo: switch
        ////create and send push notification using OneSignal
        //public async void Push(string[] oneSignalId, string messageTitle, string messageText, string messageLink) {
        //    oneSignal.Push(oneSignalId, messageTitle, messageText, messageLink);
        //}

        //create and send push notification using OneSignal
        public void SendPushNotification(String[] oneSignalId, String messageTitle, String messageText,
            String messageLink)
        {
            //this.PostSlack(String.Join("-", new List<string> { messageTitle, messageText, messageLink}));
            oneSignal.Push(oneSignalId, messageTitle, messageText, messageLink);
        }

        public void PostSlack(String msg)
        {
            Retry.Do(() => slack.Post(msg), TimeSpan.FromSeconds(1), 5);
        }
    }

    public class BlobStorageMessenger
    {
        public String connectionString;
        private Boolean createContainerIfMissing;
        public CloudStorageAccount storageAccount;

        public BlobStorageMessenger(String connectionString)
        {
            this.connectionString = connectionString;
            storageAccount = CloudStorageAccount.Parse(connectionString);
        }

        //create and post a Blob to heedbook Blob container
        public async void SendBlob(String containerName, String fileName, Stream blobStream,
            IDictionary<String, String> metadata = null)
        {
            var client = storageAccount.CreateCloudBlobClient();
            var container = client.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();

            var blob = container.GetBlockBlobReference(fileName);
            await blob.UploadFromStreamAsync(blobStream);

            //todo: not necessary, apparently
            await blob.FetchAttributesAsync();

            if (metadata != null)
            {
                foreach (var kvp in metadata) blob.Metadata.Add(kvp.Key, kvp.Value);
                await blob.SetMetadataAsync();
            }
        }

        //create and post a Blob to heedbook Blob container
        public void SendBlob(String containerName, String fileName, String localFileName,
            IDictionary<String, String> metadata = null)
        {
            using (var fileStream = File.OpenRead(localFileName))
            {
                SendBlob(containerName, Path.GetFileName(localFileName), fileStream, metadata);
            }
        }

        //return the SAS for blob
        public String GetBlobSASUrl(String containerName, String blobName)
        {
            var blobClient = storageAccount.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference(containerName);

            var blob = blobContainer.GetBlockBlobReference(blobName);

            var sasConstraints = new SharedAccessBlobPolicy();
            sasConstraints.SharedAccessExpiryTime = DateTime.UtcNow.AddHours(1);
            sasConstraints.Permissions = SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Read;

            var sasContainerToken = blob.GetSharedAccessSignature(sasConstraints);

            return blob.Uri + sasContainerToken;
        }

        public CloudBlockBlob GetBlob(String containerName, String blobName)
        {
            var blobClient = storageAccount.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference(containerName);

            var blob = blobContainer.GetBlockBlobReference(blobName);
            return blob;
        }

        //return the SAS for blobs
        public List<String> GetBlobsSASUrl(String containerName, List<String> blobNames)
        {
            var blobClient = storageAccount.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference(containerName);

            var sasConstraints = new SharedAccessBlobPolicy
            {
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(1),
                Permissions = SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Read
            };

            var blobLinks = new List<String>();

            foreach (var blobName in blobNames)
            {
                var blob = blobContainer.GetBlockBlobReference(blobName);
                var sasContainerToken = blob.GetSharedAccessSignature(sasConstraints);
                blobLinks.Add(blob.Uri + sasContainerToken);
            }

            return blobLinks;
        }

        // create and post a Queue to heedbook Queue container
        public async void SendQueue(String queueContainerName, String queueMessageText)
        {
            var queueClient = storageAccount.CreateCloudQueueClient();
            var queueFolder = queueClient.GetQueueReference(queueContainerName);
            await queueFolder.CreateIfNotExistsAsync();

            var queueMessage = new CloudQueueMessage(queueMessageText);
            await queueFolder.AddMessageAsync(queueMessage);
        }

        public async void CopyBlob(String containerName, String targetContainerName, String blobName,
            String targetBlobName = null)
        {
            var cloudBlobClient = storageAccount.CreateCloudBlobClient();
            var sourceContainer = cloudBlobClient.GetContainerReference(containerName);
            var targetContainer = cloudBlobClient.GetContainerReference(targetContainerName);
            var sourceBlob = sourceContainer.GetBlockBlobReference(blobName);

            targetBlobName = targetBlobName == null
                ? blobName
                : targetBlobName;

            var targetBlob = targetContainer.GetBlockBlobReference(targetBlobName);

            await targetBlob.StartCopyAsync(sourceBlob);
        }

        public async Task<Boolean> Exist(String containerName, String fileName)
        {
            var client = storageAccount.CreateCloudBlobClient();
            var container = client.GetContainerReference(containerName);
            if (createContainerIfMissing && await container.CreateIfNotExistsAsync())
                await container.SetPermissionsAsync(new BlobContainerPermissions
                    {PublicAccess = BlobContainerPublicAccessType.Blob});
            var blob = container.GetBlockBlobReference(fileName);
            return await blob.ExistsAsync();
        }
    }

    public class ServiceBusMessenger
    {
        public String connectionString;

        public ServiceBusMessenger(String connectionString)
        {
            this.connectionString = connectionString;
        }

        //create and post a Queue to heedbook service bus
        public void Publish(String topic, String msg, Boolean sendRaw = false)
        {
            var client = TopicClient.CreateFromConnectionString(connectionString, topic);
            //var message = new BrokeredMessage(new Dictionary<string, string> { { "a", "b" } }, new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(Dictionary<string, string>)));
            BrokeredMessage message;
            if (sendRaw)
                message = new BrokeredMessage(msg, new DataContractJsonSerializer(typeof(String)));
            else
                message = new BrokeredMessage(msg);
            client.Send(message);

            // todo: handle properly connections
            client.Close();
        }


        //create and post a Queue to heedbook service bus
        public void SendQueue(String queue, String msg, Boolean createIfNotExists = true)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
            var exist = namespaceManager.QueueExists(queue);

            if (!exist)
            {
                if (createIfNotExists)
                {
                    namespaceManager.CreateQueue(queue);

                    // todo: make properly
                    Thread.Sleep(1000);
                    while (!namespaceManager.QueueExists(queue)) Thread.Sleep(1000);
                }
                else
                {
                    throw new Exception($"Queue {queue} does not exist!");
                }
            }

            var client = QueueClient.CreateFromConnectionString(connectionString, queue);
            client.Send(new BrokeredMessage(msg));
            // todo: handle properly
            client.Close();
        }

        public void SendQueueBatch(String queue, List<String> msgs, Boolean createIfNotExists = true)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
            var exist = namespaceManager.QueueExists(queue);

            if (!exist)
            {
                if (createIfNotExists)
                {
                    namespaceManager.CreateQueue(queue);

                    // todo: make properly
                    Thread.Sleep(1000);
                    while (!namespaceManager.QueueExists(queue)) Thread.Sleep(1000);
                }
                else
                {
                    throw new Exception($"Queue {queue} does not exist!");
                }
            }

            var client = QueueClient.CreateFromConnectionString(connectionString, queue);
            client.SendBatch(msgs.Select(msg => new BrokeredMessage(msg)).ToList());
            // todo: handle properly
            client.Close();
        }
    }

    public class OneSignalMessenger
    {
        public String appId;
        public String url;

        public OneSignalMessenger(String appId)
        {
            this.appId = appId;
            url = "https://onesignal.com/api/v1/notifications";
        }

        //create and send push notification using OneSignal
        public async void Push(String[] oneSignalId, String messageTitle, String messageText, String messageLink)
        {
            // todo: handle properly connections
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                var request = new
                {
                    app_id = appId,
                    include_player_ids = oneSignalId,
                    url = messageLink,
                    headings = new
                    {
                        en = messageTitle
                    },
                    contents = new
                    {
                        en = messageText
                    }
                };
                var response = await client.PostAsync(url,
                    new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json"));
            }
        }
    }

    public class MongoDBMessenger : MongoClient
    {
        public String connectionString;
        public String databaseName;
        public IMongoDatabase db;

        public MongoDBMessenger(String connectionString, String dbName) : base(GetInitSettings(connectionString))
        {
            this.connectionString = connectionString;
            databaseName = dbName;
            db = GetDatabase(dbName);
        }

        public static MongoClientSettings GetInitSettings(String connectionString)
        {
            var settings = MongoClientSettings.FromUrl(MongoUrl.Create(connectionString));
            settings.SocketTimeout = TimeSpan.FromSeconds(3600 * 12);
            return settings;
        }

        public void SafeInsert(IMongoCollection<BsonDocument> collection, BsonDocument doc)
        {
            Retry.Do(() => collection.InsertOne(doc), TimeSpan.FromSeconds(3), 5);
        }

        public void SafeInsert(IMongoCollection<BsonDocument> collection, List<BsonDocument> docs)
        {
            Retry.Do(() => collection.InsertMany(docs), TimeSpan.FromSeconds(3), 5);
        }

        public void SafeReplaceOne(IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter,
            BsonDocument replacement, UpdateOptions options = null)
        {
            Retry.Do(() => collection.ReplaceOne(filter,
                    replacement,
                    new UpdateOptions {IsUpsert = true}),
                TimeSpan.FromSeconds(3),
                5);
        }
    }

    // todo: make proper slackmessenger, using template from Slack_Http_AlertWebhookBridge
    public class SlackMessenger
    {
        public String webhookUrl;

        public SlackMessenger(String webhookUrl)
        {
            this.webhookUrl = webhookUrl;
        }

        public void Post(String text,
            String color = "#e01563")
        {
            var js_str = $@"{{
    'attachments': [
        {{
            'color': '{color}',
            'text': '{text}',
        }}
    ]
}}";
            dynamic message = JsonConvert.DeserializeObject(js_str);

            // todo: handle properly connections
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                var response = client.PostAsync(webhookUrl,
                    new StringContent(JsonConvert.SerializeObject(message).ToString(),
                        Encoding.UTF8,
                        "application/json")).Result;
            }
        }
    }
}
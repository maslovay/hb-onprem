using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HBData;
using HBLib.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;


public static class HeedbookMessengerStatic
{
    //accounts
    private static CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(BlobStorageConnectionString);

    //clients
    public static HttpClient HttpClient = new HttpClient();


    public static CloudBlobClient BlobClient = StorageAccount.CreateCloudBlobClient();
    public static TopicClient TopicClient;
    public static MongoClient MongoClient = new MongoClient(MongoDbSettings());
    public static IMongoDatabase MongoDB = MongoClient.GetDatabase(MongoDBDataBase);

    public static MemoryStream MemoryStream = new MemoryStream();

    //variables
    private static string BlobStorageConnectionString = EnvVar.Get("heedbook_STORAGE");
    private static string MongoDBConnectionString = EnvVar.Get("MongoDBConnectionString");

    private static string MongoDBDataBase = EnvVar.Get("MongoDBDataBase");
    private static string OneSignalAppId = EnvVar.Get("OneSignalAppId");
    private static string ServiceBusConnectionString = EnvVar.Get("heedbook_SERVICEBUS");
    private static string SlackWebhookURL = EnvVar.Get("SlackWebhookAlertURL");
    private static RecordsContext _context;
    public static RecordsContext Context()
    {
        if (_context != null)
        {
            return _context;
        }
        var optionsBuilder = new DbContextOptionsBuilder<RecordsContext>();
        optionsBuilder.UseNpgsql(EnvVar.Get("PostgresConnectionString"));
        _context = new RecordsContext(optionsBuilder.Options);
        return _context;
    }

    private static MongoClientSettings MongoDbSettings()
    {
        var settings = MongoClientSettings.FromUrl(MongoUrl.Create(MongoDBConnectionString));
        settings.ConnectTimeout = TimeSpan.FromMinutes(10);
        settings.SocketTimeout = TimeSpan.FromMinutes(10);
        settings.MaxConnectionIdleTime = TimeSpan.FromMinutes(10);
        settings.ClusterConfigurator = builder =>
            builder.ConfigureTcp(tcp => tcp.With(socketConfigurator: (Action<Socket>) SocketConfigurator));
        return settings;
    }

    private static void SocketConfigurator(Socket s)
    {
        s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
    }

    //HTTP REQUESTS
    public static class HTTPMessenger
    {
        //Send post request
        public static async Task<string> PostAsync(string url, HttpContent content)
        {
            HttpClient.DefaultRequestHeaders.Clear();
            var response = await HttpClient.PostAsync(url, content);
            return response.ToString();
        }


        //Send get request
        public static async Task<string> GetAsync(string url, HttpContent content)
        {
            HttpClient.DefaultRequestHeaders.Clear();
            var response = await HttpClient.GetAsync(url);
            return response.ToString();
        }
    }

    //BLOBS
    public static class BlobStorageMessenger
    {
        //create and post a Blob to heedbook Blob container
        public static void SendBlob(string containerName, string fileName, Stream blobStream,
            IDictionary<string, string> metadata = null, string topicName = null)
        {
            var container = BlobClient.GetContainerReference(containerName);
            container.CreateIfNotExistsAsync();

            var blob = container.GetBlockBlobReference(fileName);
            blob.UploadFromStreamAsync(blobStream);

            //todo: not necessary, apparently
            blob.FetchAttributesAsync();

            if (metadata != null)
            {
                foreach (var kvp in metadata) blob.Metadata.Add(kvp.Key, kvp.Value);

                blob.SetMetadataAsync();
            }

            if (topicName != null) ServiceBusMessenger.Publish(topicName, $"{containerName}/{fileName}");
        }

        // delete blob from container
        public static void DeleteBlob(string containerName, string blobName)
        {
            var blobContainer = BlobClient.GetContainerReference(containerName);
            var blob = blobContainer.GetBlockBlobReference(blobName);
            blob.DeleteIfExistsAsync();
        }

        //create and post a Blob to heedbook Blob container
        public static void SendBlob(string containerName, string fileName, string localFileName,
            IDictionary<string, string> metadata = null, string topicName = null)
        {
            using (var fileStream = File.OpenRead(localFileName))
            {
                SendBlob(containerName, Path.GetFileName(localFileName), fileStream, metadata, topicName);
            }
        }

        //return the SAS for blob
        public static string GetBlobSASUrl(string containerName, string blobName, int? expirationHours = null)
        {
            var blobContainer = BlobClient.GetContainerReference(containerName);
            var blob = blobContainer.GetBlockBlobReference(blobName);
            var sasConstraints = new SharedAccessBlobPolicy();

            expirationHours = expirationHours == null
                ? 1
                : expirationHours;

            sasConstraints.SharedAccessExpiryTime = DateTime.UtcNow.AddHours((int) expirationHours);
            sasConstraints.Permissions = SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Read;

            var sasContainerToken = blob.GetSharedAccessSignature(sasConstraints);

            return blob.Uri + sasContainerToken;
        }


        //return the SAS for container
        public static string GetContainerSASUrl(string containerName, int? expirationHours = null)
        {
            var blobContainer = BlobClient.GetContainerReference(containerName);
            var sasConstraints = new SharedAccessBlobPolicy();

            expirationHours = expirationHours == null
                ? 1
                : expirationHours;

            sasConstraints.SharedAccessExpiryTime = DateTime.UtcNow.AddHours((int) expirationHours);
            sasConstraints.Permissions = SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Read;

            var sasContainerToken = blobContainer.GetSharedAccessSignature(sasConstraints);

            return sasContainerToken;
        }

        //return the SAS for blob
        public static List<string> GetBlobSASUrl(string containerName, List<string> blobNames,
            int? expirationHours = null)
        {
            var blobContainer = BlobClient.GetContainerReference(containerName);

            expirationHours = expirationHours == null
                ? 1
                : expirationHours;

            var sasConstraints = new SharedAccessBlobPolicy();
            sasConstraints.SharedAccessExpiryTime = DateTime.UtcNow.AddHours((int) expirationHours);
            sasConstraints.Permissions = SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Read;

            var blobLinks = new List<string>();

            foreach (var blobName in blobNames)
            {
                var blob = blobContainer.GetBlockBlobReference(blobName);
                var sasContainerToken = blob.GetSharedAccessSignature(sasConstraints);
                blobLinks.Add(blob.Uri + sasContainerToken);
            }

            return blobLinks;
        }


        //copy blob from one container to another
        public static void CopyBlob(string containerName, string targetContainerName, string blobName,
            string targetBlobName = null)
        {
            var cloudBlobClient = StorageAccount.CreateCloudBlobClient();
            var sourceContainer = cloudBlobClient.GetContainerReference(containerName);
            var targetContainer = cloudBlobClient.GetContainerReference(targetContainerName);
            var sourceBlob = sourceContainer.GetBlockBlobReference(blobName);

            targetBlobName = targetBlobName == null
                ? blobName
                : targetBlobName;

            var targetBlob = targetContainer.GetBlockBlobReference(targetBlobName);

            targetBlob.StartCopyAsync(sourceBlob);
        }

        //crop images (f.e. avatar)
        public static async Task CreateAvatar(string FileName, string DialogueId, int FaceRectangleWidth,
            int FaceRectangleHeight, int FaceRectangleLeft, int FaceRectangleTop)
        {
            //get frame
            var containerframes =
                BlobClient.GetContainerReference(EnvVar.Get("BlobContainerFrames"));
            var blob = containerframes.GetBlockBlobReference(FileName);

            Stream blobMemoryStream = new MemoryStream();
            await blob.DownloadToStreamAsync(blobMemoryStream);
            blobMemoryStream.Position = 0;

            //read image from blob
            var image = Image.FromStream(blobMemoryStream);
            var imageHeight = image.Height;
            var imageWidth = image.Width;

            //set parameters to crop

            var left = Convert.ToInt32(Math.Max(0, FaceRectangleLeft - 0.2 * FaceRectangleWidth));
            var top = Convert.ToInt32(Math.Max(0, FaceRectangleTop - 0.2 * FaceRectangleHeight));
            var width = Convert.ToInt32(Math.Min(1.4 * FaceRectangleWidth, imageWidth - left));
            var height = Convert.ToInt32(Math.Min(1.4 * FaceRectangleHeight, imageHeight - top));

            var destinationImage = new Bitmap(width, height);
            var g = Graphics.FromImage(destinationImage);
            g.DrawImage(
                image,
                new Rectangle(0, 0, width, height),
                new Rectangle(left, top, height, height),
                GraphicsUnit.Pixel
            );
            Stream outputStream = new MemoryStream();
            destinationImage.Save(outputStream, ImageFormat.Jpeg);
            var containerclientavatars =
                BlobClient.GetContainerReference(EnvVar.Get("BlobContainerClientAvatars"));
            var blockBlob = containerclientavatars.GetBlockBlobReference(DialogueId + ".jpg");
            blockBlob.Properties.ContentType = blob.Properties.ContentType;
            outputStream.Position = 0;
            await blockBlob.UploadFromStreamAsync(outputStream);
            outputStream.Position = 0;
            outputStream.SetLength(0);
        }

        public static bool Exist(string containerName, string fileName)
        {
            var client = StorageAccount.CreateCloudBlobClient();
            var blob = client.GetContainerReference(containerName).GetBlockBlobReference(fileName);
            return blob.ExistsAsync().GetAwaiter().GetResult();
        }

        public static string GetBlobUrl(string containerName, string blobName)
        {
            var blobContainer = BlobClient.GetContainerReference(containerName);
            var blob = blobContainer.GetBlockBlobReference(blobName);
            return blob.Uri.ToString();
        }

        public static CloudBlockBlob GetBlob(string containerName, string blobName)
        {
            var blobClient = StorageAccount.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference(containerName);

            var blob = blobContainer.GetBlockBlobReference(blobName);
            return blob;
        }

        public static CloudBlockBlob GetBlob(string path)
        {
            var msgSplit = Regex.Split(path, "/");
            var containerName = msgSplit[0];
            var blobName = msgSplit[1];
            return GetBlob(containerName, blobName);
        }
    }


    //SERVICE BUS
    public static class ServiceBusMessenger
    {
        //create and post a Queue to heedbook service bus
        public static void Publish(string topic, string msg, bool sendRaw = false)
        {
            TopicClient = TopicClient.CreateFromConnectionString(ServiceBusConnectionString, topic);
            BrokeredMessage message;
            if (sendRaw)
                message = new BrokeredMessage(msg,
                    new DataContractJsonSerializer(typeof(string)));
            else
                message = new BrokeredMessage(msg);

            TopicClient.Send(message);
        }
    }

    //PUSH NOTIFICATION 
    public static class PushNotificationMessenger
    {
        public static string oneSignalUrl = "https://onesignal.com/api/v1/notifications";

        //create and send push notification using OneSignal
        public static async void Push(string[] oneSignalId, string messageTitle, string messageText,
            string messageLink)
        {
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            var request = new
            {
                app_id = OneSignalAppId,
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
            var response = await HttpClient.PostAsync(oneSignalUrl,
                new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8,
                    "application/json"));
        }

        //create and send push notification using OneSignal
        public static async void SendNotification(string[] oneSignalId, string messageTitle, string messageText,
            string messageLink)
        {
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            var request = new
            {
                app_id = OneSignalAppId,
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
            var response = await HttpClient.PostAsync(oneSignalUrl,
                new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8,
                    "application/json"));
        }

        //send push notifiation to companies managers
        public static void SendNotificationToCompanyManagers(Guid CompanyId, string mesHeading, string mesContent,
            string url = null)
        {
            //todo! MVP - first user in company is Manager
            var user = Context().ApplicationUsers.Where(p => p.CompanyId == CompanyId).OrderBy(p => p.CreationDate)
                .First();
            var OneSignalIds = new List<string>();

            if (user.OneSignalId != null)
            {
                var UserIds = JsonConvert.DeserializeObject<List<string>>(user.OneSignalId);
                OneSignalIds.AddRange(UserIds);
            }

            var oneSignalIdsArray = OneSignalIds.ToArray();
            SendNotification(oneSignalIdsArray, mesHeading, mesContent, url);
        }

        //send notifaction to user
        public static void SendNotificationToUser(Guid applicationUserId, string mesHeading, string mesContent,
            string url = null)
        {
            //get oneSignalIds
            var user = Context().ApplicationUsers.First(p => p.ApplicationUserId == applicationUserId);
            if (user.OneSignalId != null)
            {
                var OneSignalIds = JsonConvert.DeserializeObject<List<string>>(user.OneSignalId);

                var oneSignalIdsArray = OneSignalIds.ToArray();
                SendNotification(oneSignalIdsArray, mesHeading, mesContent, url);
            }
        }
    }


    //MONGO DB MESSENGER
    public static class MongoDBMessenger
    {
        public static void SafeInsert(string collection, BsonDocument doc)
        {
            var collectionMongo = MongoDB.GetCollection<BsonDocument>(collection);
            Retry.Do(() => collectionMongo.InsertOne(doc), TimeSpan.FromSeconds(3), 5);
        }

        public static void SafeInsert(IMongoCollection<BsonDocument> collection, BsonDocument doc)
        {
            Retry.Do(() => collection.InsertOne(doc), TimeSpan.FromSeconds(3), 5);
        }

        public static void SafeInsert(IMongoCollection<BsonDocument> collection, List<BsonDocument> docs)
        {
            Retry.Do(() => collection.InsertMany(docs), TimeSpan.FromSeconds(3), 5);
        }

        public static void SafeReplaceOne(IMongoCollection<BsonDocument> collection,
            FilterDefinition<BsonDocument> filter, BsonDocument replacement, UpdateOptions options = null)
        {
            Retry.Do(() => collection.ReplaceOne(filter,
                    replacement,
                    new UpdateOptions {IsUpsert = true}),
                TimeSpan.FromSeconds(3),
                5);
        }

        public static void SafeReplaceOne(string collection, FilterDefinition<BsonDocument> filter,
            BsonDocument replacement, UpdateOptions options = null)
        {
            var collectionMongo = MongoDB.GetCollection<BsonDocument>(collection);
            Retry.Do(() => collectionMongo.ReplaceOne(filter,
                    replacement,
                    new UpdateOptions {IsUpsert = true}),
                TimeSpan.FromSeconds(3),
                5);
        }
    }

    //SLACK MESSENGER
    public class SlackMessenger
    {
        public void Post(string text, string color = "#e01563")
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

            HttpClient.DefaultRequestHeaders.Accept.Clear();
            var response = HttpClient.PostAsync(SlackWebhookURL,
                new StringContent(JsonConvert.SerializeObject(message).ToString(), Encoding.UTF8,
                    "application/json")).Result;
        }
    }
}
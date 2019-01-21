using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.ProjectOxford.Face;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HBLib.Utils
{
    public static class HeedbookIdentity
    {
        // params
        private static String locale = EnvVar.Get("locale");
        private static String contentTypeHeader = EnvVar.Get("contentTypeHeader");
        private static readonly String jsonContentHeaderValue = EnvVar.Get("jsonContentHeaderValue");
        private static readonly String streamContentHeaderValue = EnvVar.Get("streamContentHeaderValue");
        private static String operationLocationHeader = EnvVar.Get("operationLocationHeader");
        private static readonly String subscriptionKeyHeader = EnvVar.Get("subscriptionKeyHeader");

        // URI
        private static readonly String uriPersonGroupCreation = EnvVar.Get("uriPersonGroupCreation");
        private static readonly String uriIdentifyFace = EnvVar.Get("uriIdentifyFace");

        //clients
        public static HttpClient httpClient = new HttpClient();

        public class IdentificationMessenger
        {
            // get random subscription key
            public static String GetSubscriptionKey()
            {
                var faceClientStrings = EnvVar.GetAll().Where(kvp => kvp.Key.StartsWith("EmotionServiceClient"))
                                              .Select(kvp => kvp.Value).ToList();
                var faceClientIndex = DateTime.Now.Millisecond % faceClientStrings.Count;
                var subsciptionKey = faceClientStrings[faceClientIndex];
                return subsciptionKey;
            }

            // create new large person group 
            public static async Task<String> CreateLargePersonGroup(String companyId, String largePersonGroupId,
                String personGroupName, String metadata = null)
            {
                // get face subscription key
                var subsciptionKey = GetSubscriptionKey();

                // query params
                var queryString = HttpUtility.ParseQueryString(String.Empty);

                // headers
                httpClient.DefaultRequestHeaders.Add(subscriptionKeyHeader, subsciptionKey);
                var uri = uriPersonGroupCreation + "/" + largePersonGroupId + queryString;

                // body
                var body = new Dictionary<String, String>();
                body["name"] = personGroupName;
                body["userData"] = metadata;

                var json = JsonConvert.SerializeObject(body);
                var con = new StringContent(json);

                // main request
                HttpResponseMessage response;
                con.Headers.ContentType = new MediaTypeHeaderValue(jsonContentHeaderValue);
                response = await httpClient.PutAsync(uri, con);
                var result = await response.Content.ReadAsStringAsync();

                // add data to mongo
                var collectionFaceGroups =
                    HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(
                        EnvVar.Get("CollectionCompanyFaceGroups"));
                var faceGroupInfo = new BsonDocument
                {
                    ["CompanyId"] = companyId,
                    ["CompanyName"] = personGroupName,
                    ["FaceGroupId"] = largePersonGroupId,
                    ["SubscriptionKey"] = subsciptionKey,
                    ["Status"] = "Active",
                    ["ForbiddenFaces"] = new BsonArray()
                };

                //faceGroupInfo["Metadata"] = metadata;
                ;

                collectionFaceGroups.InsertOne(faceGroupInfo);

                return result;
            }

            // get person groups information
            public static async Task<String> GetLargePersonGroup(String subscriptionKey)
            {
                // query params
                var queryString = HttpUtility.ParseQueryString(String.Empty);

                // headers
                httpClient.DefaultRequestHeaders.Add(subscriptionKeyHeader, subscriptionKey);
                var uri = uriPersonGroupCreation + queryString;

                // main request
                HttpResponseMessage response;
                response = await httpClient.GetAsync(uri);
                var result = await response.Content.ReadAsStringAsync();
                return result;
            }

            // get face list information in large person group
            public static async Task<String> GetLargePersonGroup(String subscriptionKey, String largePersonGroupId)
            {
                // query params
                var queryString = HttpUtility.ParseQueryString(String.Empty);

                // headers
                httpClient.DefaultRequestHeaders.Add(subscriptionKeyHeader, subscriptionKey);
                var uri = $"{uriPersonGroupCreation}/{largePersonGroupId}/persons" + queryString;

                // main request
                HttpResponseMessage response;
                response = await httpClient.GetAsync(uri);
                var result = await response.Content.ReadAsStringAsync();
                return result;
            }

            // add forbidden face to companys
            public static void AddForbiddenFace(String largePersonGroupId, String faceId)
            {
                var collectionFaceGroups =
                    HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(
                        EnvVar.Get("CollectionCompanyFaceGroups"));
                var mask = new BsonDocument
                {
                    {"Status", "Active"},
                    {"FaceGroupId", largePersonGroupId}
                };

                var docs = collectionFaceGroups.Find(mask).ToList();
                collectionFaceGroups.UpdateMany(mask,
                    new BsonDocument {{"push", new BsonDocument {{"ForbiddenFaces", faceId}}}});
            }

            // add user to large face group 
            public static async Task<String> AddPersonFace(String subscriptionKey, String largePersonGroupId,
                String fileName)
            {
                // query params
                var queryString = HttpUtility.ParseQueryString(String.Empty);

                // headers
                httpClient.DefaultRequestHeaders.Add(subscriptionKeyHeader, subscriptionKey);

                // url
                var uri = uriPersonGroupCreation + "/" + largePersonGroupId + "/persons?" + queryString;

                // body
                var body = new Dictionary<String, String>();
                body["name"] = fileName;
                body["userData"] = $"Blob storage filename {fileName}";

                var con = new StringContent(JsonConvert.SerializeObject(body));

                // get response
                HttpResponseMessage response;
                con.Headers.ContentType = new MediaTypeHeaderValue(jsonContentHeaderValue);
                response = await httpClient.PostAsync(uri, con);
                var result = JObject.Parse(await response.Content.ReadAsStringAsync());
                return result["personId"].ToString();
            }

            //  add user face to large face group
            public static async Task<String> AddFace(String subscriptionKey, String largePersonGroupId, String faceId,
                Byte[] byteData)
            {
                // query params
                var queryString = HttpUtility.ParseQueryString(String.Empty);

                // face service client
                var cli = new FaceServiceClient(subscriptionKey, EnvVar.Get("FaceServiceApiRoot"));

                // loading photo
                Stream stream = new MemoryStream(byteData);
                var faces = await cli.DetectAsync(stream, true, true);
                if (faces.Count() == 0) return "";

                // target face
                var targetFace = faces[0].FaceRectangle.Left + "," +
                                 faces[0].FaceRectangle.Top + "," +
                                 faces[0].FaceRectangle.Width + "," +
                                 faces[0].FaceRectangle.Height;

                // headers
                httpClient.DefaultRequestHeaders.Add(subscriptionKeyHeader, subscriptionKey);

                // request params
                queryString["userData"] = "first_photo";
                queryString["targetFace"] = targetFace;
                var uri = uriPersonGroupCreation + "/" + largePersonGroupId + "/persons/" + faceId +
                          "/persistedFaces?" + queryString;

                // content
                var content = new ByteArrayContent(byteData);
                content.Headers.ContentType = new MediaTypeHeaderValue(jsonContentHeaderValue);

                // resopnse
                HttpResponseMessage response;
                response = await httpClient.PostAsync(uri, content);
                var result = JObject.Parse(await response.Content.ReadAsStringAsync());
                return result["persistedFaceId"].ToString();
            }

            //  add user face to large face group
            public static async Task<String> AddFace(String subscriptionKey, String largePersonGroupId, String faceId,
                String sas)
            {
                // query params
                var queryString = HttpUtility.ParseQueryString(String.Empty);

                // face service client
                var cli = new FaceServiceClient(subscriptionKey, EnvVar.Get("FaceServiceApiRoot"));

                var faces = await cli.DetectAsync(sas, true, true);
                if (faces.Count() == 0) return "";

                // target face
                var targetFace = faces[0].FaceRectangle.Left + "," +
                                 faces[0].FaceRectangle.Top + "," +
                                 faces[0].FaceRectangle.Width + "," +
                                 faces[0].FaceRectangle.Height;

                // headers
                httpClient.DefaultRequestHeaders.Add(subscriptionKeyHeader, subscriptionKey);

                // request params
                queryString["userData"] = "first_photo";
                queryString["targetFace"] = targetFace;
                var uri = uriPersonGroupCreation + "/" + largePersonGroupId + "/persons/" + faceId +
                          "/persistedFaces?" + queryString;

                //body
                var bodyDict = new Dictionary<String, String>();
                bodyDict["url"] = sas;

                // content
                var json = JsonConvert.SerializeObject(bodyDict);
                var content = new StringContent(json);
                content.Headers.ContentType = new MediaTypeHeaderValue(jsonContentHeaderValue);

                // resopnse
                HttpResponseMessage response;
                response = await httpClient.PostAsync(uri, content);
                var result = JObject.Parse(await response.Content.ReadAsStringAsync());
                return result["persistedFaceId"].ToString();
            }

            //  add user face to large face group
            public static async Task<String> AddFace(String subscriptionKey, String largePersonGroupId, String faceId,
                Byte[] byteData, Microsoft.ProjectOxford.Face.Contract.Face[] faces)
            {
                // query params
                var queryString = HttpUtility.ParseQueryString(String.Empty);

                // face service client
                var cli = new FaceServiceClient(subscriptionKey, EnvVar.Get("FaceServiceApiRoot"));

                // loading photo
                Stream stream = new MemoryStream(byteData);
                if (faces.Count() == 0) return "";

                // target face
                var targetFace = faces[0].FaceRectangle.Left + "," +
                                 faces[0].FaceRectangle.Top + "," +
                                 faces[0].FaceRectangle.Width + "," +
                                 faces[0].FaceRectangle.Height;

                // headers
                httpClient.DefaultRequestHeaders.Add(subscriptionKeyHeader, subscriptionKey);

                // request params
                queryString["userData"] = "first_photo";
                queryString["targetFace"] = targetFace;
                var uri = uriPersonGroupCreation + "/" + largePersonGroupId + "/persons/" + faceId +
                          "/persistedFaces?" + queryString;

                // content
                var content = new ByteArrayContent(byteData);
                content.Headers.ContentType = new MediaTypeHeaderValue(jsonContentHeaderValue);

                // resopnse
                HttpResponseMessage response;
                response = await httpClient.PostAsync(uri, content);
                var result = JObject.Parse(await response.Content.ReadAsStringAsync());
                return result["persistedFaceId"].ToString();
            }

            //  add user face to large face group
            public static async Task<String> AddFace(ILogger log, String subscriptionKey,
                String largePersonGroupId, String faceId, String sas,
                Microsoft.ProjectOxford.Face.Contract.Face[] faces)
            {
                // query params
                var queryString = HttpUtility.ParseQueryString(String.Empty);
                if (faces.Count() == 0) return "";

                // target face
                var targetFace = faces[0].FaceRectangle.Left + "," +
                                 faces[0].FaceRectangle.Top + "," +
                                 faces[0].FaceRectangle.Width + "," +
                                 faces[0].FaceRectangle.Height;

                log.LogInformation($"Target face {targetFace}");
                // headers
                httpClient.DefaultRequestHeaders.Add(subscriptionKeyHeader, subscriptionKey);

                // request params
                queryString["userData"] = "first_photo";
                queryString["targetFace"] = targetFace;
                var uri = uriPersonGroupCreation + "/" + largePersonGroupId + "/persons/" + faceId +
                          "/persistedFaces?" + queryString;

                //body
                var bodyDict = new Dictionary<String, String>();
                bodyDict["url"] = sas;

                // content
                var json = JsonConvert.SerializeObject(bodyDict);
                var content = new StringContent(json);
                content.Headers.ContentType = new MediaTypeHeaderValue(streamContentHeaderValue);

                // resopnse
                HttpResponseMessage response;
                response = await httpClient.PostAsync(uri, content);
                var result = JObject.Parse(await response.Content.ReadAsStringAsync());
                log.LogInformation($"{result}");
                return result["persistedFaceId"].ToString();
            }

            // training
            public static async Task TrainFaceAsync(String subscriptionKey, String largePersonGroupId)
            {
                // train client
                var queryStringTrain = HttpUtility.ParseQueryString(String.Empty);

                // train header
                httpClient.DefaultRequestHeaders.Add(subscriptionKeyHeader, subscriptionKey);

                // uri
                var uriTrain = uriPersonGroupCreation + "/" + largePersonGroupId + "/train?" + queryStringTrain;

                // body
                var bodyTrain = new Dictionary<String, String>();
                var jsonTrain = JsonConvert.SerializeObject(bodyTrain);
                var conTrain = new StringContent(jsonTrain);

                // RESPONSE
                HttpResponseMessage responseTrain;
                responseTrain = await httpClient.PostAsync(uriTrain, conTrain);
                var resultTrain = await responseTrain.Content.ReadAsStringAsync();
            }

            // find face in large face group 
            public static async Task<Face> FindFace(String subscriptionKey, String personGroupId, String sas,
                Int32 maxCandidates = 1, Double confidence = 0.5)
            {
                // face clietn
                var cli = new FaceServiceClient(subscriptionKey, EnvVar.Get("FaceServiceApiRoot"));

                var faceAttr = new List<FaceAttributeType>
                {
                    FaceAttributeType.HeadPose,
                    FaceAttributeType.Smile
                };
                var faces = await cli.DetectAsync(sas, true, false, faceAttr);
                if (faces.Count() == 0)
                    return new Face
                    {
                        status = Face.Status.NoFaceFound
                    };
                var faceId = faces[0].FaceId;

                // uri
                var queryString = HttpUtility.ParseQueryString(String.Empty);
                var uri = uriIdentifyFace + queryString;

                // body
                var faceIds = new List<String>();
                faceIds.Add(faceId.ToString());

                var body = new FaceFindClass
                {
                    largePersonGroupId = personGroupId,
                    faceIds = faceIds,
                    maxNumOfCandidatesReturned = maxCandidates,
                    confidenceThreshold = confidence
                };

                var json = JsonConvert.SerializeObject(body);
                var con = new StringContent(json);
                con.Headers.ContentType = new MediaTypeHeaderValue(jsonContentHeaderValue);

                httpClient.DefaultRequestHeaders.Add(subscriptionKeyHeader, subscriptionKey);

                // response
                HttpResponseMessage response;
                response = await httpClient.PostAsync(uri, con);
                var result =
                    JsonConvert.DeserializeObject<List<FaceResult>>(await response.Content.ReadAsStringAsync());

                // [{"faceId":"8dab855a-d25e-4258-b95c-d44cdf4c1605","candidates":[{"personId":"1f822740-d239-4567-979f-6444438e2a07","confidence":0.7473}]}]

                try
                {
                    var candidate = result[0].candidates[0]["personId"];
                    return new Face
                    {
                        status = Face.Status.FaceFound,
                        faceId = candidate,
                        smileIntensity = faces[0].FaceAttributes.Smile,
                        headRotation = faces[0].FaceAttributes.HeadPose.Yaw
                    };
                }
                catch (Exception e)
                {
                    return new Face
                    {
                        status = Face.Status.Error
                    };
                }
            }

            // find face in large face group 
            public static async Task<Face> FindFace(String subscriptionKey, String personGroupId, Byte[] byteData,
                Int32 maxCandidates = 1, Double confidence = 0.5)
            {
                // face clietn
                var cli = new FaceServiceClient(subscriptionKey, EnvVar.Get("FaceServiceApiRoot"));

                // load to stream
                Stream stream = new MemoryStream(byteData);
                var faceAttr = new List<FaceAttributeType>
                {
                    FaceAttributeType.HeadPose,
                    FaceAttributeType.Smile
                };
                var faces = await cli.DetectAsync(stream, true, false, faceAttr);
                if (faces.Count() == 0)
                    return new Face
                    {
                        status = Face.Status.NoFaceFound
                    };
                var faceId = faces[0].FaceId;

                // uri
                var queryString = HttpUtility.ParseQueryString(String.Empty);
                var uri = uriIdentifyFace + queryString;

                // body
                var faceIds = new List<String>();
                faceIds.Add(faceId.ToString());

                var body = new FaceFindClass();
                body.largePersonGroupId = personGroupId;
                body.faceIds = faceIds;
                body.maxNumOfCandidatesReturned = maxCandidates;
                body.confidenceThreshold = confidence;

                var json = JsonConvert.SerializeObject(body);
                var con = new StringContent(json);
                con.Headers.ContentType = new MediaTypeHeaderValue(jsonContentHeaderValue);

                httpClient.DefaultRequestHeaders.Add(subscriptionKeyHeader, subscriptionKey);

                // response
                HttpResponseMessage response;
                response = await httpClient.PostAsync(uri, con);
                var result =
                    JsonConvert.DeserializeObject<List<FaceResult>>(await response.Content.ReadAsStringAsync());

                // [{"faceId":"8dab855a-d25e-4258-b95c-d44cdf4c1605","candidates":[{"personId":"1f822740-d239-4567-979f-6444438e2a07","confidence":0.7473}]}]

                try
                {
                    var candidate = result[0].candidates[0]["personId"];
                    return new Face
                    {
                        status = Face.Status.FaceFound,
                        faceId = candidate,
                        smileIntensity = faces[0].FaceAttributes.Smile,
                        headRotation = faces[0].FaceAttributes.HeadPose.Yaw
                    };
                }
                catch (Exception e)
                {
                    return new Face
                    {
                        status = Face.Status.Error
                    };
                }
            }

            // find face in large face group 
            public static async Task<Face> FindFace(String subscriptionKey, String personGroupId,
                String sas, Microsoft.ProjectOxford.Face.Contract.Face[] faces, Int32 maxCandidates = 1,
                Double confidence = 0.5)
            {
                // face clietn
                var cli = new FaceServiceClient(subscriptionKey, EnvVar.Get("FaceServiceApiRoot"));
                if (faces.Count() == 0)
                    return new Face
                    {
                        status = Face.Status.NoFaceFound
                    };
                var faceId = faces[0].FaceId;

                // uri
                var queryString = HttpUtility.ParseQueryString(String.Empty);
                var uri = uriIdentifyFace + queryString;

                // body
                var faceIds = new List<String>();
                faceIds.Add(faceId.ToString());

                var body = new FaceFindClass();
                body.largePersonGroupId = personGroupId;
                body.faceIds = faceIds;
                body.maxNumOfCandidatesReturned = maxCandidates;
                body.confidenceThreshold = confidence;

                var json = JsonConvert.SerializeObject(body);
                var con = new StringContent(json);
                con.Headers.ContentType = new MediaTypeHeaderValue(jsonContentHeaderValue);

                httpClient.DefaultRequestHeaders.Add(subscriptionKeyHeader, subscriptionKey);

                // response
                HttpResponseMessage response;
                response = await httpClient.PostAsync(uri, con);
                var result =
                    JsonConvert.DeserializeObject<List<FaceResult>>(await response.Content.ReadAsStringAsync());

                // [{"faceId":"8dab855a-d25e-4258-b95c-d44cdf4c1605","candidates":[{"personId":"1f822740-d239-4567-979f-6444438e2a07","confidence":0.7473}]}]

                try
                {
                    var candidate = result[0].candidates[0]["personId"];
                    return new Face
                    {
                        status = Face.Status.FaceFound,
                        faceId = candidate,
                        smileIntensity = faces[0].FaceAttributes.Smile,
                        headRotation = faces[0].FaceAttributes.HeadPose.Yaw
                    };
                }
                catch (Exception e)
                {
                    return new Face
                    {
                        status = Face.Status.Error
                    };
                }
            }

            // find face in large face group 
            public static async Task<Face> FindFace(String subscriptionKey, String personGroupId, Byte[] byteData,
                Microsoft.ProjectOxford.Face.Contract.Face[] faces, Int32 maxCandidates = 1, Double confidence = 0.5)
            {
                // face clietn
                var cli = new FaceServiceClient(subscriptionKey, EnvVar.Get("FaceServiceApiRoot"));

                // load to stream
                Stream stream = new MemoryStream(byteData);
                if (faces.Count() == 0)
                    return new Face
                    {
                        status = Face.Status.NoFaceFound
                    };
                var faceId = faces[0].FaceId;

                // uri
                var queryString = HttpUtility.ParseQueryString(String.Empty);
                var uri = uriIdentifyFace + queryString;

                // body
                var faceIds = new List<String>();
                faceIds.Add(faceId.ToString());

                var body = new FaceFindClass();
                body.largePersonGroupId = personGroupId;
                body.faceIds = faceIds;
                body.maxNumOfCandidatesReturned = maxCandidates;
                body.confidenceThreshold = confidence;

                var json = JsonConvert.SerializeObject(body);
                var con = new StringContent(json);
                con.Headers.ContentType = new MediaTypeHeaderValue(jsonContentHeaderValue);

                httpClient.DefaultRequestHeaders.Add(subscriptionKeyHeader, subscriptionKey);

                // response
                HttpResponseMessage response;
                response = await httpClient.PostAsync(uri, con);
                var result =
                    JsonConvert.DeserializeObject<List<FaceResult>>(await response.Content.ReadAsStringAsync());

                // [{"faceId":"8dab855a-d25e-4258-b95c-d44cdf4c1605","candidates":[{"personId":"1f822740-d239-4567-979f-6444438e2a07","confidence":0.7473}]}]

                try
                {
                    var candidate = result[0].candidates[0]["personId"];
                    return new Face
                    {
                        status = Face.Status.FaceFound,
                        faceId = candidate,
                        smileIntensity = faces[0].FaceAttributes.Smile,
                        headRotation = faces[0].FaceAttributes.HeadPose.Yaw
                    };
                }
                catch (Exception e)
                {
                    return new Face
                    {
                        status = Face.Status.Error
                    };
                }
            }


            public class FaceFindClass
            {
                public String largePersonGroupId { get; set; }
                public List<String> faceIds { get; set; }
                public Int32 maxNumOfCandidatesReturned { get; set; }
                public Double confidenceThreshold { get; set; }
            }

            public class FaceResult
            {
                public String faceId { get; set; }
                public List<Dictionary<String, String>> candidates { get; set; }
            }

            public class Face
            {
                public enum Status
                {
                    FaceFound = 0,
                    NoFaceFound,
                    TooManyFaces,
                    Error
                }

                public Status status;
                public String faceId { get; set; } = "";
                public Double smileIntensity { get; set; }
                public Double happiness { get; set; } = 0;
                public Double neutral { get; set; } = 0;
                public Double surprise { get; set; } = 0;
                public Double headRotation { get; set; }

                public override String ToString()
                {
                    return $"faceId {faceId};smileIntensity {smileIntensity};headRotation {headRotation}";
                }
            }
        }

        public class CompareMessenger
        {
            // compare two faces
            public static async Task<Boolean> FaceCompare(String subscriptionKey, String fileName1, String fileName2)
            {
                // sas url
                var url1 = HeedbookMessengerStatic.BlobStorageMessenger.GetBlobSASUrl(EnvVar.Get("BlobContainerFrames"),
                    fileName1);
                var url2 = HeedbookMessengerStatic.BlobStorageMessenger.GetBlobSASUrl(EnvVar.Get("BlobContainerFrames"),
                    fileName2);

                // face client
                var cli = new FaceServiceClient(subscriptionKey, EnvVar.Get("FaceServiceApiRoot"));

                // identify first face
                var Identify = await cli.DetectAsync(url1);
                if (Identify.Length == 0) return false;
                var mainFace = (from f in Identify
                    orderby f.FaceRectangle.Width
                    select f).FirstOrDefault();
                var mainFaceId = mainFace.FaceId;

                Identify = await cli.DetectAsync(url2);
                if (Identify.Length == 0) return false;

                mainFace = (from f in Identify
                    orderby f.FaceRectangle.Width
                    select f).FirstOrDefault();
                var comparableFaceId = mainFace.FaceId;
                var verifyRes = cli.VerifyAsync(mainFaceId, comparableFaceId);
                return verifyRes.Result.IsIdentical;
            }

            public static async Task<Boolean> FaceCompareSas(String subscriptionKey, String fileName1, String sasUrl)
            {
                // sas url
                var url1 = HeedbookMessengerStatic.BlobStorageMessenger.GetBlobSASUrl(EnvVar.Get("BlobContainerFrames"),
                    fileName1);

                // face client
                var cli = new FaceServiceClient(subscriptionKey, EnvVar.Get("FaceServiceApiRoot"));

                // identify first face
                var Identify = await cli.DetectAsync(url1);
                if (Identify.Length == 0) return false;
                var mainFace = (from f in Identify
                    orderby f.FaceRectangle.Width
                    select f).FirstOrDefault();
                var mainFaceId = mainFace.FaceId;

                Identify = await cli.DetectAsync(sasUrl);
                if (Identify.Length == 0) return false;

                mainFace = (from f in Identify
                    orderby f.FaceRectangle.Width
                    select f).FirstOrDefault();
                var comparableFaceId = mainFace.FaceId;
                var verifyRes = cli.VerifyAsync(mainFaceId, comparableFaceId);
                return verifyRes.Result.IsIdentical;
            }
        }
    }
}
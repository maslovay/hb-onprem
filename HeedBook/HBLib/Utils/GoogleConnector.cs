using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using RsaKey = System.Security.Cryptography.RSA;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using HBData.Models;
using HBData.Repository;
using HBLib.Model;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace HBLib.Utils
{
    public class GoogleConnector
    {
        private const String GoogleScopes = @"https://www.googleapis.com/auth/devstorage.full_control";

        private const String GoogleCloudContainerName = "hbfiles";

        private const String GoogleAccount = "hb-989@heedbook-project-195914.iam.gserviceaccount.com";

        private const String GoogleKeyFileName = "heedbook-project-195914-c1665bca2cd3.p12";

        private const String FileGoogleCloudPublicAccess = "GoogleFilePublic.xml";
        
        private readonly HttpClient _httpClient;

        private readonly SftpClient _sftpClient;

        private readonly IGenericRepository _repository;
        
        private readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public GoogleConnector(SftpClient sftpClient,
            IServiceScopeFactory scopeFactory,
            HttpClient httpClient)
        {
            _sftpClient = sftpClient;
            _httpClient = httpClient;
            var scope = scopeFactory.CreateScope();
            _repository = scope.ServiceProvider.GetRequiredService<IGenericRepository>();
        }

        public async Task<String> LoadFileToGoogleDrive(String blobGoogleDriveName, String path, String token)
        {
            try
            {
                //transfer blob to byte array
                var stream = await _sftpClient.DownloadFromFtpAsMemoryStreamAsync(path);
                var data = stream.ToArray();

                //prepare the request
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

                //send the request
                HttpContent cont = new ByteArrayContent(data);

                var response = await _httpClient.PostAsync(
                    @"https://www.googleapis.com/upload/storage/v1/b/" + GoogleCloudContainerName +
                    @"/o?uploadType=media&name=" + blobGoogleDriveName, cont);

                response.EnsureSuccessStatusCode();
                var results = await response.Content.ReadAsStringAsync();
                return results;
            }
            catch (Exception e)
            {
                return $"Exception occured {e}";
            }
        }

        public async Task<String> MakeFilePublicGoogleCloud(String filename, String binPath, String token)
        {
            _httpClient.DefaultRequestHeaders.Clear();

            var googleFileGoogleCloudPublicAccess = Path.Combine(binPath, FileGoogleCloudPublicAccess);


            var data = File.ReadAllBytes(googleFileGoogleCloudPublicAccess);

            HttpContent cont = new ByteArrayContent(data);

            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

            var response = await _httpClient.PutAsync(
                @"https://storage.googleapis.com/" + GoogleCloudContainerName + @"/" + filename + @"?acl", cont);

            var results = await response.Content.ReadAsStringAsync();
            return results;
        }

        public async Task<String> MakeFilePublicGoogleCloudPost(String filename, String binPath,
            String token)
        {
            var queryString = HttpUtility.ParseQueryString(String.Empty);

            var uri = "https://www.googleapis.com/storage/v1/b/" + GoogleCloudContainerName + "/o/" + filename +
                      "/acl?" + queryString;

            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

            var googleConfig = Path.Combine(binPath, "GoogleConfig.json");

            var file = File.ReadAllBytes(googleConfig);

            var content = new ByteArrayContent(file);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await _httpClient.PostAsync(uri, content);
            // Just as an example I'm turning the response into a string here
            var responseAsString = await response.Content.ReadAsStringAsync();

            return responseAsString;
        }

        public async Task DeleteFileGoogleCloud(String filename, String token)
        {
            _httpClient.DefaultRequestHeaders.Clear();

            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

            await _httpClient.DeleteAsync(@"https://storage.googleapis.com/" + GoogleCloudContainerName + @"/" +
                                          filename + @"?acl");
        }

        public async Task<GoogleSttLongrunningResult> GetGoogleSTTResults(String googleTransactionId)
        {
            var googleApiKey = await GetApiKey();
            _httpClient.DefaultRequestHeaders.Clear();

            var response = _httpClient.GetAsync("https://speech.googleapis.com/v1/operations/" +
                                                googleTransactionId + "?key=" + googleApiKey).Result;

            var results = await response.Content.ReadAsStringAsync();
            Console.WriteLine(results);
            return JsonConvert.DeserializeObject<GoogleSttLongrunningResult>(results);
        }

        public async Task<String> GetAuthorizationToken(String binPath)
        {
            _httpClient.DefaultRequestHeaders.Clear();

            var jwt = CreateJwt(binPath);

            var dic = new Dictionary<String, String>

            {
                {"grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"},

                {"assertion", jwt}
            };

            var content = new FormUrlEncodedContent(dic);

            var response = await _httpClient.PostAsync("https://accounts.google.com/o/oauth2/token", content);
            dynamic dyn = await response.Content.ReadAsStringAsync();
            var token = JsonConvert.DeserializeObject<JWTToken>(dyn.ToString());
            return token.access_token;
        }

        public class JWTToken
        {
            public string access_token;
            public string token_type;
            public int expires_in;
        }

        public async Task<GoogleTransactionId> Recognize(String fileName, Int32 languageId, string dialogueId, Boolean enableWordTimeOffsets = true,
            Boolean enableSpeakerDiarization = true)
        {
            var apiKey = await GetApiKey();
            if (apiKey == null)
            {
                return null;
            }

            var googleTransactionId = await Retry.Do(async () => { return await RecognizeLongRunning(fileName, apiKey, languageId); },
                TimeSpan.FromSeconds(1), 5);

            return googleTransactionId;
        }

        private async Task<String> GetApiKey()
        {
            //3 - is active. 
            var googleAccount = await _repository.FindOneByConditionAsync<GoogleAccount>(item => item.StatusId == 3);
            return googleAccount.GoogleKey;
        }
        
        private async Task<GoogleTransactionId> RecognizeLongRunning(String fn, String apiKey, Int32 languageId,
            Boolean enableWordTimeOffsets = true)
        {
            var httpClient = new HttpClient();
            /*test example:
                body = {
                "config": {
                    "encoding":"FLAC",
                    "sampleRateHertz": 16000,
                    "languageCode": "en-US",
                    "enableWordTimeOffsets": true,
                    "enableSpeakerDiarization" : true
                },
                "audio": {
                    "uri":"gs://cloud-samples-tests/speech/brooklyn.flac"
                }
            }*/
            var request = new
            {
                config = new
                {
                    encoding = "LINEAR16",
                    sampleRateHertz = 16000,
                    languageCode = GetLanguageName(languageId),
                    enableWordTimeOffsets = enableWordTimeOffsets,
                    //enableSpeakerDiarization,
                    //enableWordConfidence = true,
                    //enableAutomaticPunctuation = true
                    //model = 'video'
                },
                audio = new
                {
                    uri = "gs://hbfiles/" + fn
                }
            };

            var response = httpClient
                          .PostAsync("https://speech.googleapis.com/v1/speech:longrunningrecognize?key=" + apiKey,
                               new StringContent(JsonConvert.SerializeObject(request).ToString(), Encoding.UTF8,
                                   "application/json")).Result;
            response.EnsureSuccessStatusCode();
            var result = JsonConvert.DeserializeObject<GoogleTransactionId>(await response.Content.ReadAsStringAsync());
            return result;
        }

        private static String GetLanguageName(Int32 languageId)
        {
            switch (languageId)
            {
                case 1:
                    return "en-US";
                case 2:
                    return "ru-RU";
                case 3:
                    return "es-ES";
                case 4:
                    return "fr-FR";
                case 5:
                    return "it-IT";
                case 6:
                    return "pt-PT";
                case 7:
                    return "tr-TR";
                case 8:
                    return "de-DE";
                case 9:
                    return "da-DK";
                case 10:
                    return "hu-HU";
                case 11:
                    return "nl-NL";
                case 12:
                    return "nb-NO";
                case 13:
                    return "pl-PL";
                case 14:
                    return "vi-VN";
                case 15:
                    return "ar-SA";
                case 16:
                    return "hi-IN";
                case 17:
                    return "th-TH";
                case 18:
                    return "ko-KR";
                case 19:
                    return "cmn-Hant-TW";
                case 20:
                    return "yue-Hant-HK";
                case 21:
                    return "ja-JP";
                case 22:
                    return "cmn-Hans-HK";
                case 23:
                    return "cmn-Hans-CN";
                case 24:
                    return "pt-BR";
                default:
                    return "en-US";
            }
        }

        private String CreateJwt(String binPath)
        {
            var googleKeyPath = Path.Combine(binPath, GoogleKeyFileName);

            var certificateBytes = File.ReadAllBytes(googleKeyPath);

            var certificate = new X509Certificate2(certificateBytes, "notasecret",
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable |
                X509KeyStorageFlags.PersistKeySet);

            var now = DateTime.UtcNow;

            var claimset = new

            {
                iss = GoogleAccount,

                scope = GoogleScopes,

                aud = "https://accounts.google.com/o/oauth2/token",

                iat = ((Int32) now.Subtract(_unixEpoch).TotalSeconds).ToString(CultureInfo.InvariantCulture),

                exp = ((Int32) now.AddMinutes(10).Subtract(_unixEpoch).TotalSeconds).ToString(CultureInfo
                   .InvariantCulture)
            };
            // header
            var header = new {typ = "JWT", alg = "RS256"};

            // encoded header
            var headerSerialized = JsonConvert.SerializeObject(header);

            var headerBytes = Encoding.UTF8.GetBytes(headerSerialized);

            var headerEncoded = Convert.ToBase64String(headerBytes);

            // encoded claimset
            var claimsetSerialized = JsonConvert.SerializeObject(claimset);

            var claimsetBytes = Encoding.UTF8.GetBytes(claimsetSerialized);

            var claimsetEncoded = Convert.ToBase64String(claimsetBytes);

            // input
            var input = String.Join(".", headerEncoded, claimsetEncoded);

            var inputBytes = Encoding.UTF8.GetBytes(input);

            // dotnet4.6
            try
            {
                // signiture
                var rsa = (RSACryptoServiceProvider) certificate.PrivateKey;
                //rsa.PersistKeyInCsp = true;

                var cspParam = new CspParameters
                {
                    KeyContainerName = rsa.CspKeyContainerInfo.KeyContainerName,

                    KeyNumber = rsa.CspKeyContainerInfo.KeyNumber == KeyNumber.Exchange ? 1 : 2,

                    Flags = CspProviderFlags.NoPrompt | CspProviderFlags.UseExistingKey |
                            CspProviderFlags.UseMachineKeyStore
                };
                var cryptoServiceProvider = new RSACryptoServiceProvider(cspParam) {PersistKeyInCsp = false};

                var signatureBytes = cryptoServiceProvider.SignData(inputBytes, "SHA256");
                var signatureEncoded = Convert.ToBase64String(signatureBytes);
                return String.Join(".", headerEncoded, claimsetEncoded, signatureEncoded);
            }
            // dotnet 2.2
            catch
            {
                var rsa = certificate.GetRSAPrivateKey();
                var signatureBytes =  rsa.SignData(inputBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                var signatureEncoded = Convert.ToBase64String(signatureBytes);

                Console.WriteLine($"{String.Join(".", headerEncoded, claimsetEncoded, signatureEncoded)}");
                return String.Join(".", headerEncoded, claimsetEncoded, signatureEncoded);
            }
            
        }
    }
}
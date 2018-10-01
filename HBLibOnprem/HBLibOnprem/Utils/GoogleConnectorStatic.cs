/*using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Security.DataHandler.Encoder;
using Newtonsoft.Json;

namespace HBLib.Utils
{
  
    public static class GoogleConnectorStatic
    {

        public static HttpClient httpClient = new HttpClient();

        static string googleScopes = @"https://www.googleapis.com/auth/devstorage.full_control";
        static string googleCloudContainerName = "hbfiles";
        static string googleAccount = "hb-989@heedbook-project-195914.iam.gserviceaccount.com";
        static string googleKeyFileName = "Heedbookproject-c88faf773a3e.p12";
        static string fileGoogleCloudPublicAccess = "GoogleFilePublic.xml";
        

        public static async Task<string> LoadFileToGoogleDrive(CloudBlockBlob blob, string blobGoogleDriveName, string binPath, string token)
        {
            try
            {

                //transfer blob to byte array
                byte[] data;
                using (var output = new MemoryStream())
                {
                    blob.DownloadToStream(output);
                    data = output.ToArray();
                }


                //prepare the request
                httpClient.DefaultRequestHeaders.Clear(); 
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token.ToString());

                //send the request
                HttpContent cont = new ByteArrayContent(data);
                var response = await httpClient.PostAsync(@"https://www.googleapis.com/upload/storage/v1/b/" + googleCloudContainerName + @"/o?uploadType=media&name=" + blobGoogleDriveName, cont);
                response.EnsureSuccessStatusCode();

                string results = await response.Content.ReadAsStringAsync();

                return results.ToString();
            }
            catch(Exception e)
            {
                return $"Exception occured {e}";
            }
        }


        public static async Task<string> MakeFilePublicGoogleCloud(string filename, string BinPath, string token)
        {
            httpClient.DefaultRequestHeaders.Clear();
            var googleFileGoogleCloudPublicAccess = Path.Combine(BinPath, fileGoogleCloudPublicAccess);
            var data = File.ReadAllBytes(googleFileGoogleCloudPublicAccess);
            HttpContent cont = new ByteArrayContent(data);
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token.ToString());
            var response = await httpClient.PutAsync(@"https://storage.googleapis.com/"+ googleCloudContainerName +@"/"+filename+@"?acl", cont);
            string results = await response.Content.ReadAsStringAsync();
            return results.ToString();
        }


        public static async Task DeleteFileGoogleCloud(string filename, string token)
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token.ToString());
            await httpClient.DeleteAsync(@"https://storage.googleapis.com/" + googleCloudContainerName + @"/" + filename + @"?acl");
        }

        public static async Task<string> GetGoogleSTTResults(string googleTransactionId, string googleApiKey)
        {
            httpClient.DefaultRequestHeaders.Clear();
            var response = httpClient.GetAsync("https://speech.googleapis.com/v1/operations/" + googleTransactionId + "?key=" + googleApiKey).Result;
            string results = await response.Content.ReadAsStringAsync();
            return results.ToString();
        }


        public static async Task<string> GetAuthorizationToken(string binPath)
        {
            httpClient.DefaultRequestHeaders.Clear();
            string jwt = CreateJwt(binPath);

            var dic = new Dictionary<string, string>
            {
                { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" },
                { "assertion", jwt }
            };
            var content = new FormUrlEncodedContent(dic);
            var response = await httpClient.PostAsync("https://accounts.google.com/o/oauth2/token", content);
         //   response.EnsureSuccessStatusCode();

            dynamic dyn = await response.Content.ReadAsAsync<dynamic>(); 
            //dynamic dyn = await response.Content.ReadAsStringAsync();
           return dyn.access_token; 
           // return dyn; 
        }

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public  static string CreateJwt(string binPath)
        {
            var googleKeyPath = Path.Combine(binPath, googleKeyFileName);

            var certificate = new X509Certificate2(googleKeyPath, "notasecret", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);

            DateTime now = DateTime.UtcNow;
            var claimset = new
            {
                iss = googleAccount,
                scope = googleScopes,
                aud = "https://accounts.google.com/o/oauth2/token",
                iat = ((int)now.Subtract(UnixEpoch).TotalSeconds).ToString(CultureInfo.InvariantCulture),
                exp = ((int)now.AddMinutes(10).Subtract(UnixEpoch).TotalSeconds).ToString(CultureInfo.InvariantCulture)
            };


            // header
            var header = new { typ = "JWT", alg = "RS256" };

            // encoded header
            var headerSerialized = JsonConvert.SerializeObject(header);
            var headerBytes = Encoding.UTF8.GetBytes(headerSerialized);
            //Microsoft.Owin.Security
            var headerEncoded = TextEncodings.Base64Url.Encode(headerBytes);

            // encoded claimset
            var claimsetSerialized = JsonConvert.SerializeObject(claimset);
            var claimsetBytes = Encoding.UTF8.GetBytes(claimsetSerialized);
            //Microsoft.Owin.Security
            var claimsetEncoded = TextEncodings.Base64Url.Encode(claimsetBytes);

            // input
            var input = String.Join(".", headerEncoded, claimsetEncoded);
            var inputBytes = Encoding.UTF8.GetBytes(input);

            // signiture
            var rsa = (RSACryptoServiceProvider)certificate.PrivateKey;
            //rsa.PersistKeyInCsp = true;
            var cspParam = new CspParameters
            {
                KeyContainerName = rsa.CspKeyContainerInfo.KeyContainerName,
                KeyNumber = rsa.CspKeyContainerInfo.KeyNumber == KeyNumber.Exchange ? 1 : 2
            };

            //using (var csp = (RSACryptoServiceProvider)certificate.PrivateKey)
            //{
            //    var hashAlgorithm = CryptoConfig.MapNameToOID("SHA256");

            //    var privateKeyBlob = csp.ExportCspBlob(true);
            //    var cp = new CspParameters(24);
            //    var newCsp = new RSACryptoServiceProvider(cp);
            //    newCsp.ImportCspBlob(privateKeyBlob);

            //    var signature = newCsp.SignData(inputBytes, hashAlgorithm);
            //    return String.Join(".", headerEncoded, claimsetEncoded, signature);
            //}

            //using (var csp = (RSACryptoServiceProvider)certificate.PrivateKey)
            //{
            //    //csp.PersistKeyInCsp = false;
            //    var hashAlgorithm = CryptoConfig.MapNameToOID("SHA256");
            //    var signature = csp.SignData(inputBytes, hashAlgorithm);
            //    // return Convert.ToBase64String(signature);
            //    return String.Join(".", headerEncoded, claimsetEncoded, signature);
            //}

            var cryptoServiceProvider = new RSACryptoServiceProvider(cspParam) { PersistKeyInCsp = false };
            var signatureBytes = cryptoServiceProvider.SignData(inputBytes, "SHA256");
            var signatureEncoded = TextEncodings.Base64Url.Encode(signatureBytes);
            return String.Join(".", headerEncoded, claimsetEncoded, signatureEncoded);

            // jwt

        }

    }
}
*/
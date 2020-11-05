using System.Threading.Tasks;
using HBLib.Model;

namespace HBLib.Utils.Interfaces
{
    public interface IGoogleConnector
    {
        Task<bool> CheckApiKey();
        Task DeleteFileGoogleCloud(string filename, string token);
        Task<string> GetAuthorizationToken(string binPath);
        Task<GoogleSttLongrunningResult> GetGoogleSTTResults(string googleTransactionId);
        Task<string> LoadFileToGoogleDrive(string blobGoogleDriveName, string path, string token);
        Task<string> MakeFilePublicGoogleCloud(string filename, string binPath, string token);
        Task<string> MakeFilePublicGoogleCloudPost(string filename, string binPath, string token);
        Task<GoogleTransactionId> Recognize(string fileName, int languageId, string dialogueId, bool enableWordTimeOffsets = true, bool enableSpeakerDiarization = true);
    }
}
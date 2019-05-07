using System;

namespace FaceAnalyzeService.Exceptions
{
    [Serializable]
    public class FaceAnalyzeServiceException : Exception
    {
        public FaceAnalyzeServiceException(string message, Exception innerException = null) : base(message, innerException)
        {
            
        }
    }
}
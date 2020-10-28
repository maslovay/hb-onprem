using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HBMLHttpClient.Model;

namespace HBMLHttpClient
{
    public interface IHbMlHttpClient
    {
        Task<List<FaceResult>> GetFaceResult(String base64StringFile);
        Task<List<FaceResult>> GetFaceResultWithParams(String base64StringFile, 
            bool description=true, bool emotions=true, bool headpose=true, bool attributes=true);
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CloneFtpOnAzure;
using HBData;
using Microsoft.AspNetCore.Mvc;

namespace LinkToBlobController.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class LinkController : ControllerBase
    {
        private CreateLinkToBlob _create;
        private StorageAccInfo _accInfo;

        public LinkController(CreateLinkToBlob create,
            StorageAccInfo accInfo)
        {
            _accInfo = accInfo;
            _create = create;
        }
        
        [HttpGet("GetBlobFile")]
        public async Task GetBlobFile([FromQuery] Guid dialogId)
        {
            var containerName = new string[] {_accInfo.VideoName, _accInfo.AvatarName, _accInfo.AudioName};
            
            var key = _accInfo.AccKey;
            
            var dictList = new List<string>()
            {
                dialogId + ".jpg",
                dialogId + ".mkv",
                dialogId + ".wav"
            };
            foreach (var nameOfContainer in containerName)
            {
                var uriPath = _accInfo.UriPath + nameOfContainer + "/";

                foreach (var searchingDialogue in dictList)
                {
                    _create.CreateToken(uriPath, searchingDialogue, key);
                }
            }
        }
        
    }
}
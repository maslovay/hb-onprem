using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using HBData.Models;
using Microsoft.EntityFrameworkCore;
using HBLib.Utils;
using System.Collections;
using System.Text.RegularExpressions;
using static HBLib.Utils.SftpClient;
using UserOperations.CommonModels;
using HBData.Repository;

namespace UserOperations.Services
{
    public class DemonstrationV2Service
    {
        private readonly SftpClient _sftpClient;
        private readonly IGenericRepository _repository;
        private readonly LoginService _loginService;

        public DemonstrationV2Service(
            SftpClient sftpClient,
            IGenericRepository repository,
            LoginService loginService
            )
        {
            _sftpClient = sftpClient;
            _repository = repository;
            _loginService = loginService;
        }

        public async Task FlushStats( List<SlideShowSession> stats)
        {
            var userId = _loginService.GetCurrentUserId();
            foreach (SlideShowSession stat in stats)
            {
                if(stat.ContentType == "url")//"url" "media" "content"
                    stat.IsPoll = false;
                else
                {
                    var html = _repository.GetAsQueryable<CampaignContent>()
                        .Where(x => x.CampaignContentId == stat.CampaignContentId)
                        .Select(x => x.Content.RawHTML).FirstOrDefault();

                    stat.IsPoll = html.Contains("PollAnswer") ? true : false;
                }
                stat.SlideShowSessionId = Guid.NewGuid();
                stat.ApplicationUserId = userId;
                await _repository.CreateAsync<SlideShowSession>(stat);
                await _repository.SaveAsync();
            }
        }

        public async Task<string> PollAnswer( CampaignContentAnswer answer)
        {
            answer.CampaignContentAnswerId = Guid.NewGuid();
            answer.ApplicationUserId = _loginService.GetCurrentUserId();
            //answer.Time = DateTime.UtcNow;
            await _repository.CreateAsync<CampaignContentAnswer>(answer);
            await _repository.SaveAsync();
            return "Saved";
        }
    }   
}
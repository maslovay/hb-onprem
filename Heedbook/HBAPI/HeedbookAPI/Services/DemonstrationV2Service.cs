using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;

namespace UserOperations.Services
{
    public class DemonstrationV2Service
    {
        private readonly IGenericRepository _repository;
        private readonly LoginService _loginService;

        public DemonstrationV2Service(
            IGenericRepository repository,
            LoginService loginService
            )
        {
            _repository = repository;
            _loginService = loginService;
        }

        public async Task FlushStats( List<SlideShowSession> stats)
        {
            Guid? userId = null;
            try//---check is it user token authorization
            {
                userId = _loginService.GetCurrentUserId();
            }
            catch
            {
                userId = null;
            }

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
            try
            {
                answer.ApplicationUserId = _loginService.GetCurrentUserId();
            }
            catch
            {
                answer.ApplicationUserId = null;
            }
            //answer.Time = DateTime.UtcNow;
            await _repository.CreateAsync<CampaignContentAnswer>(answer);
            await _repository.SaveAsync();
            return "Saved";
        }
    }   
}
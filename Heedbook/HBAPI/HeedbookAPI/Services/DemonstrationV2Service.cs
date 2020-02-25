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
            foreach (SlideShowSession stat in stats)
            {
                if(stat.ContentType == "url")//"url" "media" "content"
                    stat.IsPoll = false;
                else
                {
                    var json = _repository.GetAsQueryable<CampaignContent>()
                        .Where(x => x.CampaignContentId == stat.CampaignContentId)
                        .Select(x => x.Content.JSONData).FirstOrDefault();

                    stat.IsPoll = json.Contains("answerText") ? true : false;
                }
                stat.SlideShowSessionId = Guid.NewGuid();
                await _repository.CreateAsync<SlideShowSession>(stat);
                await _repository.SaveAsync();
            }
        }

        public async Task<string> PollAnswer( CampaignContentAnswer answer)
        {
            answer.CampaignContentAnswerId = Guid.NewGuid();
            await _repository.CreateAsync<CampaignContentAnswer>(answer);
            await _repository.SaveAsync();
            return "Saved";
        }
    }   
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils.Interfaces;
using UserOperations.Models;

namespace UserOperations.Services
{
    public class DemonstrationV2Service
    {
        private readonly IGenericRepository _repository;
        private readonly ILoginService _loginService;

        public DemonstrationV2Service(
            IGenericRepository repository,
            ILoginService loginService
            )
        {
            _repository = repository;
            _loginService = loginService;
        }

        public async Task FlushStats( List<SlideShowSession> stats)
        {
            //_loginService.GetCurrentCompanyId();
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

        public async Task<string> PollAnswer( CampaignContentAnswerModel answer)
        {
           // _loginService.GetCurrentCompanyId();
            CampaignContentAnswer entity = new CampaignContentAnswer
            {
                CampaignContentAnswerId = Guid.NewGuid(),
                Answer = answer.Answer ?? answer.AnswerText,
                ApplicationUserId = answer.ApplicationUserId,
                CampaignContentId = answer.CampaignContentId,
                DeviceId = answer.DeviceId,
                Time = answer.Time
            };
            await _repository.CreateAsync<CampaignContentAnswer>(entity);
            await _repository.SaveAsync();
            return "Saved";
        }
    }   
}
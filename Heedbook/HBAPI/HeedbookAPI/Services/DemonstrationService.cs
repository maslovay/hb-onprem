using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using Microsoft.EntityFrameworkCore;
using HBData.Repository;

namespace UserOperations.Services
{
    public class DemonstrationService
    {
        private readonly IGenericRepository _repository;

        public DemonstrationService(
            IGenericRepository repository
            )
        {
            _repository = repository;
        }      
        public async Task FlushStats( List<SlideShowSession> stats)
        {
            foreach (SlideShowSession stat in stats)
            {                
                if(stat.ContentType == "url")
                {
                    stat.IsPoll = false;
                }
                else
                {
                    var html = _repository.GetAsQueryable<CampaignContent>()
                        .Include(p => p.Content)
                        .Where(x=>x.CampaignContentId == stat.CampaignContentId).Select(x=>x.Content).FirstOrDefault().RawHTML;
                    stat.IsPoll = html.Contains("PollAnswer") ? true : false;
                }
                stat.SlideShowSessionId = Guid.NewGuid();
                _repository.Create<SlideShowSession>(stat);
                _repository.Save();
            }
        }

        public async Task<string> PollAnswer( CampaignContentAnswer answer)
        {              
            answer.CampaignContentAnswerId = Guid.NewGuid();
            answer.Time = DateTime.UtcNow;
            await _repository.CreateAsync<CampaignContentAnswer>(answer);
            await _repository.SaveAsync();
            return "Saved";
        }
    }   
}
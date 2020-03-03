using System;
using System.Collections.Generic;
using System.Linq;
using UserOperations.Utils;
using System.Threading.Tasks;
using HBData.Repository;
using HBData.Models;
using UserOperations.AccountModels;
using UserOperations.Models;
using UserOperations.Controllers;
using Microsoft.EntityFrameworkCore;

namespace UserOperations.Services
{
    public class SalesStageService
    {
        private readonly LoginService _loginService;
        private readonly RequestFilters _requestFilters;
        private readonly IGenericRepository _repository;

        public SalesStageService(
            LoginService loginService,
            RequestFilters requestFilters,
            IGenericRepository repository
            )
        {
            _loginService = loginService;
            _requestFilters = requestFilters;
            _repository = repository;
        }

        public async Task<List<GetSalesStage>> GetAll(Guid? companyId)
        {
            //---only for admin can view another company. Supervisors get own corporation stages
            var role = _loginService.GetCurrentRoleName();
            if (role != "admin")
                companyId = _loginService.GetCurrentCompanyId();

            var corporationId  = (await _repository.FindOrExceptionOneByConditionAsync<Company>(x => x.CompanyId == companyId)).CorporationId;
            List<SalesStagePhrase> salesStagePhrase = null;

          
            if (corporationId != null)
                salesStagePhrase = _repository.GetAsQueryable<SalesStagePhrase>()
                    .Where(c => c.CorporationId == corporationId).Include(x => x.Phrase).Include(x => x.SalesStage).ToList();
            else
                salesStagePhrase = _repository.GetAsQueryable<SalesStagePhrase>()
                  .Where(c => c.CompanyId == companyId).Include(x => x.Phrase).Include(x => x.SalesStage).ToList();         

            return salesStagePhrase
                    .GroupBy(x => x.SalesStageId)
                    .Select(x => new GetSalesStage
                    {
                        SalesStageId = x.Key,
                        SalesStageName = x.FirstOrDefault().SalesStage.Name,
                        SalesStageNumber = x.FirstOrDefault().SalesStage.SequenceNumber,
                        Phrases = x.Select(p => p.Phrase).ToList()
                    } )
                    .ToList();
        }

        public async Task CreateSalesStageForNewAccount(Guid? companyId, Guid? corporationId)
        {
            var tempaleSalesStagePhrases = _repository.GetAsQueryable<SalesStagePhrase>().Where(x => x.CompanyId == null && x.CorporationId == null).ToList();
            if (corporationId == null && companyId == null)
                throw new Exception("Can't create sales stages phrases");
            if (corporationId != null) companyId = null;
            var newSalesStagePhrases = tempaleSalesStagePhrases.Select(x => new SalesStagePhrase
            {
                SalesStagePhraseId = Guid.NewGuid(),
                CompanyId = companyId,
                CorporationId = corporationId,
                PhraseId = x.PhraseId,
                SalesStageId = x.SalesStageId
            }).ToList();
            _repository.CreateRange<SalesStagePhrase>(newSalesStagePhrases);
        }

        //---PRIVATE---

        private int GetStatusId(string statusName)
        {
            return _repository.GetAsQueryable<Status>().FirstOrDefault(p => p.StatusName == statusName).StatusId;
        }
   
    }
}
using HBData.Models;
using HBData.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserOperations.Controllers;
using UserOperations.Models;
using UserOperations.Services;

namespace UserOperations.Services
{
    public class PhraseService
    {
        private readonly IGenericRepository _repository;
        private readonly LoginService _loginService;

        public PhraseService(IGenericRepository repository, LoginService loginService)
        {
            _repository = repository;
            _loginService = loginService;
        }

        public async Task<List<Guid>> GetPhraseIdsByCompanyIdAsync(bool isTemplate)
        {
            var companyIdInToken = _loginService.GetCurrentCompanyId();
            var languageId = _loginService.GetCurrentLanguagueId();
            return await _repository.GetAsQueryable<PhraseCompany>()
                   .Where( x =>
                       x.CompanyId == companyIdInToken &&
                       x.Phrase.IsTemplate == isTemplate &&
                       x.Phrase.PhraseText != null &&
                       x.Phrase.LanguageId == languageId)
                   .Select(x => x.Phrase.PhraseId).ToListAsync();
        }

        public async Task<List<Phrase>> GetPhrasesNotBelongToCompanyByIdsAsync(List<Guid> phraseIds, bool isTemplate)
        {
            var languageId = _loginService.GetCurrentLanguagueId();
            return await _repository.GetAsQueryable<Phrase>()
                   .Where( x => 
                        x.IsTemplate == true &&
                        x.PhraseText != null &&
                        !phraseIds.Contains(x.PhraseId) &&
                        x.LanguageId == languageId).ToListAsync();
        }
       
        public async Task<Phrase> GetPhraseByIdAsync(Guid phraseId)
        {
            var phrase =  await _repository.GetAsQueryable<Phrase>()
                  .Include(x => x.PhraseCompanys).FirstOrDefaultAsync(x => x.PhraseId == phraseId);
            if (phrase == null) throw new NoFoundException("No such phrase");
            return phrase;
        }

        public async Task<Phrase> GetPhraseInCompanyByIdAsync(Guid phraseId, bool isTemplate)
        {
            var companyIdInToken = _loginService.GetCurrentCompanyId();
            return await _repository.GetAsQueryable<PhraseCompany>()
                    .Where(p =>
                        p.Phrase.PhraseId == phraseId
                        && p.CompanyId == companyIdInToken
                        && p.Phrase.IsTemplate == isTemplate)
                    .Select(p => p.Phrase)
                    .FirstOrDefaultAsync();
        }

        public async Task<List<Phrase>> GetPhrasesInCompanyByIdsAsync (List<Guid> companyIds)
        {
            var companyIdInToken = _loginService.GetCurrentCompanyId();
            companyIds = !companyIds.Any() ? new List<Guid> { companyIdInToken } : companyIds;
            return await _repository.GetAsQueryable<PhraseCompany>()
                    .Where(p =>
                        companyIds.Contains((Guid)p.CompanyId))
                    .Select(p => p.Phrase).ToListAsync();
        }

        public async Task<Phrase> CreateNewPhrasAsync(PhrasePost message)
        {
            var languageId = _loginService.GetCurrentLanguagueId();
            var companyIdInToken = _loginService.GetCurrentCompanyId();
            var corporationIdInToken = _loginService.GetCurrentCorporationId();


            Phrase phrase = await GetPhraseByTextAsync(message.PhraseText, true);
            if (phrase == null)
            {
                await CreateNewPhraseAsync(message, languageId);
                await CreateNewPhraseCompanyAsync(phrase.PhraseId, companyIdInToken);
                await CreateNewSalesStagePhraseAsync(phrase.PhraseId, (Guid)message.SalesStageId, corporationIdInToken == null? (Guid?)companyIdInToken : null, corporationIdInToken);
            }
            else
            {
                //1-phrase+company
                await CreateNewPhraseCompanyAsync(phrase.PhraseId, companyIdInToken);

                //2-phrase+sales stage
                var defaultSalesStageIdOfPhrase = await GetSalesStageOfPhraseByPhraseId(phrase.PhraseId);
                if (message.SalesStageId == null && defaultSalesStageIdOfPhrase == null)
                    throw new NoDataException("what SalesStage do you want to use?");

                if (message.SalesStageId == null)//nothing wishes in requst
                    await CreateNewSalesStagePhraseAsync(phrase.PhraseId, (Guid)defaultSalesStageIdOfPhrase, corporationIdInToken == null ? (Guid?)companyIdInToken : null, corporationIdInToken);

                else if (defaultSalesStageIdOfPhrase == null || message.SalesStageId != defaultSalesStageIdOfPhrase)//no template phrase//wishes some diff sales stage
                    await CreateNewSalesStagePhraseAsync(phrase.PhraseId, (Guid)message.SalesStageId, corporationIdInToken == null ? (Guid?)companyIdInToken : null, corporationIdInToken);
            }

            await _repository.SaveAsync();
            return phrase;
        }

        public async Task CreateNewPhrasesCompanyAsync(List<Guid> phraseIds)
        {
            var companyIdInToken = _loginService.GetCurrentCompanyId();
            foreach (var phraseId in phraseIds)
            {
                await CreateNewPhraseCompanyAsync(phraseId, companyIdInToken);
            }
            await _repository.SaveAsync();
        }

        public async Task<Phrase> EditAndSavePhraseAsync(Phrase entity, Phrase newPhrase)
        {
            if (entity == null) throw new AccessException("No permission for changing phrase");
            foreach (var p in typeof(Phrase).GetProperties())
            {
                if (p.GetValue(newPhrase, null) != null && p.GetValue(newPhrase, null).ToString() != Guid.Empty.ToString())
                    p.SetValue(entity, p.GetValue(newPhrase, null), null);
            }
            _repository.Update<Phrase>(entity);
            await _repository.SaveAsync();
            return entity;
        }

        public async Task<string> DeleteAndSavePhraseWithPhraseCompanyAsync(Phrase phraseIncluded)
        {
            var companyIdInToken = _loginService.GetCurrentCompanyId();
            var phrasesCompany = phraseIncluded.PhraseCompanys.Where(x => x.CompanyId == companyIdInToken).ToList();
            _repository.Delete<PhraseCompany>(phrasesCompany);//--remove connections to phrase in library
            try
            {
                if (!phraseIncluded.IsTemplate)
                {
                    _repository.Delete<Phrase>(phraseIncluded);//--remove own phrase
                    await _repository.SaveAsync();
                    return "Deleted from PhraseCompany and Phrases";
                }
            }
            catch { }
            await _repository.SaveAsync();
            return "Deleted from PhraseCompany";
        }


        //---PRIVATE---

        private async Task<Guid?> GetSalesStageOfPhraseByPhraseId(Guid phraseId)
        {
            var salesStageId = _repository.GetAsQueryable<SalesStagePhrase>()
                    .Where(x =>
                    x.PhraseId == phraseId
                    && x.CompanyId == null && x.CorporationId == null).FirstOrDefault()?.SalesStageId;
            return salesStageId;
        }

        private async Task<Phrase> GetPhraseByTextAsync(string phraseText, bool isTemplate)
        {
            //---search phrase first - that is in library, second - any phrase with the same text
            var phrase =  await _repository.GetAsQueryable<Phrase>()
                   .Include(x => x.PhraseCompanys)
                   .Where(x =>
                        x.PhraseText.ToLower() == phraseText.ToLower()
                        && x.IsTemplate == isTemplate).FirstOrDefaultAsync();
            if(phrase == null)
                phrase = await _repository.GetAsQueryable<Phrase>()
                   .Include(x => x.PhraseCompanys)
                   .Where(x => x.PhraseText.ToLower() == phraseText.ToLower()).FirstOrDefaultAsync();
            return phrase;
        }

        private async Task<Phrase> CreateNewPhraseAsync(PhrasePost message, int languageId)
        {
            var phrase = new Phrase
            {
                PhraseId = Guid.NewGuid(),
                PhraseText = message.PhraseText,
                PhraseTypeId = message.PhraseTypeId,
                LanguageId = languageId,
                WordsSpace = message.WordsSpace,
                Accurancy = message.Accurancy,
                IsTemplate = false
            };
            await _repository.CreateAsync<Phrase>(phrase);
            return phrase;
        }

        private async Task CreateNewPhraseCompanyAsync(Guid phraseId, Guid companyIdInToken)
        {
            var phraseCompany = new PhraseCompany
            {
                CompanyId = companyIdInToken,
                PhraseCompanyId = Guid.NewGuid(),
                PhraseId = phraseId
            };
            await _repository.CreateAsync<PhraseCompany>(phraseCompany);
        }

        private async Task CreateNewSalesStagePhraseAsync(Guid phraseId, Guid salesStageId, Guid? companyId, Guid? corporationId)
        {
            var salesStagePhrase = new SalesStagePhrase
            {
                SalesStagePhraseId = Guid.NewGuid(),
                SalesStageId = (Guid)salesStageId,
                PhraseId = phraseId,
                CompanyId = companyId,
                CorporationId = corporationId
            };
            await _repository.CreateAsync<SalesStagePhrase>(salesStagePhrase);
        }

        private async Task SaveChangesAsync()
        {
            await _repository.SaveAsync();
        }
    }
}

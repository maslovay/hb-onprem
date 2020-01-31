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
                  .Include(x => x.PhraseCompany).FirstOrDefaultAsync(x => x.PhraseId == phraseId);
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

        public async Task<Phrase> CreateNewPhraseAndAddToCompanyAsync(PhrasePost message)
        {
            var languageId = _loginService.GetCurrentLanguagueId();
            var companyIdInToken = _loginService.GetCurrentCompanyId();

            Phrase phrase = await GetLibraryPhraseByTextAsync(message.PhraseText, true);
            phrase = phrase?? await CreateNewPhraseAsync(message, languageId);
            await CreateNewPhraseCompanyAsync(phrase.PhraseId, companyIdInToken);

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
            var phrasesCompany = phraseIncluded.PhraseCompany.Where(x => x.CompanyId == companyIdInToken).ToList();
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

        private async Task<Phrase> GetLibraryPhraseByTextAsync(string phraseText, bool isTemplate)
        {
            //---search phrase that is in library or that is not belong to any company
            return await _repository.GetAsQueryable<Phrase>()
                   .Include(x => x.PhraseCompany)
                   .Where(x =>
                        x.PhraseText.ToLower() == phraseText.ToLower()
                        && (x.IsTemplate == isTemplate || x.PhraseCompany.Count() == 0)).FirstOrDefaultAsync();
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

        private async Task SaveChangesAsync()
        {
            await _repository.SaveAsync();
        }
    }
}

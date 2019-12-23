using HBData;
using HBData.Models;
using HBData.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserOperations.Models;
using UserOperations.Models.AnalyticModels;
using UserOperations.Services;

namespace UserOperations.Providers
{
    public class PhraseProvider
    {
        private readonly IGenericRepository _repository;
        private readonly LoginService _loginService;
        //private readonly int activeStatus;
        //private readonly int disabledStatus;

        public PhraseProvider(IGenericRepository repository, LoginService loginService)
        {
            _repository = repository;
            _loginService = loginService;
            //activeStatus = 3;
            //disabledStatus = 4;
        }

        public async Task<List<Guid>> GetPhraseIdsByCompanyIdAsync(Guid companyIdInToken, string languageId, bool isTemplate)
        {
            return await _repository.GetAsQueryable<PhraseCompany>()
                   .Where( x =>
                       x.CompanyId == companyIdInToken &&
                       x.Phrase.IsTemplate == isTemplate &&
                       x.Phrase.PhraseText != null &&
                       x.Phrase.LanguageId.ToString() == languageId)
                   .Select(x => x.Phrase.PhraseId).ToListAsync();
        }
        public async Task<List<Phrase>> GetPhrasesNotBelongToCompanyByIdsAsync(List<Guid> phraseIds, string languageId, bool isTemplate)
        {
            return await _repository.GetAsQueryable<Phrase>()
                   .Where( x => 
                        x.IsTemplate == true &&
                        x.PhraseText != null &&
                        !phraseIds.Contains(x.PhraseId) &&
                        x.LanguageId.ToString() == languageId).ToListAsync();
        }
        public async Task<Phrase> GetLibraryPhraseByTextAsync(string phraseText, bool isTemplate)
        {
            //---search phrase that is in library or that is not belong to any company
            return await _repository.GetAsQueryable<Phrase>()
                   .Include(x => x.PhraseCompany)
                   .Where(x => 
                        x.PhraseText.ToLower() == phraseText.ToLower()
                        && (x.IsTemplate == isTemplate || x.PhraseCompany.Count() == 0)).FirstOrDefaultAsync();
        }
        public async Task<Phrase> GetPhraseByIdAsync(Guid phraseId)
        {
            return await _repository.GetAsQueryable<Phrase>()
                  .Include(x => x.PhraseCompany).FirstOrDefaultAsync(x => x.PhraseId == phraseId);
        }
        public async Task<Phrase> GetPhraseInCompanyByIdAsync(Guid phraseId, Guid companyId, bool isTemplate)
        {
            return await _repository.GetAsQueryable<PhraseCompany>()
                    .Where(p =>
                        p.Phrase.PhraseId == phraseId
                        && p.CompanyId == companyId
                        && p.Phrase.IsTemplate == isTemplate)
                    .Select(p => p.Phrase)
                    .FirstOrDefaultAsync();
        }
        public async Task<List<Phrase>> GetPhrasesInCompanyByIdsAsync (List<Guid> companyIds)
        {
            return await _repository.GetAsQueryable<PhraseCompany>()
                    .Where(p =>
                        companyIds.Contains((Guid)p.CompanyId))
                    .Select(p => p.Phrase).ToListAsync();
        }
        public async Task<Phrase> CreateNewPhraseAsync(PhrasePost message, int languageId)
        {
            var phrase = new Phrase
            {
                PhraseId = Guid.NewGuid(),
                PhraseText = message.PhraseText,
                PhraseTypeId = message.PhraseTypeId,
                LanguageId = languageId,
                IsClient = message.IsClient,
                WordsSpace = message.WordsSpace,
                Accurancy = message.Accurancy,
                IsTemplate = false
            };
            await _repository.CreateAsync<Phrase>(phrase);
          //  await _repository.SaveAsync();
            return phrase;
        }
        public async Task CreateNewPhraseCompanyAsync(Guid companyId, Guid phraseId)
        {
            var phraseCompany = new PhraseCompany
            {
                CompanyId = companyId,
                PhraseCompanyId = Guid.NewGuid(),
                PhraseId = phraseId
             };
            await _repository.CreateAsync<PhraseCompany>(phraseCompany);           
        }
        public async Task<Phrase> EditAndSavePhraseAsync(Phrase entity, Phrase newPhrase)
        {
            foreach (var p in typeof(Phrase).GetProperties())
            {
                if (p.GetValue(newPhrase, null) != null && p.GetValue(newPhrase, null).ToString() != Guid.Empty.ToString())
                    p.SetValue(entity, p.GetValue(newPhrase, null), null);
            }
            _repository.Update<Phrase>(entity);
            await _repository.SaveAsync();
            return entity;
        }
        public async Task<string> DeleteAndSavePhraseWithPhraseCompanyAsync(Phrase phraseIncluded, Guid companyId)
        {
            var phrasesCompany = phraseIncluded.PhraseCompany.Where(x => x.CompanyId == companyId).ToList();
            _repository.Delete<PhraseCompany>(phrasesCompany);//--remove connections to phrase in library           
            if (!phraseIncluded.IsTemplate)
            {
                _repository.Delete<Phrase>(phraseIncluded);//--remove own phrase
                await _repository.SaveAsync();
                return "Deleted from PhraseCompany and Phrases";
            }
            await _repository.SaveAsync();
            return "Template! Deleted from PhraseCompany";
        }
        public async Task SaveChangesAsync()
        {
            await _repository.SaveAsync();
        }
    }
}

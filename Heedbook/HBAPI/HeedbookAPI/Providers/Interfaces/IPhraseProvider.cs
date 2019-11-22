using HBData;
using HBData.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserOperations.Models;

namespace UserOperations.Providers
{
    public interface IPhraseProvider
    {
        Task<List<Guid>> GetPhraseIdsByCompanyIdAsync(Guid companyIdInToken, string languageId, bool isTemplate);
        Task<List<Phrase>> GetPhrasesNotBelongToCompanyByIdsAsync(List<Guid> phraseIds, string languageId, bool isTemplate);
        Task<Phrase> GetLibraryPhraseByTextAsync(string phraseText, bool isTemplate);
        Task<Phrase> CreateNewPhraseAsync(PhrasePost message, int languageId);
        Task CreateNewPhraseCompanyAsync(Guid companyId, Guid phraseId);
        Task<Phrase> GetPhraseInCompanyByIdAsync(Guid phraseId, Guid companyId, bool isTemplate);
        Task<Phrase> EditPhraseAsync(Phrase entity, Phrase newPhrase);
        Task<Phrase> GetPhraseByIdAsync(Guid phraseId);
        Task<string> DeletePhraseWithPhraseCompanyAsync(Phrase phraseIncluded, Guid companyId);
    }
}
